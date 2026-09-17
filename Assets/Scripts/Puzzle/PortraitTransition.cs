using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

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
    private bool preparing;
    private bool playbackFinished;
    private string playbackError;

    private Canvas PortraitCanvas => portraitVideoCanvasGroup != null
        ? portraitVideoCanvasGroup.GetComponentInParent<Canvas>(true) : null;
    private RawImage PortraitDisplay => portraitVideoCanvasGroup != null
        ? portraitVideoCanvasGroup.GetComponent<RawImage>() : null;

    private void Awake()
    {
        HidePresentation();
        if (portraitVideo == null) return;
        portraitVideo.playOnAwake = false;
        portraitVideo.isLooping = false;
        portraitVideo.prepareCompleted += OnPrepared;
        portraitVideo.loopPointReached += OnPlaybackFinished;
        portraitVideo.errorReceived += OnVideoError;
    }

    public void PrepareTransition()
    {
        GameObjectActivationUtility.SetActiveWithParents(gameObject, true);
        CancelTransition();
        if (playTransitionVideo && HasVideoSource())
        {
            GameObjectActivationUtility.SetActiveWithParents(portraitVideo.gameObject, true);
            BeginPrepare();
        }
    }

    private void BeginPrepare()
    {
        if (portraitVideo.isPrepared || preparing) return;
        playbackError = null;
        preparing = true;
        portraitVideo.Prepare();
    }

    private void OnPrepared(VideoPlayer player) { preparing = false; }
    private void OnPlaybackFinished(VideoPlayer player) { playbackFinished = true; }
    private void OnVideoError(VideoPlayer player, string message)
    {
        preparing = false;
        playbackError = message;
        HidePresentation();
        Debug.LogError("PortraitTransition: " + message, this);
    }

    private bool HasVideoSource()
    {
        return portraitVideo != null &&
            (portraitVideo.source == VideoSource.VideoClip
                ? portraitVideo.clip != null
                : !string.IsNullOrWhiteSpace(portraitVideo.url));
    }

    public void PlayTransition()
    {
        GameObjectActivationUtility.SetActiveWithParents(gameObject, true);
        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(TransitionRoutine());
    }

    private IEnumerator TransitionRoutine()
    {
        HidePresentation();
        NarrativeFlowUI.HideProgress();
        if (playTransitionVideo && portraitVideo != null)
            yield return PlayPortraitVideoRoutine();

        transitionRoutine = null;
        ActivatePuzzle();
    }

    private IEnumerator PlayPortraitVideoRoutine()
    {
        if (!HasVideoSource())
        {
            Debug.LogError("PortraitTransition: falta el video de la cinematica.");
            yield break;
        }

        GameObjectActivationUtility.SetActiveWithParents(portraitVideo.gameObject, true);
        if (portraitVideoCanvasGroup != null)
            GameObjectActivationUtility.SetActiveWithParents(portraitVideoCanvasGroup.gameObject, true);

        Canvas canvas = PortraitCanvas;
        RawImage display = PortraitDisplay;
        if (canvas == null || display == null)
        {
            Debug.LogError("PortraitTransition: falta el Canvas o RawImage del retrato.");
            yield break;
        }
        canvas.transform.localScale = Vector3.one;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 20;
        foreach (CanvasGroup group in display.GetComponentsInParent<CanvasGroup>())
        {
            if (group != portraitVideoCanvasGroup) group.alpha = 1f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        BeginPrepare();
        float elapsed = 0f;
        while (!portraitVideo.isPrepared && playbackError == null && elapsed < prepareTimeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        if (!portraitVideo.isPrepared || playbackError != null) yield break;

        portraitVideo.time = 0;
        playbackFinished = false;
        portraitVideo.Play();
        elapsed = 0f;
        while ((portraitVideo.texture == null || portraitVideo.frame < 0) &&
               playbackError == null && elapsed < prepareTimeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        if (playbackError != null || portraitVideo.texture == null || portraitVideo.frame < 0)
            yield break;

        // Enable the image only after a valid video texture is bound.
        display.texture = portraitVideo.texture;
        display.color = Color.white;
        display.enabled = true;
        canvas.enabled = true;
        yield return FadePortrait(0f, 1f);

        elapsed = 0f;
        float maxPlaybackTime = portraitVideo.length > 0
            ? Mathf.Max(playbackTimeout, (float)portraitVideo.length + 1f) : playbackTimeout;
        while (!playbackFinished && playbackError == null && elapsed < maxPlaybackTime)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        if (playbackError == null) yield return FadePortrait(1f, 0f);
    }

    // A transparent parent Image can still intercept touches. Disable the entire
    // cinematic raycaster, and hide the RawImage BEFORE releasing its video texture.
    private void HidePresentation()
    {
        SetPortraitVisible(0f);
        RawImage display = PortraitDisplay;
        if (display != null)
        {
            display.enabled = false;
            display.raycastTarget = false;
            display.texture = null;
        }
        Canvas canvas = PortraitCanvas;
        if (canvas == null) return;
        canvas.enabled = false;
        foreach (GraphicRaycaster raycaster in canvas.GetComponentsInChildren<GraphicRaycaster>(true))
            raycaster.enabled = false;
        foreach (Graphic graphic in canvas.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;
    }

    public void CancelTransition()
    {
        StopAllCoroutines();
        transitionRoutine = null;
        HidePresentation();
        if (portraitVideo != null) portraitVideo.Stop();
        preparing = false;
        playbackFinished = false;
        playbackError = null;
    }

    private void OnDisable() { CancelTransition(); }

    private void OnDestroy()
    {
        if (portraitVideo == null) return;
        portraitVideo.prepareCompleted -= OnPrepared;
        portraitVideo.loopPointReached -= OnPlaybackFinished;
        portraitVideo.errorReceived -= OnVideoError;
    }

    private IEnumerator FadePortrait(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration && playbackError == null)
        {
            elapsed += Time.unscaledDeltaTime;
            SetPortraitVisible(Mathf.Lerp(from, to, elapsed / fadeDuration));
            yield return null;
        }
        SetPortraitVisible(playbackError == null ? to : 0f);
    }

    private void SetPortraitVisible(float alpha)
    {
        if (portraitVideoCanvasGroup == null) return;
        portraitVideoCanvasGroup.alpha = alpha;
        portraitVideoCanvasGroup.interactable = false;
        portraitVideoCanvasGroup.blocksRaycasts = false;
    }

    private void ActivatePuzzle()
    {
        HidePresentation();
        if (portraitVideo != null) portraitVideo.Stop();
        preparing = false;
        NarrativeFlowUI.EnterMinigame(puzzleGame);
        if (puzzleGame == null) return;
        CanvasGroup group = puzzleGame.GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }
    }
}
