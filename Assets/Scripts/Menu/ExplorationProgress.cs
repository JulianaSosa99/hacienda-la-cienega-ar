using System.Collections.Generic;

[System.Serializable]
public class ExplorationProgress
{
    // Datos persistidos en PlayerPrefs como JSON.
    public List<string> unlockedMemories =
        new List<string>();

    public List<string> completedMinigames =
        new List<string>();

    public List<string> discoveredCollectibles =
        new List<string>();

    public List<string> earnedRewards =
        new List<string>();
}
