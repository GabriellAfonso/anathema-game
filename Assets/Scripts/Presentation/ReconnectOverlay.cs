#nullable enable
using Anathema.Client.Scenes;
using Anathema.Net.Facade;
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
///
/// Desde a 005 é um componente da BootstrapScene ligado pelo hospedeiro, e assina a saúde da conexão ativa pela
/// fachada: a de fila enquanto procura, a de partida do pareamento em diante. O socket de presenca, que ficava de
/// fora de proposito, saiu do backend.
/// </summary>
/// <example><code>// Componente do objeto ReconnectOverlay da BootstrapScene; o hospedeiro chama BindClient.</code></example>
public class ReconnectOverlay : MonoBehaviour, ISceneClientConsumer
{
    // Acima de qualquer UI da cena: e o unico elemento que nunca pode ficar
    // atras de outra coisa.
    private const int SortingOrder = 32767;

    private readonly SceneSubscriptions subscriptions = new SceneSubscriptions();
    private GameObject panel = null!;
    private TextMeshProUGUI label = null!;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Build();
    }

    /// <summary>Liga o overlay à saúde da conexão ativa e ao estado do app.</summary>
    /// <example><code>overlay.BindClient(client);</code></example>
    public void BindClient(AnathemaClient client)
    {
        subscriptions.Add(client.Health.Reconnecting.Subscribe(notice => ShowRetrying(notice.Attempt)));
        subscriptions.Add(client.Health.Recovered.Subscribe(_ => Hide()));
        subscriptions.Add(client.Health.GaveUp.Subscribe(notice => ShowFailed(notice.PlayerText)));
        subscriptions.Add(client.StageChanged.Subscribe(OnStageChanged));
    }

    private void OnDestroy() => subscriptions.DisposeAll();

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

        BuildLabel();
        panel.SetActive(false);
    }

    private void BuildLabel()
    {
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
    }

    private void ShowRetrying(int attempt)
    {
        label.text = attempt <= 1
            ? "Conexao perdida.\nReconectando..."
            : $"Conexao perdida.\nReconectando... (tentativa {attempt})";

        panel.SetActive(true);
    }

    private void ShowFailed(string reason)
    {
        // Sem auto-hide: desistiu, entao a mensagem fica ate o jogador agir.
        label.text = $"Nao foi possivel reconectar.\n\n{reason}";

        panel.SetActive(true);
    }

    private void OnStageChanged(ClientStageChange change)
    {
        // A saúde é só da conexão ativa: voltar ao início ou ao login não tem mais conexão com problema na tela.
        if (change.Current.Stage == ClientStage.SignedIn || change.Current.Stage == ClientStage.SignedOut)
            Hide();
    }

    private void Hide() => panel.SetActive(false);
}
