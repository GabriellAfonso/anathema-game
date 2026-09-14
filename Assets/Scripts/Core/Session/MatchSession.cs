#nullable enable
using System;
using Anathema.Net.Match;
using UnityEngine;

/// <summary>
/// O estado da partida em vigor, do jeito que o servidor mandou.
///
/// Fica fora da MatchScene porque o `match_start` chega antes dela existir, e
/// chega de novo a cada reconexao, quando a cena ja esta montada. Guardar aqui
/// e o que deixa os dois casos usarem o mesmo caminho.
///
/// O cliente nunca mescla: cada snapshot substitui o anterior inteiro. O
/// servidor e a autoridade, e mesclar e como card game cria dessincronizacao
/// silenciosa.
///
/// Desde a feature 004 o estado mora no espelho da <see cref="LiveMatch"/>; esta
/// classe so aponta para a sessao atual e repassa a troca de estado para a cena
/// (specs/004-match-session/research.md, R12).
/// </summary>
/// <example><code>MatchSession.Instance.OnStateChanged += view => Redraw(view);</code></example>
public class MatchSession : MonoBehaviour
{
    private static MatchSession? instance;

    /// <summary>
    /// Cria sob demanda: quem escreve aqui e o MatchClient, que roda antes de
    /// qualquer cena de partida existir.
    /// </summary>
    /// <example><code>MatchSession.Instance.Attach(live);</code></example>
    public static MatchSession Instance
    {
        get
        {
            if (instance != null)
                return instance;

            var host = new GameObject(nameof(MatchSession));
            DontDestroyOnLoad(host);

            // AddComponent roda o Awake na hora, que ja preenche `instance`.
            host.AddComponent<MatchSession>();

            return instance!;
        }
    }

    /// <summary>A sessao de partida atual, ou null se nao ha partida em curso.</summary>
    /// <example><code>LiveMatch? live = MatchSession.Instance.Live;</code></example>
    public LiveMatch? Live { get; private set; }

    /// <summary>Ultimo snapshot aceito, ou null se nao ha partida em curso.</summary>
    /// <example><code>PlayerView? view = MatchSession.Instance.State;</code></example>
    public PlayerView? State => Live?.Mirror.Current;

    /// <summary>Ha estado espelhado para desenhar.</summary>
    /// <example><code>if (MatchSession.Instance.HasState) Redraw(MatchSession.Instance.State!);</code></example>
    public bool HasState => State != null;

    /// <summary>
    /// Chegou snapshot novo. A MatchScene escuta para se redesenhar quando o
    /// estado troca embaixo dela, que e o que acontece numa reconexao.
    /// </summary>
    /// <example><code>MatchSession.Instance.OnStateChanged += view => Redraw(view);</code></example>
    public event Action<PlayerView>? OnStateChanged;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>Aponta para a sessao nova, desligando a anterior antes.</summary>
    /// <example><code>MatchSession.Instance.Attach(live);</code></example>
    public void Attach(LiveMatch live)
    {
        LiveMatch required = live ?? throw new ArgumentNullException(nameof(live), "live match is null: expected the LiveMatch created by MatchClient");
        Clear();
        Live = required;
        required.Mirror.ViewReplaced += Relay;
    }

    /// <summary>Fim de partida. Sem isto, a proxima comecaria com o estado da anterior.</summary>
    /// <example><code>MatchSession.Instance.Clear();</code></example>
    public void Clear()
    {
        if (Live != null)
            Live.Mirror.ViewReplaced -= Relay;

        Live = null;
    }

    private void Relay(ViewReplaced change)
    {
        OnStateChanged?.Invoke(change.Current);
    }
}
