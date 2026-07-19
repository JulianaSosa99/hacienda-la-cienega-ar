using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class Dial : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    private RectTransform rectTransform;

    [SerializeField] private float dragThreshold = 60f;

    private float lastDragX;

    private int currentNumber = 0;

    private bool isLocked = false;

    public int CurrentNumber => currentNumber;

    public event Action<int> OnNumberChanged;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        UpdateRotation();
    }

    public void LockDial()
    {
        isLocked = true;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isLocked)
            return;

        lastDragX = eventData.position.x;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isLocked)
            return;

        float deltaX = eventData.position.x - lastDragX;

        if (Mathf.Abs(deltaX) < dragThreshold)
            return;

        if (deltaX > 0)
        {
           PreviousNumber();
        }
        else
        {
            NextNumber();
        }

        lastDragX = eventData.position.x;
    }

    private void NextNumber()
    {
        currentNumber++;

        if (currentNumber > 9)
            currentNumber = 0;

        UpdateRotation();

        OnNumberChanged?.Invoke(currentNumber);

        

        Debug.Log("CurrentNumber: " + currentNumber);
    }

    private void PreviousNumber()
    {
        currentNumber--;

        if (currentNumber < 0)
            currentNumber = 9;

        UpdateRotation();

        OnNumberChanged?.Invoke(currentNumber);

        

        Debug.Log("CurrentNumber: " + currentNumber);
    }

    private void UpdateRotation()
    {
        float angle = -currentNumber * 36f;

        rectTransform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}