#nullable enable

namespace Anathema.Net.Connection
{
    /// <summary>O que tirou a conexão da suspensão, ou mudou a rede debaixo dela.</summary>
    internal enum ResumeCause
    {
        /// <summary>O app voltou ao primeiro plano.</summary>
        Foreground,

        /// <summary>A rede voltou depois de sumir.</summary>
        NetworkBack,

        /// <summary>A rede trocou de tipo sem sumir (Wi-Fi ↔ dados móveis).</summary>
        NetworkKindChanged,
    }
}
