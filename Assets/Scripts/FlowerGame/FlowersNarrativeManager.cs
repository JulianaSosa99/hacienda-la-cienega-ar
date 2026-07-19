using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class FlowersNarrativeManager : MonoBehaviour, INarrativeManager, INarrativeResettable
{
    [Header("Narrative UI")]
    public CanvasGroup narrativePanel;

    [Header("Firebase Videos")]
    [Tooltip("Storage path. Example: videos/novios/matriarca-video-novios.mp4")]
    public string introFirebasePath;
    [Tooltip("Storage path. Example: videos/novios/novios_1080p.mp4")]
    public string noviosFirebasePath;

    [Header("Intro")]
    public GameObject introQuad;
    public VideoPlayer introPlayer;

    [Header("Novios")]
    public GameObject noviosQuad;
    public VideoPlayer noviosPlayer;

    [Header("Minijuego")]
    public GameObject flowersGame;

    private bool alreadyStarted = false;

    private bool introPrepared = false;
    private bool noviosPrepared = false;

    private Renderer introRenderer;
    private Renderer noviosRenderer;

    void Start()
    {
        HideNarrativePanelImmediate();

        introRenderer = introQuad.GetComponent<Renderer>();
        noviosRenderer = noviosQuad.GetComponent<Renderer>();

        // NO desactivar los quads
        introRenderer.enabled = false;
        noviosRenderer.enabled = false;

        flowersGame.SetActive(false);

        introPlayer.skipOnDrop = false;
        introPlayer.waitForFirstFrame = true;

        noviosPlayer.skipOnDrop = false;
        noviosPlayer.waitForFirstFrame = true;

        introPlayer.loopPointReached += OnIntroFinished;
        noviosPlayer.loopPointReached += OnNoviosFinished;

        introPlayer.prepareCompleted += OnIntroPrepared;
        noviosPlayer.prepareCompleted += OnNoviosPrepared;

        StartCoroutine(VideoPlayerCacheUtility.PrepareFromFirebaseOrCurrentUrl(introPlayer, introFirebasePath));
        StartCoroutine(VideoPlayerCacheUtility.PrepareFromFirebaseOrCurrentUrl(noviosPlayer, noviosFirebasePath));

        Debug.Log("FlowersNarrativeManager iniciado");
    }

    // =========================================
    // VIDEOS PREPARADOS
    // =========================================

    void OnIntroPrepared(VideoPlayer vp)
    {
        introPrepared = true;

        Debug.Log("Intro Flowers preparada");
    }

    void OnNoviosPrepared(VideoPlayer vp)
    {
        noviosPrepared = true;

        Debug.Log("Video Novios preparado");
    }

    // =========================================
    // MOSTRAR PANEL
    // =========================================

    public void ShowNarrativePanel()
    {
        if (alreadyStarted)
            return;

        Debug.Log("SHOW PANEL");

        NarrativeFlowUI.ShowDiscoveryPrompt(narrativePanel);
    }

    // =========================================
    // OCULTAR PANEL
    // =========================================

    public void HideNarrativePanel()
    {
        if (alreadyStarted)
            return;

        Debug.Log("HIDE PANEL");

        narrativePanel.alpha = 0;
        narrativePanel.interactable = false;
        narrativePanel.blocksRaycasts = false;

        narrativePanel.gameObject.SetActive(false);
    }

    // =========================================
    // OCULTAR INMEDIATO
    // =========================================

    void HideNarrativePanelImmediate()
    {
        narrativePanel.alpha = 0;
        narrativePanel.interactable = false;
        narrativePanel.blocksRaycasts = false;

        narrativePanel.gameObject.SetActive(false);
    }

    // =========================================
    // BOTON
    // =========================================

    public void StartNarrative()
    {
        if (alreadyStarted)
            return;

        alreadyStarted = true;
        ReleaseTargetVisuals();
        AppLog.Info("GUARDIAN_START", "Inicia Los Novios");
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
                ExplorationIds.LOS_NOVIOS
            );
        }

        introPlayer.time = 0;
        introPlayer.Play();

        yield return VideoPlayerCacheUtility.WaitForFirstVisibleFrame(introPlayer);
        introRenderer.enabled = true;

        Debug.Log("Video intro flowers iniciado");
    }

    // =========================================
    // VIDEO INTRO
    // =========================================

    void OnIntroFinished(VideoPlayer vp)
    {
        StartCoroutine(IntroToNoviosRoutine());
    }

    IEnumerator IntroToNoviosRoutine()
    {
        introPlayer.Stop();

        introRenderer.enabled = false;

        while (!noviosPrepared)
        {
            yield return null;
        }

        noviosPlayer.time = 0;
        noviosPlayer.Play();

        yield return VideoPlayerCacheUtility.WaitForFirstVisibleFrame(noviosPlayer);
        noviosRenderer.enabled = true;

        Debug.Log("Video novios iniciado");
    }

    // =========================================
    // VIDEO NOVIOS
    // =========================================

    void OnNoviosFinished(VideoPlayer vp)
    {
        noviosPlayer.Stop();

        noviosRenderer.enabled = false;

        NarrativeFlowUI.EnterMinigame(flowersGame);
        Debug.Log("FlowersGame activo: " + flowersGame.activeInHierarchy);

        Debug.Log("Minijuego Flowers iniciado");
    }

    // =========================================
    // FINAL MINIJUEGO
    // =========================================

    public void OnFlowersCompleted()
    {
        if (ExplorationManager.Instance != null)
        {
            ExplorationManager.Instance.RegisterMinigameCompleted(
                ExplorationIds.LOS_NOVIOS
            );
        }

        NarrativeFlowUI.ExitMinigameToNarrative();

        Debug.Log("Guardian Flores completado");

        NarrativeFlowUI.ReturnToMemories(ExplorationIds.LOS_NOVIOS);
    }

    // =========================================
    // CLEANUP
    // =========================================
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

        if (noviosPlayer != null)
        {
            noviosPlayer.loopPointReached -= OnNoviosFinished;
            noviosPlayer.prepareCompleted -= OnNoviosPrepared;
        }
    }

    public void ResetNarrativeState()
    {
        alreadyStarted = false;
    }
}








