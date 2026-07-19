using System.Collections;
using UnityEngine;
using Vuforia;

public class TargetUIBlocker : MonoBehaviour
{
    [Header("Narrative Manager")]
    public MonoBehaviour narrativeManagerBehaviour;

    [Header("Target Visual")]
    [SerializeField] private bool hideChildRenderersOnPrompt = true;

    private INarrativeManager narrativeManager;
    private bool alreadyTriggered = false;
    private DefaultObserverEventHandler observerHandler;
    private Coroutine hideTargetVisualsRoutine;
    private bool keepTargetVisualsHidden;
    private static TargetUIBlocker activeBlocker;

    private void Awake()
    {
        observerHandler = GetComponent<DefaultObserverEventHandler>();
        narrativeManager = narrativeManagerBehaviour as INarrativeManager;

        if (narrativeManager == null)
        {
            UnityEngine.Debug.LogError("El manager no implementa INarrativeManager");
        }
    }

    private void LateUpdate()
    {
        if (keepTargetVisualsHidden)
            HideTargetVisualsOnce();
    }

    public void OnTargetFound()
    {
        if (alreadyTriggered)
            return;

        if (activeBlocker != null && activeBlocker != this)
            return;

        activeBlocker = this;
        alreadyTriggered = true;
        UnityEngine.Debug.Log("TARGET DETECTADO");

        StartHidingTargetVisuals();

        if (observerHandler != null)
        {
            observerHandler.enabled = false;
        }

        narrativeManager?.ShowNarrativePanel();
    }

    public void ResetForScanning()
    {
        alreadyTriggered = false;

        if (activeBlocker == this)
            activeBlocker = null;

        INarrativeResettable resettable = narrativeManagerBehaviour as INarrativeResettable;
        if (resettable != null)
            resettable.ResetNarrativeState();

        StopHidingTargetVisuals();
        HideTargetVisualsOnce();

        if (observerHandler != null)
            observerHandler.enabled = true;
    }

    public void StopHidingTargetVisuals()
    {
        keepTargetVisualsHidden = false;

        if (hideTargetVisualsRoutine != null)
        {
            StopCoroutine(hideTargetVisualsRoutine);
            hideTargetVisualsRoutine = null;
        }
    }

    private void StartHidingTargetVisuals()
    {
        if (!hideChildRenderersOnPrompt)
            return;

        keepTargetVisualsHidden = true;
        HideTargetVisualsOnce();

        if (hideTargetVisualsRoutine != null)
            StopCoroutine(hideTargetVisualsRoutine);

        hideTargetVisualsRoutine = StartCoroutine(HideTargetVisualsForFrames());
    }

    private IEnumerator HideTargetVisualsForFrames()
    {
        for (int i = 0; i < 8; i++)
        {
            HideTargetVisualsOnce();
            yield return null;
        }

        hideTargetVisualsRoutine = null;
    }

    private void HideTargetVisualsOnce()
    {
        if (!hideChildRenderersOnPrompt)
            return;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].enabled = false;
        }
    }

    public static void ResetAllForScanning()
    {
        activeBlocker = null;

        TargetUIBlocker[] blockers = FindObjectsOfType<TargetUIBlocker>(true);
        for (int i = 0; i < blockers.Length; i++)
        {
            blockers[i].ResetForScanning();
        }
    }
}
