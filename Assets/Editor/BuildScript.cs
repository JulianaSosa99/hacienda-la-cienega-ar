using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildScript
{
    [MenuItem("Build/Build Android APK")]
    public static void BuildAndroid()
    {
        // ===== ESCENAS MANUALES =====
        // MUCHÍSIMO más estable para CI/CD
        string[] scenes =
        {
            "Assets/Scenes/SplashScene.unity",
            "Assets/Scenes/LoginScene.unity",
            "Assets/Scenes/ARScene.unity"
        };

        // ===== OUTPUT APK =====
        string path = "Builds/Android/AppHacienda.apk";

        // ===== GENERAR APK (NO GRADLE PROJECT) =====
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;

        // ===== KEYSTORE =====
        PlayerSettings.Android.useCustomKeystore = true;

        PlayerSettings.Android.keystoreName = "Keystore/hlc.keystore";
        PlayerSettings.Android.keystorePass = "LavaStudio";

        PlayerSettings.Android.keyaliasName = "lavastudio";
        PlayerSettings.Android.keyaliasPass = "LavaStudio";

        // ===== BUILD OPTIONS =====
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = path,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        Debug.Log("=== STARTING ANDROID BUILD ===");

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);

        Debug.Log("=== BUILD FINISHED ===");

        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log("APK generado correctamente en: " + path);
        }
        else
        {
            Debug.LogError("Falló el build Android");
            throw new System.Exception("Build Android failed");
        }
    }
}