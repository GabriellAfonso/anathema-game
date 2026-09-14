#nullable enable
using System;
using System.Collections.Generic;
using Anathema.Net.Account;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary>
    /// O que a apresentação precisa para mirar uma carta da mão, lido do catálogo e da visão atual. Só informa:
    /// custo e energia vêm lado a lado, sem comparação; candidatos são as unidades daquele lado, sem filtro de
    /// regra. A camada não esconde nem bloqueia jogada nenhuma: o servidor decide (FR-028 a FR-030).
    /// </summary>
    /// <example>
    /// <code>
    /// HandCardHint hint = match.HintFor(card.Instance);
    /// costLabel.text = $"{hint.Cost}/{hint.EnergyCurrent}";
    /// </code>
    /// </example>
    public sealed class HandCardHint
    {
        private HandCardHint(CardInstanceId instance, HintCardKind kind, PlayerView? view)
        {
            Instance = instance;
            Kind = kind;
            EnergyCurrent = view?.You.EnergyCurrent ?? 0;
            Phase = view?.Phase ?? MatchPhase.Unknown;
        }

        /// <summary>A cópia consultada.</summary>
        /// <example><code>CardInstanceId card = hint.Instance;</code></example>
        public CardInstanceId Instance { get; }

        /// <summary>A entrada do catálogo; nula fora da mão.</summary>
        /// <example><code>CardId? card = hint.Card;</code></example>
        public CardId? Card { get; private set; }

        /// <summary>Unidade, feitiço, carta desconhecida ou fora da mão.</summary>
        /// <example><code>bool spell = hint.Kind == HintCardKind.Spell;</code></example>
        public HintCardKind Kind { get; }

        /// <summary>Custo de energia do catálogo; nulo sem entrada no catálogo.</summary>
        /// <example><code>long? cost = hint.Cost;</code></example>
        public long? Cost { get; private set; }

        /// <summary>Energia atual do próprio lado, para mostrar ao lado do custo.</summary>
        /// <example><code>long energy = hint.EnergyCurrent;</code></example>
        public long EnergyCurrent { get; }

        /// <summary>Lado do alvo, só em feitiço.</summary>
        /// <example><code>bool aimsEnemy = hint.Target == SpellTargetKind.EnemyUnit;</code></example>
        public SpellTargetKind? Target { get; private set; }

        /// <summary>Cópias no banco do lado do alvo, na ordem do banco; vazia sem alvo ou sem unidade.</summary>
        /// <example><code>foreach (CardInstanceId candidate in hint.Candidates) Highlight(candidate);</code></example>
        public IReadOnlyList<CardInstanceId> Candidates { get; private set; } = Array.Empty<CardInstanceId>();

        /// <summary>O feitiço é marcado no catálogo como só na declaração.</summary>
        /// <example><code>bool declaring = hint.DeclarationOnly;</code></example>
        public bool DeclarationOnly { get; private set; }

        /// <summary>A fase atual, para mostrar junto de <see cref="DeclarationOnly"/>.</summary>
        /// <example><code>MatchPhase phase = hint.Phase;</code></example>
        public MatchPhase Phase { get; }

        internal static HandCardHint NotInHand(CardInstanceId instance, PlayerView? view) => new HandCardHint(instance, HintCardKind.NotInHand, view);

        internal static HandCardHint UnknownCard(MatchCard card, PlayerView view) => new HandCardHint(card.Instance, HintCardKind.UnknownCard, view) { Card = card.Card };

        internal static HandCardHint ForUnit(MatchCard card, UnitCard unit, PlayerView view)
        {
            return new HandCardHint(card.Instance, HintCardKind.Unit, view) { Card = card.Card, Cost = unit.Energy };
        }

        internal static HandCardHint ForSpell(MatchCard card, SpellCard spell, PlayerView view, IReadOnlyList<CardInstanceId> candidates)
        {
            return new HandCardHint(card.Instance, HintCardKind.Spell, view)
            {
                Card = card.Card,
                Cost = spell.Energy,
                Target = spell.Effect.TargetKind,
                Candidates = candidates,
                DeclarationOnly = spell.Effect.DeclarationOnly,
            };
        }
    }
}
