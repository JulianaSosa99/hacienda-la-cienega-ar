using System.Collections;
using UnityEngine;

public class AuthUIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject loginPanel;
    public GameObject registerPanel;
    public GameObject forgotPasswordPanel;

    [Header("Controllers")]
    public LoginController loginController;
    public RegisterController registerController;
    public ForgotPasswordController forgotPasswordController;

    private void Start()
    {
        CacheControllers();
        StartCoroutine(ShowInitialPanel());
    }

    public void ShowLoadingOnly()
    {
        ShowOnly(null);

        if (LoadingUI.Instance != null)
            LoadingUI.Instance.Show();
    }

    private IEnumerator ShowInitialPanel()
    {
        ShowLoadingOnly();

        while (!FirebaseManager.IsInitialized)
            yield return null;

        if (FirebaseManager.Auth != null && FirebaseManager.Auth.CurrentUser != null)
            yield break;

        ShowLogin();
    }

    public void ShowLogin()
    {
        ResetForms();
        ShowOnly(loginPanel);
    }

    public void ShowRegister()
    {
        ResetForms();
        ShowOnly(registerPanel);
    }

    public void ShowForgotPassword()
    {
        ResetForms();
        ShowOnly(forgotPasswordPanel);
    }

    private void CacheControllers()
    {
        if (loginController == null)
            loginController = GetComponent<LoginController>();

        if (registerController == null)
            registerController = GetComponent<RegisterController>();

        if (forgotPasswordController == null)
            forgotPasswordController = GetComponent<ForgotPasswordController>();
    }

    private void ResetForms()
    {
        CacheControllers();

        if (loginController != null)
            loginController.ClearForm();

        if (registerController != null)
            registerController.ClearForm();

        if (forgotPasswordController != null)
            forgotPasswordController.ClearForm();

        if (LoadingUI.Instance != null)
            LoadingUI.Instance.Hide();
    }

    private void ShowOnly(GameObject panelToShow)
    {
        if (loginPanel != null)
            loginPanel.SetActive(panelToShow == loginPanel);

        if (registerPanel != null)
            registerPanel.SetActive(panelToShow == registerPanel);

        if (forgotPasswordPanel != null)
            forgotPasswordPanel.SetActive(panelToShow == forgotPasswordPanel);
    }
}
