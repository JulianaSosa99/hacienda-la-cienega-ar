using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MemoryListItemUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Texto")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text statusText;

    [Header("Imagen")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private Image stateIconImage;
    [SerializeField] private Sprite lockedIcon;
    [SerializeField] private Sprite completedIcon;

    [Header("Estado visual")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float pendingAlpha = 0.55f;

    [Header("Accion")]
    [SerializeField] private Button button;

    [Header("Area de toque")]
    [SerializeField] private Image touchAreaImage;

    private MemoryData memory;
    private Action<MemoryData> onSelected;

    public void Setup(MemoryData memoryData, Action<MemoryData> selectedCallback)
    {
        memory = memoryData;
        onSelected = selectedCallback;

        ResolveReferences();

        bool completed = memory != null && memory.isUnlocked && memory.minigameCompleted;

        if (nameText != null)
            nameText.text = string.IsNullOrEmpty(memory.title) ? memory.guardianName : memory.title;

        if (statusText != null)
            statusText.text = completed ? "Tarea completa" : "Por descubrir";

        if (portraitImage != null)
        {
            Sprite sprite = completed ? memory.portrait : memory.lockedPortrait;

            if (sprite == null)
                sprite = memory.portrait;

            if (sprite == null)
                sprite = memory.scanSilhouette;

            portraitImage.sprite = sprite;
            portraitImage.enabled = sprite != null;
        }

        if (stateIconImage != null)
        {
            stateIconImage.sprite = completed ? completedIcon : lockedIcon;
            stateIconImage.enabled = stateIconImage.sprite != null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = completed ? 1f : pendingAlpha;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        if (button != null)
        {
            button.onClick.RemoveListener(Select);
            button.onClick.AddListener(Select);
            button.interactable = true;
        }

        ConfigureTouchArea();
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void ResolveReferences()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (portraitImage == null)
            portraitImage = FindImageByName("PortraitImage");

        if (stateIconImage == null)
            stateIconImage = FindImageByName("StateIconImage");

        if (touchAreaImage == null)
            touchAreaImage = FindImageByName("TouchAreaImage");

        if (touchAreaImage == null)
            touchAreaImage = GetComponent<Image>();
    }

    private void ConfigureTouchArea()
    {
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);

        for (int i = 0; i < graphics.Length; i++)
            graphics[i].raycastTarget = false;

        if (touchAreaImage != null)
            touchAreaImage.raycastTarget = true;

        if (button != null && touchAreaImage != null)
            button.targetGraphic = touchAreaImage;
    }

    private Image FindImageByName(string targetName)
    {
        Image[] images = GetComponentsInChildren<Image>(true);

        for (int i = 0; i < images.Length; i++)
        {
            if (images[i].name == targetName)
                return images[i];
        }

        return null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Select();
    }

    private void Select()
    {
        if (memory != null && onSelected != null)
            onSelected.Invoke(memory);
    }
}
