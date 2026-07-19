using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MemoryListUI : MonoBehaviour
{
    private const float MinimumLogoutButtonBottomMargin = 210f;

    [Header("Pantallas")]
    [SerializeField] private GameObject headerPanel;
    [SerializeField] private GameObject listPanel;

    [Header("Titulo del marco")]
    [SerializeField] private GameObject haciendaTitleObject;
    [SerializeField] private GameObject memoriesTitleObject;

    [Header("Progreso")]
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Image progressFillImage;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text progressCounterText;
    [SerializeField] private float progressAnimationDuration = 0.8f;
    [SerializeField] private float progressPulseScale = 1.12f;

    [Header("Lista")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private MemoryListItemUI itemPrefab;
    [SerializeField] private bool autoConfigureScrollRect = false;
    [SerializeField] private float itemSpacing = 42f;
    [SerializeField] private int topPadding = 8;
    [SerializeField] private int bottomPadding = 48;
    [SerializeField] private float itemHeight = 225f;

    [Header("Detalle")]
    [SerializeField] private MemoryDetailUI detailUI;

    [Header("Escaneo")]
    [SerializeField] private GameObject scanFrameOverlay;
    [SerializeField] private Button scanButton;
    [SerializeField] private float scanButtonBottomMargin = 28f;
    [SerializeField] private float scanButtonListGap = 24f;

    [Header("Cerrar sesion")]
    [SerializeField] private Button logoutButton;
    [SerializeField] private bool autoConfigureLogoutButtonLayout = false;
    [SerializeField] private Sprite logoutButtonSprite;
    [SerializeField] private string logoutButtonText = "Cerrar sesion";
    [SerializeField] private Vector2 logoutButtonSize = new Vector2(420f, 92f);
    [SerializeField] private float logoutButtonBottomMargin = 210f;
    [SerializeField] private float logoutButtonListGap = 36f;

    private CanvasGroup canvasGroup;
    private readonly List<SpawnedMemoryItem> spawnedItems =
        new List<SpawnedMemoryItem>();
    private Canvas parentCanvas;
    private Vector2 pointerDownPosition;
    private bool pointerStartedOnList;
    private Coroutine progressAnimation;
    private Coroutine refreshWhenReadyRoutine;
    private Vector3 progressDefaultScale = Vector3.one;
    private int lastShownPercent = -1;

    public bool IsMemoriesOpen => gameObject.activeInHierarchy;
    public bool IsDetailOpen => detailUI != null && detailUI.gameObject.activeInHierarchy;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        parentCanvas = GetComponentInParent<Canvas>();
        CacheProgressDefaultScale();
        ResolveScanButton();
        ResolveLogoutButton();
        ResolveMarcoReferences();
    }

    private void Update()
    {
        HandleDirectItemTap();
    }

    private void OnEnable()
    {
        PreparePanelForInteraction();
        CameraMemoryButtonUI.HideButton();

        if (ExplorationProgressBarUI.Instance != null)
            ExplorationProgressBarUI.Instance.Hide();

        ResolveScanButton();
        ResolveLogoutButton();
        ResolveMarcoReferences();
        SetMemoriesTitleVisible(true);
        SetScanFrameVisible(false);
        DisableHeaderRaycasts();
        SubscribeToProgressChanges();
        ShowList();

        if (refreshWhenReadyRoutine != null)
            StopCoroutine(refreshWhenReadyRoutine);

        refreshWhenReadyRoutine = StartCoroutine(RefreshWhenReady());
    }

    private void OnDisable()
    {
        if (refreshWhenReadyRoutine != null)
        {
            StopCoroutine(refreshWhenReadyRoutine);
            refreshWhenReadyRoutine = null;
        }

        UnsubscribeFromProgressChanges();
    }

    private IEnumerator RefreshWhenReady()
    {
        float elapsed = 0f;

        while (ExplorationManager.Instance == null && elapsed < 2f)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (ExplorationManager.Instance != null)
            ExplorationManager.Instance.LoadForCurrentUser();

        Refresh();
        TryShowCompletionRewardPanel();

        refreshWhenReadyRoutine = null;
    }

    public void Refresh()
    {
        ExplorationManager manager = ExplorationManager.Instance;

        if (manager == null)
        {
            Debug.LogWarning("MemoryListUI necesita un ExplorationManager en escena.");
            return;
        }

        int currentPercent = manager.GetExplorationPercent();

        if (lastShownPercent >= 0 && currentPercent > lastShownPercent)
        {
            PlayProgressAnimation(lastShownPercent, currentPercent, true);
        }
        else
        {
            UpdateProgress(currentPercent);
        }

        BuildList(manager);
        TryShowCompletionRewardPanel();
    }

    private void UpdateProgress(int percent)
    {
        SetProgressVisual(percent);
    }

    private void SetProgressVisual(float percent)
    {
        percent = Mathf.Clamp(percent, 0f, 100f);

        if (progressSlider != null)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.interactable = false;
            progressSlider.value = percent / 100f;
        }

        if (progressFillImage != null)
            progressFillImage.fillAmount = percent / 100f;

        if (progressText != null)
            progressText.text = "Exploracion " + Mathf.RoundToInt(percent) + "%";

        lastShownPercent = Mathf.RoundToInt(percent);
    }

    private void BuildList(ExplorationManager manager)
    {
        if (contentParent == null || itemPrefab == null)
            return;

        ConfigureContentLayout();
        spawnedItems.Clear();

        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            Destroy(contentParent.GetChild(i).gameObject);
        }

        for (int i = 0; i < manager.Memories.Count; i++)
        {
            MemoryData memory = manager.Memories[i];
            MemoryListItemUI item = Instantiate(itemPrefab, contentParent);

            ConfigureItemLayout(item);
            item.Setup(memory, OnMemorySelected);
            spawnedItems.Add(new SpawnedMemoryItem(item.GetComponent<RectTransform>(), memory));
        }

        UpdateProgressCounter(manager);
        ForceRebuildListLayout();
    }


    private void UpdateProgressCounter(ExplorationManager manager)
    {
        if (progressCounterText == null || manager == null)
            return;

        int completed = 0;
        int total = manager.Memories.Count;

        for (int i = 0; i < total; i++)
        {
            MemoryData memory = manager.Memories[i];

            if (memory != null && memory.isUnlocked && memory.minigameCompleted)
                completed++;
        }

        progressCounterText.text = completed + "/" + total;
    }

    private void OnMemorySelected(MemoryData memory)
    {
        ShowMemoryDetail(memory);
    }

    public void ShowMemoryDetailById(string memoryId)
    {
        if (string.IsNullOrWhiteSpace(memoryId))
        {
            ShowList();
            return;
        }

        ExplorationManager manager = ExplorationManager.Instance;

        if (manager == null)
        {
            ShowList();
            return;
        }

        for (int i = 0; i < manager.Memories.Count; i++)
        {
            MemoryData memory = manager.Memories[i];

            if (memory != null && memory.id == memoryId)
            {
                ShowMemoryDetail(memory);
                return;
            }
        }

        ShowList();
    }

    private void ShowMemoryDetail(MemoryData memory)
    {
        if (memory == null)
            return;

        Debug.Log("Memoria seleccionada: " + memory.guardianName);

        if (detailUI == null)
            detailUI = GetComponentInChildren<MemoryDetailUI>(true);

        if (headerPanel != null)
            headerPanel.SetActive(false);

        if (listPanel != null)
            listPanel.SetActive(false);

        if (detailUI != null)
        {
            detailUI.Show(memory);
        }
        else
        {
            Debug.LogWarning("No se encontro MemoryDetailUI para mostrar el detalle.");
        }
    }

    public void ShowList()
    {
        if (headerPanel != null)
            headerPanel.SetActive(true);

        if (listPanel != null)
            listPanel.SetActive(true);

        DisableHeaderRaycasts();

        if (detailUI != null)
            detailUI.gameObject.SetActive(false);
    }

    private void SubscribeToProgressChanges()
    {
        if (ExplorationManager.Instance == null)
            return;

        ExplorationManager.Instance.ProgressChanged -= OnExplorationProgressChanged;
        ExplorationManager.Instance.ProgressChanged += OnExplorationProgressChanged;
    }

    private void UnsubscribeFromProgressChanges()
    {
        if (ExplorationManager.Instance == null)
            return;

        ExplorationManager.Instance.ProgressChanged -= OnExplorationProgressChanged;
    }

    private void OnExplorationProgressChanged(
        int previousPercent,
        int currentPercent,
        bool important
    )
    {
        if (!gameObject.activeInHierarchy)
            return;

        PlayProgressAnimation(previousPercent, currentPercent, important);

        ExplorationManager manager = ExplorationManager.Instance;

        if (manager != null)
            BuildList(manager);
    }

    private void PlayProgressAnimation(
        int previousPercent,
        int currentPercent,
        bool important
    )
    {
        if (progressAnimation != null)
            StopCoroutine(progressAnimation);

        progressAnimation = StartCoroutine(
            AnimateProgress(previousPercent, currentPercent, important)
        );
    }

    private IEnumerator AnimateProgress(
        int previousPercent,
        int currentPercent,
        bool important
    )
    {
        CacheProgressDefaultScale();

        float elapsed = 0f;

        while (elapsed < progressAnimationDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(elapsed / progressAnimationDuration);

            float easedT =
                Mathf.SmoothStep(0f, 1f, t);

            float percent =
                Mathf.Lerp(previousPercent, currentPercent, easedT);

            SetProgressVisual(percent);
            ApplyProgressPulse(t, important);

            yield return null;
        }

        SetProgressVisual(currentPercent);
        ResetProgressScale();

        progressAnimation = null;
    }

    private void ApplyProgressPulse(float t, bool important)
    {
        if (!important || progressSlider == null)
            return;

        float pulse =
            Mathf.Sin(t * Mathf.PI);

        progressSlider.transform.localScale =
            Vector3.Lerp(
                progressDefaultScale,
                progressDefaultScale * progressPulseScale,
                pulse
            );
    }

    private void CacheProgressDefaultScale()
    {
        if (progressSlider == null)
            return;

        progressDefaultScale =
            progressSlider.transform.localScale;
    }

    private void ResetProgressScale()
    {
        if (progressSlider == null)
            return;

        progressSlider.transform.localScale = progressDefaultScale;
    }

    public void OpenScanner()
    {
        NarrativeFlowUI.HideDiscoveryPrompts();
        TargetUIBlocker.ResetAllForScanning();

        if (detailUI != null)
            detailUI.gameObject.SetActive(false);

        SetMemoriesTitleVisible(false);

        SetScanFrameVisible(true);

        if (ExplorationProgressBarUI.Instance != null)
            ExplorationProgressBarUI.Instance.Show();

        CameraMemoryButtonUI.ShowButton();
        ScanBackButtonUI.ShowButton();

        gameObject.SetActive(false);
    }

    public void CloseForNarrative()
    {
        HideContentForModal();
        gameObject.SetActive(false);
    }

    public void HideContentForModal()
    {
        if (detailUI != null)
            detailUI.gameObject.SetActive(false);

        if (headerPanel != null)
            headerPanel.SetActive(false);

        if (listPanel != null)
            listPanel.SetActive(false);

        SetMemoriesTitleVisible(false);
        CameraMemoryButtonUI.HideButton();
        ScanBackButtonUI.HideButton();
    }

    public void OpenMemories()
    {
        NarrativeFlowUI.HideDiscoveryPrompts();
        CameraMemoryButtonUI.HideButton();
        ScanBackButtonUI.HideButton();
        gameObject.SetActive(true);
        PreparePanelForInteraction();
        ShowList();
        TryShowCompletionRewardPanel();
    }

    private void TryShowCompletionRewardPanel()
    {
        CompletionRewardPanelUI.TryShowAnyCompletedPanel();
    }

    private void PreparePanelForInteraction()
    {
        DisableSiblingBlockers();

        transform.SetAsLastSibling();
        transform.localScale = Vector3.one;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    private void SetMemoriesTitleVisible(bool visible)
    {
        if (haciendaTitleObject != null)
            haciendaTitleObject.SetActive(!visible);

        if (memoriesTitleObject != null)
            memoriesTitleObject.SetActive(visible);
    }

    private void SetScanFrameVisible(bool visible)
    {
        if (scanFrameOverlay == null)
            return;

        scanFrameOverlay.SetActive(visible);
        SetGraphicRaycasts(scanFrameOverlay, false);
    }

    private void ResolveMarcoReferences()
    {
        Transform marcoRoot = FindParentByName(transform, "Marco");

        if (marcoRoot == null)
            marcoRoot = transform.root;

        if (haciendaTitleObject == null || haciendaTitleObject.name == "Marco")
            haciendaTitleObject = FindChildByName(marcoRoot, "LogoHacienda");

        if (scanFrameOverlay == null || scanFrameOverlay.name == "Marco")
            scanFrameOverlay = FindChildByName(marcoRoot, "MarcoPrincipal");

        if (memoriesTitleObject == null && headerPanel != null)
            memoriesTitleObject = FindChildByName(headerPanel.transform, "TitleText");
    }

    private Transform FindParentByName(Transform current, string targetName)
    {
        while (current != null)
        {
            if (current.name == targetName)
                return current;

            current = current.parent;
        }

        return null;
    }

    private GameObject FindChildByName(Transform parent, string targetName)
    {
        if (parent == null)
            return null;

        Transform[] children =
            parent.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == targetName)
                return children[i].gameObject;
        }

        return null;
    }

    private void SetGraphicRaycasts(GameObject root, bool enabled)
    {
        if (root == null)
            return;

        Graphic[] graphics =
            root.GetComponentsInChildren<Graphic>(true);

        for (int i = 0; i < graphics.Length; i++)
        {
            graphics[i].raycastTarget = enabled;
        }
    }

    private void ConfigureContentLayout()
    {
        ConfigureScrollRect();
        ConfigureLogoutButtonLayout();
        ConfigureScanButtonLayout();

        RectTransform contentRect =
            contentParent as RectTransform;

        if (contentRect != null)
        {
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.localScale = Vector3.one;
        }

        VerticalLayoutGroup layout =
            contentParent.GetComponent<VerticalLayoutGroup>();

        if (layout == null)
            layout = contentParent.gameObject.AddComponent<VerticalLayoutGroup>();

        layout.padding = new RectOffset(0, 0, topPadding, bottomPadding);
        layout.spacing = itemSpacing;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter =
            contentParent.GetComponent<ContentSizeFitter>();

        if (fitter == null)
            fitter = contentParent.gameObject.AddComponent<ContentSizeFitter>();

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void ConfigureScrollRect()
    {
        if (contentParent == null)
            return;

        ScrollRect scrollRect =
            contentParent.GetComponentInParent<ScrollRect>();

        if (scrollRect == null)
            return;

        if (scrollRect.content == null)
            scrollRect.content = contentParent as RectTransform;

        if (!autoConfigureScrollRect)
            return;

        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        RectTransform scrollRectTransform =
            scrollRect.GetComponent<RectTransform>();

        if (scrollRectTransform != null)
        {
            float reservedBottomSpace =
                GetScanButtonReservedHeight();

            scrollRectTransform.offsetMin =
                new Vector2(scrollRectTransform.offsetMin.x, reservedBottomSpace);
        }

        if (scrollRect.horizontalScrollbar != null)
        {
            scrollRect.horizontalScrollbar.gameObject.SetActive(false);
        }

        Image scrollViewImage =
            scrollRect.GetComponent<Image>();

        if (scrollViewImage != null)
            scrollViewImage.raycastTarget = false;

        if (scrollRect.viewport != null)
        {
            Image viewportImage =
                scrollRect.viewport.GetComponent<Image>();

            if (viewportImage != null)
                viewportImage.raycastTarget = false;
        }
    }

    private void ResolveScanButton()
    {
        if (scanButton != null)
            return;

        GameObject scanButtonObject =
            FindChildByName(transform, "ScanButton");

        if (scanButtonObject != null)
            scanButton = scanButtonObject.GetComponent<Button>();
    }

    private void ResolveLogoutButton()
    {
        if (logoutButton == null)
        {
            GameObject logoutButtonObject =
                FindChildByName(transform, "LogoutButton");

            if (logoutButtonObject != null)
                logoutButton = logoutButtonObject.GetComponent<Button>();
        }

        if (logoutButton == null)
            logoutButton = CreateLogoutButton();

        ConfigureLogoutButtonAction();
        ConfigureLogoutButtonVisual();
    }

    private void ConfigureScanButtonLayout()
    {
        if (scanButton == null || listPanel == null)
            return;

        RectTransform buttonRect =
            scanButton.GetComponent<RectTransform>();

        RectTransform listRect =
            listPanel.GetComponent<RectTransform>();

        if (buttonRect == null || listRect == null)
            return;

        if (buttonRect.parent != listRect)
            buttonRect.SetParent(listRect, false);

        float bottomPosition =
            scanButtonBottomMargin + GetLogoutButtonReservedHeight();

        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition =
            new Vector2(0f, bottomPosition);
        buttonRect.localScale = Vector3.one;
        buttonRect.SetAsLastSibling();
    }

    private void ConfigureLogoutButtonLayout()
    {
        if (!autoConfigureLogoutButtonLayout)
            return;

        if (logoutButton == null || listPanel == null)
            return;

        RectTransform buttonRect =
            logoutButton.GetComponent<RectTransform>();

        RectTransform listRect =
            listPanel.GetComponent<RectTransform>();

        if (buttonRect == null || listRect == null)
            return;

        if (buttonRect.parent != listRect)
            buttonRect.SetParent(listRect, false);

        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition =
            new Vector2(0f, GetLogoutButtonBottomMargin());
        buttonRect.sizeDelta = logoutButtonSize;
        buttonRect.localScale = Vector3.one;
        buttonRect.SetAsLastSibling();
    }

    private float GetScanButtonReservedHeight()
    {
        if (scanButton == null)
            return 0f;

        RectTransform buttonRect =
            scanButton.GetComponent<RectTransform>();

        if (buttonRect == null)
            return 0f;

        return
            buttonRect.rect.height
            + scanButtonBottomMargin
            + scanButtonListGap
            + GetLogoutButtonReservedHeight();
    }

    private float GetLogoutButtonReservedHeight()
    {
        if (logoutButton == null)
            return 0f;

        RectTransform buttonRect =
            logoutButton.GetComponent<RectTransform>();

        if (buttonRect == null)
            return 0f;

        float resolvedHeight =
            logoutButtonSize.y > 0f
                ? logoutButtonSize.y
                : buttonRect.rect.height;

        return resolvedHeight + GetLogoutButtonBottomMargin() + logoutButtonListGap;
    }

    private float GetLogoutButtonBottomMargin()
    {
        return Mathf.Max(logoutButtonBottomMargin, MinimumLogoutButtonBottomMargin);
    }

    private Button CreateLogoutButton()
    {
        if (listPanel == null)
            return null;

        GameObject buttonObject = new GameObject(
            "LogoutButton",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button),
            typeof(SessionLogoutButton)
        );

        buttonObject.layer = listPanel.layer;
        buttonObject.transform.SetParent(listPanel.transform, false);

        GameObject textObject = new GameObject(
            "Text (TMP)",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );

        textObject.layer = listPanel.layer;
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.text = logoutButtonText;
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 28f;
        label.color = Color.white;
        label.raycastTarget = false;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();

        return button;
    }

    private void ConfigureLogoutButtonAction()
    {
        if (logoutButton == null)
            return;

        SessionLogoutButton logout =
            logoutButton.GetComponent<SessionLogoutButton>();

        if (logout == null)
            logout = logoutButton.gameObject.AddComponent<SessionLogoutButton>();

        logout.Configure(logoutButton);
    }

    private void ConfigureLogoutButtonVisual()
    {
        if (logoutButton == null)
            return;

        Image image = logoutButton.GetComponent<Image>();

        if (image != null)
        {
            image.raycastTarget = true;

            if (logoutButtonSprite != null)
            {
                image.sprite = logoutButtonSprite;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }
            else if (image.sprite == null)
            {
                image.color = new Color(0.25f, 0.16f, 0.12f, 0.92f);
            }
        }

        TextMeshProUGUI label =
            logoutButton.GetComponentInChildren<TextMeshProUGUI>(true);

        if (label != null)
            label.text = logoutButtonText;
    }

    private void DisableHeaderRaycasts()
    {
        if (headerPanel == null)
            return;

        Graphic[] graphics =
            headerPanel.GetComponentsInChildren<Graphic>(true);

        for (int i = 0; i < graphics.Length; i++)
        {
            graphics[i].raycastTarget = false;
        }
    }

    private void DisableSiblingBlockers()
    {
        Transform parent = transform.parent;

        if (parent == null)
            return;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform sibling = parent.GetChild(i);

            if (sibling == transform)
                continue;

            if (!ShouldDisableSiblingRaycasts(sibling))
                continue;

            CanvasGroup[] canvasGroups =
                sibling.GetComponentsInChildren<CanvasGroup>(true);

            for (int j = 0; j < canvasGroups.Length; j++)
            {
                canvasGroups[j].interactable = false;
                canvasGroups[j].blocksRaycasts = false;
            }

        }
    }

    private bool ShouldDisableSiblingRaycasts(Transform sibling)
    {
        string siblingName = sibling.name;

        if (siblingName.Contains("NarrativePanel"))
            return true;

        if (siblingName.Contains("IntroJourneyPanel"))
            return true;

        CanvasGroup group = sibling.GetComponent<CanvasGroup>();
        return group != null && group.alpha <= 0.01f;
    }

    private void ConfigureItemLayout(MemoryListItemUI item)
    {
        LayoutElement layoutElement =
            item.GetComponent<LayoutElement>();

        if (layoutElement == null)
            layoutElement = item.gameObject.AddComponent<LayoutElement>();

        RectTransform rectTransform =
            item.GetComponent<RectTransform>();

        float resolvedHeight = itemHeight;

        if (rectTransform != null && rectTransform.sizeDelta.y > 0)
            resolvedHeight = rectTransform.sizeDelta.y;

        layoutElement.minHeight = resolvedHeight;
        layoutElement.preferredHeight = resolvedHeight;
        layoutElement.flexibleHeight = 0f;
    }

    private void ForceRebuildListLayout()
    {
        RectTransform contentRect =
            contentParent as RectTransform;

        if (contentRect == null)
            return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

        for (int i = 0; i < contentParent.childCount; i++)
        {
            RectTransform child =
                contentParent.GetChild(i) as RectTransform;

            if (child == null)
                continue;

            child.localScale = Vector3.one;
        }
    }

    private void HandleDirectItemTap()
    {
        if (listPanel == null || !listPanel.activeInHierarchy)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            pointerDownPosition = Input.mousePosition;
            pointerStartedOnList = IsScreenPointInsideList(pointerDownPosition);
            return;
        }

        if (!Input.GetMouseButtonUp(0) || !pointerStartedOnList)
            return;

        Vector2 pointerUpPosition = Input.mousePosition;

        if (Vector2.Distance(pointerDownPosition, pointerUpPosition) > 25f)
            return;

        TrySelectMemoryAt(pointerUpPosition);
    }

    private bool IsScreenPointInsideList(Vector2 screenPoint)
    {
        if (listPanel == null)
            return false;

        RectTransform listRect =
            listPanel.GetComponent<RectTransform>();

        if (listRect == null)
            return false;

        return RectTransformUtility.RectangleContainsScreenPoint(
            listRect,
            screenPoint,
            GetCanvasCamera());
    }

    private void TrySelectMemoryAt(Vector2 screenPoint)
    {
        Camera canvasCamera = GetCanvasCamera();

        for (int i = 0; i < spawnedItems.Count; i++)
        {
            SpawnedMemoryItem spawnedItem = spawnedItems[i];

            if (spawnedItem.rectTransform == null || spawnedItem.memory == null)
                continue;

            if (!spawnedItem.rectTransform.gameObject.activeInHierarchy)
                continue;

            if (!RectTransformUtility.RectangleContainsScreenPoint(
                spawnedItem.rectTransform,
                screenPoint,
                canvasCamera))
                continue;

            OnMemorySelected(spawnedItem.memory);
            return;
        }
    }

    private Camera GetCanvasCamera()
    {
        if (parentCanvas == null)
            parentCanvas = GetComponentInParent<Canvas>();

        if (parentCanvas == null)
            return null;

        if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return parentCanvas.worldCamera;
    }

    private struct SpawnedMemoryItem
    {
        public readonly RectTransform rectTransform;
        public readonly MemoryData memory;

        public SpawnedMemoryItem(RectTransform rectTransform, MemoryData memory)
        {
            this.rectTransform = rectTransform;
            this.memory = memory;
        }
    }
}


