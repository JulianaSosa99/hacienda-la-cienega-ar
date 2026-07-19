using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.Controller
{
    public class SplashController : MonoBehaviour
    {
        public CanvasGroup logoCanvasGroup;
        public CanvasGroup textCanvasGroup;
        public float logoFadeDuration = 1.2f;
        public float waitAfterFade = 1.5f;
        public string loginScene = "LoginScene";
        public string authenticatedScene = "ARScene";

        private void Start()
        {
            EnsureFirebaseManager();
            StartCoroutine(SplashSequence());
        }

        private IEnumerator SplashSequence()
        {
            float elapsed = 0f;

            while (elapsed < logoFadeDuration)
            {
                elapsed += Time.deltaTime;

                float alpha = Mathf.Clamp01(elapsed / logoFadeDuration);

                if (logoCanvasGroup != null)
                    logoCanvasGroup.alpha = alpha;

                if (textCanvasGroup != null)
                    textCanvasGroup.alpha = alpha;

                yield return null;
            }

            yield return new WaitForSeconds(waitAfterFade);

            while (!global::FirebaseManager.IsInitialized)
                yield return null;

            if (global::FirebaseManager.Auth != null &&
                global::FirebaseManager.Auth.CurrentUser != null)
            {
                global::FirebaseManager.User =
                    global::FirebaseManager.Auth.CurrentUser;

                if (global::ExplorationManager.Instance != null)
                    global::ExplorationManager.Instance.LoadForCurrentUser();

                SceneManager.LoadScene(authenticatedScene);
                yield break;
            }

            SceneManager.LoadScene(loginScene);
        }

        private void EnsureFirebaseManager()
        {
            if (FindObjectOfType<global::FirebaseManager>() != null)
                return;

            GameObject firebaseObject = new GameObject("FirebaseManager");
            firebaseObject.AddComponent<global::FirebaseManager>();
        }
    }
}
