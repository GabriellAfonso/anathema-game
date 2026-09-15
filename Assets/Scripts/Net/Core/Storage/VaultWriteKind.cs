#nullable enable

namespace Anathema.Net.Core
{
    /// <summary>Como terminou a gravação na guarda segura.</summary>
    /// <example><code>if (outcome.Kind == VaultWriteKind.Failed) log.Warning("vault_save_failed");</code></example>
    internal enum VaultWriteKind
    {
        /// <summary>Gravado e protegido.</summary>
        Saved,

        /// <summary>Não gravou; o login continua valendo em memória.</summary>
        Failed,
    }
}
