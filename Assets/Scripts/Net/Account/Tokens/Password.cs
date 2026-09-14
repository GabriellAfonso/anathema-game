#nullable enable
using System;

namespace Anathema.Net.Account
{
    /// <summary>
    /// Senha digitada pelo jogador. Tipo próprio para nunca cair num log por engano:
    /// <see cref="ToString"/> mascara, e só quem monta o corpo do pedido chama
    /// <see cref="RevealForRequest"/> (FR-040).
    /// </summary>
    /// <example>
    /// <code>
    /// SignInOutcome outcome = await session.SignInAsync(username, new Password(passwordInput.text));
    /// </code>
    /// </example>
    public sealed class Password
    {
        private readonly string text;

        /// <summary>Cria a senha; vazia lança.</summary>
        /// <example><code>Password password = new Password("123456");</code></example>
        public Password(string text)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentException($"password is {(text == null ? "null" : "empty")}: expected at least one character", nameof(text));

            this.text = text;
        }

        /// <summary>O texto, só para o corpo do pedido.</summary>
        /// <example><code>writer.WriteText("password", password.RevealForRequest());</code></example>
        public string RevealForRequest() => text;

        /// <summary>Forma para log, sempre mascarada.</summary>
        /// <example><code>string text = password.ToString(); // password=&lt;redacted&gt;</code></example>
        public override string ToString() => "password=<redacted>";
    }
}
