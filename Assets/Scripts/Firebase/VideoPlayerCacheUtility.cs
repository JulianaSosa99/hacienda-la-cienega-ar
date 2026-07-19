using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Video;

public static class VideoPlayerCacheUtility
{
    public static IEnumerator PrepareFromFirebaseOrCurrentUrl(VideoPlayer player, string firebasePath)
    {
        if (player == null)
        {
            yield break;
        }

        if (!string.IsNullOrWhiteSpace(firebasePath))
        {
            if (VideoCacheManager.Instance == null)
            {
                AppLog.Warning("VIDEO_PLAYER", "VideoCacheManager no existe en la escena. Se usara la URL actual del VideoPlayer.");
            }
            else
            {
                Task<string> pathTask = VideoCacheManager.Instance.GetVideoPathAsync(firebasePath.Trim());

                while (!pathTask.IsCompleted)
                {
                    yield return null;
                }

                if (pathTask.IsFaulted || pathTask.IsCanceled)
                {
                    Exception error = pathTask.Exception != null ? pathTask.Exception.GetBaseException() : null;
                    AppLog.Error("VIDEO_PLAYER", "No se pudo descargar el video: " + firebasePath + " " + error);
                }
                else
                {
                    player.source = VideoSource.Url;
                    player.url = pathTask.Result;
                    AppLog.Info("VIDEO_PLAYER", "Video listo desde cache local: " + player.url);
                }
            }
        }

        player.Prepare();
    }

    public static IEnumerator WaitForFirstVisibleFrame(VideoPlayer player)
    {
        if (player == null)
        {
            yield break;
        }

        float timeout = 2f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            bool hasAdvancedFrame = player.frame > 0 || player.time > 0.03f;
            if (player.isPlaying && player.texture != null && hasAdvancedFrame)
            {
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }
}



