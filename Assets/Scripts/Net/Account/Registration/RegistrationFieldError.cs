#nullable enable
using System;
using System.Collections.Generic;

namespace Anathema.Net.Account
{
    /// <summary>
    /// As mensagens que o servidor mandou para um campo do cadastro. As mensagens são para o
    /// jogador ler; o cliente decide só pelo campo, nunca pelo texto.
    /// </summary>
    /// <example>
    /// <code>
    /// foreach (RegistrationFieldError error in refusal.Fields) ShowNextTo(error.Field, error.Messages);
    /// </code>
    /// </example>
    public sealed class RegistrationFieldError
    {
        private static readonly Dictionary<string, RegistrationField> KnownFields = new Dictionary<string, RegistrationField>(StringComparer.Ordinal)
        {
            ["username"] = RegistrationField.Username,
            ["email"] = RegistrationField.Email,
            ["password"] = RegistrationField.Password,
            ["password_confirmation"] = RegistrationField.PasswordConfirmation,
        };

        private RegistrationFieldError(RegistrationField field, string fieldName, IReadOnlyList<string> messages)
        {
            Field = field;
            FieldName = fieldName;
            Messages = messages;
        }

        /// <summary>Campo tipado, ou <see cref="RegistrationField.Other"/>.</summary>
        /// <example><code>RegistrationField field = error.Field;</code></example>
        public RegistrationField Field { get; }

        /// <summary>Nome do campo como o servidor mandou.</summary>
        /// <example><code>string name = error.FieldName; // non_field_errors</code></example>
        public string FieldName { get; }

        /// <summary>Mensagens do servidor, na ordem.</summary>
        /// <example><code>string first = error.Messages[0];</code></example>
        public IReadOnlyList<string> Messages { get; }

        /// <summary>Monta o erro a partir do nome do campo no corpo 400.</summary>
        /// <example><code>RegistrationFieldError error = RegistrationFieldError.Create("email", messages);</code></example>
        public static RegistrationFieldError Create(string fieldName, IReadOnlyList<string> messages)
        {
            if (string.IsNullOrEmpty(fieldName))
                throw new ArgumentException($"registration field name is '{fieldName}': expected the key from the 400 body", nameof(fieldName));

            RegistrationField field = KnownFields.TryGetValue(fieldName, out RegistrationField known) ? known : RegistrationField.Other;
            return new RegistrationFieldError(field, fieldName, messages ?? throw new ArgumentNullException(nameof(messages), $"messages of '{fieldName}' are null: expected the server's list"));
        }
    }
}
