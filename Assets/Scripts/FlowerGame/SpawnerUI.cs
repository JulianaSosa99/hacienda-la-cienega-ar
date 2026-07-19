using UnityEngine;

public class SpawnerUI : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject flowerPrefab;
    public RectTransform activeParent;
    public RectTransform gameArea;
    public RectTransform dropLine;

    [Header("Manager")]
    public GameManagerUI gameManager;

    [Header("Camera")]
    public Camera uiCamera;

    [Header("Spawn")]
    public float spawnOffsetY = 650f;

    [Header("Movimiento")]
    public float horizontalPadding = 100f;
    public float releaseImpulseMultiplier = 18f;
    public float maxReleaseImpulse = 420f;
    public float initialDropVelocityY = 0f;

    [Header("Drop Line")]
    [SerializeField] private float lineWidth = 20f;
    [SerializeField] private float lineStartOffset = 90f;
    [SerializeField] private float floorOffset = 50f;

    private GameObject current;
    private Canvas parentCanvas;
    private bool isDragging = false;
    private Vector2 lastSpawnPosition;
    private bool hasSpawnPosition = false;
    private float previousDragX = 0f;
    private float releaseVelocityX = 0f;

    private void Awake()
    {
        ResolveManager();
        ResolveCanvas();
    }

    private void OnEnable()
    {
        ResolveManager();
        ResolveCanvas();
        EnsureInitialSpawnPosition();

        if (current == null)
            SpawnNew();
    }

    private void OnDisable()
    {
        isDragging = false;
        SetDropLineVisible(false);
    }

    private void Update()
    {
        if (current == null)
            return;

        if (IsTouchStart())
            BeginDrag(GetInputPosition());

        if (IsTouching())
        {
            if (!isDragging)
                BeginDrag(GetInputPosition());

            Drag(GetInputPosition());
        }

        if (isDragging && IsTouchEnd())
            Drop();

        UpdateDropLine();
    }

    private bool IsTouching()
    {
        return Input.touchCount > 0 || Input.GetMouseButton(0);
    }

    private bool IsTouchStart()
    {
        return (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            || Input.GetMouseButtonDown(0);
    }

    private bool IsTouchEnd()
    {
        if (Input.touchCount > 0)
        {
            TouchPhase phase = Input.GetTouch(0).phase;
            return phase == TouchPhase.Ended || phase == TouchPhase.Canceled;
        }

        return Input.GetMouseButtonUp(0);
    }

    private Vector2 GetInputPosition()
    {
        if (Input.touchCount > 0)
            return Input.GetTouch(0).position;

        return Input.mousePosition;
    }

    private void BeginDrag(Vector2 screenPosition)
    {
        isDragging = true;
        releaseVelocityX = 0f;

        Vector2 spawnPosition = GetClampedSpawnPosition(screenPosition, out float gameAreaX);
        previousDragX = gameAreaX;
        MoveCurrentTo(spawnPosition);
    }

    private void Drag(Vector2 screenPosition)
    {
        Vector2 spawnPosition = GetClampedSpawnPosition(screenPosition, out float gameAreaX);
        float deltaX = gameAreaX - previousDragX;

        releaseVelocityX = Mathf.Clamp(
            deltaX / Mathf.Max(Time.deltaTime, 0.001f) * releaseImpulseMultiplier,
            -maxReleaseImpulse,
            maxReleaseImpulse
        );

        previousDragX = gameAreaX;
        MoveCurrentTo(spawnPosition);
    }

    private Vector2 GetClampedSpawnPosition(Vector2 screenPosition, out float clampedGameAreaX)
    {
        clampedGameAreaX = 0f;

        if (gameArea == null || activeParent == null)
            return lastSpawnPosition;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            gameArea,
            screenPosition,
            GetCanvasCamera(),
            out Vector2 gameAreaPoint
        );

        float leftLimit = -gameArea.rect.width * 0.5f + horizontalPadding;
        float rightLimit = gameArea.rect.width * 0.5f - horizontalPadding;
        clampedGameAreaX = Mathf.Clamp(gameAreaPoint.x, leftLimit, rightLimit);

        Vector3 worldPoint = gameArea.TransformPoint(new Vector3(clampedGameAreaX, spawnOffsetY, 0f));
        Vector3 activeParentPoint = activeParent.InverseTransformPoint(worldPoint);

        return new Vector2(activeParentPoint.x, activeParentPoint.y);
    }

    private void MoveCurrentTo(Vector2 anchoredPosition)
    {
        RectTransform rt = current.GetComponent<RectTransform>();

        if (rt == null)
            return;

        rt.anchoredPosition = anchoredPosition;
        lastSpawnPosition = anchoredPosition;
    }

    private void UpdateDropLine()
    {
        if (dropLine == null || current == null || gameArea == null)
            return;

        RectTransform flowerRT = current.GetComponent<RectTransform>();
        RectTransform lineParent = dropLine.parent as RectTransform;

        if (flowerRT == null || lineParent == null)
            return;

        Vector3 flowerWorld = flowerRT.TransformPoint(Vector3.zero);
        Vector3 flowerInLineParent = lineParent.InverseTransformPoint(flowerWorld);

        Vector3[] gameAreaWorldCorners = new Vector3[4];
        gameArea.GetWorldCorners(gameAreaWorldCorners);

        Vector3 floorWorld = gameAreaWorldCorners[0] + Vector3.up * floorOffset;
        Vector3 floorInLineParent = lineParent.InverseTransformPoint(floorWorld);

        float startY = flowerInLineParent.y - lineStartOffset;
        float height = Mathf.Max(0f, startY - floorInLineParent.y);

        dropLine.anchoredPosition = new Vector2(flowerInLineParent.x, startY);
        dropLine.sizeDelta = new Vector2(lineWidth, height);
        dropLine.SetAsLastSibling();
        SetDropLineVisible(true);
    }

    private void SpawnNew()
    {
        GameManagerUI manager = ResolveManager();

        if (manager == null)
        {
            Debug.LogError("Spawner mal configurado: falta GameManagerUI.");
            return;
        }

        if (flowerPrefab == null)
        {
            Debug.LogError("Spawner mal configurado: falta Flower Prefab.");
            return;
        }

        if (activeParent == null)
        {
            Debug.LogError("Spawner mal configurado: falta Active Parent.");
            return;
        }

        EnsureInitialSpawnPosition();

        current = Instantiate(flowerPrefab, activeParent);
        current.SetActive(false);

        RectTransform rt = current.GetComponent<RectTransform>();

        if (rt != null)
            rt.anchoredPosition = lastSpawnPosition;

        FlowerUI flower = current.GetComponent<FlowerUI>();
        manager.ConfigureFlower(flower, manager.GetRandomSpawnLevel(), true);

        current.SetActive(true);
        manager.ConfigureFlower(flower, flower.level, true);
        MoveCurrentTo(lastSpawnPosition);

        Debug.Log("Nueva flor preparada con nivel: " + flower.level);
    }

    private void Drop()
    {
        if (current == null)
            return;

        GameManagerUI manager = ResolveManager();

        if (manager == null)
        {
            Debug.LogError("No se puede soltar la flor: falta GameManagerUI.");
            return;
        }

        isDragging = false;

        FlowerUI flower = current.GetComponent<FlowerUI>();

        if (flower != null)
        {
            flower.isSpawnerFlower = false;
            flower.isFalling = true;
            flower.velocity = new Vector2(releaseVelocityX, initialDropVelocityY);
            manager.RegisterFlower(flower);
        }

        Debug.Log("Flor liberada");

        current = null;
        SpawnNew();
    }


    private void EnsureInitialSpawnPosition()
    {
        if (hasSpawnPosition)
            return;

        lastSpawnPosition = GetCenterSpawnPosition();
        hasSpawnPosition = true;
    }

    private Vector2 GetCenterSpawnPosition()
    {
        if (gameArea == null || activeParent == null)
            return lastSpawnPosition;

        Vector3 worldPoint = gameArea.TransformPoint(new Vector3(0f, spawnOffsetY, 0f));
        Vector3 activeParentPoint = activeParent.InverseTransformPoint(worldPoint);

        return new Vector2(activeParentPoint.x, activeParentPoint.y);
    }
    private GameManagerUI ResolveManager()
    {
        if (gameManager != null)
            return gameManager;

        if (GameManagerUI.Instance != null)
        {
            gameManager = GameManagerUI.Instance;
            return gameManager;
        }

        gameManager = FindObjectOfType<GameManagerUI>(true);
        return gameManager;
    }

    private void ResolveCanvas()
    {
        if (parentCanvas == null)
            parentCanvas = GetComponentInParent<Canvas>();
    }

    private Camera GetCanvasCamera()
    {
        ResolveCanvas();

        if (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        if (uiCamera != null)
            return uiCamera;

        return parentCanvas != null ? parentCanvas.worldCamera : null;
    }

    private void SetDropLineVisible(bool visible)
    {
        if (dropLine != null && dropLine.gameObject.activeSelf != visible)
            dropLine.gameObject.SetActive(visible);
    }
}







