using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PasswordVisibilityToggle : MonoBehaviour
{
    public TMP_InputField passwordInput;

    public Image eyeIcon;

    public Sprite visibleIcon;
    public Sprite hiddenIcon;

    private bool isVisible = false;

    private void Start()
    {
        passwordInput.contentType =
            TMP_InputField.ContentType.Password;

        passwordInput.ForceLabelUpdate();
    }

    public void TogglePasswordVisibility()
    {
        isVisible = !isVisible;

        if (isVisible)
        {
            passwordInput.contentType =
                TMP_InputField.ContentType.Standard;

            eyeIcon.sprite = visibleIcon;
        }
        else
        {
            passwordInput.contentType =
                TMP_InputField.ContentType.Password;

            eyeIcon.sprite = hiddenIcon;
        }

        passwordInput.ForceLabelUpdate();
    }
}