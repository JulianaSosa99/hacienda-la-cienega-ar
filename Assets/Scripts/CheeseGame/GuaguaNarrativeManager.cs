using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class GuaguaNarrativeManager : MonoBehaviour, INarrativeManager, INarrativeResettable
{
    [Header("Narrative UI")]
    public CanvasGroup narrativePanel;

    [Header("Intro")]
    public GameObject introQuad;
    public VideoPlayer introPlayer;

    [Header("Guagua")]
    public GameObject finalQuad;
    public VideoPlayer finalPlayer;

    [Header("Minijuego")]
    public GameObject guaguaGame;

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

        // NO apagar los quads
        introRenderer.enabled = false;
        finalRenderer.enabled = false;

        guaguaGame.SetActive(false);

        introPlayer.loopPointReached += OnIntroFinished;
        finalPlayer.loopPointReached += OnFinalFinished;

        introPlayer.prepareCompleted += OnIntroPrepared;
        finalPlayer.prepareCompleted += OnFinalPrepared;

        introPlayer.skipOnDrop = false;
        introPlayer.waitForFirstFrame = true;

        finalPlayer.skipOnDrop = false;
        finalPlayer.waitForFirstFrame = true;

        introPlayer.Prepare();
        finalPlayer.Prepare();

        Debug.Log("GuaguaNarrativeManager iniciado");
    }

    void OnIntroPrepared(VideoPlayer vp)
    {
        introPrepared = true;

        Debug.Log("Intro Guagua preparada");
    }

    void OnFinalPrepared(VideoPlayer vp)
    {
        finalPrepared = true;

        Debug.Log("Video Guagua preparado");
    }

    public void ShowNarrativePanel()
    {
        if (alreadyStarted)
            return;

        Debug.Log("SHOW PANEL GUAGUA");

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
        AppLog.Info("GUARDIAN_START", "Inicia La Guagua Mayor");
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
                ExplorationIds.GUAGUA_MAYOR
            );
        }

        introPlayer.time = 0;
        introPlayer.Play();

        yield return VideoPlayerCacheUtility.WaitForFirstVisibleFrame(introPlayer);
        introRenderer.enabled = true;

        Debug.Log("Intro Guagua iniciada");
    }

    void OnIntroFinished(VideoPlayer vp)
    {
        StartCoroutine(IntroToGuaguaRoutine());
    }

    IEnumerator IntroToGuaguaRoutine()
    {
        introPlayer.Stop();

        introRenderer.enabled = false;

        while (!finalPrepared)
        {
            yield return null;
        }

        finalPlayer.time = 0;
        finalPlayer.Play();

        yield return VideoPlayerCacheUtility.WaitForFirstVisibleFrame(finalPlayer);
        finalRenderer.enabled = true;

        Debug.Log("Video Guagua iniciado");
    }

    void OnFinalFinished(VideoPlayer vp)
    {
        finalPlayer.Stop();

        finalRenderer.enabled = false;

        NarrativeFlowUI.EnterMinigame(guaguaGame);

        Debug.Log("Minijuego Guagua iniciado");
    }

    public void OnGameCompleted()
    {
        if (ExplorationManager.Instance != null)
        {
            ExplorationManager.Instance.RegisterMinigameCompleted(
                ExplorationIds.GUAGUA_MAYOR
            );
        }

        NarrativeFlowUI.ExitMinigameToNarrative();

        Debug.Log("Guardian Guagua completado");

        NarrativeFlowUI.ReturnToMemories(ExplorationIds.GUAGUA_MAYOR);
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





