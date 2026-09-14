#nullable enable

namespace Anathema.Net.Account
{
    /// <summary>Campo do formulário de cadastro a que uma recusa se refere.</summary>
    /// <example><code>if (error.Field == RegistrationField.Email) emailHint.text = error.Messages[0];</code></example>
    public enum RegistrationField
    {
        /// <summary><c>username</c>.</summary>
        Username,

        /// <summary><c>email</c>.</summary>
        Email,

        /// <summary><c>password</c>.</summary>
        Password,

        /// <summary><c>password_confirmation</c>.</summary>
        PasswordConfirmation,

        /// <summary>Campo que o cliente não conhece (ex.: <c>non_field_errors</c>); o nome fica em <c>FieldName</c>.</summary>
        Other,
    }
}
