using UnityEngine;
using Firebase.Auth;
using Firebase.Extensions;
using Google;
using UnityEngine.SceneManagement;
using TMPro;
using System.Threading.Tasks;

public class GoogleSignInController : MonoBehaviour
{
    private const string DefaultWebClientId =
        "335915892331-6ta7o5j6u9ie4uqaljmtl40v8kl98dcp.apps.googleusercontent.com";

    [Header("Google")]
    public string webClientId = DefaultWebClientId;

    [Header("UI")]
    public TextMeshProUGUI messageText;

    [Header("Scene")]
    public string nextScene = "ARScene";

    private GoogleSignInConfiguration configuration;
    private bool isLoading = false;

    private void Awake()
    {
        configuration = CreateConfiguration();
    }

    public void SignInWithGoogle()
    {
        if (isLoading)
            return;

        if (string.IsNullOrWhiteSpace(GetResolvedWebClientId()))
        {
            FinishLoading("Google no esta configurado");
            AppLog.Error("AUTH", "Google Sign-In sin WebClientId.");
            return;
        }

        if (IsOffline())
        {
            FinishLoading("Sin conexión a internet");
            return;
        }

        if (FirebaseManager.Auth == null)
        {
            SetMessage("Firebase aún se está inicializando");
            return;
        }

        isLoading = true;
        SetMessage("Abriendo Google...");
        ShowLoading();

        try
        {
            EnsureGoogleConfiguration();

            GoogleSignIn.DefaultInstance.SignIn()
                .ContinueWithOnMainThread(OnGoogleAuthenticated);
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            AppLog.Error("AUTH", "Error iniciando Google Sign-In: " + ex);
            FinishLoading(IsOffline() ? "Sin conexión a internet" : "No se pudo abrir Google");
        }
    }

    private void OnGoogleAuthenticated(Task<GoogleSignInUser> task)
    {
        try
        {
            if (task.IsFaulted)
            {
                UnityEngine.Debug.LogError(task.Exception);
                AppLog.Error("AUTH", "Google Sign-In fallo: " + task.Exception);
                FinishLoading(IsOffline() ? "Sin conexión a internet" : "Error con Google");
                return;
            }

            if (task.IsCanceled)
            {
                FinishLoading("Login cancelado");
                return;
            }

            if (task.Result == null || string.IsNullOrEmpty(task.Result.IdToken))
            {
                AppLog.Error("AUTH", "Google Sign-In no devolvio IdToken.");
                FinishLoading("No se recibió token de Google");
                return;
            }

            SetMessage("Ingresando con Google...");

            Credential credential = GoogleAuthProvider.GetCredential(task.Result.IdToken, null);

            FirebaseManager.Auth
                .SignInWithCredentialAsync(credential)
                .ContinueWithOnMainThread(firebaseTask =>
                {
                    try
                    {
                        if (firebaseTask.IsCanceled)
                        {
                            FinishLoading("Cancelado");
                            return;
                        }

                        if (firebaseTask.IsFaulted)
                        {
                            UnityEngine.Debug.LogError(firebaseTask.Exception);
                            AppLog.Error("AUTH", "Firebase Google credential fallo: " + firebaseTask.Exception);
                            FinishLoading(IsOffline() ? "Sin conexión a internet" : "Error Firebase");
                            return;
                        }

                        FirebaseUser user = ResolveFirebaseUser(firebaseTask.Result);

                        if (user == null)
                        {
                            AppLog.Error("AUTH", "Firebase devolvió usuario nulo con Google.");
                            FinishLoading("No se pudo iniciar sesión");
                            return;
                        }

                        FirebaseManager.User = user;
                        ReloadExplorationProgress();

                        SetMessage("Bienvenido " + user.DisplayName);
                        isLoading = false;
                        HideLoading();

                        SceneManager.LoadScene(nextScene);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogException(ex);
                        AppLog.Error("AUTH", "Error procesando Firebase Google auth: " + ex);
                        FinishLoading(IsOffline() ? "Sin conexión a internet" : "Error Firebase");
                    }
                });
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            AppLog.Error("AUTH", "Error procesando respuesta de Google: " + ex);
            FinishLoading(IsOffline() ? "Sin conexión a internet" : "Error con Google");
        }
    }

    private void FinishLoading(string message)
    {
        isLoading = false;
        SetMessage(message);
        HideLoading();
    }

    private void SetMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;
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

    private bool IsOffline()
    {
        return Application.internetReachability == NetworkReachability.NotReachable;
    }

    private GoogleSignInConfiguration CreateConfiguration()
    {
        return new GoogleSignInConfiguration
        {
            WebClientId = GetResolvedWebClientId(),
            RequestIdToken = true,
            RequestEmail = true,
            UseGameSignIn = false
        };
    }

    private void EnsureGoogleConfiguration()
    {
        configuration = CreateConfiguration();

        if (GoogleSignIn.Configuration == null)
        {
            GoogleSignIn.Configuration = configuration;
            return;
        }

        if (GoogleSignIn.Configuration.WebClientId == GetResolvedWebClientId())
            return;

        try
        {
            GoogleSignIn.Configuration = configuration;
        }
        catch (System.Exception ex)
        {
            AppLog.Info(
                "AUTH",
                "Google Sign-In ya tenía una configuración activa: " + ex.Message
            );
        }
    }

    private string GetResolvedWebClientId()
    {
        return string.IsNullOrWhiteSpace(webClientId)
            ? DefaultWebClientId
            : webClientId.Trim();
    }

    private FirebaseUser ResolveFirebaseUser(object authResult)
    {
        if (authResult == null)
            return null;

        FirebaseUser user = authResult as FirebaseUser;

        if (user != null)
            return user;

        System.Reflection.PropertyInfo userProperty =
            authResult.GetType().GetProperty("User");

        if (userProperty != null)
            return userProperty.GetValue(authResult, null) as FirebaseUser;

        return null;
    }

    private void ReloadExplorationProgress()
    {
        if (ExplorationManager.Instance != null)
            ExplorationManager.Instance.LoadForCurrentUser();
    }
}
