using UnityEngine;
using UnityEngine.UI;

public static class UIRaycastUtility
{
    public static void EnableButtons(GameObject root)
    {
        if (root == null)
            return;

        Button[] buttons =
            root.GetComponentsInChildren<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];

            button.interactable = true;

            if (button.targetGraphic != null)
                button.targetGraphic.raycastTarget = true;
        }
    }
}
