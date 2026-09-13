using System;
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
/// </summary>
public class MatchSession : MonoBehaviour
{
    private static MatchSession instance;

    /// <summary>
    /// Cria sob demanda: quem escreve aqui e o MatchClient, que roda antes de
    /// qualquer cena de partida existir.
    /// </summary>
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

            return instance;
        }
    }

    /// <summary>Ultimo snapshot recebido, ou null se nao ha partida em curso.</summary>
    public MatchStateDTO State { get; private set; }

    public bool HasState => State != null;

    /// <summary>
    /// Chegou snapshot novo. A MatchScene escuta para se redesenhar quando o
    /// estado troca embaixo dela, que e o que acontece numa reconexao.
    /// </summary>
    public event Action<MatchStateDTO> OnStateChanged;

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

    public void ApplyState(MatchStateDTO state)
    {
        State = state;

        OnStateChanged?.Invoke(state);
    }

    /// <summary>Fim de partida. Sem isto, a proxima comecaria com o estado da anterior.</summary>
    public void Clear()
    {
        State = null;
    }
}
