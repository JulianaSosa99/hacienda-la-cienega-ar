using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManagerUI : MonoBehaviour
{
    public static GameManagerUI Instance;

    [Header("References")]
    public GameObject flowerPrefab;
    public Transform activeParent;
    public SpawnerUI spawner;

    [Header("Panels")]
    public GameObject gamePanel;
    public GameObject gameplayRoot;
    public CanvasGroup completionPanel;

    [Header("Narrative")]
    public FlowersNarrativeManager narrativeManager;

    [Header("Flowers")]
    public List<FlowerUI> flowers = new List<FlowerUI>();

    [Header("Spawn Levels")]
    [Min(1)] public int initialMinLevel = 1;
    [Min(1)] public int initialMaxLevel = 2;
    [Min(1)] public int highestUnlockedLevel = 2;

    [Header("Flower Defaults")]
    public Vector2 flowerSize = new Vector2(172f, 175f);
    public float mergedSpawnOffsetY = 35f;
    public float mergedImpulseX = 25f;
    public float mergedImpulseY = 60f;
    public float mergedCooldown = 0.15f;
    public float finalBouquetScale = 1.55f;

    [Header("Win")]
    [Min(2)] public int winLevel = 4;
    public float bouquetViewDelay = 1f;
    public bool completeAfterTime = false;
    public float maxGameDuration = 90f;

    private bool gameWon = false;
    private Coroutine timeoutRoutine;
    private Coroutine completionRoutine;

    private void Awake()
    {
        Instance = this;
        ClampLevelSettings();
    }

    private void OnEnable()
    {
        Instance = this;
        gameWon = false;
        flowers.RemoveAll(flower => flower == null);
        ClampLevelSettings();
        SetCompletionPanelVisible(false);

        if (gameplayRoot != null)
            gameplayRoot.SetActive(true);

        if (spawner != null)
            spawner.enabled = true;

        StartOptionalTimeout();
    }

    private void OnDisable()
    {
        StopOptionalTimeout();

        if (completionRoutine != null)
        {
            StopCoroutine(completionRoutine);
            completionRoutine = null;
        }
    }

    private void OnValidate()
    {
        ClampLevelSettings();
    }

    public void RegisterFlower(FlowerUI flower)
    {
        if (flower == null || flowers.Contains(flower))
            return;

        flowers.Add(flower);
    }

    public void RemoveFlower(FlowerUI flower)
    {
        if (flower == null)
            return;

        flowers.Remove(flower);
    }

    public int GetRandomSpawnLevel()
    {
        ClampLevelSettings();

        int maxPlayableLevel = Mathf.Min(initialMaxLevel, highestUnlockedLevel, winLevel - 1);
        int minPlayableLevel = Mathf.Min(initialMinLevel, maxPlayableLevel);

        return Random.Range(minPlayableLevel, maxPlayableLevel + 1);
    }

    public void ConfigureFlower(FlowerUI flower, int level, bool isSpawnerFlower)
    {
        if (flower == null)
            return;

        RectTransform rt = flower.GetComponent<RectTransform>();

        if (rt != null)
        {
            rt.sizeDelta = flowerSize;
            rt.localScale = Vector3.one;
        }

        flower.level = level;
        flower.isSpawnerFlower = isSpawnerFlower;
        flower.isFalling = !isSpawnerFlower;
        flower.isMerging = false;
        flower.mergeCooldown = isSpawnerFlower ? 0f : mergedCooldown;
        flower.velocity = Vector2.zero;
        flower.ApplyVisual();
    }

    public void SpawnMerged(Vector2 position, int level)
    {
        if (gameWon || flowerPrefab == null || activeParent == null)
            return;

        Vector2 spawnPos = position + new Vector2(0f, mergedSpawnOffsetY);
        GameObject newFlower = Instantiate(flowerPrefab, activeParent);
        newFlower.SetActive(false);

        RectTransform rt = newFlower.GetComponent<RectTransform>();

        if (rt != null)
            rt.anchoredPosition = spawnPos;

        FlowerUI flower = newFlower.GetComponent<FlowerUI>();
        ConfigureFlower(flower, level, false);

        flower.velocity = new Vector2(Random.Range(-mergedImpulseX, mergedImpulseX), mergedImpulseY);

        if (rt != null && level >= winLevel)
            rt.localScale = Vector3.one * finalBouquetScale;

        newFlower.SetActive(true);

        UnlockLevel(level);
        RegisterFlower(flower);

        Debug.Log("Nueva flor creada con nivel: " + level);

        if (level >= winLevel)
            CompleteGame();
    }

    public void CompleteGame()
    {
        if (gameWon)
            return;

        gameWon = true;
        StopOptionalTimeout();
        FreezeGameplay();

        Debug.Log("WIN RAMO");

        if (completionRoutine != null)
            StopCoroutine(completionRoutine);

        completionRoutine = StartCoroutine(ShowCompletionRoutine());
    }

    public void ContinueAfterBouquet()
    {
        SetCompletionPanelVisible(false);

        if (narrativeManager == null)
            narrativeManager = FindObjectOfType<FlowersNarrativeManager>();

        if (narrativeManager != null)
            narrativeManager.OnFlowersCompleted();

        if (gamePanel != null)
            gamePanel.SetActive(false);
    }

    private IEnumerator ShowCompletionRoutine()
    {
        if (bouquetViewDelay > 0f)
            yield return new WaitForSeconds(bouquetViewDelay);

        if (gameplayRoot != null)
            gameplayRoot.SetActive(false);

        SetCompletionPanelVisible(true);
    }

    private void FreezeGameplay()
    {
        if (spawner == null)
            spawner = GetComponentInChildren<SpawnerUI>(true);

        if (spawner != null)
            spawner.enabled = false;

        flowers.RemoveAll(flower => flower == null);

        foreach (FlowerUI flower in flowers)
        {
            if (flower == null)
                continue;

            flower.isFalling = false;
            flower.velocity = Vector2.zero;
        }
    }

    private void SetCompletionPanelVisible(bool visible)
    {
        if (completionPanel == null)
            return;

        completionPanel.gameObject.SetActive(visible);
        completionPanel.alpha = visible ? 1f : 0f;
        completionPanel.interactable = visible;
        completionPanel.blocksRaycasts = visible;
    }

    private void UnlockLevel(int level)
    {
        if (level <= highestUnlockedLevel)
            return;

        highestUnlockedLevel = Mathf.Min(level, winLevel);
        Debug.Log("Nuevo nivel desbloqueado: " + highestUnlockedLevel);
    }

    private void ClampLevelSettings()
    {
        winLevel = Mathf.Max(2, winLevel);
        initialMinLevel = Mathf.Max(1, initialMinLevel);
        initialMaxLevel = Mathf.Max(initialMinLevel, initialMaxLevel);
        highestUnlockedLevel = Mathf.Clamp(highestUnlockedLevel, initialMinLevel, winLevel);
    }

    private void StartOptionalTimeout()
    {
        StopOptionalTimeout();

        if (completeAfterTime && maxGameDuration > 0f)
            timeoutRoutine = StartCoroutine(CompletionTimeoutRoutine());
    }

    private void StopOptionalTimeout()
    {
        if (timeoutRoutine == null)
            return;

        StopCoroutine(timeoutRoutine);
        timeoutRoutine = null;
    }

    private IEnumerator CompletionTimeoutRoutine()
    {
        yield return new WaitForSeconds(maxGameDuration);

        if (!gameWon)
        {
            Debug.LogWarning("FlowersGame completado por tiempo maximo.");
            CompleteGame();
        }
    }
}
