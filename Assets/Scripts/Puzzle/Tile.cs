using UnityEngine;
using UnityEngine.EventSystems;

public class Tile : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public int number;
    public SlidingPuzzleController controller;

    [SerializeField, Min(1f)] private float minimumSwipeDistance = 30f;
    private Vector2 pointerStart;
    private int? activePointer;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || activePointer.HasValue)
            return;

        activePointer = eventData.pointerId;
        pointerStart = eventData.position;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (activePointer != eventData.pointerId) return;
        activePointer = null;
        if (controller == null) return;

        Vector2 delta = eventData.position - pointerStart;
        Canvas canvas = GetComponentInParent<Canvas>();
        float scale = canvas != null ? canvas.rootCanvas.scaleFactor : 1f;
        float threshold = minimumSwipeDistance * scale;
        if (delta.sqrMagnitude < threshold * threshold)
        {
            controller.PlayInvalidMoveSound();
            return;
        }

        // Resolve the gesture to one axis; a gesture never swaps diagonal tiles.
        Vector2Int direction = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
            ? new Vector2Int(delta.x > 0 ? 1 : -1, 0)
            : new Vector2Int(0, delta.y > 0 ? -1 : 1);
        controller.TryMoveTile(this, direction);
    }

    // Capture drag events so parent scroll handlers do not consume the gesture.
    public void OnDrag(PointerEventData eventData) { }

    // Existing prefab Button events still reference this method.
    // PointerUp handles both taps and swipes, avoiding duplicate audio.
    public void OnClick() { }

    private void OnDisable()
    {
        activePointer = null;
    }
    private void OnApplicationPause(bool paused)
    {
        if (paused) activePointer = null;
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused) activePointer = null;
    }
}
