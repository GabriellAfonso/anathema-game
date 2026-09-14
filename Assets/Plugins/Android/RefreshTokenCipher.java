package com.anathema.net;

import android.content.Context;
import android.security.keystore.KeyGenParameterSpec;
import android.security.keystore.KeyProperties;

import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.security.GeneralSecurityException;
import java.security.KeyStore;
import java.util.Arrays;

import javax.crypto.Cipher;
import javax.crypto.KeyGenerator;
import javax.crypto.SecretKey;
import javax.crypto.spec.GCMParameterSpec;

/**
 * Guarda o refresh token do Anathema cifrado com uma chave AES-256-GCM que nunca sai do
 * Android Keystore (specs/002-player-account/research.md, R4).
 *
 * O arquivo fica em noBackupFilesDir: o Auto Backup restauraria o texto cifrado num aparelho
 * novo, sem a chave. Chamado por AndroidKeystoreRefreshTokenVault via JNI; exceções sobem para o
 * C#, que as transforma em resultado sem expor o valor.
 */
public final class RefreshTokenCipher {
    private static final String KEY_ALIAS = "anathema_refresh_token_v1";
    private static final String KEY_STORE = "AndroidKeyStore";
    private static final String TRANSFORMATION = "AES/GCM/NoPadding";
    private static final String FILE_NAME = "refresh_token.bin";
    private static final int IV_LENGTH = 12;
    private static final int TAG_BITS = 128;

    private RefreshTokenCipher() {
    }

    public static void save(Context context, String token) throws GeneralSecurityException, IOException {
        Cipher cipher = Cipher.getInstance(TRANSFORMATION);
        cipher.init(Cipher.ENCRYPT_MODE, keyForSaving());
        byte[] sealed = cipher.doFinal(token.getBytes(StandardCharsets.UTF_8));
        writeAtomically(context, concat(cipher.getIV(), sealed));
    }

    public static String read(Context context) throws GeneralSecurityException, IOException {
        File file = file(context);
        if (!file.exists()) {
            return null;
        }

        byte[] stored = readAll(file);
        if (stored.length <= IV_LENGTH) {
            throw new GeneralSecurityException("refresh token file has " + stored.length + " bytes: expected a 12 byte iv followed by ciphertext");
        }

        Cipher cipher = Cipher.getInstance(TRANSFORMATION);
        cipher.init(Cipher.DECRYPT_MODE, existingKey(), new GCMParameterSpec(TAG_BITS, stored, 0, IV_LENGTH));
        return new String(cipher.doFinal(stored, IV_LENGTH, stored.length - IV_LENGTH), StandardCharsets.UTF_8);
    }

    public static void delete(Context context) {
        File target = file(context);
        target.delete();
        new File(target.getPath() + ".tmp").delete();
    }

    private static SecretKey keyForSaving() throws GeneralSecurityException, IOException {
        KeyStore store = keyStore();
        if (store.containsAlias(KEY_ALIAS)) {
            return (SecretKey) store.getKey(KEY_ALIAS, null);
        }

        KeyGenerator generator = KeyGenerator.getInstance(KeyProperties.KEY_ALGORITHM_AES, KEY_STORE);
        generator.init(new KeyGenParameterSpec.Builder(KEY_ALIAS, KeyProperties.PURPOSE_ENCRYPT | KeyProperties.PURPOSE_DECRYPT)
                .setBlockModes(KeyProperties.BLOCK_MODE_GCM)
                .setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE)
                .setKeySize(256)
                .build());
        return generator.generateKey();
    }

    private static SecretKey existingKey() throws GeneralSecurityException, IOException {
        SecretKey key = (SecretKey) keyStore().getKey(KEY_ALIAS, null);
        if (key == null) {
            throw new GeneralSecurityException("keystore alias " + KEY_ALIAS + " is missing: expected the key created by save");
        }

        return key;
    }

    private static KeyStore keyStore() throws GeneralSecurityException, IOException {
        KeyStore store = KeyStore.getInstance(KEY_STORE);
        store.load(null);
        return store;
    }

    private static File file(Context context) {
        return new File(context.getNoBackupFilesDir(), FILE_NAME);
    }

    private static void writeAtomically(Context context, byte[] bytes) throws IOException {
        File target = file(context);
        File temporary = new File(target.getPath() + ".tmp");
        try (FileOutputStream out = new FileOutputStream(temporary)) {
            out.write(bytes);
            out.getFD().sync();
        }

        if (!temporary.renameTo(target)) {
            throw new IOException("could not rename " + temporary.getName() + " to " + target.getName() + ": expected a rename inside noBackupFilesDir");
        }
    }

    private static byte[] readAll(File file) throws IOException {
        byte[] bytes = new byte[(int) file.length()];
        try (FileInputStream in = new FileInputStream(file)) {
            int offset = 0;
            int read;
            while (offset < bytes.length && (read = in.read(bytes, offset, bytes.length - offset)) >= 0) {
                offset += read;
            }
        }

        return bytes;
    }

    private static byte[] concat(byte[] first, byte[] second) {
        byte[] joined = Arrays.copyOf(first, first.length + second.length);
        System.arraycopy(second, 0, joined, first.length, second.length);
        return joined;
    }
}
