using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonForceTest : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("POINTER CLICK DETECTADO");
    }
}