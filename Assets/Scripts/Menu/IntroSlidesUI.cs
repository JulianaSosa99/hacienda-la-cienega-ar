using System.Collections;
using Firebase.Auth;
using UnityEngine;
using UnityEngine.UI;

public class IntroSlidesUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject introSlidesPanel;

    [Header("Imagen de fondo")]
    public UnityEngine.UI.Image slideImage;

    [Header("Boton")]
    public Button nextButton;
    public UnityEngine.UI.Image nextButtonImage;

    [Header("Slides")]
    public Sprite[] slides;

    [Header("Imagenes del boton")]
    public Sprite startButtonSprite;
    public Sprite nextButtonSprite;

    [Header("Comportamiento")]
    public bool showOnlyOnce = true;
    public bool showIntroWhenUserHasNoProgress = true;
    public bool bringIntroToFront = true;

    [Header("Al terminar")]
    public GameObject memoriesPanel;
    public MemoryListUI memoriesUI;
    public MemoryGuidePopupUI memoryGuidePopup;

    private const string IntroSeenKeyBase = "intro_slides_seen";
    private int currentSlideIndex;

    private string IntroSeenKey
    {
        get
        {
            FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;

            if (user == null || string.IsNullOrEmpty(user.UserId))
                return IntroSeenKeyBase;

            return IntroSeenKeyBase + "_" + user.UserId;
        }
    }

    private void Awake()
    {
        if (introSlidesPanel == null)
            introSlidesPanel = gameObject;

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(ShowNextSlide);
            nextButton.onClick.AddListener(ShowNextSlide);
        }
    }

    private void OnEnable()
    {
        StopAllCoroutines();
        StartCoroutine(BeginIntroFlow());
    }

    private IEnumerator BeginIntroFlow()
    {
        HideScannerAndProgressUI();
        SetMemoriesVisible(false);

        if (memoryGuidePopup != null)
            memoryGuidePopup.HidePopupWithoutSaving();

        yield return null;

        HideScannerAndProgressUI();
        SetMemoriesVisible(false);

        if (!ShouldShowIntro())
        {
            if (introSlidesPanel != null)
                introSlidesPanel.SetActive(false);

            OpenMemories();
            yield break;
        }

        ShowIntro();
    }

    private bool ShouldShowIntro()
    {
        bool alreadySeen = PlayerPrefs.GetInt(IntroSeenKey, 0) == 1;

        if (showOnlyOnce && alreadySeen)
            return false;

        if (showIntroWhenUserHasNoProgress && !UserHasExplorationProgress())
            return true;

        return !showOnlyOnce;
    }

    private bool UserHasExplorationProgress()
    {
        if (ExplorationManager.Instance == null)
            return false;

        ExplorationManager.Instance.LoadForCurrentUser();
        return ExplorationManager.Instance.GetExplorationPercent() > 0;
    }

    private void ShowIntro()
    {
        currentSlideIndex = 0;

        if (introSlidesPanel != null)
        {
            introSlidesPanel.SetActive(true);

            if (bringIntroToFront)
                introSlidesPanel.transform.SetAsLastSibling();
        }

        HideScannerAndProgressUI();
        SetMemoriesVisible(false);
        ShowCurrentSlide();
    }

    public void ShowNextSlide()
    {
        currentSlideIndex++;

        if (slides == null || currentSlideIndex >= slides.Length)
        {
            FinishIntro(true);
            return;
        }

        ShowCurrentSlide();
    }

    private void ShowCurrentSlide()
    {
        if (slideImage != null && slides != null && slides.Length > 0)
            slideImage.sprite = slides[Mathf.Clamp(currentSlideIndex, 0, slides.Length - 1)];

        if (nextButtonImage != null)
            nextButtonImage.sprite = currentSlideIndex == 0 ? startButtonSprite : nextButtonSprite;
    }

    private void FinishIntro(bool markAsSeen)
    {
        if (markAsSeen)
        {
            PlayerPrefs.SetInt(IntroSeenKey, 1);
            PlayerPrefs.Save();
        }

        if (introSlidesPanel != null)
            introSlidesPanel.SetActive(false);

        HideScannerAndProgressUI();

        if (memoryGuidePopup != null)
        {
            SetMemoriesVisible(true);

            if (memoriesUI != null)
                memoriesUI.ShowList();

            memoryGuidePopup.ShowPopupForIntro(OpenMemories);
            return;
        }

        OpenMemories();
    }

    private void OpenMemories()
    {
        HideScannerAndProgressUI();
        SetMemoriesVisible(true);

        if (memoriesUI != null)
            memoriesUI.OpenMemories();
    }

    private void SetMemoriesVisible(bool visible)
    {
        if (memoriesPanel != null)
        {
            memoriesPanel.SetActive(visible);
            return;
        }

        if (memoriesUI != null)
            memoriesUI.gameObject.SetActive(visible);
    }

    private void HideScannerAndProgressUI()
    {
        NarrativeFlowUI.HideDiscoveryPrompts();
        NarrativeFlowUI.HideProgress();

        CameraMemoryButtonUI.HideButton();
        ScanBackButtonUI.HideButton();

        if (ExplorationProgressBarUI.Instance != null)
            ExplorationProgressBarUI.Instance.Hide();
    }
}
