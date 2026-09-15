#nullable enable
using Anathema.Net.Core;

namespace Anathema.Net.Fakes
{
    /// <summary>
    /// Guarda segura em memória, roteirizável: o teste pré-carrega um token, faz a próxima
    /// gravação falhar ou torna o conteúdo ilegível, e confere quantas vezes gravou e apagou.
    /// </summary>
    /// <example>
    /// <code>
    /// FakeRefreshTokenVault vault = new FakeRefreshTokenVault();
    /// vault.Preload("refresh-1");
    /// Assert.That(vault.Stored, Is.EqualTo("refresh-1"));
    /// </code>
    /// </example>
    internal sealed class FakeRefreshTokenVault : IRefreshTokenVault
    {
        private RefreshToken? stored;
        private string? nextSaveFailure;
        private string? unreadableDetail;

        /// <summary>Texto guardado agora, ou nulo.</summary>
        /// <example><code>string? text = vault.Stored;</code></example>
        public string? Stored => stored?.RevealForRequest();

        /// <summary>Quantas vezes <see cref="Save"/> foi chamado, com ou sem falha.</summary>
        /// <example><code>Assert.That(vault.SaveCount, Is.EqualTo(1));</code></example>
        public int SaveCount { get; private set; }

        /// <summary>Quantas vezes <see cref="Delete"/> foi chamado.</summary>
        /// <example><code>Assert.That(vault.DeleteCount, Is.EqualTo(1));</code></example>
        public int DeleteCount { get; private set; }

        /// <summary>Guarda um token antes do teste, sem contar como gravação.</summary>
        /// <example><code>vault.Preload("refresh-1");</code></example>
        public void Preload(string refreshText)
        {
            stored = new RefreshToken(refreshText);
            unreadableDetail = null;
        }

        /// <summary>A próxima gravação falha com esse motivo.</summary>
        /// <example><code>vault.FailNextSave("disk_full");</code></example>
        public void FailNextSave(string detail) => nextSaveFailure = detail;

        /// <summary>A partir de agora a leitura devolve ilegível, até gravar ou apagar.</summary>
        /// <example><code>vault.MakeUnreadable("key_invalidated");</code></example>
        public void MakeUnreadable(string detail) => unreadableDetail = detail;

        /// <summary>Lê conforme o roteiro.</summary>
        /// <example><code>VaultReadOutcome read = vault.Read();</code></example>
        public VaultReadOutcome Read()
        {
            if (unreadableDetail != null)
                return VaultReadOutcome.Unreadable(unreadableDetail);

            return stored == null ? VaultReadOutcome.Empty() : VaultReadOutcome.Found(stored);
        }

        /// <summary>Grava, ou falha se o roteiro mandou.</summary>
        /// <example><code>VaultWriteOutcome saved = vault.Save(new RefreshToken("refresh-1"));</code></example>
        public VaultWriteOutcome Save(RefreshToken token)
        {
            SaveCount++;
            if (nextSaveFailure != null)
                return ConsumeSaveFailure();

            stored = token;
            unreadableDetail = null;
            return VaultWriteOutcome.Saved();
        }

        /// <summary>Apaga o que houver.</summary>
        /// <example><code>vault.Delete();</code></example>
        public void Delete()
        {
            DeleteCount++;
            stored = null;
            unreadableDetail = null;
        }

        private VaultWriteOutcome ConsumeSaveFailure()
        {
            string detail = nextSaveFailure!;
            nextSaveFailure = null;
            return VaultWriteOutcome.Failed(detail);
        }
    }
}
