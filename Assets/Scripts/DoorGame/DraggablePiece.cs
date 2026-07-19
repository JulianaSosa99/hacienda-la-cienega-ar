using UnityEngine;
using UnityEngine.EventSystems;

public class DraggablePiece :
    MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("Target")]
    [SerializeField] private DoorSlot correctSlot;

    [Header("Settings")]
    [SerializeField] private float snapDistance = 50f;

    private RectTransform rectTransform;
    private Canvas canvas;

    private Vector2 startPosition;

    private bool placed;

    private DoorGameController gameController;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        canvas = GetComponentInParent<Canvas>();

        gameController = FindObjectOfType<DoorGameController>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (placed)
            return;

        transform.SetAsLastSibling();

        startPosition = rectTransform.anchoredPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (placed)
            return;

        rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (placed)
            return;

        float distance = Vector2.Distance(
            rectTransform.position,
            correctSlot.RectTransform.position);

        if (distance <= snapDistance)
        {
            SnapToSlot();
        }
        else
        {
            ReturnToStart();
        }
    }

    private void SnapToSlot()
    {
        rectTransform.SetParent(correctSlot.transform);

        rectTransform.anchoredPosition = Vector2.zero;

        placed = true;

        gameController.RegisterPiecePlaced();
    }

    private void ReturnToStart()
    {
        rectTransform.anchoredPosition = startPosition;

        gameController.PlayWrong();
    }
}