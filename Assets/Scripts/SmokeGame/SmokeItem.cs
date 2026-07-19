using UnityEngine;
using UnityEngine.UI;

public class SmokeItem : MonoBehaviour
{
    public bool esCorrecto;
    public bool yaEncontrado = false;

    private Image image;

    void Start()
    {
        image = GetComponent<Image>();
    }

    public void CheckClick(Vector2 screenPos)
    {
        if (yaEncontrado) return;

        RectTransform rt = GetComponent<RectTransform>();

        if (!RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, Camera.main))
            return;

        Debug.Log("INTENTO CLICK");

        if (SmokeGameController.Instance.smokeWipe != null &&
            SmokeGameController.Instance.smokeWipe.EstaCubiertoPorHumoEnRect(rt))
        {
            Debug.Log("BLOQUEADO POR HUMO");
            return;
        }

        Debug.Log("CLICK VÁLIDO");

        SmokeGameController.Instance.OnItemClicked(this);
    }

    
}