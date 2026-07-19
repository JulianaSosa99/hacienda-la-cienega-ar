using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Functions;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseAuth Auth;
    public static FirebaseFunctions Functions;
    public static FirebaseUser User;

    public static bool IsInitialized = false;

    private void Awake()
    {
        if (FindObjectsOfType<FirebaseManager>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        EnsureVideoCacheManager();

        AppLog.Info("FIREBASE", "FirebaseManager Awake ejecutado");

        InitializeFirebase();
    }

    private void EnsureVideoCacheManager()
    {
        if (VideoCacheManager.Instance != null)
        {
            return;
        }

        if (GetComponent<VideoCacheManager>() == null)
        {
            gameObject.AddComponent<VideoCacheManager>();
        }
    }
    void InitializeFirebase()
    {
        AppLog.Info("FIREBASE", "Inicializando Firebase...");

        FirebaseApp
            .CheckAndFixDependenciesAsync()
            .ContinueWithOnMainThread(task =>
            {
                DependencyStatus status = task.Result;

                if (status == DependencyStatus.Available)
                {
                    Auth = FirebaseAuth.DefaultInstance;
                    Functions = FirebaseFunctions.DefaultInstance;

                    IsInitialized = true;

                    AppLog.Info(
                        "FIREBASE",
                        "Firebase inicializado correctamente"
                    );
                }
                else
                {
                    AppLog.Error(
                        "FIREBASE",
                        "No se pudieron resolver dependencias: "
                        + status
                    );
                }
            });
    }
}