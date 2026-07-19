using UnityEngine;
using System;

public class DoorGameController : MonoBehaviour
{
    [Header("Puzzle")]
    [SerializeField] private int totalPieces = 12;

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;

    [SerializeField] private AudioClip correctClip;
    [SerializeField] private AudioClip wrongClip;
    [SerializeField] private AudioClip winClip;

    private int placedPieces;

    public event Action OnGameCompleted;

    public void RegisterPiecePlaced()
    {
        placedPieces++;

        PlayCorrect();

        Debug.Log($"Piezas colocadas: {placedPieces}/{totalPieces}");

        if (placedPieces >= totalPieces)
        {
            CompleteGame();
        }
    }

    public void PlayWrong()
    {
        if (wrongClip != null)
            sfxSource.PlayOneShot(wrongClip);
    }

    private void PlayCorrect()
    {
        if (correctClip != null)
            sfxSource.PlayOneShot(correctClip);
    }

    private void PlayWin()
    {
        if (winClip != null)
            sfxSource.PlayOneShot(winClip);
    }

    private void CompleteGame()
    {
        Debug.Log("AGUILA COMPLETADA");

        PlayWin();

        OnGameCompleted?.Invoke();
    }
}