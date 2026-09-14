#nullable enable

namespace Anathema.Net.Account
{
    /// <summary>
    /// Cadastro aceito (201). Não traz tokens: o backend só registra, e o login vem em seguida.
    /// </summary>
    /// <example>
    /// <code>
    /// if (registered.IsSuccess) await session.SignInAsync(form.Username, form.Password);
    /// </code>
    /// </example>
    public sealed class AccountCreated
    {
        private AccountCreated()
        {
        }

        /// <summary>O único valor, porque não há o que carregar.</summary>
        /// <example><code>return AccountCallOutcome&lt;AccountCreated, RegistrationRefusal&gt;.Success(AccountCreated.Instance);</code></example>
        public static AccountCreated Instance { get; } = new AccountCreated();
    }
}
