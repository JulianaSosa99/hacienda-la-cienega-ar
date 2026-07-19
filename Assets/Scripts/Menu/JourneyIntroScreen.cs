using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class JourneyIntroScreen : MonoBehaviour
{
    [Header("Pantallas")]
    [SerializeField] private GameObject introPanel;
    [SerializeField] private GameObject memoriesPanel;
    [SerializeField] private MemoryListUI memoriesUI;

    [Header("Boton")]
    [SerializeField] private Button startButton;

    [Header("Comportamiento")]
    [SerializeField] private bool enableLegacyJourneyIntro = false;
    [SerializeField] private bool skipIntroWhenProgressExists = true;

    private CanvasGroup introCanvasGroup;
    private Coroutine initialScreenRoutine;

    private void Awake()
    {
        ResolveReferences();

        if (enableLegacyJourneyIntro)
            WireStartButton();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (!enableLegacyJourneyIntro)
        {
            SetIntroVisible(false);
            return;
        }

        WireStartButton();

        if (initialScreenRoutine != null)
            StopCoroutine(initialScreenRoutine);

        initialScreenRoutine = StartCoroutine(ShowInitialScreen());
    }

    private void OnDisable()
    {
        if (initialScreenRoutine != null)
        {
            StopCoroutine(initialScreenRoutine);
            initialScreenRoutine = null;
        }
    }

    private IEnumerator ShowInitialScreen()
    {
        float elapsed = 0f;

        while (ExplorationManager.Instance == null && elapsed < 1.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (ShouldSkipIntro())
            StartJourney();
        else
            ShowIntro();

        initialScreenRoutine = null;
    }

    public void ShowIntro()
    {
        if (ExplorationProgressBarUI.Instance != null)
            ExplorationProgressBarUI.Instance.Hide();

        CameraMemoryButtonUI.HideButton();

        if (introPanel != null)
            introPanel.SetActive(true);

        SetIntroVisible(true);

        if (memoriesPanel != null)
            memoriesPanel.SetActive(false);
    }

    public void StartJourney()
    {
        ResolveReferences();

        Debug.Log("JourneyIntroScreen StartJourney");

        if (ExplorationProgressBarUI.Instance != null)
            ExplorationProgressBarUI.Instance.Hide();

        CameraMemoryButtonUI.HideButton();
        SetIntroVisible(false);

        if (memoriesUI != null)
        {
            memoriesUI.OpenMemories();
        }
        else if (memoriesPanel != null)
        {
            memoriesPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("JourneyIntroScreen no tiene MemoriasPanel ni MemoryListUI asignado.");
        }

    }

    private void ResolveReferences()
    {
        if (introPanel == null)
            introPanel = gameObject;

        if (introCanvasGroup == null && introPanel != null)
            introCanvasGroup = introPanel.GetComponent<CanvasGroup>();

        if (startButton == null && introPanel != null)
            startButton = introPanel.GetComponentInChildren<Button>(true);

        if (memoriesUI == null && memoriesPanel != null)
            memoriesUI = memoriesPanel.GetComponent<MemoryListUI>();

        if (memoriesUI == null)
            memoriesUI = FindObjectOfType<MemoryListUI>(true);

        if (memoriesPanel == null && memoriesUI != null)
            memoriesPanel = memoriesUI.gameObject;
    }

    private void WireStartButton()
    {
        if (startButton == null)
            return;

        startButton.onClick.RemoveListener(StartJourney);
        startButton.onClick.AddListener(StartJourney);
        startButton.interactable = true;

        Graphic targetGraphic = startButton.targetGraphic;

        if (targetGraphic != null)
            targetGraphic.raycastTarget = true;
    }

    private bool ShouldSkipIntro()
    {
        if (!skipIntroWhenProgressExists)
            return false;

        ExplorationManager manager = ExplorationManager.Instance;

        if (manager == null || manager.Progress == null)
            return false;

        manager.LoadForCurrentUser();

        return
            HasItems(manager.Progress.unlockedMemories) ||
            HasItems(manager.Progress.completedMinigames) ||
            HasItems(manager.Progress.earnedRewards) ||
            HasItems(manager.Progress.discoveredCollectibles);
    }

    private bool HasItems(System.Collections.Generic.List<string> items)
    {
        return items != null && items.Count > 0;
    }

    private void SetIntroVisible(bool visible)
    {
        if (introPanel == null)
            return;

        if (introCanvasGroup == null)
            introCanvasGroup = introPanel.GetComponent<CanvasGroup>();

        if (introCanvasGroup != null)
        {
            introCanvasGroup.alpha = visible ? 1f : 0f;
            introCanvasGroup.interactable = visible;
            introCanvasGroup.blocksRaycasts = visible;
        }

        introPanel.SetActive(visible);
    }
}
