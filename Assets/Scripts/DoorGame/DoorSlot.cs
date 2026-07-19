using UnityEngine;

public class DoorSlot : MonoBehaviour
{
    public RectTransform RectTransform { get; private set; }

    private void Awake()
    {
        RectTransform = GetComponent<RectTransform>();
    }
}