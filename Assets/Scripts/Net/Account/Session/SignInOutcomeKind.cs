#nullable enable

namespace Anathema.Net.Account
{
    /// <summary>Como terminou um pedido de login.</summary>
    /// <example><code>if (outcome.Kind == SignInOutcomeKind.CredentialsRefused) ShowWrongPassword();</code></example>
    public enum SignInOutcomeKind
    {
        /// <summary>Entrou; tokens em memória e refresh guardado.</summary>
        SignedIn,

        /// <summary>Usuário ou senha errados (401).</summary>
        CredentialsRefused,

        /// <summary>O servidor respondeu outro status.</summary>
        ServerRefused,

        /// <summary>O pedido não chegou ao servidor.</summary>
        TransportFailed,

        /// <summary>A resposta 200 não trouxe tokens legíveis.</summary>
        OutOfContract,

        /// <summary>Já havia um login em curso; nada foi enviado.</summary>
        AlreadyInProgress,
    }
}
