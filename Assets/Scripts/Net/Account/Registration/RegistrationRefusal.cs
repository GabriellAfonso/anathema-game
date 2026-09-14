#nullable enable
using System;
using System.Collections.Generic;

namespace Anathema.Net.Account
{
    /// <summary>
    /// Recusa do cadastro: os campos recusados com as mensagens do servidor (FR-003), ou uma
    /// resposta que não tem essa forma.
    /// </summary>
    /// <example>
    /// <code>
    /// if (outcome.Refusal?.Unrecognized != null) ShowGenericError();
    /// </code>
    /// </example>
    public sealed class RegistrationRefusal
    {
        private RegistrationRefusal(IReadOnlyList<RegistrationFieldError> fields, UnrecognizedRefusal? unrecognized)
        {
            Fields = fields;
            Unrecognized = unrecognized;
        }

        /// <summary>Campos recusados, na ordem do corpo; vazio quando a resposta não foi reconhecida.</summary>
        /// <example><code>int refused = refusal.Fields.Count;</code></example>
        public IReadOnlyList<RegistrationFieldError> Fields { get; }

        /// <summary>Status e trecho do corpo quando a resposta não é um dicionário de listas.</summary>
        /// <example><code>int? status = refusal.Unrecognized?.Status;</code></example>
        public UnrecognizedRefusal? Unrecognized { get; }

        /// <summary>Recusa por campos; lista vazia lança.</summary>
        /// <example><code>return RegistrationRefusal.FromFields(errors);</code></example>
        public static RegistrationRefusal FromFields(IReadOnlyList<RegistrationFieldError> fields)
        {
            if (fields == null || fields.Count == 0)
                throw new ArgumentException($"registration refusal has {fields?.Count ?? 0} fields: expected at least one refused field", nameof(fields));

            return new RegistrationRefusal(fields, null);
        }

        /// <summary>Resposta não reconhecida.</summary>
        /// <example><code>return RegistrationRefusal.FromUnrecognized(new UnrecognizedRefusal(500, body));</code></example>
        public static RegistrationRefusal FromUnrecognized(UnrecognizedRefusal unrecognized)
        {
            return new RegistrationRefusal(Array.Empty<RegistrationFieldError>(), unrecognized ?? throw new ArgumentNullException(nameof(unrecognized), "unrecognized registration refusal is null: expected status and body"));
        }

        /// <summary>Forma para log, só com os nomes dos campos.</summary>
        /// <example><code>string text = refusal.ToString(); // fields: email, password</code></example>
        public override string ToString()
        {
            if (Unrecognized != null)
                return Unrecognized.ToString();

            List<string> names = new List<string>();
            foreach (RegistrationFieldError error in Fields)
                names.Add(error.FieldName);

            return "fields: " + string.Join(", ", names);
        }
    }
}
