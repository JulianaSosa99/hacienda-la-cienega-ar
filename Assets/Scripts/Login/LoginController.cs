using Firebase.Auth;
using Firebase.Extensions;
using Google;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginController : MonoBehaviour
{
    private const string DefaultGoogleWebClientId =
        "335915892331-6ta7o5j6u9ie4uqaljmtl40v8kl98dcp.apps.googleusercontent.com";

    [Header("Inputs")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;

    [Header("UI")]
    public TextMeshProUGUI messageText;

    [Header("Buttons")]
    public Button loginButton;

    [Header("Scene")]
    public string nextScene = "ARScene";

    [Header("Session")]
    [SerializeField] private bool autoLoginExistingUser = true;
    [SerializeField] private bool clearSavedSessionOnStart = false;
    [SerializeField] private float autoLoginDelay = 0.3f;
    [SerializeField] private string googleWebClientId = DefaultGoogleWebClientId;

    private bool isLoading = false;
    private bool autoLoginChecked = false;
    private bool manualLoginStarted = false;
    private bool sceneLoadStarted = false;
    private Coroutine autoLoginCoroutine;

    private void Awake()
    {
        if (autoLoginExistingUser && !clearSavedSessionOnStart)
            ShowAuthLoadingOnly();
    }

    private void Start()
    {
        if (clearSavedSessionOnStart)
        {
            StartCoroutine(ClearSavedSession());
        }
        else if (autoLoginExistingUser)
        {
            autoLoginCoroutine = StartCoroutine(TryAutoLogin());
        }
    }

    private IEnumerator ClearSavedSession()
    {
        while (!FirebaseManager.IsInitialized)
            yield return null;

        if (FirebaseManager.Auth != null)
            FirebaseManager.Auth.SignOut();

        FirebaseManager.User = null;

        SignOutGoogleSafely();
    }

    private IEnumerator TryAutoLogin()
    {
        while (!FirebaseManager.IsInitialized)
            yield return null;

        if (autoLoginChecked)
            yield break;

        autoLoginChecked = true;

        yield return new WaitForSeconds(autoLoginDelay);

        if (manualLoginStarted || sceneLoadStarted)
            yield break;

        if (FirebaseManager.Auth == null || FirebaseManager.Auth.CurrentUser == null)
            yield break;

        FirebaseManager.User = FirebaseManager.Auth.CurrentUser;
        ReloadExplorationProgress();

        AppLog.Info("AUTH", "Sesion existente encontrada: " + FirebaseManager.User.Email);

        ShowLoadingPanel();

        if (messageText != null)
            messageText.text = "Bienvenido";

        yield return new WaitForSeconds(autoLoginDelay);

        if (manualLoginStarted || sceneLoadStarted)
            yield break;

        HideLoadingPanel();
        LoadNextSceneOnce();
    }

    public void LoginUser()
    {
        if (isLoading)
            return;

        manualLoginStarted = true;

        if (autoLoginCoroutine != null)
        {
            StopCoroutine(autoLoginCoroutine);
            autoLoginCoroutine = null;
        }

        string email = emailInput.text.Trim();
        string password = passwordInput.text.Trim();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            messageText.text = "Complete todos los campos";
            return;
        }

        if (FirebaseManager.Auth == null)
        {
            messageText.text = "Firebase aún se está inicializando";
            return;
        }

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            messageText.text = "Sin conexión a internet";
            return;
        }

        isLoading = true;
        messageText.text = "Ingresando...";
        SetLoadingState(true, false);

        FirebaseManager.Auth.SignOut();
        FirebaseManager.User = null;

        FirebaseManager.Auth
            .SignInWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled)
                {
                    FirebaseManager.Auth.SignOut();
                    FirebaseManager.User = null;

                    messageText.text = "Login cancelado";
                    ResetUI();
                    return;
                }

                if (task.IsFaulted)
                {
                    FirebaseManager.Auth.SignOut();
                    FirebaseManager.User = null;

                    messageText.text = FirebaseAuthErrorMessages.ForLogin(task.Exception);
                    ResetUI();
                    return;
                }

                FirebaseUser user = task.Result.User;
                FirebaseManager.User = user;
                ReloadExplorationProgress();

                AppLog.Info("AUTH", "Login correcto: " + user.Email);

                messageText.text = "Bienvenido";
                StartCoroutine(LoadSceneDelayed());
            });
    }

    public void ClearForm()
    {
        if (emailInput != null)
            emailInput.text = string.Empty;

        if (passwordInput != null)
            passwordInput.text = string.Empty;

        if (messageText != null)
            messageText.text = string.Empty;

        isLoading = false;
        manualLoginStarted = false;

        if (loginButton != null)
            loginButton.interactable = true;
    }

    private IEnumerator LoadSceneDelayed()
    {
        yield return new WaitForSeconds(0.7f);

        HideLoadingPanel();
        isLoading = false;

        LoadNextSceneOnce();
    }

    private void LoadNextSceneOnce()
    {
        if (sceneLoadStarted)
            return;

        sceneLoadStarted = true;
        SceneManager.LoadScene(nextScene);
    }

    private void ResetUI()
    {
        isLoading = false;
        SetLoadingState(false, false);
    }

    private void SetLoadingState(bool loading, bool hideAuthPanels)
    {
        if (loginButton != null)
            loginButton.interactable = !loading;

        if (!loading)
        {
            HideLoadingPanel();
            return;
        }

        if (hideAuthPanels)
        {
            ShowLoadingPanel();
            return;
        }

        HideLoadingPanel();
    }

    private void ShowLoadingPanel()
    {
        ShowAuthLoadingOnly();

        if (LoadingUI.Instance != null)
            LoadingUI.Instance.Show();
    }

    private void HideLoadingPanel()
    {
        if (LoadingUI.Instance != null)
            LoadingUI.Instance.Hide();
    }

    private void ShowAuthLoadingOnly()
    {
        AuthUIManager authUIManager = GetComponent<AuthUIManager>();

        if (authUIManager == null)
            authUIManager = FindObjectOfType<AuthUIManager>(true);

        if (authUIManager != null)
            authUIManager.ShowLoadingOnly();
    }

    private void ReloadExplorationProgress()
    {
        if (ExplorationManager.Instance != null)
            ExplorationManager.Instance.LoadForCurrentUser();
    }

    private void SignOutGoogleSafely()
    {
        TryEnsureGoogleConfiguration();

        try
        {
            GoogleSignIn.DefaultInstance.SignOut();
            GoogleSignIn.DefaultInstance.Disconnect();
        }
        catch (System.Exception ex)
        {
            AppLog.Info("AUTH", "No se pudo limpiar Google Sign-In: " + ex.Message);
        }
    }

    private void TryEnsureGoogleConfiguration()
    {
        if (string.IsNullOrWhiteSpace(googleWebClientId))
            return;

        if (GoogleSignIn.Configuration != null)
            return;

        try
        {
            GoogleSignIn.Configuration = new GoogleSignInConfiguration
            {
                WebClientId = googleWebClientId,
                RequestIdToken = true,
                RequestEmail = true,
                UseGameSignIn = false
            };
        }
        catch (System.Exception ex)
        {
            AppLog.Info("AUTH", "No se pudo configurar Google para limpiar sesión: " + ex.Message);
        }
    }
}