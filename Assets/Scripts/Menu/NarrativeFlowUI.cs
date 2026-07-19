using UnityEngine;

public static class NarrativeFlowUI
{
    public static void ShowDiscoveryPrompt(CanvasGroup panel)
    {
        HideMemoryPanels();
        HideDiscoveryPrompts();
        CameraMemoryButtonUI.HideButton();
        ScanBackButtonUI.ShowButton();
        HideProgress();
        SetCanvasGroup(panel, true);

        if (panel != null)
            UIRaycastUtility.EnableButtons(panel.gameObject);
    }

    public static void HideDiscoveryPrompt(CanvasGroup panel)
    {
        SetCanvasGroup(panel, false);
    }

    public static void StartNarrative()
    {
        HideMemoryPanels();
        HideDiscoveryPrompts();
        CameraMemoryButtonUI.HideButton();
        ScanBackButtonUI.HideButton();
        ShowProgress();
    }

    public static void EnterMinigame(GameObject minigame)
    {
        HideDiscoveryPrompts();
        CameraMemoryButtonUI.HideButton();
        ScanBackButtonUI.HideButton();
        HideProgress();
        GameObjectActivationUtility.SetOnlyChildActiveWithParents(minigame);
    }

    public static void ExitMinigameToNarrative()
    {
        HideDiscoveryPrompts();
        CameraMemoryButtonUI.ShowButton();
        ScanBackButtonUI.HideButton();
        ShowProgress();
    }

    public static void ReturnToMemories(string memoryId = null)
    {
        HideDiscoveryPrompts();
        HideProgress();
        CameraMemoryButtonUI.HideButton();
        ScanBackButtonUI.HideButton();

        MemoryListUI[] memoryPanels = Object.FindObjectsOfType<MemoryListUI>(true);

        if (memoryPanels.Length == 0)
            return;

        MemoryListUI memoryPanel = memoryPanels[0];
        memoryPanel.OpenMemories();
        memoryPanel.Refresh();

        if (CompletionRewardPanelUI.TryShowAnyCompletedPanel())
            return;

        if (!string.IsNullOrWhiteSpace(memoryId))
            memoryPanel.ShowMemoryDetailById(memoryId);
    }

    public static void ShowProgress()
    {
        if (ExplorationProgressBarUI.Instance != null)
            ExplorationProgressBarUI.Instance.Show();
    }

    public static void HideProgress()
    {
        if (ExplorationProgressBarUI.Instance != null)
            ExplorationProgressBarUI.Instance.Hide();
    }

    public static void HideMemoryPanels()
    {
        MemoryListUI[] memoryPanels =
            Object.FindObjectsOfType<MemoryListUI>(true);

        for (int i = 0; i < memoryPanels.Length; i++)
        {
            memoryPanels[i].CloseForNarrative();
        }
    }

    public static void HideDiscoveryPrompts()
    {
        CanvasGroup[] groups = Object.FindObjectsOfType<CanvasGroup>(true);

        for (int i = 0; i < groups.Length; i++)
        {
            CanvasGroup group = groups[i];

            if (group == null)
                continue;

            if (!IsNarrativePrompt(group.gameObject.name))
                continue;

            SetCanvasGroup(group, false);
        }
    }

    public static bool CloseDiscoveryPromptForScanning()
    {
        bool closedAnyPrompt = false;
        CanvasGroup[] groups = Object.FindObjectsOfType<CanvasGroup>(true);

        for (int i = 0; i < groups.Length; i++)
        {
            CanvasGroup group = groups[i];

            if (group == null)
                continue;

            if (!IsNarrativePrompt(group.gameObject.name))
                continue;

            if (!group.gameObject.activeInHierarchy && group.alpha <= 0f)
                continue;

            SetCanvasGroup(group, false);
            closedAnyPrompt = true;
        }

        if (!closedAnyPrompt)
            return false;

        TargetUIBlocker.ResetAllForScanning();
        CameraMemoryButtonUI.ShowButton();
        ScanBackButtonUI.ShowButton();
        HideProgress();

        AppLog.Info("NAVIGATION", "Cerrar panel narrativo y volver a escaneo");
        return true;
    }

    private static bool IsNarrativePrompt(string objectName)
    {
        return
            objectName == "PozoNarrativePanel" ||
            objectName == "PuzzleNarrativePanel" ||
            objectName == "FlowersNarrativePanel" ||
            objectName == "SmokeNarrativePanel" ||
            objectName == "GuaguaNarrativePanel" ||
            objectName == "PadreMarioNarrativePanel" ||
            objectName == "MatriarcaNarrativePanel";
    }

    private static void SetCanvasGroup(CanvasGroup group, bool visible)
    {
        if (group == null)
            return;

        group.gameObject.SetActive(visible);
        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }
}
