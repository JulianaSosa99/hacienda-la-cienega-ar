using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CompletionRewardPanelUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Contenido")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image rewardImage;
    [SerializeField] private Sprite rewardSprite;

    [Header("Boton")]
    [SerializeField] private Button actionButton;
    [SerializeField] private Button closeButton;

    [Header("Textos")]
    [SerializeField] private string completedTitle = "Exploracion completada";
    [TextArea(2, 5)]
    [SerializeField] private string completedMessage = "Has reunido todas las memorias de La Cienega. Acercate al equipo de la hacienda para reclamar tu recompensa.";

    [Header("Comportamiento")]
    [SerializeField] private bool showOnlyOnce = false;
    [SerializeField] private bool showIfAlreadyCompletedOnStart = false;
    [SerializeField] private bool autoConfigureCloseButtonLayout = false;
    [SerializeField] private Vector2 closeButtonSize = new Vector2(92f, 92f);
    [SerializeField] private Vector2 closeButtonMargin = new Vector2(120f, 105f);

    private const string FINAL_PANEL_SHOWN_KEY = "FINAL_COMPLETION_PANEL_SHOWN";
    private static bool dismissedForSession;

    private bool isVisible;
    private EpicRewardRevealAnimator revealAnimator;
    public bool IsVisible => isVisible;

    private void Awake()
    {
        ResolveReferences();
        WireCloseButton();
        WireActionButton();
        DisableDecorativeRaycasts();
        Hide();
    }

    private void OnEnable()
    {
        Subscribe();

        if (showIfAlreadyCompletedOnStart)
            TryShowIfCompleted();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void TryShowIfCompleted()
    {
        if (dismissedForSession)
            return;

        if (isVisible)
        {
            BringToFront();
            HideCompetingUI();
            return;
        }

        if (showOnlyOnce && PlayerPrefs.GetInt(GetShownKey(), 0) == 1)
            return;

        ExplorationManager manager = ExplorationManager.Instance;

        if (manager == null || !AreAllMinigamesCompleted(manager))
            return;

        Show();
    }

    public void Show()
    {
        ResolveReferences();
        WireCloseButton();
        WireActionButton();
        DisableDecorativeRaycasts();

        HideCompetingUI();

        if (titleText != null)
            titleText.text = completedTitle;

        if (descriptionText != null)
            descriptionText.text = completedMessage;

        if (rewardImage != null)
        {
            Sprite resolvedRewardSprite = ResolveRewardSprite();
            rewardImage.sprite = resolvedRewardSprite;
            rewardImage.enabled = resolvedRewardSprite != null;
            rewardImage.preserveAspect = true;
            rewardImage.raycastTarget = false;
        }

        if (panelRoot != null)
            panelRoot.SetActive(true);

        BringToFront();
        WireCloseButton();
        WireActionButton();
        DisableDecorativeRaycasts();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        isVisible = true;

        PlayRevealAnimation();

        if (showOnlyOnce)
        {
            PlayerPrefs.SetInt(GetShownKey(), 1);
            PlayerPrefs.Save();
        }
    }

    public void Hide()
    {
        ResolveReferences();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (panelRoot != null && panelRoot != gameObject)
            panelRoot.SetActive(false);

        isVisible = false;
    }

    private void PlayRevealAnimation()
    {
        if (panelRoot == null)
            return;

        if (revealAnimator == null)
            revealAnimator = panelRoot.GetComponent<EpicRewardRevealAnimator>();

        if (revealAnimator == null)
            revealAnimator = panelRoot.AddComponent<EpicRewardRevealAnimator>();

        // El animador solo modifica propiedades visuales. No registra listeners ni
        // cambia el estado interactable de ninguno de los botones.
        revealAnimator.Play(
            titleText,
            descriptionText,
            rewardImage,
            actionButton,
            closeButton
        );
    }

    public void CloseAndReturnToMemories()
    {
        dismissedForSession = true;

        CompletionRewardPanelUI[] rewardPanels =
            FindObjectsOfType<CompletionRewardPanelUI>(true);

        for (int i = 0; i < rewardPanels.Length; i++)
        {
            rewardPanels[i].Hide();
        }

        MemoryListUI[] memoryPanels =
            FindObjectsOfType<MemoryListUI>(true);

        if (memoryPanels.Length == 0)
            return;

        MemoryListUI memoryPanel = memoryPanels[0];
        memoryPanel.OpenMemories();
        memoryPanel.Refresh();
    }

    [ContextMenu("Reset Completion Panel Shown State")]
    public void ResetShownState()
    {
        PlayerPrefs.DeleteKey(GetShownKey());
        PlayerPrefs.Save();
    }

    private string GetShownKey()
    {
        return ExplorationManager.GetCurrentUserScopedKey(FINAL_PANEL_SHOWN_KEY);
    }

    private void OnProgressChanged(int previousPercent, int currentPercent, bool important)
    {
        dismissedForSession = false;
        TryShowIfCompleted();
    }

    public static bool TryShowAnyCompletedPanel()
    {
        CompletionRewardPanelUI[] rewardPanels =
            Object.FindObjectsOfType<CompletionRewardPanelUI>(true);

        bool shown = false;

        for (int i = 0; i < rewardPanels.Length; i++)
        {
            rewardPanels[i].TryShowIfCompleted();
            shown = shown || rewardPanels[i].IsVisible;
        }

        return shown;
    }

    private void HideCompetingUI()
    {
        NarrativeFlowUI.HideDiscoveryPrompts();
        NarrativeFlowUI.HideProgress();
        CameraMemoryButtonUI.HideButton();
        ScanBackButtonUI.HideButton();

        MemoryListUI[] memoryPanels =
            FindObjectsOfType<MemoryListUI>(true);

        foreach (MemoryListUI panel in memoryPanels)
        {
            if (IsRelatedToPanel(panel.transform))
                panel.HideContentForModal();
            else
                panel.CloseForNarrative();
        }
    }

    private bool IsRelatedToPanel(Transform other)
    {
        if (other == null || panelRoot == null)
            return false;

        Transform panelTransform = panelRoot.transform;
        return other == panelTransform ||
            other.IsChildOf(panelTransform) ||
            panelTransform.IsChildOf(other);
    }

    private Sprite ResolveRewardSprite()
    {
        if (rewardSprite != null)
            return rewardSprite;

        if (rewardImage != null && rewardImage.sprite != null)
            return rewardImage.sprite;

        ExplorationManager manager = ExplorationManager.Instance;

        if (manager == null || manager.Memories == null)
            return null;

        for (int i = manager.Memories.Count - 1; i >= 0; i--)
        {
            MemoryData memory = manager.Memories[i];

            if (memory == null || !memory.rewardEarned)
                continue;

            if (memory.rewardSprite != null)
                return memory.rewardSprite;
        }

        return null;
    }

    private void BringToFront()
    {
        Transform target = panelRoot != null
            ? panelRoot.transform
            : transform;

        target.SetAsLastSibling();
    }

    private bool AreAllMinigamesCompleted(ExplorationManager manager)
    {
        if (manager.Memories == null || manager.Memories.Count == 0)
            return false;

        for (int i = 0; i < manager.Memories.Count; i++)
        {
            MemoryData memory = manager.Memories[i];

            if (memory == null || string.IsNullOrEmpty(memory.minigameId))
                return false;

            if (!manager.IsMinigameCompleted(memory.minigameId))
                return false;
        }

        return true;
    }

    private void Subscribe()
    {
        if (ExplorationManager.Instance == null)
            return;

        ExplorationManager.Instance.ProgressChanged -= OnProgressChanged;
        ExplorationManager.Instance.ProgressChanged += OnProgressChanged;
    }

    private void Unsubscribe()
    {
        if (ExplorationManager.Instance == null)
            return;

        ExplorationManager.Instance.ProgressChanged -= OnProgressChanged;
    }

    private void ResolveReferences()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        if (canvasGroup == null)
            canvasGroup = panelRoot.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = panelRoot.AddComponent<CanvasGroup>();

        if (closeButton == null)
            closeButton = FindButtonByName("RewardCloseButton");

        if (closeButton == null)
            closeButton = FindButtonByName("CloseButton");

        if (actionButton == null)
            actionButton = FindButtonByName("ClaimRewardButton");

        if (actionButton == null)
            actionButton = FindButtonByName("RewardClaimButton");

        if (actionButton == null)
            actionButton = FindButtonByName("ActionButton");

        if (actionButton == null)
            actionButton = FindButtonByText("Reclamar");

        if (closeButton == null)
        {
            Debug.LogWarning(
                "CompletionRewardPanelUI necesita un Button llamado RewardCloseButton dentro de FinalCompletionPanel."
            );
        }
    }

    private void WireCloseButton()
    {
        if (closeButton == null)
            return;

        closeButton.onClick.RemoveListener(CloseAndReturnToMemories);
        closeButton.onClick.AddListener(CloseAndReturnToMemories);
        closeButton.interactable = true;

        if (!autoConfigureCloseButtonLayout)
            return;

        RectTransform rectTransform =
            closeButton.GetComponent<RectTransform>();

        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(1f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(1f, 1f);
            rectTransform.sizeDelta = closeButtonSize;
            rectTransform.anchoredPosition =
                new Vector2(-closeButtonMargin.x, -closeButtonMargin.y);
            rectTransform.localScale = Vector3.one;
        }

        closeButton.transform.SetAsLastSibling();
    }

    private void WireActionButton()
    {
        if (actionButton == null)
            return;

        actionButton.interactable = true;

        RewardClaimController claimController =
            actionButton.GetComponent<RewardClaimController>();

        if (claimController == null)
            claimController = actionButton.GetComponentInParent<RewardClaimController>(true);

        if (claimController == null)
            claimController = actionButton.gameObject.AddComponent<RewardClaimController>();

        claimController.Configure(actionButton);
    }

    private void DisableDecorativeRaycasts()
    {
        if (panelRoot == null)
            return;

        Graphic[] graphics =
            panelRoot.GetComponentsInChildren<Graphic>(true);

        for (int i = 0; i < graphics.Length; i++)
        {
            Button parentButton =
                graphics[i].GetComponentInParent<Button>(true);

            if (parentButton != null)
                continue;

            graphics[i].raycastTarget = false;
        }
    }

    private Button FindButtonByName(string buttonName)
    {
        if (panelRoot == null)
            return null;

        Transform[] children =
            panelRoot.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name != buttonName)
                continue;

            return children[i].GetComponent<Button>();
        }

        return null;
    }

    private Button FindButtonByText(string text)
    {
        if (panelRoot == null)
            return null;

        Button[] buttons =
            panelRoot.GetComponentsInChildren<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            TMP_Text label =
                buttons[i].GetComponentInChildren<TMP_Text>(true);

            if (label == null || string.IsNullOrWhiteSpace(label.text))
                continue;

            if (label.text.Contains(text))
                return buttons[i];
        }

        return null;
    }
}
