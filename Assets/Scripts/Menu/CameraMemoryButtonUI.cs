using UnityEngine;
using UnityEngine.UI;

public class CameraMemoryButtonUI : MonoBehaviour
{
    public static CameraMemoryButtonUI Instance;

    [Header("References")]
    [SerializeField] private MemoryListUI memoryListUI;
    [SerializeField] private Button button;
    [SerializeField] private CanvasGroup canvasGroup;

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

    public void OpenMemories()
    {
        ResolveReferences();
        SetVisible(false);

        if (memoryListUI != null)
        {
            memoryListUI.OpenMemories();
        }
        else
        {
            Debug.LogWarning("CameraMemoryButtonUI necesita una referencia a MemoryListUI.");
        }
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

        if (memoryListUI == null)
            memoryListUI = FindObjectOfType<MemoryListUI>(true);
    }

    private void WireButton()
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(OpenMemories);
        button.onClick.AddListener(OpenMemories);
    }
}
