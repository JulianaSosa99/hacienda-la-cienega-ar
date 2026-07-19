using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FlowerUI : MonoBehaviour
{
    [Header("Flower")]
    public int level = 1;
    public Sprite[] levelSprites;
    public Image flowerImage;

    [Header("Physics")]
    public Vector2 velocity;
    public float gravity = 2000f;
    public float damping = 0.98f;
    public float settleDamping = 0.92f;
    public float floorOffset = 120f;
    public float wallBounce = -0.3f;

    [Header("Collision")]
    public float collisionRadiusScale = 0.45f;
    public float pushForce = 8f;

    [Header("State")]
    public bool isMerging = false;
    public bool isFalling = true;
    public bool isSpawnerFlower = false;

    [HideInInspector] public float mergeCooldown = 0f;

    private RectTransform rt;
    private RectTransform gameArea;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        ResolveGameArea();
    }

    private void Update()
    {
        if (GameManagerUI.Instance == null || isMerging)
            return;

        if (mergeCooldown > 0f)
            mergeCooldown -= Time.deltaTime;

        ApplyGravity();
        ResolveCollisions();
        CheckMerge();
    }

    private void ResolveGameArea()
    {
        if (gameArea != null)
            return;

        GameObject areaObject = GameObject.Find("GameArea");

        if (areaObject != null)
            gameArea = areaObject.GetComponent<RectTransform>();

        if (gameArea == null && transform.parent != null)
            gameArea = transform.parent.GetComponent<RectTransform>();
    }

    private void ApplyGravity()
    {
        if (rt == null || isSpawnerFlower)
            return;

        if (!isFalling)
        {
            velocity *= settleDamping;
            return;
        }

        velocity.y -= gravity * Time.deltaTime;
        rt.anchoredPosition += velocity * Time.deltaTime;
        velocity *= damping;

        ClampInsideGameArea();
    }

    private void ClampInsideGameArea()
    {
        ResolveGameArea();

        if (gameArea == null || rt == null)
            return;

        Vector3[] corners = new Vector3[4];
        gameArea.GetLocalCorners(corners);

        float left = corners[0].x;
        float right = corners[3].x;
        float floorY = corners[0].y + floorOffset;

        Vector2 pos = rt.anchoredPosition;
        float halfWidth = rt.sizeDelta.x * rt.localScale.x * 0.5f;
        float halfHeight = rt.sizeDelta.y * rt.localScale.y * 0.5f;

        if (pos.y - halfHeight <= floorY)
        {
            pos.y = floorY + halfHeight;
            velocity.y = 0f;
            isFalling = false;
        }

        if (pos.x - halfWidth < left)
        {
            pos.x = left + halfWidth;
            velocity.x *= wallBounce;
        }

        if (pos.x + halfWidth > right)
        {
            pos.x = right - halfWidth;
            velocity.x *= wallBounce;
        }

        rt.anchoredPosition = pos;
    }

    private void ResolveCollisions()
    {
        if (rt == null || isSpawnerFlower)
            return;

        foreach (FlowerUI other in GameManagerUI.Instance.flowers)
        {
            if (!CanCollideWith(other))
                continue;

            float distance = Vector2.Distance(rt.anchoredPosition, other.rt.anchoredPosition);
            float combinedRadius = GetRadius() + other.GetRadius();

            if (distance <= 0f || distance >= combinedRadius)
                continue;

            Vector2 direction = (rt.anchoredPosition - other.rt.anchoredPosition).normalized;
            Vector2 push = direction * (combinedRadius - distance) * 0.5f;

            rt.anchoredPosition += push;
            velocity += push * pushForce;
            isFalling = true;
        }
    }

    private bool CanCollideWith(FlowerUI other)
    {
        return other != null
            && other != this
            && other.rt != null
            && !other.isSpawnerFlower
            && !other.isMerging;
    }

    private void CheckMerge()
    {
        if (isSpawnerFlower || mergeCooldown > 0f)
            return;

        GameManagerUI.Instance.flowers.RemoveAll(flower => flower == null);

        foreach (FlowerUI other in GameManagerUI.Instance.flowers)
        {
            if (!CanMergeWith(other))
                continue;

            float distance = Vector2.Distance(rt.anchoredPosition, other.rt.anchoredPosition);
            float combinedRadius = GetRadius() + other.GetRadius();

            if (distance <= combinedRadius)
            {
                StartCoroutine(MergeRoutine(other));
                break;
            }
        }
    }

    private bool CanMergeWith(FlowerUI other)
    {
        return CanCollideWith(other)
            && other.level == level
            && other.mergeCooldown <= 0f;
    }

    private IEnumerator MergeRoutine(FlowerUI other)
    {
        isMerging = true;
        other.isMerging = true;

        Vector2 midpoint = (rt.anchoredPosition + other.rt.anchoredPosition) * 0.5f;
        int nextLevel = level + 1;

        GameManagerUI.Instance.RemoveFlower(this);
        GameManagerUI.Instance.RemoveFlower(other);
        GameManagerUI.Instance.SpawnMerged(midpoint, nextLevel);

        Destroy(other.gameObject);
        Destroy(gameObject);

        yield return null;
    }

    private float GetRadius()
    {
        if (rt == null)
            return 0f;

        return rt.sizeDelta.x * rt.localScale.x * collisionRadiusScale;
    }

    public void ApplyVisual()
    {
        if (flowerImage == null || levelSprites == null || levelSprites.Length == 0)
            return;

        int spriteIndex = Mathf.Clamp(level - 1, 0, levelSprites.Length - 1);
        flowerImage.sprite = levelSprites[spriteIndex];
        transform.localScale = Vector3.one;

        Debug.Log("Visual aplicada nivel: " + level);
    }
}
