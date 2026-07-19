using TMPro;
using UnityEngine;

public class UIContador : MonoBehaviour
{
    public TextMeshProUGUI texto;

    public void Actualizar(int actuales, int total)
    {
        texto.text = actuales + " / " + total;
    }
}