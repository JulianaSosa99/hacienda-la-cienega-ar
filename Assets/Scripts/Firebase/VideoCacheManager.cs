using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Storage;

public class VideoCacheManager : MonoBehaviour
{
    public static VideoCacheManager Instance;

    [Header("Preload")]
    [SerializeField] private bool preloadOnStart = true;
    [SerializeField] private string[] videosToPreload =
    {
        "videos/pozo/matriarca-intro-pozo.mp4",
        "videos/pozo/ninio_prefinal.mp4",
        "videos/adolfo/Matriarca_Presenta_T_Adolfo.mp4",
        "videos/adolfo/adolfo_final_1080p.mp4",
        "videos/juan-lasso/Matriarca_Presenta_JuanMLasso (1).mp4",
        "videos/juan-lasso/Dialogo_JuanM_Lasso_ultimo.mp4",
        "videos/novios/matriarca-video-novios.mp4",
        "videos/novios/novios_1080p.mp4"
    };

    private readonly Dictionary<string, Task<string>> downloadTasks = new Dictionary<string, Task<string>>();

    private FirebaseStorage storage;
    private string videosFolder;
    private bool preloadStarted = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        storage = FirebaseStorage.DefaultInstance;

        videosFolder = Path.Combine(
            Application.persistentDataPath,
            "videos"
        );

        if (!Directory.Exists(videosFolder))
        {
            Directory.CreateDirectory(videosFolder);
        }
    }

    private async void Start()
    {
        if (!preloadOnStart)
        {
            return;
        }

        await PreloadConfiguredVideosAsync();
    }

    public async Task PreloadConfiguredVideosAsync()
    {
        if (preloadStarted)
        {
            return;
        }

        preloadStarted = true;

        while (!FirebaseManager.IsInitialized)
        {
            await Task.Yield();
        }

        if (videosToPreload == null || videosToPreload.Length == 0)
        {
            AppLog.Warning("VIDEO_CACHE", "No hay videos configurados para precarga.");
            return;
        }

        AppLog.Info("VIDEO_CACHE", "PRECARGA DE VIDEOS INICIADA: " + videosToPreload.Length);

        List<Task<string>> tasks = new List<Task<string>>();

        foreach (string path in videosToPreload)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            tasks.Add(GetVideoPathAsync(path));
        }

        try
        {
            await Task.WhenAll(tasks);
            AppLog.Info("VIDEO_CACHE", "PRECARGA DE VIDEOS COMPLETA");
        }
        catch (System.Exception exception)
        {
            AppLog.Error("VIDEO_CACHE", "PRECARGA DE VIDEOS CON ERRORES: " + exception.Message);
        }
    }

    public Task<string> GetVideoPathAsync(string firebasePath)
    {
        string normalizedPath = NormalizeFirebasePath(firebasePath);

        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            return Task.FromResult(string.Empty);
        }

        if (downloadTasks.TryGetValue(normalizedPath, out Task<string> runningTask))
        {
            AppLog.Info("VIDEO_CACHE", "VIDEO YA DESCARGANDOSE: " + normalizedPath);
            return runningTask;
        }

        string localPath = GetLocalPath(normalizedPath);

        if (IsValidCachedFile(localPath))
        {
            AppLog.Info("VIDEO_CACHE", "VIDEO EN CACHE: " + normalizedPath);
            return Task.FromResult(localPath);
        }

        Task<string> downloadTask = DownloadVideoAsync(normalizedPath, localPath);
        downloadTasks[normalizedPath] = downloadTask;
        return downloadTask;
    }

    private async Task<string> DownloadVideoAsync(string normalizedPath, string localPath)
    {
        string tempPath = localPath + ".download";
        string markerPath = GetCompleteMarkerPath(localPath);

        try
        {
            string localDirectory = Path.GetDirectoryName(localPath);
            if (!string.IsNullOrEmpty(localDirectory) && !Directory.Exists(localDirectory))
            {
                Directory.CreateDirectory(localDirectory);
            }

            DeleteIfExists(tempPath);
            DeleteIfExists(markerPath);

            StorageReference reference = storage.GetReference(normalizedPath);
            await reference.GetFileAsync(tempPath);

            if (!IsNonEmptyFile(tempPath))
            {
                throw new IOException("El video descargado esta vacio o incompleto: " + normalizedPath);
            }

            DeleteIfExists(localPath);
            File.Move(tempPath, localPath);
            File.WriteAllText(markerPath, normalizedPath);

            AppLog.Info("VIDEO_CACHE", "VIDEO DESCARGADO: " + normalizedPath);
            return localPath;
        }
        catch (System.Exception exception)
        {
            DeleteIfExists(tempPath);
            DeleteIfExists(markerPath);
            AppLog.Error("VIDEO_CACHE", "NO SE PUDO DESCARGAR: " + normalizedPath + " | " + exception.Message);
            throw;
        }
        finally
        {
            downloadTasks.Remove(normalizedPath);
        }
    }

    private string NormalizeFirebasePath(string firebasePath)
    {
        if (string.IsNullOrWhiteSpace(firebasePath))
        {
            return string.Empty;
        }

        return firebasePath
            .Replace("\\", "/")
            .TrimStart('/');
    }

    private string GetLocalPath(string normalizedPath)
    {
        string relativeCachePath = normalizedPath.StartsWith("videos/")
            ? normalizedPath.Substring("videos/".Length)
            : normalizedPath;

        return Path.Combine(
            videosFolder,
            relativeCachePath.Replace("/", Path.DirectorySeparatorChar.ToString())
        );
    }

    private bool IsValidCachedFile(string path)
    {
        return IsNonEmptyFile(path) && File.Exists(GetCompleteMarkerPath(path));
    }

    private bool IsNonEmptyFile(string path)
    {
        if (!File.Exists(path))
        {
            return false;
        }

        FileInfo fileInfo = new FileInfo(path);
        return fileInfo.Length > 0;
    }

    private string GetCompleteMarkerPath(string path)
    {
        return path + ".complete";
    }

    private void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}