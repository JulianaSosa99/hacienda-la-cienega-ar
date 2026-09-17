using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class AdolfoNarrativeManager : MonoBehaviour, INarrativeManager, INarrativeResettable
{
    [Header("Narrative UI")]
    public CanvasGroup narrativePanel;

    [Header("Firebase Videos")]
    [Tooltip("Storage path. Example: videos/adolfo/Matriarca_Presenta_T_Adolfo.mp4")]
    public string introFirebasePath;
    [Tooltip("Storage path. Example: videos/adolfo/adolfo_final_1080p.mp4")]
    public string finalFirebasePath;

    [Header("Intro")]
    public GameObject introQuad;
    public VideoPlayer introPlayer;

    [Header("Puzzle")]
    public GameObject puzzleGame;
    public PortraitTransition portraitTransition;

    [Header("Puzzle Fade")]
    public CanvasGroup puzzleCanvasGroup;
    public float puzzleFadeDuration = 1f;

    [Header("Final")]
    public GameObject finalQuad;
    public VideoPlayer finalPlayer;

    private bool alreadyStarted = false;

    private bool introPrepared = false;
    private bool introPreparationStarted = false;
    private bool finalPrepared = false;
    private bool finalPreparationStarted = false;

    private Renderer introRenderer;
    private Renderer finalRenderer;
    private Material introMaterial;
    private Material finalMaterial;

    void Start()
    {
        HideNarrativePanelImmediate();

        introRenderer = introQuad.GetComponent<Renderer>();
        finalRenderer = finalQuad.GetComponent<Renderer>();

        // Explicitly bind each player to its own surface and material instance.
        introMaterial = new Material(introRenderer.sharedMaterial);
        finalMaterial = new Material(finalRenderer.sharedMaterial);
        introRenderer.sharedMaterial = introMaterial;
        finalRenderer.sharedMaterial = finalMaterial;
        introPlayer.renderMode = VideoRenderMode.MaterialOverride;
        introPlayer.targetMaterialRenderer = introRenderer;
        introPlayer.targetMaterialProperty = "_MainTex";
        finalPlayer.renderMode = VideoRenderMode.MaterialOverride;
        finalPlayer.targetMaterialRenderer = finalRenderer;
        finalPlayer.targetMaterialProperty = "_MainTex";

        introRenderer.enabled = false;
        finalRenderer.enabled = false;

        puzzleGame.SetActive(false);

        introPlayer.loopPointReached += OnIntroFinished;
        finalPlayer.loopPointReached += OnFinalFinished;

        introPlayer.prepareCompleted += OnIntroPrepared;
        finalPlayer.prepareCompleted += OnFinalPrepared;

        introPlayer.skipOnDrop = false;
        introPlayer.waitForFirstFrame = true;

        finalPlayer.skipOnDrop = false;
        finalPlayer.waitForFirstFrame = true;

        PrepareIntro();
        // Prepare the final only after the puzzle: mobile video decoders are limited.
    }

    private void PrepareIntro()
    {
        if (introPreparationStarted) return;
        introPreparationStarted = true;
        StartCoroutine(VideoPlayerCacheUtility.PrepareFromFirebaseOrCurrentUrl(introPlayer, introFirebasePath));
    }

    void OnIntroPrepared(VideoPlayer vp)
    {
        introPrepared = true;
        Debug.Log("Intro Adolfo preparada");
    }

    void OnFinalPrepared(VideoPlayer vp)
    {
        finalPrepared = true;
        Debug.Log("Final Adolfo preparado");
    }

    public void ShowNarrativePanel()
    {
        if (alreadyStarted)
            return;

        Debug.Log("SHOW PANEL ADOLFO");

        NarrativeFlowUI.ShowDiscoveryPrompt(narrativePanel);
    }

    public void HideNarrativePanel()
    {
        if (alreadyStarted)
            return;

        HideNarrativePanelImmediate();
    }

    void HideNarrativePanelImmediate()
    {
        if (narrativePanel == null)
            return;

        narrativePanel.alpha = 0;
        narrativePanel.interactable = false;
        narrativePanel.blocksRaycasts = false;

        narrativePanel.gameObject.SetActive(false);
    }

    public void StartNarrative()
    {
        if (alreadyStarted)
            return;

        Debug.Log("StartNarrative Adolfo click recibido");

        alreadyStarted = true;
        ReleaseTargetVisuals();
        AppLog.Info("GUARDIAN_START", "Inicia Tio Adolfo");
        HideNarrativePanelImmediate();

        if (ExplorationManager.Instance != null)
        {
            ExplorationManager.Instance.RegisterGuardianScanned(
                ExplorationIds.TIO_ADOLFO
            );
        }

        NarrativeFlowUI.StartNarrative();
        PrepareIntro();

        if (portraitTransition != null)
            portraitTransition.PrepareTransition();

        StartCoroutine(StartNarrativeRoutine());
    }

    IEnumerator StartNarrativeRoutine()
    {
        while (!introPrepared)
        {
            yield return null;
        }

        introPlayer.time = 0;
        introPlayer.Play();

        yield return VideoPlayerCacheUtility.WaitForFirstVisibleFrame(introPlayer);
        introRenderer.enabled = true;

        Debug.Log("Intro Adolfo iniciada");
    }

    void OnIntroFinished(VideoPlayer vp)
    {
        StartCoroutine(IntroToPuzzleRoutine());
    }

    IEnumerator IntroToPuzzleRoutine()
    {
        yield return null;

        introRenderer.enabled = false;
        introPlayer.Stop();

        portraitTransition.PlayTransition();
    }

    IEnumerator FadeOutPuzzleAndShowFinal()
    {
        float t = 0;

        while (t < puzzleFadeDuration)
        {
            t += Time.deltaTime;

            puzzleCanvasGroup.alpha =
                Mathf.Lerp(1, 0, t / puzzleFadeDuration);

            yield return null;
        }

        puzzleCanvasGroup.alpha = 0;

        puzzleGame.SetActive(false);

        NarrativeFlowUI.ExitMinigameToNarrative();

        while (!finalPrepared)
        {
            yield return null;
        }

        finalPlayer.time = 0;
        finalPlayer.Play();

        yield return VideoPlayerCacheUtility.WaitForFirstVisibleFrame(finalPlayer);
        float frameWait = 0f;
        while ((finalPlayer.texture == null || finalPlayer.frame < 0) && frameWait < 8f)
        {
            frameWait += Time.unscaledDeltaTime;
            yield return null;
        }
        // The utility has a timeout: never reveal the material's white fallback texture.
        if (finalPlayer.texture == null || finalPlayer.frame < 0)
        {
            Debug.LogError("Adolfo: el video final no produjo una imagen; se mantiene oculto.");
            yield break;
        }
        finalRenderer.enabled = true;
    }

    public void StartFinalNarrative()
    {
        if (!finalPreparationStarted)
        {
            finalPreparationStarted = true;
            StartCoroutine(VideoPlayerCacheUtility.PrepareFromFirebaseOrCurrentUrl(finalPlayer, finalFirebasePath));
        }
        StartCoroutine(FadeOutPuzzleAndShowFinal());
    }

    void OnFinalFinished(VideoPlayer vp)
    {
        finalRenderer.enabled = false;
        finalPlayer.Stop();

        if (ExplorationManager.Instance != null)
        {
            ExplorationManager.Instance.RegisterMinigameCompleted(
                ExplorationIds.TIO_ADOLFO
            );
        }

        Debug.Log("Guardian Adolfo completado");

        NarrativeFlowUI.ReturnToMemories(ExplorationIds.TIO_ADOLFO);
}

    public void ResetNarrativeState()
    {
        StopAllCoroutines();
        if (portraitTransition != null) portraitTransition.CancelTransition();
        // Hide surfaces before Stop releases their decoded textures.
        if (introRenderer != null) introRenderer.enabled = false;
        if (finalRenderer != null) finalRenderer.enabled = false;
        if (introPlayer != null) introPlayer.Stop();
        if (finalPlayer != null) finalPlayer.Stop();
        if (puzzleGame != null) puzzleGame.SetActive(false);
        if (puzzleCanvasGroup != null)
        {
            puzzleCanvasGroup.alpha = 1f;
            puzzleCanvasGroup.interactable = true;
            puzzleCanvasGroup.blocksRaycasts = true;
        }
        introPrepared = false;
        introPreparationStarted = false;
        finalPrepared = false;
        finalPreparationStarted = false;
        alreadyStarted = false;
    }

    private void OnDisable() { ResetNarrativeState(); }

    private void OnDestroy()
    {
        if (introMaterial != null) Destroy(introMaterial);
        if (finalMaterial != null) Destroy(finalMaterial);
        if (introPlayer != null)
        {
            introPlayer.loopPointReached -= OnIntroFinished;
            introPlayer.prepareCompleted -= OnIntroPrepared;
        }
        if (finalPlayer != null)
        {
            finalPlayer.loopPointReached -= OnFinalFinished;
            finalPlayer.prepareCompleted -= OnFinalPrepared;
        }
    }
    private void ReleaseTargetVisuals()
    {
        TargetUIBlocker blocker = GetComponentInParent<TargetUIBlocker>();
        if (blocker != null)
            blocker.StopHidingTargetVisuals();
    }
}

