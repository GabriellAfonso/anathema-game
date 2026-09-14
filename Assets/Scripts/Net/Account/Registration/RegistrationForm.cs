#nullable enable
using System;

namespace Anathema.Net.Account
{
    /// <summary>
    /// Os quatro campos de <c>POST /accounts/register/</c>. O cliente não valida senha, e-mail nem
    /// confirmação: quem recusa é o servidor, campo a campo.
    /// </summary>
    /// <example>
    /// <code>
    /// RegistrationForm form = new RegistrationForm("one", "one@example.com", new Password("s3cret!"), new Password("s3cret!"));
    /// </code>
    /// </example>
    public sealed class RegistrationForm
    {
        /// <summary>Cria o formulário; campo nulo lança.</summary>
        /// <example><code>RegistrationForm form = new RegistrationForm(username, email, password, confirmation);</code></example>
        public RegistrationForm(string username, string email, Password password, Password confirmation)
        {
            Username = username ?? throw new ArgumentNullException(nameof(username), "registration username is null: expected the typed text, even if empty");
            Email = email ?? throw new ArgumentNullException(nameof(email), "registration email is null: expected the typed text, even if empty");
            Password = password ?? throw new ArgumentNullException(nameof(password), "registration password is null: expected the typed password");
            Confirmation = confirmation ?? throw new ArgumentNullException(nameof(confirmation), "registration confirmation is null: expected the typed confirmation");
        }

        /// <summary>Nome de usuário.</summary>
        /// <example><code>string username = form.Username;</code></example>
        public string Username { get; }

        /// <summary>E-mail.</summary>
        /// <example><code>string email = form.Email;</code></example>
        public string Email { get; }

        /// <summary>Senha.</summary>
        /// <example><code>Password password = form.Password;</code></example>
        public Password Password { get; }

        /// <summary>Confirmação da senha.</summary>
        /// <example><code>Password confirmation = form.Confirmation;</code></example>
        public Password Confirmation { get; }
    }
}
