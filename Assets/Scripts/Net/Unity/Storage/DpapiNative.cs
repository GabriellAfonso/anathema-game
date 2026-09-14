#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
#nullable enable
using System;
using System.Runtime.InteropServices;

namespace Anathema.Net.Unity
{
    /// <summary>
    /// DPAPI do Windows por P/Invoke. Com <c>apiCompatibilityLevel: 6</c> (.NET Standard 2.1), o
    /// <c>ProtectedData</c> não existe no perfil de compilação; só há fachadas internas da
    /// instalação do Unity, ausentes no Android (specs/002-player-account/research.md, R3).
    /// Escopo do usuário atual, sem interface do sistema.
    /// </summary>
    /// <example>
    /// <code>
    /// if (DpapiNative.TryProtect(plain, entropy, out byte[] cipher, out int error)) File.WriteAllBytes(path, cipher);
    /// </code>
    /// </example>
    internal static class DpapiNative
    {
        private const int UiForbidden = 0x1;

        /// <summary>Cifra para o usuário atual.</summary>
        /// <example><code>bool ok = DpapiNative.TryProtect(plain, entropy, out byte[] cipher, out int error);</code></example>
        internal static bool TryProtect(byte[] plain, byte[] entropy, out byte[] cipher, out int error)
        {
            return Run(plain, entropy, true, out cipher, out error);
        }

        /// <summary>Decifra; falha com o código do Windows quando o dado é de outro usuário ou está corrompido.</summary>
        /// <example><code>bool ok = DpapiNative.TryUnprotect(cipher, entropy, out byte[] plain, out int error);</code></example>
        internal static bool TryUnprotect(byte[] cipher, byte[] entropy, out byte[] plain, out int error)
        {
            return Run(cipher, entropy, false, out plain, out error);
        }

        private static bool Run(byte[] input, byte[] entropy, bool protect, out byte[] output, out int error)
        {
            DataBlob inputBlob = Allocate(input);
            DataBlob entropyBlob = Allocate(entropy);
            DataBlob outputBlob = default;
            try
            {
                bool succeeded = protect ? Protect(ref inputBlob, ref entropyBlob, ref outputBlob) : Unprotect(ref inputBlob, ref entropyBlob, ref outputBlob);
                error = succeeded ? 0 : Marshal.GetLastWin32Error();
                output = succeeded ? CopyOut(outputBlob) : Array.Empty<byte>();
                return succeeded;
            }
            finally
            {
                Release(inputBlob, entropyBlob, outputBlob);
            }
        }

        private static bool Protect(ref DataBlob input, ref DataBlob entropy, ref DataBlob output)
        {
            return CryptProtectData(ref input, null, ref entropy, IntPtr.Zero, IntPtr.Zero, UiForbidden, ref output);
        }

        private static bool Unprotect(ref DataBlob input, ref DataBlob entropy, ref DataBlob output)
        {
            return CryptUnprotectData(ref input, IntPtr.Zero, ref entropy, IntPtr.Zero, IntPtr.Zero, UiForbidden, ref output);
        }

        private static DataBlob Allocate(byte[] bytes)
        {
            IntPtr memory = Marshal.AllocHGlobal(Math.Max(bytes.Length, 1));
            Marshal.Copy(bytes, 0, memory, bytes.Length);
            return new DataBlob { Size = bytes.Length, Data = memory };
        }

        private static byte[] CopyOut(DataBlob blob)
        {
            byte[] bytes = new byte[blob.Size];
            Marshal.Copy(blob.Data, bytes, 0, blob.Size);
            return bytes;
        }

        private static void Release(DataBlob input, DataBlob entropy, DataBlob output)
        {
            Marshal.FreeHGlobal(input.Data);
            Marshal.FreeHGlobal(entropy.Data);
            // O blob de saída é alocado pela DPAPI com LocalAlloc; só LocalFree o libera.
            if (output.Data != IntPtr.Zero)
                LocalFree(output.Data);
        }

        [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CryptProtectData(ref DataBlob dataIn, string? description, ref DataBlob entropy, IntPtr reserved, IntPtr prompt, int flags, ref DataBlob dataOut);

        [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CryptUnprotectData(ref DataBlob dataIn, IntPtr description, ref DataBlob entropy, IntPtr reserved, IntPtr prompt, int flags, ref DataBlob dataOut);

        [DllImport("kernel32.dll")]
        private static extern IntPtr LocalFree(IntPtr memory);

        [StructLayout(LayoutKind.Sequential)]
        private struct DataBlob
        {
            public int Size;
            public IntPtr Data;
        }
    }
}
#endif
