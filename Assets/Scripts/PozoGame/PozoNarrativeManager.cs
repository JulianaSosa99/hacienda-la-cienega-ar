using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class PozoNarrativeManager : MonoBehaviour, INarrativeManager, INarrativeResettable
{
    [Header("Narrative UI")]
    public CanvasGroup narrativePanel;

    [Header("Firebase Videos")]
    [Tooltip("Storage path. Example: videos/pozo/matriarca-intro-pozo.mp4")]
    public string introFirebasePath;
    [Tooltip("Storage path. Example: videos/pozo/Nino_Pozo_Final_2.mp4")]
    public string ninoFirebasePath;

    [Header("Intro")]
    public GameObject introQuad;
    public VideoPlayer introPlayer;

    [Header("Nino")]
    public GameObject ninoQuad;
    public VideoPlayer ninoPlayer;

    [Header("Pozo Game")]
    public GameObject pozoGame;

    [Header("Gameplay UI")]
    public CanvasGroup backgroundDimGroup;
    public CanvasGroup riddlePanelGroup;

    [Header("Win Flash")]
    public Image winFlash;

    [Header("Ambient")]
    public AudioSource ambientSource;
    public AudioClip ambientLoop;

    [Header("Transition")]
    public float fadeDuration = 0.35f;

    private bool alreadyStarted = false;

    private bool introPrepared = false;
    private bool ninoPrepared = false;

    private Renderer introRenderer;
    private Renderer ninoRenderer;

    // =========================================
    // START
    // =========================================

    void Start()
    {
        HideNarrativePanelImmediate();

        introRenderer = introQuad.GetComponent<Renderer>();
        ninoRenderer = ninoQuad.GetComponent<Renderer>();

        // OCULTAR SOLO RENDERERS
        introRenderer.enabled = false;
        ninoRenderer.enabled = false;

        pozoGame.SetActive(false);

        // SOLO VISUAL
        SetCanvasGroup(backgroundDimGroup, 0);
        SetCanvasGroup(riddlePanelGroup, 0);

        if (winFlash != null)
        {
            Color c = winFlash.color;
            c.a = 0;
            winFlash.color = c;
        }

        // CONFIG VIDEO PLAYER
        if (introPlayer != null)
        {
            introPlayer.skipOnDrop = false;
            introPlayer.waitForFirstFrame = true;
        }

        if (ninoPlayer != null)
        {
            ninoPlayer.skipOnDrop = false;
            ninoPlayer.waitForFirstFrame = true;
        }

        // EVENTOS
        introPlayer.loopPointReached += OnIntroFinished;
        ninoPlayer.loopPointReached += OnNinoFinished;

        introPlayer.prepareCompleted += OnIntroPrepared;
        ninoPlayer.prepareCompleted += OnNinoPrepared;

        // PREPARAR VIDEOS DESDE START
        StartCoroutine(VideoPlayerCacheUtility.PrepareFromFirebaseOrCurrentUrl(introPlayer, introFirebasePath));
        StartCoroutine(VideoPlayerCacheUtility.PrepareFromFirebaseOrCurrentUrl(ninoPlayer, ninoFirebasePath));

        Debug.Log("PozoNarrativeManager iniciado");
    }

    // =========================================
    // VIDEOS PREPARADOS
    // =========================================

    void OnIntroPrepared(VideoPlayer vp)
    {
        introPrepared = true;

        Debug.Log("Intro Pozo preparada");
    }

    void OnNinoPrepared(VideoPlayer vp)
    {
        ninoPrepared = true;

        Debug.Log("Video Nino preparado");
    }

    // =========================================
    // MOSTRAR PANEL
    // =========================================

    public void ShowNarrativePanel()
    {
        if (alreadyStarted)
            return;

        NarrativeFlowUI.ShowDiscoveryPrompt(narrativePanel);
    }

    // =========================================
    // OCULTAR PANEL
    // =========================================

    public void HideNarrativePanel()
    {
        if (alreadyStarted)
            return;

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
    // BOTON START
    // =========================================

    public void StartNarrative()
    {
        if (alreadyStarted)
            return;

        alreadyStarted = true;
        ReleaseTargetVisuals();
        AppLog.Info("GUARDIAN_START", "Inicia El Nino del Pozo");
        HideNarrativePanelImmediate();

        if (ExplorationManager.Instance != null)
        {
            ExplorationManager.Instance.RegisterGuardianScanned(
                ExplorationIds.NINO_POZO
            );
        }

        NarrativeFlowUI.StartNarrative();

        StartCoroutine(StartNarrativeRoutine());
    }

    IEnumerator StartNarrativeRoutine()
    {
        // ESPERAR VIDEO PREPARADO
        while (!introPrepared)
        {
            yield return null;
        }

        introPlayer.time = 0;
        introPlayer.Play();

        yield return VideoPlayerCacheUtility.WaitForFirstVisibleFrame(introPlayer);
        introRenderer.enabled = true;

        Debug.Log("Video intro pozo iniciado");
    }

    // =========================================
    // VIDEO INTRO TERMINA
    // =========================================

    void OnIntroFinished(VideoPlayer vp)
    {
        StartCoroutine(HandleIntroFinish());
    }

    IEnumerator HandleIntroFinish()
    {
        introPlayer.Stop();

        introRenderer.enabled = false;

        // ESPERAR VIDEO NINO PREPARADO
        while (!ninoPrepared)
        {
            yield return null;
        }

        ninoPlayer.time = 0;
        ninoPlayer.Play();

        yield return VideoPlayerCacheUtility.WaitForFirstVisibleFrame(ninoPlayer);
        ninoRenderer.enabled = true;

        Debug.Log("Video nino iniciado");
    }

    // =========================================
    // VIDEO NINO TERMINA
    // =========================================

    void OnNinoFinished(VideoPlayer vp)
    {
        StartCoroutine(HandleNinoFinish());
    }

    IEnumerator HandleNinoFinish()
    {
        ninoPlayer.Stop();

        yield return null;

        ninoRenderer.enabled = false;

        NarrativeFlowUI.EnterMinigame(pozoGame);
        Debug.Log("PozoGame activo: " + pozoGame.activeInHierarchy);

        StartCoroutine(StartPozoSequence());
    }

    // =========================================
    // SECUENCIA GAMEPLAY
    // =========================================

    IEnumerator StartPozoSequence()
    {
        StartCoroutine(FadeCanvasGroup(backgroundDimGroup, 0, 1));
        StartCoroutine(FadeCanvasGroup(riddlePanelGroup, 0, 1));

        yield return new WaitForSeconds(fadeDuration);

        StartAmbient();

        Debug.Log("Gameplay Pozo iniciado");
    }

    // =========================================
    // SONIDO AMBIENTE
    // =========================================

    void StartAmbient()
    {
        if (ambientSource != null && ambientLoop != null)
        {
            ambientSource.clip = ambientLoop;
            ambientSource.loop = true;
            ambientSource.Play();
        }
    }

    // =========================================
    // EFECTO VICTORIA
    // =========================================

    public void PlayWinFlash()
    {
        StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        if (winFlash == null)
            yield break;

        Color c = winFlash.color;

        float t = 0;

        // FADE IN
        while (t < 1)
        {
            t += Time.deltaTime * 2f;

            c.a = Mathf.Lerp(0, 0.6f, t);

            winFlash.color = c;

            yield return null;
        }

        t = 0;

        // FADE OUT
        while (t < 1)
        {
            t += Time.deltaTime * 1.5f;

            c.a = Mathf.Lerp(0.6f, 0, t);

            winFlash.color = c;

            yield return null;
        }
    }

    // =========================================
    // FADE CANVAS GROUP
    // =========================================

    IEnumerator FadeCanvasGroup(CanvasGroup group, float start, float end)
    {
        if (group == null)
            yield break;

        float time = 0;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;

            float value = Mathf.Lerp(start, end, time / fadeDuration);

            group.alpha = value;

            yield return null;
        }

        group.alpha = end;
    }

    // =========================================
    // SET CANVAS GROUP
    // =========================================

    void SetCanvasGroup(CanvasGroup group, float alpha)
    {
        if (group == null)
            return;

        group.alpha = alpha;
    }

    // =========================================
    // FINAL MINIJUEGO
    // =========================================

    public void OnPozoCompleted()
    {
        if (ExplorationManager.Instance != null)
        {
            ExplorationManager.Instance.RegisterMinigameCompleted(
                ExplorationIds.NINO_POZO
            );
        }

        NarrativeFlowUI.ExitMinigameToNarrative();

        PlayWinFlash();

        Debug.Log("Guardian Pozo completado");

        NarrativeFlowUI.ReturnToMemories(ExplorationIds.NINO_POZO);
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

