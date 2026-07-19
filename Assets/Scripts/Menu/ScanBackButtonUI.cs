using UnityEngine;
using UnityEngine.UI;

public class ScanBackButtonUI : MonoBehaviour
{
    public static ScanBackButtonUI Instance;

    [Header("References")]
    [SerializeField] private Button button;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private AppBackNavigation backNavigation;

    [Header("State")]
    [SerializeField] private bool startHidden = true;

    private void Awake()
    {
        Instance = this;
        ResolveReferences();
        WireButton();

        if (startHidden)
            SetVisible(false);
    }

    private void OnEnable()
    {
        Instance = this;
        ResolveReferences();
        WireButton();
    }

    public void SetVisible(bool visible)
    {
        ResolveReferences();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        if (button != null)
            button.interactable = visible;
    }

    public static void ShowButton()
    {
        if (Instance != null)
            Instance.SetVisible(true);
    }

    public static void HideButton()
    {
        if (Instance != null)
            Instance.SetVisible(false);
    }

    private void ResolveReferences()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (backNavigation == null)
            backNavigation = FindObjectOfType<AppBackNavigation>(true);
    }

    private void WireButton()
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(GoBack);
        button.onClick.AddListener(GoBack);
    }

    private void GoBack()
    {
        if (backNavigation != null)
            backNavigation.HandleBack();
    }
}