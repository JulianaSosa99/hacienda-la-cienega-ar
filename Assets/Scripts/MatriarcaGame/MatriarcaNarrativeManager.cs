using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class MatriarcaNarrativeManager : MonoBehaviour, INarrativeManager, INarrativeResettable
{
    [Header("Narrative UI")]
    public CanvasGroup narrativePanel;

    [Header("Intro")]
    [SerializeField] private GameObject introQuad;
    [SerializeField] private VideoPlayer introPlayer;

    [Header("Game")]
    [SerializeField] private GameObject matriarcaGame;

    [Header("Final")]
    [SerializeField] private GameObject finalQuad;
    [SerializeField] private VideoPlayer finalPlayer;

    private bool alreadyStarted = false;
    private bool gameCompleted = false;

    private bool introPrepared = false;
    private bool finalPrepared = false;

    private Renderer introRenderer;
    private Renderer finalRenderer;

    private void Start()
    {
        HideNarrativePanelImmediate();

        introRenderer = introQuad.GetComponent<Renderer>();
        finalRenderer = finalQuad.GetComponent<Renderer>();

        // NO desactivar GameObjects
        introRenderer.enabled = false;
        finalRenderer.enabled = false;

        if (matriarcaGame != null)
            matriarcaGame.SetActive(false);

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

        Debug.Log("MatriarcaNarrativeManager iniciado");
    }

    private void OnIntroPrepared(VideoPlayer vp)
    {
        introPrepared = true;
        Debug.Log("Intro Matriarca preparada");
    }

    private void OnFinalPrepared(VideoPlayer vp)
    {
        finalPrepared = true;
        Debug.Log("Final Matriarca preparado");
    }

    public void ShowNarrativePanel()
    {
        if (alreadyStarted)
            return;

        Debug.Log("SHOW PANEL MATRIARCA");

        NarrativeFlowUI.ShowDiscoveryPrompt(narrativePanel);
    }

    public void HideNarrativePanel()
    {
        if (alreadyStarted)
            return;

        HideNarrativePanelImmediate();
    }

    private void HideNarrativePanelImmediate()
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

        alreadyStarted = true;
        ReleaseTargetVisuals();
        AppLog.Info("GUARDIAN_START", "Inicia La Gran Matriarca");
        HideNarrativePanelImmediate();

        if (ExplorationManager.Instance != null)
        {
            ExplorationManager.Instance.RegisterGuardianScanned(
                ExplorationIds.GRAN_MATRIARCA
            );
        }

        NarrativeFlowUI.StartNarrative();

        StartCoroutine(StartNarrativeRoutine());
    }

    private IEnumerator StartNarrativeRoutine()
    {
        while (!introPrepared)
        {
            yield return null;
        }

        introPlayer.time = 0;
        introPlayer.Play();

        yield return VideoPlayerCacheUtility.WaitForFirstVisibleFrame(introPlayer);
        introRenderer.enabled = true;

        Debug.Log("Intro Matriarca iniciada");
    }

    private void OnIntroFinished(VideoPlayer vp)
    {
        StartCoroutine(FinishIntroRoutine());
    }

    private IEnumerator FinishIntroRoutine()
    {
        yield return null;

        introPlayer.Stop();

        introRenderer.enabled = false;

        if (matriarcaGame != null)
        {
            NarrativeFlowUI.EnterMinigame(matriarcaGame);
            Debug.Log("MatriarcaGame activo: " + matriarcaGame.activeInHierarchy);
        }
    }

    public void OnGameCompleted()
    {
        if (gameCompleted)
            return;

        gameCompleted = true;

        StartCoroutine(PlayFinalRoutine());
    }

    private IEnumerator PlayFinalRoutine()
    {
        if (matriarcaGame != null)
            matriarcaGame.SetActive(false);

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

    private void OnFinalFinished(VideoPlayer vp)
    {
        finalPlayer.Stop();

        finalRenderer.enabled = false;

        if (ExplorationManager.Instance != null)
        {
            ExplorationManager.Instance.RegisterMinigameCompleted(
                ExplorationIds.GRAN_MATRIARCA
            );
        }

        Debug.Log("Narrativa de la Matriarca finalizada.");

        NarrativeFlowUI.ReturnToMemories(ExplorationIds.GRAN_MATRIARCA);
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






