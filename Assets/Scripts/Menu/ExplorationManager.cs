using System;
using System.Collections.Generic;
using UnityEngine;

public class ExplorationManager : MonoBehaviour
{
    public static ExplorationManager Instance;
    public event Action<int, int, bool> ProgressChanged;

    public ExplorationProgress Progress =
        new ExplorationProgress();

    [Header("Memorias")]
    [SerializeField] private List<MemoryData> memories =
        new List<MemoryData>();

    private const string SAVE_KEY =
        "EXPLORATION_PROGRESS";

    public IReadOnlyList<MemoryData> Memories => memories;
    private int lastExplorationPercent = -1;
    private bool suppressProgressNotifications;
    private string activeUserId;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            DontDestroyOnLoad(gameObject);

            EnsureDefaultMemories();
            LoadForCurrentUser();
            RefreshMemoryStates();
            lastExplorationPercent = GetExplorationPercent();
        }
        else
        {
            if (HasConfiguredMemoryAssets() && !Instance.HasConfiguredMemoryAssets())
            {
                Destroy(Instance.gameObject);

                Instance = this;

                DontDestroyOnLoad(gameObject);

                EnsureDefaultMemories();
                LoadForCurrentUser();
                RefreshMemoryStates();
                lastExplorationPercent = GetExplorationPercent();

                return;
            }

            Destroy(gameObject);
        }
    }

    private void Update()
    {
        string currentUserId = ResolveCurrentUserId();

        if (activeUserId == currentUserId)
            return;

        SwitchToUser(currentUserId);
    }

    public bool IsMemoryUnlocked(string id)
    {
        EnsureProgressLists();

        return Progress.unlockedMemories.Contains(id);
    }

    public bool IsMinigameCompleted(string id)
    {
        EnsureProgressLists();

        return Progress.completedMinigames.Contains(id);
    }

    public bool IsCollectibleFound(string id)
    {
        EnsureProgressLists();

        return Progress.discoveredCollectibles.Contains(id);
    }

    public bool IsRewardEarned(string id)
    {
        EnsureProgressLists();

        return Progress.earnedRewards.Contains(id);
    }

    public void UnlockMemory(string id)
    {
        EnsureProgressLists();

        if (!Progress.unlockedMemories.Contains(id))
        {
            Progress.unlockedMemories.Add(id);

            Save();
        }

        RefreshMemoryStates();
        NotifyProgressChanged(false);
    }

    public void CompleteMinigame(string id)
    {
        EnsureProgressLists();

        if (!Progress.completedMinigames.Contains(id))
        {
            Progress.completedMinigames.Add(id);

            Save();
        }

        RefreshMemoryStates();
        NotifyProgressChanged(false);
    }

    public void DiscoverCollectible(string id)
    {
        EnsureProgressLists();

        if (!Progress.discoveredCollectibles.Contains(id))
        {
            Progress.discoveredCollectibles.Add(id);

            Save();
        }

        NotifyProgressChanged(false);
    }

    public void EarnReward(string id)
    {
        EnsureProgressLists();

        if (!Progress.earnedRewards.Contains(id))
        {
            Progress.earnedRewards.Add(id);

            Save();
        }

        RefreshMemoryStates();
        NotifyProgressChanged(false);
    }

    public void RegisterGuardianScanned(string guardianId)
    {
        suppressProgressNotifications = true;

        UnlockMemory(guardianId);

        suppressProgressNotifications = false;
        NotifyProgressChanged(false);
    }

    public void RegisterMinigameCompleted(string guardianId)
    {
        MemoryData memory = GetMemory(guardianId);

        suppressProgressNotifications = true;

        UnlockMemory(guardianId);

        if (memory == null)
        {
            suppressProgressNotifications = false;
            NotifyProgressChanged(false);
            return;
        }

        CompleteMinigame(memory.minigameId);
        EarnReward(memory.rewardId);

        suppressProgressNotifications = false;
        NotifyProgressChanged(true);
    }

    public MemoryData GetMemory(string id)
    {
        EnsureDefaultMemories();

        for (int i = 0; i < memories.Count; i++)
        {
            if (memories[i].id == id)
                return memories[i];
        }

        return null;
    }

    public int GetExplorationPercent()
    {
        EnsureDefaultMemories();

        int totalSteps = memories.Count * 3;

        if (totalSteps == 0)
            return 0;

        int completedSteps = 0;

        for (int i = 0; i < memories.Count; i++)
        {
            MemoryData memory = memories[i];

            if (IsMemoryUnlocked(memory.id))
                completedSteps++;

            if (IsMinigameCompleted(memory.minigameId))
                completedSteps++;

            if (IsRewardEarned(memory.rewardId))
                completedSteps++;
        }

        return Mathf.RoundToInt(completedSteps * 100f / totalSteps);
    }

    public void Save()
    {
        EnsureProgressLists();

        string json =
            JsonUtility.ToJson(Progress);

        PlayerPrefs.SetString(
            GetSaveKey(),
            json
        );

        PlayerPrefs.Save();
    }

    public void Load()
    {
        string saveKey = GetSaveKey();

        if (!PlayerPrefs.HasKey(saveKey))
        {
            Progress = new ExplorationProgress();
            EnsureProgressLists();
            RefreshMemoryStates();
            return;
        }

        string json =
            PlayerPrefs.GetString(
                saveKey
            );

        Progress =
            JsonUtility.FromJson<ExplorationProgress>(
                json
            );

        EnsureProgressLists();
        RefreshMemoryStates();
    }

    public void LoadForCurrentUser()
    {
        activeUserId = ResolveCurrentUserId();
        AdoptBestAvailableProgressForUser(activeUserId);
        Load();
        RefreshMemoryStates();
        lastExplorationPercent = GetExplorationPercent();
    }

    public void ResetProgress()
    {
        Progress =
            new ExplorationProgress();

        Save();
        RefreshMemoryStates();
        NotifyProgressChanged(false);
    }

    public void InitializeProgressForNewUser(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return;

        activeUserId = userId;
        Progress = new ExplorationProgress();
        EnsureProgressLists();
        Save();
        RefreshMemoryStates();
        lastExplorationPercent = GetExplorationPercent();
    }

    private void NotifyProgressChanged(bool important)
    {
        if (suppressProgressNotifications)
            return;

        int currentPercent = GetExplorationPercent();

        if (lastExplorationPercent < 0)
        {
            lastExplorationPercent = currentPercent;
            return;
        }

        if (currentPercent == lastExplorationPercent)
            return;

        int previousPercent = lastExplorationPercent;
        lastExplorationPercent = currentPercent;

        if (ProgressChanged != null)
            ProgressChanged.Invoke(previousPercent, currentPercent, important);
    }

    private void RefreshMemoryStates()
    {
        EnsureDefaultMemories();
        EnsureProgressLists();

        for (int i = 0; i < memories.Count; i++)
        {
            MemoryData memory = memories[i];

            memory.isUnlocked = IsMemoryUnlocked(memory.id);
            memory.minigameCompleted = IsMinigameCompleted(memory.minigameId);
            memory.rewardEarned = IsRewardEarned(memory.rewardId);
        }
    }

    private void EnsureProgressLists()
    {
        if (Progress == null)
            Progress = new ExplorationProgress();

        if (Progress.unlockedMemories == null)
            Progress.unlockedMemories = new List<string>();

        if (Progress.completedMinigames == null)
            Progress.completedMinigames = new List<string>();

        if (Progress.discoveredCollectibles == null)
            Progress.discoveredCollectibles = new List<string>();

        if (Progress.earnedRewards == null)
            Progress.earnedRewards = new List<string>();
    }

    private string GetSaveKey()
    {
        return GetUserScopedKey(SAVE_KEY, activeUserId);
    }

    private void SwitchToUser(string userId)
    {
        string previousUserId = activeUserId;
        ExplorationProgress inMemoryProgress = Progress;

        activeUserId = userId;

        if (!string.IsNullOrEmpty(userId))
            AdoptProgressDuringUserTransition(previousUserId, inMemoryProgress);

        Load();
        RefreshMemoryStates();
        lastExplorationPercent = GetExplorationPercent();
    }

    private void AdoptProgressDuringUserTransition(
        string previousUserId,
        ExplorationProgress inMemoryProgress
    )
    {
        string userSaveKey = GetSaveKey();

        // Una cuenta que ya tiene guardado propio nunca debe heredar el
        // progreso local ni el de la cuenta que estaba activa anteriormente.
        if (PlayerPrefs.HasKey(userSaveKey))
            return;

        ExplorationProgress bestProgress = GetBestMigrationProgress(
            previousUserId,
            inMemoryProgress
        );

        bestProgress = PickProgressWithHigherPercent(
            LoadProgressFromKey(userSaveKey),
            bestProgress
        );

        if (!HasAnyProgress(bestProgress))
            return;

        if (!ShouldReplaceSavedProgress(userSaveKey, bestProgress))
            return;

        Progress = bestProgress;
        EnsureProgressLists();
        Save();

        AppLog.Info(
            "PROGRESS",
            "Progreso local migrado a la cuenta actual."
        );
    }

    private void AdoptBestAvailableProgressForUser(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return;

        string userSaveKey = GetUserScopedKey(SAVE_KEY, userId);

        // La migracion solo aplica la primera vez, antes de que exista un
        // guardado asociado especificamente a esta cuenta.
        if (PlayerPrefs.HasKey(userSaveKey))
            return;

        ExplorationProgress bestProgress =
            GetBestMigrationProgress(string.Empty, Progress);

        bestProgress = PickProgressWithHigherPercent(
            LoadProgressFromKey(userSaveKey),
            bestProgress
        );

        if (!HasAnyProgress(bestProgress))
            return;

        if (!ShouldReplaceSavedProgress(userSaveKey, bestProgress))
            return;

        Progress = bestProgress;
        EnsureProgressLists();
        Save();

        AppLog.Info(
            "PROGRESS",
            "Progreso previo migrado a la cuenta actual."
        );
    }

    private ExplorationProgress GetBestMigrationProgress(
        string previousUserId,
        ExplorationProgress inMemoryProgress
    )
    {
        ExplorationProgress bestProgress =
            CloneProgress(inMemoryProgress);

        bestProgress = PickProgressWithHigherPercent(
            bestProgress,
            LoadProgressFromKey(GetUserScopedKey(SAVE_KEY, string.Empty))
        );

        bestProgress = PickProgressWithHigherPercent(
            bestProgress,
            LoadProgressFromKey(SAVE_KEY)
        );

        if (!string.IsNullOrEmpty(previousUserId))
        {
            bestProgress = PickProgressWithHigherPercent(
                bestProgress,
                LoadProgressFromKey(GetUserScopedKey(SAVE_KEY, previousUserId))
            );
        }

        return bestProgress;
    }

    private ExplorationProgress PickProgressWithHigherPercent(
        ExplorationProgress current,
        ExplorationProgress candidate
    )
    {
        if (!HasAnyProgress(candidate))
            return current;

        if (!HasAnyProgress(current))
            return candidate;

        int currentPercent = CalculateExplorationPercent(current);
        int candidatePercent = CalculateExplorationPercent(candidate);

        return candidatePercent > currentPercent
            ? candidate
            : current;
    }

    private bool ShouldReplaceSavedProgress(
        string saveKey,
        ExplorationProgress candidate
    )
    {
        ExplorationProgress savedProgress =
            LoadProgressFromKey(saveKey);

        if (!HasAnyProgress(savedProgress))
            return true;

        return CalculateExplorationPercent(candidate) >
            CalculateExplorationPercent(savedProgress);
    }

    private ExplorationProgress LoadProgressFromKey(string key)
    {
        if (string.IsNullOrEmpty(key) || !PlayerPrefs.HasKey(key))
            return null;

        string json = PlayerPrefs.GetString(key);

        if (string.IsNullOrWhiteSpace(json))
            return null;

        ExplorationProgress loadedProgress =
            JsonUtility.FromJson<ExplorationProgress>(json);

        EnsureProgressLists(loadedProgress);
        return loadedProgress;
    }

    private ExplorationProgress CloneProgress(ExplorationProgress source)
    {
        if (source == null)
            return null;

        ExplorationProgress clone =
            JsonUtility.FromJson<ExplorationProgress>(
                JsonUtility.ToJson(source)
            );

        EnsureProgressLists(clone);
        return clone;
    }

    private bool HasAnyProgress(ExplorationProgress progress)
    {
        EnsureProgressLists(progress);

        return progress != null &&
            (
                progress.unlockedMemories.Count > 0 ||
                progress.completedMinigames.Count > 0 ||
                progress.discoveredCollectibles.Count > 0 ||
                progress.earnedRewards.Count > 0
            );
    }

    private int CalculateExplorationPercent(ExplorationProgress progress)
    {
        EnsureDefaultMemories();
        EnsureProgressLists(progress);

        if (progress == null)
            return 0;

        int totalSteps = memories.Count * 3;

        if (totalSteps == 0)
            return 0;

        int completedSteps = 0;

        for (int i = 0; i < memories.Count; i++)
        {
            MemoryData memory = memories[i];

            if (progress.unlockedMemories.Contains(memory.id))
                completedSteps++;

            if (progress.completedMinigames.Contains(memory.minigameId))
                completedSteps++;

            if (progress.earnedRewards.Contains(memory.rewardId))
                completedSteps++;
        }

        return Mathf.RoundToInt(completedSteps * 100f / totalSteps);
    }

    private void EnsureProgressLists(ExplorationProgress progress)
    {
        if (progress == null)
            return;

        if (progress.unlockedMemories == null)
            progress.unlockedMemories = new List<string>();

        if (progress.completedMinigames == null)
            progress.completedMinigames = new List<string>();

        if (progress.discoveredCollectibles == null)
            progress.discoveredCollectibles = new List<string>();

        if (progress.earnedRewards == null)
            progress.earnedRewards = new List<string>();
    }

    public static string GetCurrentUserScopedKey(string baseKey)
    {
        return GetUserScopedKey(baseKey, ResolveCurrentUserId());
    }

    private static string GetUserScopedKey(string baseKey, string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return baseKey + "_LOCAL";

        return baseKey + "_" + userId;
    }

    private static string ResolveCurrentUserId()
    {
        if (FirebaseManager.User != null &&
            !string.IsNullOrEmpty(FirebaseManager.User.UserId))
        {
            return FirebaseManager.User.UserId;
        }

        if (FirebaseManager.Auth != null &&
            FirebaseManager.Auth.CurrentUser != null &&
            !string.IsNullOrEmpty(FirebaseManager.Auth.CurrentUser.UserId))
        {
            return FirebaseManager.Auth.CurrentUser.UserId;
        }

        return string.Empty;
    }

    private void EnsureDefaultMemories()
    {
        if (memories != null && memories.Count > 0)
            return;

        memories = new List<MemoryData>
        {
            CreateMemory(
                ExplorationIds.NINO_POZO,
                "El Nino del Pozo",
                "Pozo",
                "Gema del Pozo",
                "Busca el Image Target del pozo para revelar esta memoria."
            ),
            CreateMemory(
                ExplorationIds.TIO_ADOLFO,
                "Tio Adolfo",
                "Puzzle Adolfo",
                "Gema de Adolfo",
                "Busca el retrato o pista asociada a Tio Adolfo."
            ),
            CreateMemory(
                ExplorationIds.GUAGUA_MAYOR,
                "La Guagua Mayor",
                "Prensa de Queso",
                "Gema de la Guagua Mayor",
                "Busca la prensa de queso para desbloquear esta memoria."
            ),
            CreateMemory(
                ExplorationIds.JUAN_MANUEL_LASSO,
                "Juan Manuel Lasso",
                "Smoke Game",
                "Gema de Juan Manuel Lasso",
                "Busca el punto historico de Juan Manuel Lasso."
            ),
            CreateMemory(
                ExplorationIds.LOS_NOVIOS,
                "Los Novios",
                "Flores",
                "Gema de los Novios",
                "Busca el Image Target relacionado con los novios."
            ),
            CreateMemory(
                ExplorationIds.PADRE_MARIO_CICALA,
                "Padre Mario Cicala",
                "Aguila Bicefala",
                "Gema del Padre Mario Cicala",
                "Busca el simbolo del aguila bicefala."
            ),
            CreateMemory(
                ExplorationIds.GRAN_MATRIARCA,
                "La Gran Matriarca",
                "Caja Fuerte",
                "Gema de la Gran Matriarca",
                "Busca el punto final de la Matriarca."
            )
        };
    }

    private bool HasConfiguredMemoryAssets()
    {
        if (memories == null)
            return false;

        for (int i = 0; i < memories.Count; i++)
        {
            MemoryData memory = memories[i];

            if (
                memory.portrait != null
                || memory.lockedPortrait != null
                || memory.scanSilhouette != null
            )
            {
                return true;
            }
        }

        return false;
    }

    private MemoryData CreateMemory(
        string id,
        string guardianName,
        string minigameName,
        string rewardName,
        string scanHint
    )
    {
        return new MemoryData
        {
            id = id,
            guardianName = guardianName,
            title = guardianName,
            description = "Memoria historica de " + guardianName + ".",
            minigameId = id + "_minigame",
            minigameName = minigameName,
            rewardId = id + "_gem",
            rewardName = rewardName,
            scanHint = scanHint
        };
    }
}
