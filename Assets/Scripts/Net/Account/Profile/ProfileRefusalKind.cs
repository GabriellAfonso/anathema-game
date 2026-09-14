#nullable enable

namespace Anathema.Net.Account
{
    /// <summary>Por que o próprio perfil não veio.</summary>
    /// <example><code>if (refusal.Kind == ProfileRefusalKind.ProfileMissing) ShowSupport();</code></example>
    public enum ProfileRefusalKind
    {
        /// <summary>Conta sem <c>PlayerProfile</c> (404), criada fora do cadastro.</summary>
        ProfileMissing,

        /// <summary>Outra resposta que o cliente não tipa.</summary>
        Unrecognized,
    }
}
