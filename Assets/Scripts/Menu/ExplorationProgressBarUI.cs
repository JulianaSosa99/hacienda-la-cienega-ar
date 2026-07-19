using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExplorationProgressBarUI : MonoBehaviour
{
    public static ExplorationProgressBarUI Instance;

    [Header("UI")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Image progressFillImage;
    [SerializeField] private TMP_Text progressText;

    [Header("Animacion")]
    [SerializeField] private float animationDuration = 0.8f;
    [SerializeField] private float pulseScale = 1.12f;

    private Coroutine animationRoutine;
    private Vector3 defaultScale = Vector3.one;
    private int lastShownPercent = -1;
    private int pendingFrom = -1;
    private int pendingTo = -1;
    private bool pendingImportant;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        ResolveReferences();
        Hide();
    }

    private void OnEnable()
    {
        Subscribe();
        Refresh(false);
        StartCoroutine(RefreshWhenManagerIsReady());
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void Show()
    {
        ResolveReferences();

        gameObject.SetActive(true);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (pendingTo >= 0)
        {
            PlayAnimation(pendingFrom, pendingTo, pendingImportant);
            ClearPending();
            return;
        }

        Refresh(false);
        StartCoroutine(RefreshWhenManagerIsReady());
    }

    public void Hide()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void Refresh(bool animate)
    {
        if (ExplorationManager.Instance == null)
            return;

        int currentPercent =
            ExplorationManager.Instance.GetExplorationPercent();

        if (animate && lastShownPercent >= 0 && currentPercent > lastShownPercent)
            PlayAnimation(lastShownPercent, currentPercent, true);
        else
            SetProgress(currentPercent);
    }

    private void OnProgressChanged(
        int previousPercent,
        int currentPercent,
        bool important
    )
    {
        if (!IsVisible())
        {
            pendingFrom = previousPercent;
            pendingTo = currentPercent;
            pendingImportant = important;
            return;
        }

        PlayAnimation(previousPercent, currentPercent, important);
    }

    private void PlayAnimation(
        int previousPercent,
        int currentPercent,
        bool important
    )
    {
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        animationRoutine = StartCoroutine(
            AnimateProgress(previousPercent, currentPercent, important)
        );
    }

    private IEnumerator AnimateProgress(
        int previousPercent,
        int currentPercent,
        bool important
    )
    {
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(elapsed / animationDuration);

            float easedT =
                Mathf.SmoothStep(0f, 1f, t);

            SetProgress(
                Mathf.Lerp(previousPercent, currentPercent, easedT)
            );

            ApplyPulse(t, important);

            yield return null;
        }

        SetProgress(currentPercent);
        ResetScale();

        animationRoutine = null;
    }

    private void SetProgress(float percent)
    {
        percent = Mathf.Clamp(percent, 0f, 100f);

        if (progressSlider != null)
        {
            progressSlider.interactable = false;
            progressSlider.value = percent / 100f;
        }

        if (progressFillImage != null)
            progressFillImage.fillAmount = percent / 100f;

        if (progressText != null)
            progressText.text = Mathf.RoundToInt(percent) + "%";

        lastShownPercent = Mathf.RoundToInt(percent);
    }

    private void ApplyPulse(float t, bool important)
    {
        if (!important || progressSlider == null)
            return;

        float pulse =
            Mathf.Sin(t * Mathf.PI);

        progressSlider.transform.localScale =
            Vector3.Lerp(
                defaultScale,
                defaultScale * pulseScale,
                pulse
            );
    }

    private void ResetScale()
    {
        if (progressSlider == null)
            return;

        progressSlider.transform.localScale = defaultScale;
    }

    private bool IsVisible()
    {
        return gameObject.activeInHierarchy
            && canvasGroup != null
            && canvasGroup.alpha > 0.01f;
    }

    private void HideImmediate()
    {
        ResolveReferences();
        Hide();
    }

    private void ResolveReferences()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (progressSlider == null)
            progressSlider = GetComponentInChildren<Slider>(true);

        if (progressSlider != null)
            defaultScale = progressSlider.transform.localScale;

        SetGraphicRaycasts(false);
    }

    private void SetGraphicRaycasts(bool enabled)
    {
        Graphic[] graphics =
            GetComponentsInChildren<Graphic>(true);

        for (int i = 0; i < graphics.Length; i++)
        {
            graphics[i].raycastTarget = enabled;
        }
    }

    private void Subscribe()
    {
        if (ExplorationManager.Instance == null)
            return;

        ExplorationManager.Instance.ProgressChanged -= OnProgressChanged;
        ExplorationManager.Instance.ProgressChanged += OnProgressChanged;
    }

    private void Unsubscribe()
    {
        if (ExplorationManager.Instance == null)
            return;

        ExplorationManager.Instance.ProgressChanged -= OnProgressChanged;
    }

    private void ClearPending()
    {
        pendingFrom = -1;
        pendingTo = -1;
        pendingImportant = false;
    }

    private IEnumerator RefreshWhenManagerIsReady()
    {
        while (ExplorationManager.Instance == null)
            yield return null;

        Subscribe();
        Refresh(false);
    }
}
