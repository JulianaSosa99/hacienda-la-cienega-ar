using UnityEngine;
using UnityEngine.Video;
using System.Collections;

public class JuanLassoNarrativeManager : MonoBehaviour, INarrativeManager, INarrativeResettable
{
    [Header("Narrative UI")]
    public CanvasGroup narrativePanel;

    [Header("Firebase Videos")]
    [Tooltip("Storage path. Example: videos/juan-lasso/Matriarca_Presenta_JuanMLasso (1).mp4")]
    public string introFirebasePath;
    [Tooltip("Storage path. Example: videos/juan-lasso/Dialogo_JuanM_Lasso_ultimo.mp4")]
    public string finalFirebasePath;

    [Header("Intro")]
    public GameObject introQuad;
    public VideoPlayer introPlayer;

    [Header("Smoke Game")]
    public GameObject smokeGame;

    [Header("Final")]
    public GameObject finalQuad;
    public VideoPlayer finalPlayer;

    private bool alreadyStarted = false;

    private bool introPrepared = false;
    private bool finalPrepared = false;

    private Renderer introRenderer;
    private Renderer finalRenderer;
    private Renderer[] introRenderers;
    private Renderer[] finalRenderers;

    void Start()
    {
        HideNarrativePanelImmediate();

        introRenderer = introQuad.GetComponent<Renderer>();
        finalRenderer = finalQuad.GetComponent<Renderer>();
        introRenderers = introQuad.GetComponentsInChildren<Renderer>(true);
        finalRenderers = finalQuad.GetComponentsInChildren<Renderer>(true);

        GiveRendererUniqueMaterial(introRenderer);
        GiveRendererUniqueMaterial(finalRenderer);

        SetRenderersVisible(introRenderers, false);
        SetRenderersVisible(finalRenderers, false);

        smokeGame.SetActive(false);

        introPlayer.loopPointReached += OnIntroFinished;
        finalPlayer.loopPointReached += OnFinalFinished;

        introPlayer.prepareCompleted += OnIntroPrepared;
        finalPlayer.prepareCompleted += OnFinalPrepared;

        StartCoroutine(VideoPlayerCacheUtility.PrepareFromFirebaseOrCurrentUrl(introPlayer, introFirebasePath));
        StartCoroutine(VideoPlayerCacheUtility.PrepareFromFirebaseOrCurrentUrl(finalPlayer, finalFirebasePath));
    }

    private void GiveRendererUniqueMaterial(Renderer renderer)
    {
        if (renderer == null || renderer.sharedMaterial == null)
        {
            return;
        }

        renderer.material = new Material(renderer.sharedMaterial);
    }

    private void SetRenderersVisible(Renderer[] renderers, bool visible)
    {
        if (renderers == null)
        {
            return;
        }

        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.enabled = visible;
            }
        }
    }

    void OnIntroPrepared(VideoPlayer vp)
    {
        introPrepared = true;
        AppLog.Info("JUAN_LASSO", "Intro Juan preparada");
    }

    void OnFinalPrepared(VideoPlayer vp)
    {
        finalPrepared = true;
        AppLog.Info("JUAN_LASSO", "Final Juan preparado");
    }

    public void ShowNarrativePanel()
    {
        if (alreadyStarted)
            return;

        AppLog.Info("JUAN_LASSO", "SHOW PANEL JUAN LASSO");
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
        AppLog.Info("GUARDIAN_START", "Inicia Juan Manuel Lasso");
        HideNarrativePanelImmediate();

        if (ExplorationManager.Instance != null)
        {
            ExplorationManager.Instance.RegisterGuardianScanned(
                ExplorationIds.JUAN_MANUEL_LASSO
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

        StopFinalVideo();
        SetRenderersVisible(finalRenderers, false);

        introPlayer.Stop();
        introPlayer.time = 0;
        introPlayer.Play();

        yield return VideoPlayerCacheUtility.WaitForFirstVisibleFrame(introPlayer);
        SetRenderersVisible(introRenderers, true);

        AppLog.Info("JUAN_LASSO", "Intro Juan Lasso iniciada");
    }

    void OnIntroFinished(VideoPlayer vp)
    {
        StartCoroutine(FinishIntroRoutine());
    }

    IEnumerator FinishIntroRoutine()
    {
        yield return new WaitForSeconds(1f);

        StopIntroVideo();
        SetRenderersVisible(introRenderers, false);

        NarrativeFlowUI.EnterMinigame(smokeGame);
        AppLog.Info("JUAN_LASSO", "SmokeGame activo: " + smokeGame.activeInHierarchy);
    }

    public void StartFinalNarrative()
    {
        StartCoroutine(FinalNarrativeRoutine());
    }

    IEnumerator FinalNarrativeRoutine()
    {
        yield return null;

        smokeGame.SetActive(false);

        StopIntroVideo();
        SetRenderersVisible(introRenderers, false);
        SetRenderersVisible(finalRenderers, false);

        NarrativeFlowUI.ExitMinigameToNarrative();

        while (!finalPrepared)
        {
            yield return null;
        }

        finalPlayer.Stop();
        finalPlayer.time = 0;
        finalPlayer.Play();

        yield return VideoPlayerCacheUtility.WaitForFirstVisibleFrame(finalPlayer);
        SetRenderersVisible(finalRenderers, true);

        AppLog.Info("JUAN_LASSO", "Final Juan Lasso iniciado");
    }

    void OnFinalFinished(VideoPlayer vp)
    {
        StopFinalVideo();
        SetRenderersVisible(finalRenderers, false);

        if (ExplorationManager.Instance != null)
        {
            ExplorationManager.Instance.RegisterMinigameCompleted(
                ExplorationIds.JUAN_MANUEL_LASSO
            );
        }

        AppLog.Info("JUAN_LASSO", "Guardian Juan Manuel Lasso completado");

        NarrativeFlowUI.ReturnToMemories(ExplorationIds.JUAN_MANUEL_LASSO);
}

    private void StopIntroVideo()
    {
        if (introPlayer != null)
        {
            introPlayer.Stop();
            introPlayer.time = 0;
        }
    }

    private void StopFinalVideo()
    {
        if (finalPlayer != null)
        {
            finalPlayer.Stop();
            finalPlayer.time = 0;
        }
    }

    public void ResetNarrativeState()
    {
        alreadyStarted = false;
        StopIntroVideo();
        StopFinalVideo();
        SetRenderersVisible(introRenderers, false);
        SetRenderersVisible(finalRenderers, false);
        if (smokeGame != null)
        {
            smokeGame.SetActive(false);
        }
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
}

