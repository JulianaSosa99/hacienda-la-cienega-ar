using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class PortraitTransition : MonoBehaviour
{
    [Header("Portrait")]
    public CanvasGroup portraitVideoCanvasGroup;
    public VideoPlayer portraitVideo;

    [Header("Puzzle")]
    public GameObject puzzleGame;

    [Header("Timing")]
    public float fadeDuration = 1f;
    public float prepareTimeout = 5f;
    public float playbackTimeout = 20f;
    public bool playTransitionVideo = true;

    private Coroutine transitionRoutine;

    private void Start()
    {
        if (puzzleGame != null)
            puzzleGame.SetActive(false);

        SetPortraitVisible(0f);

        if (portraitVideo != null)
            portraitVideo.playOnAwake = false;
    }

    public void PlayTransition()
    {
        GameObjectActivationUtility.SetActiveWithParents(gameObject, true);

        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        transitionRoutine = StartCoroutine(TransitionRoutine());
    }

    private IEnumerator TransitionRoutine()
    {
        SetPortraitVisible(0f);

        if (playTransitionVideo && portraitVideo != null)
            yield return PlayPortraitVideoRoutine();

        ActivatePuzzle();
        transitionRoutine = null;
    }

    private IEnumerator PlayPortraitVideoRoutine()
    {
        portraitVideo.Prepare();

        float elapsed = 0f;

        while (!portraitVideo.isPrepared && elapsed < prepareTimeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!portraitVideo.isPrepared)
        {
            Debug.LogWarning("PortraitTransition: video no preparado, se abre puzzle.");
            yield break;
        }

        portraitVideo.time = 0;
        portraitVideo.Play();

        elapsed = 0f;

        while (!portraitVideo.isPlaying && elapsed < 1f)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!portraitVideo.isPlaying)
        {
            Debug.LogWarning("PortraitTransition: video no inicio, se abre puzzle.");
            yield break;
        }

        yield return FadePortrait(0f, 1f);

        elapsed = 0f;
        float maxPlaybackTime = playbackTimeout;

        if (portraitVideo.length > 0)
            maxPlaybackTime = Mathf.Min(playbackTimeout, (float)portraitVideo.length + 1f);

        while (portraitVideo.isPlaying && elapsed < maxPlaybackTime)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        portraitVideo.Stop();
        yield return FadePortrait(1f, 0f);
    }

    private IEnumerator FadePortrait(float from, float to)
    {
        if (portraitVideoCanvasGroup == null)
            yield break;

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            SetPortraitVisible(Mathf.Lerp(from, to, elapsed / fadeDuration));
            yield return null;
        }

        SetPortraitVisible(to);
    }

    private void SetPortraitVisible(float alpha)
    {
        if (portraitVideoCanvasGroup == null)
            return;

        portraitVideoCanvasGroup.alpha = alpha;
        portraitVideoCanvasGroup.interactable = false;
        portraitVideoCanvasGroup.blocksRaycasts = false;
    }

    private void ActivatePuzzle()
    {
        NarrativeFlowUI.EnterMinigame(puzzleGame);

        if (puzzleGame != null)
            Debug.Log("PuzzleGame activo: " + puzzleGame.activeInHierarchy);
    }
}
