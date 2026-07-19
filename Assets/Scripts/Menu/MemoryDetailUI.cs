using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MemoryDetailUI : MonoBehaviour
{
    [Header("Imagenes")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private Image scanSilhouetteImage;
    [SerializeField] private Image rewardImage;

    [Header("Checks")]
    [SerializeField] private Image guardianFoundCheckImage;
    [SerializeField] private Image minigameCompletedCheckImage;

    [Header("Texto")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text minigameStatusText;
    [SerializeField] private TMP_Text rewardStatusText;
    [SerializeField] private TMP_Text scanHintText;

    [Header("Acciones")]
    [SerializeField] private Button scanAgainButton;
    [SerializeField] private TMP_Text scanAgainButtonText;
    [SerializeField] private string unlockedScanText = "Ver experiencia";
    [SerializeField] private string lockedScanText = "Escanear";

    private CanvasGroup canvasGroup;
    private MemoryListUI memoryListUI;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        ResolveActionReferences();
        WireActions();
    }

    private void OnEnable()
    {
        ResolveActionReferences();
        WireActions();
    }

    public void Show(MemoryData memory)
    {
        if (memory == null)
            return;

        transform.SetAsLastSibling();
        transform.localScale = Vector3.one;
        gameObject.SetActive(true);
        SetVisible(true);

        if (memory.isUnlocked)
            ShowUnlocked(memory);
        else
            ShowLocked(memory);

        ConfigureScanAgainButton(memory);
    }

    private void ShowUnlocked(MemoryData memory)
    {
        SetImage(portraitImage, memory.portrait);
        SetImage(scanSilhouetteImage, memory.minigameCompleted ? null : memory.scanSilhouette);
        SetImage(rewardImage, memory.rewardEarned ? memory.rewardSprite : null);

        if (nameText != null)
            nameText.text = memory.guardianName;

        if (descriptionText != null)
            descriptionText.text = GetText(memory.guardianFoundText, "GUARDIAN ENCONTRADO");

        if (minigameStatusText != null)
        {
            minigameStatusText.text = memory.minigameCompleted
                ? GetText(memory.minigameCompletedText, "MINIJUEGO REALIZADO")
                : GetText(memory.minigamePendingText, "MINIJUEGO NO REALIZADO");
        }

        if (rewardStatusText != null)
            rewardStatusText.text = memory.rewardEarned ? GetText(memory.rewardTitleText, "RECOMPENSA") : string.Empty;

        if (scanHintText != null)
            scanHintText.text = memory.minigameCompleted ? string.Empty : memory.scanHint;

        SetCheck(guardianFoundCheckImage, true);
        SetCheck(minigameCompletedCheckImage, memory.minigameCompleted);
    }

    private void ShowLocked(MemoryData memory)
    {
        Sprite lockedPortrait = memory.lockedPortrait;

        if (lockedPortrait == null)
            lockedPortrait = memory.portrait;

        SetImage(portraitImage, lockedPortrait);
        SetImage(scanSilhouetteImage, memory.scanSilhouette);
        SetImage(rewardImage, null);

        if (nameText != null)
            nameText.text = memory.guardianName;

        if (descriptionText != null)
            descriptionText.text = GetText(memory.guardianMissingText, "GUARDIAN NO ENCONTRADO");

        if (minigameStatusText != null)
            minigameStatusText.text = GetText(memory.minigamePendingText, "MINIJUEGO NO REALIZADO");

        if (rewardStatusText != null)
            rewardStatusText.text = string.Empty;

        if (scanHintText != null)
            scanHintText.text = memory.scanHint;

        SetCheck(guardianFoundCheckImage, false);
        SetCheck(minigameCompletedCheckImage, false);
    }

    private void ConfigureScanAgainButton(MemoryData memory)
    {
        if (scanAgainButton == null || memory == null)
            return;

        scanAgainButton.gameObject.SetActive(true);

        if (scanAgainButtonText != null)
            scanAgainButtonText.text = memory.isUnlocked ? unlockedScanText : lockedScanText;
    }

    private void ResolveActionReferences()
    {
        if (memoryListUI == null)
            memoryListUI = GetComponentInParent<MemoryListUI>(true);

        if (scanAgainButton != null && scanAgainButtonText == null)
            scanAgainButtonText = scanAgainButton.GetComponentInChildren<TMP_Text>(true);
    }

    private void WireActions()
    {
        if (scanAgainButton == null)
            return;

        scanAgainButton.onClick.RemoveListener(OpenScanner);
        scanAgainButton.onClick.AddListener(OpenScanner);
    }

    private void OpenScanner()
    {
        if (memoryListUI != null)
            memoryListUI.OpenScanner();
    }

    private string GetText(string configuredText, string fallbackText)
    {
        return string.IsNullOrWhiteSpace(configuredText) ? fallbackText : configuredText;
    }

    private void SetCheck(Image image, bool visible)
    {
        if (image == null)
            return;

        image.enabled = visible;
    }

    private void SetImage(Image image, Sprite sprite)
    {
        if (image == null)
            return;

        image.sprite = sprite;
        image.enabled = sprite != null;
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }
}