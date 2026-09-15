#nullable enable

namespace Anathema.Net.Facade
{
    /// <summary>
    /// Em que ponto o jogador está, sem cena. O roteador de cena e as telas seguem este valor
    /// (specs/005-presentation-facade/data-model.md, "Estado do app").
    /// </summary>
    /// <example><code>if (client.State.Stage == ClientStage.InMatch) ShowArena();</code></example>
    public enum ClientStage
    {
        /// <summary>Sem sessão: tela de login.</summary>
        SignedOut,

        /// <summary>Com sessão, fora da fila.</summary>
        SignedIn,

        /// <summary>Na fila, esperando pareamento.</summary>
        Searching,

        /// <summary>Pareado; a partida está abrindo.</summary>
        Paired,

        /// <summary>A partida recebeu o primeiro estado do servidor.</summary>
        InMatch,

        /// <summary>A partida terminou; o resultado está no estado.</summary>
        MatchFinished,

        /// <summary>A partida não abriu ou a conexão desistiu; dá para tentar de novo ou voltar.</summary>
        MatchUnavailable,
    }
}
