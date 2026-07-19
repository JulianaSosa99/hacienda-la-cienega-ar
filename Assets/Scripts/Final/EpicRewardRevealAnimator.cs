using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Presentacion visual del panel final. Deliberadamente no modifica listeners,
/// GameObjects activos ni el estado interactable de los botones.
/// </summary>
public sealed class EpicRewardRevealAnimator : MonoBehaviour
{
    [Header("Tiempos")]
    [SerializeField] private float titleDelay = 0.15f;
    [SerializeField] private float openingDuration = 1.6f;
    [SerializeField] private float logoDuration = 0.5f;
    [SerializeField] private float textDuration = 0.55f;
    [SerializeField] private float rewardDelay = 0.25f;
    [SerializeField] private float rewardDuration = 0.9f;
    [SerializeField] private float buttonDelay = 0.18f;

    [Header("Estilo")]
    [SerializeField] private Color goldenGlow = new Color(1f, 0.72f, 0.22f, 0.9f);
    [SerializeField] private float rewardOvershoot = 1.08f;
    [SerializeField] private float buttonPulseScale = 1.045f;
    // Escala uniforme: aumenta simultaneamente el ancho y el alto de la tarjeta.
    [SerializeField] private float finalRewardScale = 1.30f;
    [SerializeField] private int goldenParticleCount = 30;

    private Coroutine sequence;
    private Coroutine particleSequence;
    private CanvasGroup titleGroup;
    private CanvasGroup descriptionGroup;
    private CanvasGroup rewardGroup;
    private CanvasGroup actionGroup;
    private CanvasGroup closeGroup;
    private CanvasGroup logoGroup;
    private CanvasGroup marcoGroup;
    private RectTransform rewardRect;
    private RectTransform actionRect;
    private RectTransform logoRect;
    private RectTransform marcoRect;
    private Outline rewardOutline;
    private Image panelBackground;
    private Color panelBackgroundColor;
    private Vector3 marcoBaseScale = Vector3.one;
    private Vector3 logoBaseScale = Vector3.one;
    private Vector3 rewardBaseScale = Vector3.one;
    private Vector3 actionBaseScale = Vector3.one;
    private RectTransform shineRect;
    private CanvasGroup shineGroup;
    private RectTransform[] particleRects;
    private CanvasGroup[] particleGroups;
    private Vector2[] particleOrigins;
    private bool baseStateCached;
    private static Sprite softParticleSprite;

    public void Play(
        TMP_Text title,
        TMP_Text description,
        Image reward,
        Button actionButton,
        Button closeButton)
    {
        CacheVisuals(title, description, reward, actionButton, closeButton);

        if (sequence != null)
            StopCoroutine(sequence);

        if (particleSequence != null)
            StopCoroutine(particleSequence);

        RestoreVisualState();
        sequence = StartCoroutine(RevealSequence());
    }

    private void CacheVisuals(
        TMP_Text title,
        TMP_Text description,
        Image reward,
        Button actionButton,
        Button closeButton)
    {
        titleGroup = GetOrAddCanvasGroup(title != null ? title.gameObject : null);
        descriptionGroup = GetOrAddCanvasGroup(description != null ? description.gameObject : null);
        rewardGroup = GetOrAddCanvasGroup(reward != null ? reward.gameObject : null);
        actionGroup = GetOrAddCanvasGroup(actionButton != null ? actionButton.gameObject : null);
        closeGroup = GetOrAddCanvasGroup(closeButton != null ? closeButton.gameObject : null);

        rewardRect = reward != null ? reward.rectTransform : null;
        actionRect = actionButton != null ? actionButton.GetComponent<RectTransform>() : null;

        if (rewardRect != null && !baseStateCached)
            rewardBaseScale = rewardRect.localScale;

        if (actionRect != null && !baseStateCached)
            actionBaseScale = actionRect.localScale;

        if (reward != null)
        {
            rewardOutline = reward.GetComponent<Outline>();
            if (rewardOutline == null)
                rewardOutline = reward.gameObject.AddComponent<Outline>();

            rewardOutline.effectDistance = new Vector2(3f, -3f);
            rewardOutline.useGraphicAlpha = true;
        }

        panelBackground = GetComponent<Image>();
        if (panelBackground != null)
            panelBackgroundColor = panelBackground.color;

        // Se anima la raiz del prefab para incluir el marco ornamental completo.
        // MarcoPrincipal es el overlay de escaneo y puede estar oculto en este flujo.
        Transform marco = FindSceneTransform("Marco");
        Transform logo = FindSceneTransform("LogoHacienda");

        marcoRect = marco as RectTransform;
        logoRect = logo as RectTransform;
        marcoGroup = GetOrAddCanvasGroup(marco != null ? marco.gameObject : null);
        logoGroup = GetOrAddCanvasGroup(logo != null ? logo.gameObject : null);

        if (marcoRect != null && !baseStateCached)
            marcoBaseScale = marcoRect.localScale;
        if (logoRect != null && !baseStateCached)
            logoBaseScale = logoRect.localScale;

        EnsureOpeningEffects();
        baseStateCached = true;
    }

    private IEnumerator RevealSequence()
    {
        SetAlpha(titleGroup, 0f);
        SetAlpha(descriptionGroup, 0f);
        SetAlpha(rewardGroup, 0f);
        SetAlpha(actionGroup, 0f);
        SetAlpha(closeGroup, 0f);
        SetAlpha(logoGroup, 0f);

        if (panelBackground != null)
        {
            Color transparentBackground = panelBackgroundColor;
            transparentBackground.a = 0f;
            panelBackground.color = transparentBackground;
        }

        if (marcoRect != null)
            marcoRect.localScale = marcoBaseScale * 0.95f;

        if (logoRect != null)
            logoRect.localScale = logoBaseScale * 0.96f;

        PrepareOpeningEffects();
        particleSequence = StartCoroutine(AnimateParticlesContinuously());

        if (rewardRect != null)
            rewardRect.localScale = new Vector3(0.08f, rewardBaseScale.y * 0.92f, rewardBaseScale.z);

        yield return AnimateOpening();
        yield return RevealLogo();
        yield return Wait(titleDelay);
        yield return FadeTogether(titleGroup, descriptionGroup, textDuration);
        yield return Wait(rewardDelay);
        yield return RevealReward();
        yield return Wait(buttonDelay);
        yield return RevealControls();

        sequence = null;
    }

    private IEnumerator RevealReward()
    {
        float elapsed = 0f;

        while (elapsed < rewardDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / rewardDuration);
            float eased = EaseOutBack(t);

            SetAlpha(rewardGroup, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * 1.8f)));

            if (rewardRect != null)
            {
                float targetX = rewardBaseScale.x * finalRewardScale;
                float targetY = rewardBaseScale.y * finalRewardScale;
                float x = Mathf.LerpUnclamped(0.08f, targetX, eased);
                float y = Mathf.Lerp(rewardBaseScale.y * 0.92f, targetY, t) +
                    Mathf.Sin(t * Mathf.PI) * (rewardOvershoot - 1f);
                rewardRect.localScale = new Vector3(x, y, rewardBaseScale.z);
            }

            if (rewardOutline != null)
            {
                Color glow = goldenGlow;
                glow.a *= Mathf.Sin(t * Mathf.PI);
                rewardOutline.effectColor = glow;
                rewardOutline.effectDistance = Vector2.one * Mathf.Lerp(2f, 10f, Mathf.Sin(t * Mathf.PI));
            }

            yield return null;
        }

        SetAlpha(rewardGroup, 1f);
        if (rewardRect != null)
            rewardRect.localScale = rewardBaseScale * finalRewardScale;
        if (rewardOutline != null)
            rewardOutline.effectColor = new Color(goldenGlow.r, goldenGlow.g, goldenGlow.b, 0.18f);
    }

    private IEnumerator RevealControls()
    {
        float duration = 0.48f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.SmoothStep(0f, 1f, t);
            SetAlpha(actionGroup, alpha);
            SetAlpha(closeGroup, alpha);

            if (actionRect != null)
            {
                float pulse = 1f + Mathf.Sin(t * Mathf.PI) * (buttonPulseScale - 1f);
                actionRect.localScale = actionBaseScale * pulse;
            }

            yield return null;
        }

        SetAlpha(actionGroup, 1f);
        SetAlpha(closeGroup, 1f);
        if (actionRect != null)
            actionRect.localScale = actionBaseScale;
    }

    private IEnumerator AnimateOpening()
    {
        float elapsed = 0f;
        while (elapsed < openingDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / openingDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);

            if (panelBackground != null)
            {
                Color color = panelBackgroundColor;
                color.a *= eased;
                panelBackground.color = color;
            }

            if (marcoRect != null)
                marcoRect.localScale = Vector3.LerpUnclamped(
                    marcoBaseScale * 0.95f,
                    marcoBaseScale,
                    EaseOutBack(t)
                );

            AnimateShine(t);
            yield return null;
        }

        if (panelBackground != null)
            panelBackground.color = panelBackgroundColor;
        if (marcoRect != null)
            marcoRect.localScale = marcoBaseScale;
        SetAlpha(shineGroup, 0f);
    }

    private IEnumerator RevealLogo()
    {
        float elapsed = 0f;
        while (elapsed < logoDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / logoDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            SetAlpha(logoGroup, eased);

            if (logoRect != null)
            {
                float pulse = 1f + Mathf.Sin(t * Mathf.PI) * 0.025f;
                logoRect.localScale = Vector3.Lerp(
                    logoBaseScale * 0.96f,
                    logoBaseScale,
                    eased
                ) * pulse;
            }
            yield return null;
        }

        SetAlpha(logoGroup, 1f);
        if (logoRect != null)
            logoRect.localScale = logoBaseScale;
    }

    private void EnsureOpeningEffects()
    {
        if (shineRect == null)
        {
            GameObject shine = CreateDecorativeImage("GoldenFrameShine", transform);
            shineRect = shine.GetComponent<RectTransform>();
            shineGroup = shine.GetComponent<CanvasGroup>();
            shineRect.anchorMin = new Vector2(0f, 0.5f);
            shineRect.anchorMax = new Vector2(0f, 0.5f);
            shineRect.pivot = new Vector2(0.5f, 0.5f);
            shineRect.sizeDelta = new Vector2(34f, 2400f);
            shineRect.localRotation = Quaternion.Euler(0f, 0f, -12f);
            shine.GetComponent<Image>().color = new Color(1f, 0.78f, 0.3f, 0.2f);
            shineRect.SetAsFirstSibling();
        }

        if (particleRects != null && particleRects.Length == goldenParticleCount)
            return;

        particleRects = new RectTransform[goldenParticleCount];
        particleGroups = new CanvasGroup[goldenParticleCount];
        particleOrigins = new Vector2[goldenParticleCount];
        Random.State previousState = Random.state;
        Random.InitState(1942);

        for (int i = 0; i < goldenParticleCount; i++)
        {
            GameObject particle = CreateDecorativeImage("GoldenDust", transform);
            RectTransform rect = particle.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(
                Random.Range(0.06f, 0.94f),
                Random.Range(0.08f, 0.92f)
            );
            rect.sizeDelta = Vector2.one * Random.Range(10f, 22f);
            // Se colocan por encima del fondo y del contenido para que sean
            // perceptibles en dispositivos; no bloquean ningun toque.
            rect.SetAsLastSibling();
            Image particleImage = particle.GetComponent<Image>();
            particleImage.sprite = GetSoftParticleSprite();
            particleImage.preserveAspect = true;
            particleImage.color = new Color(1f, 0.76f, 0.24f, 1f);
            particleRects[i] = rect;
            particleGroups[i] = particle.GetComponent<CanvasGroup>();
            particleOrigins[i] = new Vector2(Random.Range(-28f, 28f), Random.Range(-24f, 24f));
        }

        Random.state = previousState;
    }

    private void PrepareOpeningEffects()
    {
        SetAlpha(shineGroup, 0f);
        for (int i = 0; particleGroups != null && i < particleGroups.Length; i++)
        {
            SetAlpha(particleGroups[i], 0f);
            particleRects[i].anchoredPosition = particleOrigins[i];
        }
    }

    private void AnimateShine(float t)
    {
        if (shineRect == null)
            return;

        shineRect.anchoredPosition = new Vector2(Mathf.Lerp(-650f, 650f, t), 0f);
        SetAlpha(shineGroup, Mathf.Sin(t * Mathf.PI) * 0.8f);
    }

    private IEnumerator AnimateParticlesContinuously()
    {
        float elapsed = 0f;

        while (isActiveAndEnabled)
        {
            elapsed += Time.unscaledDeltaTime;
            float entrance = Mathf.Clamp01(elapsed / 0.4f);

            for (int i = 0; particleRects != null && i < particleRects.Length; i++)
            {
                float phase = Mathf.Repeat(elapsed * 0.28f + i * 0.073f, 1f);
                float pulse = 0.35f + Mathf.Sin(phase * Mathf.PI) * 0.65f;
                particleRects[i].anchoredPosition =
                    particleOrigins[i] + Vector2.up * phase * 110f;
                particleRects[i].localScale =
                    Vector3.one * Mathf.Lerp(0.8f, 1.2f, pulse);
                SetAlpha(particleGroups[i], entrance * pulse * 0.9f);
            }

            yield return null;
        }

        particleSequence = null;
    }

    private static GameObject CreateDecorativeImage(string objectName, Transform parent)
    {
        GameObject visual = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup)
        );
        visual.layer = parent.gameObject.layer;
        visual.transform.SetParent(parent, false);
        visual.GetComponent<Image>().raycastTarget = false;
        CanvasGroup group = visual.GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        return visual;
    }

    private static Sprite GetSoftParticleSprite()
    {
        if (softParticleSprite != null)
            return softParticleSprite;

        const int size = 32;
        Texture2D texture = new Texture2D(
            size,
            size,
            TextureFormat.RGBA32,
            false
        );
        texture.name = "SoftGoldenParticleTexture";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.hideFlags = HideFlags.HideAndDontSave;

        Vector2 center = Vector2.one * ((size - 1) * 0.5f);
        float radius = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                float alpha = 1f - Mathf.SmoothStep(0.45f, 1f, distance);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply(false, true);
        softParticleSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size
        );
        softParticleSprite.name = "SoftGoldenParticle";
        softParticleSprite.hideFlags = HideFlags.HideAndDontSave;
        return softParticleSprite;
    }

    private static Transform FindSceneTransform(string targetName)
    {
        Transform[] transforms = FindObjectsOfType<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == targetName)
                return transforms[i];
        }
        return null;
    }

    private IEnumerator FadeTogether(CanvasGroup first, CanvasGroup second, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            SetAlpha(first, alpha);
            SetAlpha(second, alpha);
            yield return null;
        }
        SetAlpha(first, 1f);
        SetAlpha(second, 1f);
    }

    private static IEnumerator Wait(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void OnDisable()
    {
        if (sequence != null)
        {
            StopCoroutine(sequence);
            sequence = null;
        }
        if (particleSequence != null)
        {
            StopCoroutine(particleSequence);
            particleSequence = null;
        }
        RestoreVisualState();
    }

    private void RestoreVisualState()
    {
        SetAlpha(titleGroup, 1f);
        SetAlpha(descriptionGroup, 1f);
        SetAlpha(rewardGroup, 1f);
        SetAlpha(actionGroup, 1f);
        SetAlpha(closeGroup, 1f);
        SetAlpha(logoGroup, 1f);
        SetAlpha(marcoGroup, 1f);

        if (panelBackground != null)
            panelBackground.color = panelBackgroundColor;
        if (marcoRect != null)
            marcoRect.localScale = marcoBaseScale;
        if (logoRect != null)
            logoRect.localScale = logoBaseScale;
        SetAlpha(shineGroup, 0f);
        for (int i = 0; particleGroups != null && i < particleGroups.Length; i++)
            SetAlpha(particleGroups[i], 0f);

        if (rewardRect != null)
            rewardRect.localScale = rewardBaseScale;
        if (actionRect != null)
            actionRect.localScale = actionBaseScale;
    }

    private static CanvasGroup GetOrAddCanvasGroup(GameObject target)
    {
        if (target == null)
            return null;

        CanvasGroup group = target.GetComponent<CanvasGroup>();
        return group != null ? group : target.AddComponent<CanvasGroup>();
    }

    private static void SetAlpha(CanvasGroup group, float alpha)
    {
        if (group != null)
            group.alpha = alpha;
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float x = t - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }
}
