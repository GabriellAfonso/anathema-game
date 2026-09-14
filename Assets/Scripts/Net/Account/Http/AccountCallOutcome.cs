#nullable enable
using System;

namespace Anathema.Net.Account
{
    /// <summary>
    /// Resultado de uma chamada de conta: exatamente um de sucesso, recusa tipada do recurso,
    /// ou falha comum (FR-027). Nenhum serviço de conta lança por resposta do servidor.
    /// </summary>
    /// <example>
    /// <code>
    /// AccountCallOutcome&lt;PlayerDeck, DeckRefusal&gt; created = await decks.CreateAsync(draft);
    /// if (created.IsSuccess) Show(created.Value);
    /// else if (created.Refusal != null) ShowRefusal(created.Refusal);
    /// else ShowFailure(created.Failure!);
    /// </code>
    /// </example>
    public sealed class AccountCallOutcome<TValue, TRefusal> where TRefusal : class
    {
        private readonly TValue value;

        private AccountCallOutcome(bool isSuccess, TValue value, TRefusal? refusal, AccountCallFailure? failure)
        {
            IsSuccess = isSuccess;
            this.value = value;
            Refusal = refusal;
            Failure = failure;
        }

        /// <summary>O servidor aceitou.</summary>
        /// <example><code>if (outcome.IsSuccess) Use(outcome.Value);</code></example>
        public bool IsSuccess { get; }

        /// <summary>O valor; ler fora do sucesso lança com o estado real.</summary>
        /// <example><code>PlayerDeck deck = outcome.Value;</code></example>
        public TValue Value => IsSuccess
            ? value
            : throw new InvalidOperationException($"account call {DescribeState()}: expected IsSuccess before reading Value");

        /// <summary>A recusa tipada do recurso, ou nulo.</summary>
        /// <example><code>DeckRefusal? refusal = outcome.Refusal;</code></example>
        public TRefusal? Refusal { get; }

        /// <summary>A falha comum, ou nulo.</summary>
        /// <example><code>AccountCallFailure? failure = outcome.Failure;</code></example>
        public AccountCallFailure? Failure { get; }

        /// <summary>Sucesso com o valor.</summary>
        /// <example><code>return AccountCallOutcome&lt;OwnProfile, ProfileRefusal&gt;.Success(profile);</code></example>
        public static AccountCallOutcome<TValue, TRefusal> Success(TValue value) => new AccountCallOutcome<TValue, TRefusal>(true, value, null, null);

        /// <summary>Recusa tipada.</summary>
        /// <example><code>return AccountCallOutcome&lt;PlayerDeck, DeckRefusal&gt;.Refused(refusal);</code></example>
        public static AccountCallOutcome<TValue, TRefusal> Refused(TRefusal refusal)
        {
            TRefusal required = refusal ?? throw new ArgumentNullException(nameof(refusal), $"refusal of {typeof(TRefusal).Name} is null: expected the typed refusal");
            return new AccountCallOutcome<TValue, TRefusal>(false, default!, required, null);
        }

        /// <summary>Falha comum.</summary>
        /// <example><code>return AccountCallOutcome&lt;LoadedCatalog, UnrecognizedRefusal&gt;.Failed(failure);</code></example>
        public static AccountCallOutcome<TValue, TRefusal> Failed(AccountCallFailure failure)
        {
            AccountCallFailure required = failure ?? throw new ArgumentNullException(nameof(failure), "account call failure is null: expected the common failure");
            return new AccountCallOutcome<TValue, TRefusal>(false, default!, null, required);
        }

        private string DescribeState() => Refusal != null ? $"was refused ({Refusal})" : $"failed ({Failure})";
    }
}
