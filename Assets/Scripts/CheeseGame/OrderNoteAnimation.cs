using System.Collections;
using UnityEngine;

public class OrderNoteAnimation : MonoBehaviour
{
    [SerializeField] private float startYOffset = 1200f;
    [SerializeField] private float fallDuration = 0.5f;
    [SerializeField] private float bounceAmount = 25f;
    [SerializeField] private float bounceDuration = 0.15f;

    private RectTransform rectTransform;
    private Vector2 targetPosition;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();

        targetPosition = rectTransform.anchoredPosition;

        rectTransform.anchoredPosition =
            targetPosition + Vector2.up * startYOffset;

        StartCoroutine(PlayIntro());
    }

    private IEnumerator PlayIntro()
    {
        float elapsed = 0f;

        Vector2 startPosition =
            rectTransform.anchoredPosition;

        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / fallDuration;

            rectTransform.anchoredPosition =
                Vector2.Lerp(
                    startPosition,
                    targetPosition,
                    t);

            yield return null;
        }

        rectTransform.anchoredPosition =
            targetPosition;

        yield return StartCoroutine(Bounce());
    }

    private IEnumerator Bounce()
    {
        Vector2 bouncePosition =
            targetPosition - Vector2.up * bounceAmount;

        float elapsed = 0f;

        while (elapsed < bounceDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / bounceDuration;

            rectTransform.anchoredPosition =
                Vector2.Lerp(
                    targetPosition,
                    bouncePosition,
                    t);

            yield return null;
        }

        elapsed = 0f;

        while (elapsed < bounceDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / bounceDuration;

            rectTransform.anchoredPosition =
                Vector2.Lerp(
                    bouncePosition,
                    targetPosition,
                    t);

            yield return null;
        }

        rectTransform.anchoredPosition =
            targetPosition;
    }
}