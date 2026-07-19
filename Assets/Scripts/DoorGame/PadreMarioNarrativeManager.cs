using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class PadreMarioNarrativeManager : MonoBehaviour, INarrativeManager, INarrativeResettable
{
    [Header("Narrative UI")]
    public CanvasGroup narrativePanel;

    [Header("Intro")]
    public GameObject introQuad;
    public VideoPlayer introPlayer;

    [Header("Game")]
    public GameObject doorGame;

    [Header("Final")]
    public GameObject finalQuad;
    public VideoPlayer finalPlayer;

    private bool alreadyStarted = false;
    private bool gameCompleted = false;

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

        doorGame.SetActive(false);

        introPlayer.skipOnDrop = false;
        introPlayer.waitForFirstFrame = true;

        finalPlayer.skipOnDrop = false;
        finalPlayer.waitForFirstFrame = true;

        introPlayer.loopPointReached += OnIntroFinished;
        finalPlayer.loopPointReached += OnFinalFinished;

        introPlayer.prepareCompleted += OnIntroPrepared;
        finalPlayer.prepareCompleted += OnFinalPrepared;

        introPlayer.Prepare();
        finalPlayer.Prepare();

        Debug.Log("PadreMarioNarrativeManager iniciado");
    }

    void OnIntroPrepared(VideoPlayer vp)
    {
        introPrepared = true;
    }

    void OnFinalPrepared(VideoPlayer vp)
    {
        finalPrepared = true;
    }

    public void ShowNarrativePanel()
    {
        if (alreadyStarted)
            return;

        NarrativeFlowUI.ShowDiscoveryPrompt(narrativePanel);
    }

    public void HideNarrativePanel()
    {
        if (alreadyStarted)
            return;

        narrativePanel.alpha = 0;
        narrativePanel.interactable = false;
        narrativePanel.blocksRaycasts = false;

        narrativePanel.gameObject.SetActive(false);
    }

    void HideNarrativePanelImmediate()
    {
        narrativePanel.alpha = 0;
        narrativePanel.interactable = false;
        narrativePanel.blocksRaycasts = false;

        narrativePanel.gameObject.SetActive(false);
    }

    public void StartNarrative()
    {
        if (alreadyStarted)
            return;

        alreadyStarted = true;
        ReleaseTargetVisuals();
        AppLog.Info("GUARDIAN_START", "Inicia Padre Mario Cicala");
        HideNarrativePanelImmediate();
        NarrativeFlowUI.StartNarrative();

        StartCoroutine(StartNarrativeRoutine());
    }

    IEnumerator StartNarrativeRoutine()
    {
        while (!introPrepared)
        {
            yield return null;
        }

        if (ExplorationManager.Instance != null)
        {
            ExplorationManager.Instance.RegisterGuardianScanned(
                ExplorationIds.PADRE_MARIO_CICALA
            );
        }

        introPlayer.time = 0;
        introPlayer.Play();

        yield return VideoPlayerCacheUtility.WaitForFirstVisibleFrame(introPlayer);
        introRenderer.enabled = true;

        Debug.Log("Intro Padre Mario iniciada");
    }

    void OnIntroFinished(VideoPlayer vp)
    {
        StartCoroutine(IntroToGameRoutine());
    }

    IEnumerator IntroToGameRoutine()
    {
        introPlayer.Stop();

        introRenderer.enabled = false;

        NarrativeFlowUI.EnterMinigame(doorGame);

        yield return null;
    }

    public void StartFinalNarrative()
    {
        if (gameCompleted)
            return;

        gameCompleted = true;

        StartCoroutine(PlayFinalRoutine());
    }

    IEnumerator PlayFinalRoutine()
    {
        doorGame.SetActive(false);

        NarrativeFlowUI.ExitMinigameToNarrative();

        while (!finalPrepared)
        {
            yield return null;
        }

        finalPlayer.time = 0;
        finalPlayer.Play();

        yield return VideoPlayerCacheUtility.WaitForFirstVisibleFrame(finalPlayer);
        finalRenderer.enabled = true;

        Debug.Log("Video final Padre Mario iniciado");
    }

    void OnFinalFinished(VideoPlayer vp)
    {
        finalPlayer.Stop();

        finalRenderer.enabled = false;

        if (ExplorationManager.Instance != null)
        {
            ExplorationManager.Instance.RegisterMinigameCompleted(
                ExplorationIds.PADRE_MARIO_CICALA
            );
        }

        Debug.Log("Guardian Padre Mario completado");

        NarrativeFlowUI.ReturnToMemories(ExplorationIds.PADRE_MARIO_CICALA);
}
    private void ReleaseTargetVisuals()
    {
        TargetUIBlocker blocker = GetComponentInParent<TargetUIBlocker>();
        if (blocker != null)
            blocker.StopHidingTargetVisuals();
    }


    private void OnDestroy()
    {
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

    public void ResetNarrativeState()
    {
        alreadyStarted = false;
    }
}





