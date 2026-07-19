using UnityEngine;
using TMPro;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine.SceneManagement;
using System.Collections;

public class RegisterController : MonoBehaviour
{
    [Header("Inputs")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_InputField confirmPasswordInput;

    [Header("UI")]
    public TextMeshProUGUI messageText;

    [Header("Navigation")]
    public AuthUIManager authUIManager;

    [Header("Scene")]
    public string nextScene = "ARScene";

    private bool isLoading = false;

    public void RegisterUser()
    {
        if (isLoading)
            return;

        string email = emailInput.text.Trim();
        string password = passwordInput.text.Trim();
        string confirmPassword = confirmPasswordInput.text.Trim();

        if (string.IsNullOrEmpty(email) ||
            string.IsNullOrEmpty(password) ||
            string.IsNullOrEmpty(confirmPassword))
        {
            messageText.text = "Complete todos los campos";
            return;
        }

        if (password != confirmPassword)
        {
            messageText.text = "Las contraseñas no coinciden";
            return;
        }

        if (password.Length < 6)
        {
            messageText.text = "La contraseña debe tener al menos 6 caracteres";
            return;
        }

        if (IsOffline())
        {
            messageText.text = "Sin conexión a internet";
            return;
        }

        if (FirebaseManager.Auth == null)
        {
            messageText.text = "Firebase aún se esta inicializando";
            return;
        }

        isLoading = true;
        SetLoading(true, "Validando correo...");

        FirebaseManager.Auth
            .FetchProvidersForEmailAsync(email)
            .ContinueWithOnMainThread(providerTask =>
            {
                if (providerTask.IsCanceled)
                {
                    FinishLoading("Registro cancelado");
                    return;
                }

                if (providerTask.IsFaulted)
                {
                    FinishLoading(IsOffline()
                        ? "Sin conexión a internet"
                        : FirebaseAuthErrorMessages.ForRegister(providerTask.Exception));
                    return;
                }

                if (HasRegisteredProvider(providerTask.Result))
                {
                    FinishLoading(GetExistingAccountMessage(providerTask.Result));
                    return;
                }

                CreateAccount(email, password);
            });
    }

    public void ClearForm()
    {
        if (emailInput != null)
            emailInput.text = string.Empty;

        if (passwordInput != null)
            passwordInput.text = string.Empty;

        if (confirmPasswordInput != null)
            confirmPasswordInput.text = string.Empty;

        if (messageText != null)
            messageText.text = string.Empty;

        isLoading = false;
    }

    private void CreateAccount(string email, string password)
    {
        SetLoading(true, "Creando cuenta...");

        FirebaseManager.Auth
            .CreateUserWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled)
                {
                    FinishLoading("Registro cancelado");
                    return;
                }

                if (task.IsFaulted)
                {
                    FinishLoading(IsOffline()
                        ? "Sin conexión a internet"
                        : FirebaseAuthErrorMessages.ForRegister(task.Exception));
                    return;
                }

                FirebaseUser user = task.Result.User;
                FirebaseManager.User = user;

                messageText.text = "Cuenta creada correctamente";
                isLoading = false;

                ClearInputs();
                InitializeNewUserProgress(user);
                StartCoroutine(LoadSceneDelayed());
            });
    }

    private void ClearInputs()
    {
        emailInput.text = string.Empty;
        passwordInput.text = string.Empty;
        confirmPasswordInput.text = string.Empty;
    }

    private bool HasRegisteredProvider(System.Collections.Generic.IEnumerable<string> providers)
    {
        if (providers == null)
            return false;

        foreach (string provider in providers)
        {
            if (!string.IsNullOrEmpty(provider))
                return true;
        }

        return false;
    }

    private string GetExistingAccountMessage(System.Collections.Generic.IEnumerable<string> providers)
    {
        foreach (string provider in providers)
        {
            if (provider == "google.com")
                return "Este correo ya está registrado con Google";
        }

        return "Este correo ya tiene una cuenta registrada";
    }

    private void SetLoading(bool loading, string message)
    {
        if (!string.IsNullOrEmpty(message))
            messageText.text = message;

        if (loading && LoadingUI.Instance != null)
            LoadingUI.Instance.Show();
    }

    private void FinishLoading(string message)
    {
        isLoading = false;
        messageText.text = message;
        HideLoading();
    }

    private void HideLoading()
    {
        if (LoadingUI.Instance != null)
            LoadingUI.Instance.Hide();
    }

    private bool IsOffline()
    {
        return Application.internetReachability == NetworkReachability.NotReachable;
    }

    private IEnumerator LoadSceneDelayed()
    {
        yield return new WaitForSeconds(0.7f);

        HideLoading();
        SceneManager.LoadScene(nextScene);
    }

    private void InitializeNewUserProgress(FirebaseUser user)
    {
        if (ExplorationManager.Instance == null || user == null)
            return;

        ExplorationManager.Instance.InitializeProgressForNewUser(user.UserId);
    }
}
