using System.Collections;
using UnityEngine;

public class DoorGameWinSequence : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DoorGameController gameController;

    [SerializeField] private RectTransform leftDoor;
    [SerializeField] private RectTransform rightDoor;

    [Header("Background")]
    [SerializeField] private CanvasGroup backgroundCanvasGroup;

    [Header("Door Animation")]
    [SerializeField] private float openDistance = 600f;
    [SerializeField] private float openDuration = 2f;

    [Header("Audio")]
    [SerializeField] private AudioSource doorAudioSource;
    [SerializeField] private AudioClip doorOpenClip;

    private void OnEnable()
    {
        gameController.OnGameCompleted += PlaySequence;
    }

    private void OnDisable()
    {
        gameController.OnGameCompleted -= PlaySequence;
    }

    private void PlaySequence()
    {
        StartCoroutine(SequenceCoroutine());
    }

    private IEnumerator SequenceCoroutine()
    {
        // Espera a que termine el sonido de victoria
        yield return new WaitForSeconds(1f);

        // Fade del fondo
        if (backgroundCanvasGroup != null)
        {
            StartCoroutine(FadeBackground());
        }

        // Sonido de apertura
        if (doorAudioSource != null && doorOpenClip != null)
        {
            doorAudioSource.PlayOneShot(doorOpenClip);
        }

        // Abrir puertas
        StartCoroutine(OpenDoor(
            leftDoor,
            Vector2.left * openDistance));

        StartCoroutine(OpenDoor(
            rightDoor,
            Vector2.right * openDistance));

        yield return new WaitForSeconds(openDuration);

        FindObjectOfType<PadreMarioNarrativeManager>()
            .StartFinalNarrative();
    }

    private IEnumerator FadeBackground()
    {
        float duration = 1.5f;

        float elapsed = 0f;

        float startAlpha = backgroundCanvasGroup.alpha;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            backgroundCanvasGroup.alpha =
                Mathf.Lerp(
                    startAlpha,
                    0f,
                    elapsed / duration);

            yield return null;
        }

        backgroundCanvasGroup.alpha = 0f;
    }

    private IEnumerator OpenDoor(
        RectTransform door,
        Vector2 offset)
    {
        Vector2 startPos = door.anchoredPosition;
        Vector2 targetPos = startPos + offset;

        float elapsed = 0f;

        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / openDuration;

            // Ease Out para que se vea más natural
            t = 1f - Mathf.Pow(1f - t, 3f);

            door.anchoredPosition =
                Vector2.Lerp(
                    startPos,
                    targetPos,
                    t);

            yield return null;
        }

        door.anchoredPosition = targetPos;
    }
}