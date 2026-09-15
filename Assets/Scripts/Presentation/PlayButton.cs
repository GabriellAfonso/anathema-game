#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Anathema.Client.Scenes;
using Anathema.Net.Account;
using Anathema.Net.Core;
using Anathema.Net.Facade;
using UnityEngine;

/// <summary>
/// Botao Jogar da Home: entra na fila com o primeiro deck do jogador, ate existir tela de escolha de
/// deck (specs/003-authenticated-socket-queue/spec.md, FR-040). O nome da classe fica como esta porque
/// a HomeScene liga o botao por ele. Desde a 005 usa só a fachada; a VersusScene vem pelo roteador de cena.
/// </summary>
/// <example><code>button.onClick.AddListener(playButton.OnClickAction);</code></example>
public class MyButtonScript : MonoBehaviour, ISceneClientConsumer
{
    private AnathemaClient? client;

    /// <summary>Recebe a fachada.</summary>
    /// <example><code>playButton.BindClient(client);</code></example>
    public void BindClient(AnathemaClient bound)
    {
        client = bound;
    }

    /// <summary>Chamado pelo botao da HomeScene.</summary>
    /// <example><code>playButton.OnClickAction();</code></example>
    public void OnClickAction()
    {
        if (client != null)
            _ = JoinWithFirstDeckAsync(client);
    }

    private static async Task JoinWithFirstDeckAsync(AnathemaClient client)
    {
        try
        {
            AccountCallOutcome<IReadOnlyList<PlayerDeck>, DeckRefusal> decks = await client.Decks.ListAsync();
            JoinWith(client, decks);
        }
        catch (Exception unexpected)
        {
            client.Log.Error("play_decks_unavailable", new LogField("error", unexpected.GetType().Name));
        }
    }

    private static void JoinWith(AnathemaClient client, AccountCallOutcome<IReadOnlyList<PlayerDeck>, DeckRefusal> decks)
    {
        if (!decks.IsSuccess)
        {
            client.Log.Warning("play_decks_unavailable", new LogField("failure", decks.Failure?.ToString() ?? decks.Refusal?.ToString() ?? "none"));
            return;
        }

        PlayerDeck? first = decks.Value.FirstOrDefault();
        if (first == null)
        {
            client.Log.Warning("play_without_deck");
            return;
        }

        QueueJoinResult joined = client.Queue.Join(first.Deck);
        if (joined.Kind != QueueJoinKind.Started)
            client.Log.Warning("play_join_ignored", new LogField("result", joined.ToString()));
    }
}
