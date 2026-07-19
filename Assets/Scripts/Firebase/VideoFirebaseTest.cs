using UnityEngine;
using UnityEngine.Video;

public class VideoFirebaseTest : MonoBehaviour
{
    public VideoPlayer videoPlayer;

    private async void Start()
    {
        string localPath =
            await VideoCacheManager.Instance.GetVideoPathAsync(
                "videos/pozo/matriarca-intro-pozo.mp4"
            );

        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = localPath;

        videoPlayer.Prepare();

        while (!videoPlayer.isPrepared)
        {
            await System.Threading.Tasks.Task.Yield();
        }

        Debug.Log("VIDEO PREPARADO");

        videoPlayer.Play();
    }
}