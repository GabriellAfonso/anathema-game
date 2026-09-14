#nullable enable
using System;
using Anathema.Net.Core;

namespace Anathema.Net.Account
{
    /// <summary>
    /// Lê o corpo de sucesso de uma rota autenticada com o leitor do serviço. Corpo fora do
    /// contrato vira <see cref="AccountCallFailureKind.OutOfContract"/> e um registro com status,
    /// tipo e caminho da falha, nunca com o corpo.
    /// </summary>
    /// <example>
    /// <code>
    /// return responses.ReadSuccess&lt;OwnProfile, ProfileRefusal&gt;(result, OwnProfile.Read);
    /// </code>
    /// </example>
    internal sealed class AccountResponseReader
    {
        private readonly IProtocolCodec codec;
        private readonly IClientLog log;

        /// <summary>Cria o leitor sobre o codec e o log.</summary>
        /// <example><code>AccountResponseReader responses = new AccountResponseReader(codec, log);</code></example>
        internal AccountResponseReader(IProtocolCodec codec, IClientLog log)
        {
            this.codec = codec ?? throw new ArgumentNullException(nameof(codec), "account response reader codec is null: expected the project codec");
            this.log = log ?? throw new ArgumentNullException(nameof(log), "account response reader log is null: expected the client log");
        }

        /// <summary>Sucesso com o valor lido, ou falha fora do contrato.</summary>
        /// <example><code>return responses.ReadSuccess&lt;OwnProfile, ProfileRefusal&gt;(result, OwnProfile.Read);</code></example>
        internal AccountCallOutcome<TValue, TRefusal> ReadSuccess<TValue, TRefusal>(AuthenticatedCallResult response, Func<IPayloadReader, TValue> read)
            where TRefusal : class
        {
            DecodeOutcome<TValue> value = Decode(response.BodyText, read);
            if (value.IsValid)
                return AccountCallOutcome<TValue, TRefusal>.Success(value.Value);

            return AccountCallOutcome<TValue, TRefusal>.Failed(OutOfContract(response.Status, value.Failure));
        }

        /// <summary>Lê um corpo JSON com a função; forma errada vira falha.</summary>
        /// <example><code>DecodeOutcome&lt;DeckRefusal&gt; refusal = responses.Decode(body, DeckRefusalReader.ReadBadRequest);</code></example>
        internal DecodeOutcome<TValue> Decode<TValue>(string bodyText, Func<IPayloadReader, TValue> read)
        {
            DecodeOutcome<IPayloadReader> json = codec.DecodeObject(bodyText);
            if (!json.IsValid)
                return DecodeOutcome<TValue>.Invalid(json.Failure);

            try
            {
                return DecodeOutcome<TValue>.Valid(read(json.Value));
            }
            catch (PayloadShapeException shape)
            {
                return DecodeOutcome<TValue>.Invalid(shape.Failure);
            }
        }

        /// <summary>Registra e monta a falha fora do contrato.</summary>
        /// <example><code>AccountCallFailure failure = responses.OutOfContract(200, decoded.Failure);</code></example>
        internal AccountCallFailure OutOfContract(int status, DecodeFailure failure)
        {
            log.Warning("account_response_out_of_contract", new LogField("status", status), new LogField("kind", failure.Kind.ToString()), new LogField("path", failure.Path));
            return AccountCallFailure.OutOfContract(status, failure);
        }
    }
}
