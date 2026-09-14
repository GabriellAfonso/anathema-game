#nullable enable

namespace Anathema.Net.Account
{
    /// <summary>Como terminou a tentativa de retomar a sessão ao abrir o app.</summary>
    /// <example><code>if (resumed.Kind == ResumeOutcomeKind.Resumed) LoadHome();</code></example>
    public enum ResumeOutcomeKind
    {
        /// <summary>Entrou sem senha com o refresh guardado.</summary>
        Resumed,

        /// <summary>Nada guardado (ou guardado ilegível, já apagado).</summary>
        NothingStored,

        /// <summary>O servidor recusou o refresh guardado; a guarda foi apagada.</summary>
        Refused,

        /// <summary>Não deu para perguntar ao servidor; a guarda continua para outra tentativa.</summary>
        Unavailable,
    }
}
