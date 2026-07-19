using UnityEngine;
using TMPro;
using Firebase.Extensions;

public class ForgotPasswordController : MonoBehaviour
{
    public TMP_InputField emailInput;
    public TextMeshProUGUI messageText;

    private bool isLoading = false;

    public void ResetPassword()
    {
        if (isLoading)
            return;

        string email = emailInput.text.Trim();

        if (string.IsNullOrEmpty(email))
        {
            messageText.text = "Ingrese su correo";
            return;
        }

        if (FirebaseManager.Auth == null)
        {
            messageText.text = "Firebase aun se esta inicializando";
            return;
        }

        isLoading = true;
        messageText.text = "Validando correo...";
        ShowLoading();

        FirebaseManager.Auth
            .FetchProvidersForEmailAsync(email)
            .ContinueWithOnMainThread(providerTask =>
            {
                if (providerTask.IsCanceled)
                {
                    FinishLoading("Recuperacion cancelada");
                    return;
                }

                if (providerTask.IsFaulted)
                {
                    FinishLoading(FirebaseAuthErrorMessages.ForPasswordReset(providerTask.Exception));
                    return;
                }

                DebugProviders(providerTask.Result);

                if (HasGoogleProvider(providerTask.Result) && !HasPasswordProvider(providerTask.Result))
                {
                    FinishLoading("Esta cuenta fue creada con Google. Ingresa con Google");
                    return;
                }

                if (!HasPasswordProvider(providerTask.Result))
                {
                    FinishLoading("No existe una cuenta con contrasena para este correo. Si usaste Google, ingresa con Google");
                    return;
                }

                SendPasswordResetEmail(email);
            });
    }

    public void ClearForm()
    {
        if (emailInput != null)
            emailInput.text = string.Empty;

        if (messageText != null)
            messageText.text = string.Empty;

        isLoading = false;
    }

    private void SendPasswordResetEmail(string email)
    {
        messageText.text = "Enviando correo...";

        FirebaseManager.Auth
            .SendPasswordResetEmailAsync(email)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled)
                {
                    FinishLoading("Recuperacion cancelada");
                    return;
                }

                if (task.IsFaulted)
                {
                    FinishLoading(FirebaseAuthErrorMessages.ForPasswordReset(task.Exception));
                    return;
                }

                FinishLoading("Correo de recuperacion enviado");
            });
    }

    private bool HasPasswordProvider(System.Collections.Generic.IEnumerable<string> providers)
    {
        if (providers == null)
            return false;

        foreach (string provider in providers)
        {
            if (provider == "password")
                return true;
        }

        return false;
    }

    private bool HasGoogleProvider(System.Collections.Generic.IEnumerable<string> providers)
    {
        if (providers == null)
            return false;

        foreach (string provider in providers)
        {
            if (provider == "google.com")
                return true;
        }

        return false;
    }

    private void DebugProviders(System.Collections.Generic.IEnumerable<string> providers)
    {
        if (providers == null)
        {
            Debug.Log("Password reset providers: none");
            return;
        }

        foreach (string provider in providers)
            Debug.Log("Password reset provider: " + provider);
    }

    private void FinishLoading(string message)
    {
        isLoading = false;
        messageText.text = message;
        HideLoading();
    }

    private void ShowLoading()
    {
        if (LoadingUI.Instance != null)
            LoadingUI.Instance.Show();
    }

    private void HideLoading()
    {
        if (LoadingUI.Instance != null)
            LoadingUI.Instance.Hide();
    }
}