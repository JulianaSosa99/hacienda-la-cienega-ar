using TMPro;
using UnityEngine;

public class MemoryCollectionUI : MonoBehaviour
{
    [SerializeField] private TMP_Text guardiansText;
    [SerializeField] private TMP_Text minigamesText;
    [SerializeField] private TMP_Text rewardsText;
    [SerializeField] private TMP_Text collectiblesText;

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        ExplorationManager manager = ExplorationManager.Instance;

        if (manager == null)
            return;

        int guardians = 0;
        int minigames = 0;
        int rewards = 0;
        int total = manager.Memories.Count;

        for (int i = 0; i < manager.Memories.Count; i++)
        {
            MemoryData memory = manager.Memories[i];

            if (memory.isUnlocked)
                guardians++;

            if (memory.minigameCompleted)
                minigames++;

            if (memory.rewardEarned)
                rewards++;
        }

        if (guardiansText != null)
            guardiansText.text = "Guardianes " + guardians + " / " + total;

        if (minigamesText != null)
            minigamesText.text = "Minijuegos " + minigames + " / " + total;

        if (rewardsText != null)
            rewardsText.text = "Gemas " + rewards + " / " + total;

        if (collectiblesText != null)
        {
            collectiblesText.text =
                "Objetos " + manager.Progress.discoveredCollectibles.Count;
        }
    }
}
