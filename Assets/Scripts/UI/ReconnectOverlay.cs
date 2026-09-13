using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Avisa o jogador que a conexao caiu e que o jogo esta voltando sozinho.
///
/// Sem isto a reconexao e invisivel: a tela simplesmente congela por alguns
/// segundos e o jogador fecha o jogo achando que travou.
///
/// Monta a propria interface em codigo, sem prefab nem cena: um overlay de
/// falha precisa existir em qualquer cena, inclusive nas que ainda nao foram
/// construidas. Trocar por um prefab desenhado depois e so preencher
/// <see cref="Build"/>.
/// </summary>
public class ReconnectOverlay : MonoBehaviour
{
    // Acima de qualquer UI da cena: e o unico elemento que nunca pode ficar
    // atras de outra coisa.
    private const int SortingOrder = 32767;

    private static ReconnectOverlay instance;

    // Quem esta com problema agora. O overlay some quando o ultimo se resolve,
    // e nao quando o primeiro volta.
    private readonly HashSet<BaseClient> troubled = new();

    private GameObject panel;
    private TextMeshProUGUI label;

    /// <summary>
    /// Liga o overlay aos clients que valem interromper o jogador.
    ///
    /// O socket de presenca fica de fora de proposito: ele nao sustenta nenhuma
    /// acao do jogador, e tampar a tela porque a presenca piscou seria pior que
    /// o problema. Se o servidor caiu de vez, a fila ou a partida avisam.
    /// </summary>
    public static void Attach(params BaseClient[] clients)
    {
        if (instance == null)
        {
            var host = new GameObject(nameof(ReconnectOverlay));
            DontDestroyOnLoad(host);
            instance = host.AddComponent<ReconnectOverlay>();
            instance.Build();
        }

        foreach (var client in clients)
        {
            if (client == null)
                continue;

            client.OnReconnecting += (attempt, _) => instance.ShowRetrying(client, attempt);
            client.OnReconnected += () => instance.Clear(client);
            client.OnGaveUp += reason => instance.ShowFailed(client, reason);
        }
    }

    private void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        gameObject.AddComponent<GraphicRaycaster>();

        panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, worldPositionStays: false);

        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // O Image tambem serve de bloqueio: enquanto o overlay esta de pe, um
        // clique nao pode chegar numa carta cujo estado ja nao vale.
        var background = panel.GetComponent<Image>();
        background.color = new Color(0, 0, 0, 0.75f);
        background.raycastTarget = true;

        var text = new GameObject("Label", typeof(RectTransform));
        text.transform.SetParent(panel.transform, worldPositionStays: false);

        label = text.AddComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 42;
        label.color = Color.white;
        label.raycastTarget = false;

        var labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0.1f, 0.4f);
        labelRect.anchorMax = new Vector2(0.9f, 0.6f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        panel.SetActive(false);
    }

    private void ShowRetrying(BaseClient client, int attempt)
    {
        troubled.Add(client);

        label.text = attempt <= 1
            ? "Conexao perdida.\nReconectando..."
            : $"Conexao perdida.\nReconectando... (tentativa {attempt})";

        panel.SetActive(true);
    }

    private void ShowFailed(BaseClient client, string reason)
    {
        troubled.Add(client);

        // Sem auto-hide: desistiu, entao a mensagem fica ate o jogador agir.
        label.text = $"Nao foi possivel reconectar.\n\n{reason}";

        panel.SetActive(true);
    }

    private void Clear(BaseClient client)
    {
        troubled.Remove(client);

        if (troubled.Count == 0)
            panel.SetActive(false);
    }
}
