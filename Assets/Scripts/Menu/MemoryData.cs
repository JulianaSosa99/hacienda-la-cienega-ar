using System;
using UnityEngine;

[Serializable]
public class MemoryData
{
    public string id;
    public string guardianName;
    public string title;
    public string description;

    public string minigameId;
    public string minigameName;
    public string rewardId;
    public string rewardName;
    public string scanHint;

    [Header("Detalle")]
    public string guardianFoundText;
    public string guardianMissingText;
    public string minigameCompletedText;
    public string minigamePendingText;
    public string rewardTitleText;

    public Sprite portrait;
    public Sprite lockedPortrait;
    public Sprite scanSilhouette;
    public Sprite rewardSprite;
    public Sprite lockedRewardSprite;

    public bool isUnlocked;
    public bool minigameCompleted;
    public bool rewardEarned;
}