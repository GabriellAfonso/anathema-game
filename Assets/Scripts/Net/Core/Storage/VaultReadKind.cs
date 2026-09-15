#nullable enable

namespace Anathema.Net.Core
{
    /// <summary>O que a leitura da guarda segura encontrou.</summary>
    /// <example><code>if (outcome.Kind == VaultReadKind.Found) Resume(outcome.Token!);</code></example>
    internal enum VaultReadKind
    {
        /// <summary>Havia um refresh token legível.</summary>
        Found,

        /// <summary>Nada guardado.</summary>
        Empty,

        /// <summary>Havia algo, mas não deu para decifrar (chave invalidada, arquivo corrompido).</summary>
        Unreadable,
    }
}
