using Firebase.Auth;
using Google;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SessionLogoutButton : MonoBehaviour
{
    private const string DefaultWebClientId =
        "335915892331-6ta7o5j6u9ie4uqaljmtl40v8kl98dcp.apps.googleusercontent.com";

    [SerializeField] private Button button;
    [SerializeField] private string loginScene = "LoginScene";
    [SerializeField] private string googleWebClientId = DefaultWebClientId;

    private bool isLoggingOut;

    private void Awake()
    {
        Configure(button);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(Logout);
    }

    public void Configure(Button configuredButton)
    {
        if (configuredButton != null)
            button = configuredButton;

        if (button == null)
            button = GetComponent<Button>();

        if (button == null)
            return;

        button.onClick.RemoveListener(Logout);
        button.onClick.AddListener(Logout);
    }

    public void Logout()
    {
        if (isLoggingOut)
            return;

        isLoggingOut = true;

        if (button != null)
            button.interactable = false;

        if (FirebaseManager.Auth != null)
            FirebaseManager.Auth.SignOut();

        FirebaseManager.User = null;

        SignOutGoogleSafely();

        AppLog.Info("AUTH", "Sesion cerrada localmente. El progreso de la cuenta se mantiene en Firebase.");
        SceneManager.LoadScene(loginScene);
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
            AppLog.Info("AUTH", "No se pudo cerrar Google Sign-In: " + ex.Message);
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
            AppLog.Info("AUTH", "No se pudo configurar Google para logout: " + ex.Message);
        }
    }
}
