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
    private bool finalPrepared = false;

    private Renderer introRenderer;
    private Renderer finalRenderer;

    void Start()
    {
        HideNarrativePanelImmediate();

        introRenderer = introQuad.GetComponent<Renderer>();
        finalRenderer = finalQuad.GetComponent<Renderer>();

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

        StartCoroutine(VideoPlayerCacheUtility.PrepareFromFirebaseOrCurrentUrl(introPlayer, introFirebasePath));
        StartCoroutine(VideoPlayerCacheUtility.PrepareFromFirebaseOrCurrentUrl(finalPlayer, finalFirebasePath));
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

        introPlayer.Stop();
        introRenderer.enabled = false;

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
        finalRenderer.enabled = true;
    }

    public void StartFinalNarrative()
    {
        StartCoroutine(FadeOutPuzzleAndShowFinal());
    }

    void OnFinalFinished(VideoPlayer vp)
    {
        finalPlayer.Stop();
        finalRenderer.enabled = false;

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
        alreadyStarted = false;
    }
    private void ReleaseTargetVisuals()
    {
        TargetUIBlocker blocker = GetComponentInParent<TargetUIBlocker>();
        if (blocker != null)
            blocker.StopHidingTargetVisuals();
    }
}

