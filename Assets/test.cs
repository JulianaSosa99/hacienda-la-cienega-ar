using UnityEngine;
using UnityEngine.Video;

public class TestVideo : MonoBehaviour
{
    void Start()
    {
        gameObject.AddComponent<VideoPlayer>();
    }
}