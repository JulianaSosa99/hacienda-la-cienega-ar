using Firebase.Storage;
using UnityEngine;

public class StorageTest : MonoBehaviour
{
    void Start()
    {
        FirebaseStorage storage =
            FirebaseStorage.DefaultInstance;
            UnityEngine.Debug.Log(storage);
    }
}