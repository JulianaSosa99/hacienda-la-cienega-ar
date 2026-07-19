using UnityEngine;

public class Tile : MonoBehaviour
{
    public int number; // identificador de la pieza

    public SlidingPuzzleController controller;

    public void OnClick()
    {
        controller.TryMoveTile(this);
    }
}