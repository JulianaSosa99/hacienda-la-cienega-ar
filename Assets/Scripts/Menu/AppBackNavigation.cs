using UnityEngine;

public class AppBackNavigation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MemoryListUI memoryListUI;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleBack();
        }
    }

    public void HandleBack()
    {
        ResolveReferences();

        if (NarrativeFlowUI.CloseDiscoveryPromptForScanning())
            return;

        if (memoryListUI != null && memoryListUI.IsDetailOpen)
        {
            memoryListUI.ShowList();
            AppLog.Info("NAVIGATION", "Back: detalle a lista de memorias");
            return;
        }

        if (memoryListUI != null && memoryListUI.IsMemoriesOpen)
        {
            memoryListUI.OpenScanner();
            AppLog.Info("NAVIGATION", "Back: memorias a escaneo");
            return;
        }

        if (memoryListUI != null)
        {
            memoryListUI.OpenMemories();
            AppLog.Info("NAVIGATION", "Back: escaneo a memorias");
        }
    }

    private void ResolveReferences()
    {
        if (memoryListUI == null)
            memoryListUI = FindObjectOfType<MemoryListUI>(true);
    }
}