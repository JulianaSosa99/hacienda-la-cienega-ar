using System;
using Firebase.Auth;
using UnityEngine;
using UnityEngine.UI;

public class MemoryGuidePopupUI : MonoBehaviour
{
    [Header("Popup")]
    public GameObject popupPanel;
    public UnityEngine.UI.Image dimBackground;

    [Header("Boton")]
    public Button continueButton;

    [Header("Comportamiento")]
    public bool showOnlyOnce = true;
    public bool showOnEnable = false;
    public bool createDimBackgroundIfMissing = true;
    public Color defaultDimColor = new Color(0f, 0f, 0f, 0.55f);

    private const string PopupSeenKey = "memory_guide_popup_seen";

    private bool showingByRequest;
    private Action onIntroPopupClosed;

    private string ScopedPopupSeenKey
    {
        get
        {
            FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;

            if (user == null || string.IsNullOrEmpty(user.UserId))
                return PopupSeenKey;

            return PopupSeenKey + "_" + user.UserId;
        }
    }

    private void Awake()
    {
        ResolveReferences();
        EnsureRaycastBlocker();
        WireButton();
    }

    private void OnEnable()
    {
        ResolveReferences();
        EnsureRaycastBlocker();
        WireButton();

        if (showOnEnable)
        {
            TryShowPopup();
            return;
        }

        if (!showingByRequest)
            HidePopupWithoutSaving();
    }

    public void TryShowPopup()
    {
        ResolveReferences();
        EnsureRaycastBlocker();
        WireButton();

        bool alreadySeen = PlayerPrefs.GetInt(ScopedPopupSeenKey, 0) == 1;

        if (showOnlyOnce && alreadySeen)
        {
            HidePopupWithoutSaving();
            return;
        }

        ShowPopup();
    }

    public void ShowPopup()
    {
        showingByRequest = true;
        onIntroPopupClosed = null;

        ResolveReferences();
        EnsureRaycastBlocker();
        WireButton();

        if (popupPanel != null)
        {
            popupPanel.SetActive(true);
            popupPanel.transform.SetAsLastSibling();
        }
    }

    public void ShowPopupForIntro(Action onClosed)
    {
        showingByRequest = true;
        onIntroPopupClosed = onClosed;

        ResolveReferences();
        EnsureRaycastBlocker();
        WireButton();

        if (popupPanel != null)
        {
            popupPanel.SetActive(true);
            popupPanel.transform.SetAsLastSibling();
        }
    }

    public void HidePopup()
    {
        HidePopup(true);
    }

    public void HidePopupWithoutSaving()
    {
        showingByRequest = false;
        onIntroPopupClosed = null;

        if (popupPanel != null)
            popupPanel.SetActive(false);
    }

    public void ResetPopupSeen()
    {
        PlayerPrefs.DeleteKey(ScopedPopupSeenKey);
        PlayerPrefs.Save();
    }

    private void HidePopup(bool markAsSeen)
    {
        if (markAsSeen)
        {
            PlayerPrefs.SetInt(ScopedPopupSeenKey, 1);
            PlayerPrefs.Save();
        }

        showingByRequest = false;

        if (popupPanel != null)
            popupPanel.SetActive(false);

        Action callback = onIntroPopupClosed;
        onIntroPopupClosed = null;

        if (callback != null)
            callback.Invoke();
    }

    private void ResolveReferences()
    {
        if (popupPanel == null)
            popupPanel = gameObject;

        if (continueButton == null && popupPanel != null)
            continueButton = popupPanel.GetComponentInChildren<Button>(true);

        if (dimBackground == null && popupPanel != null)
        {
            Transform dimTransform = popupPanel.transform.Find("DimBackground");

            if (dimTransform != null)
                dimBackground = dimTransform.GetComponent<UnityEngine.UI.Image>();
        }
    }

    private void WireButton()
    {
        if (continueButton == null)
            return;

        continueButton.onClick.RemoveListener(HidePopup);
        continueButton.onClick.AddListener(HidePopup);
    }

    private void EnsureRaycastBlocker()
    {
        if (popupPanel == null)
            return;

        CanvasGroup canvasGroup = popupPanel.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = popupPanel.AddComponent<CanvasGroup>();

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        if (dimBackground == null && createDimBackgroundIfMissing)
            dimBackground = CreateDimBackground();

        if (dimBackground == null)
            return;

        dimBackground.raycastTarget = true;

        if (dimBackground.color.a <= 0.01f)
            dimBackground.color = defaultDimColor;

        dimBackground.transform.SetAsFirstSibling();
    }

    private UnityEngine.UI.Image CreateDimBackground()
    {
        GameObject dimObject = new GameObject(
            "DimBackground",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(UnityEngine.UI.Image)
        );

        dimObject.layer = popupPanel.layer;
        dimObject.transform.SetParent(popupPanel.transform, false);

        RectTransform rectTransform = dimObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;

        UnityEngine.UI.Image image = dimObject.GetComponent<UnityEngine.UI.Image>();
        image.color = defaultDimColor;
        image.raycastTarget = true;

        return image;
    }
}
