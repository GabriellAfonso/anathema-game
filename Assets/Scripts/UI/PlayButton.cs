#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Anathema.Net.Account;
using Anathema.Net.Core;
using UnityEngine;

/// <summary>
/// Botao Jogar da Home: entra na fila com o primeiro deck do jogador, ate existir tela de escolha de
/// deck (specs/003-authenticated-socket-queue/spec.md, FR-040). O nome da classe fica como esta porque
/// a HomeScene liga o botao por ele.
/// </summary>
/// <example><code>button.onClick.AddListener(playButton.OnClickAction);</code></example>
public class MyButtonScript : MonoBehaviour
{
    /// <summary>Chamado pelo botao da HomeScene.</summary>
    /// <example><code>playButton.OnClickAction();</code></example>
    public void OnClickAction()
    {
        _ = JoinWithFirstDeckAsync(PlayerSession.Instance);
    }

    private static async Task JoinWithFirstDeckAsync(PlayerSession session)
    {
        try
        {
            AccountCallOutcome<IReadOnlyList<PlayerDeck>, DeckRefusal> decks = await session.Account.Decks.ListAsync();
            JoinWith(session, decks);
        }
        catch (Exception unexpected)
        {
            session.Log.Error("play_decks_unavailable", new LogField("error", unexpected.GetType().Name));
        }
    }

    private static void JoinWith(PlayerSession session, AccountCallOutcome<IReadOnlyList<PlayerDeck>, DeckRefusal> decks)
    {
        if (!decks.IsSuccess)
        {
            session.Log.Warning("play_decks_unavailable", new LogField("failure", decks.Failure?.ToString() ?? decks.Refusal?.ToString() ?? "none"));
            return;
        }

        PlayerDeck? first = decks.Value.FirstOrDefault();
        if (first == null)
        {
            session.Log.Warning("play_without_deck");
            return;
        }

        session.Matchmaking.Join(first.Deck);
    }
}
