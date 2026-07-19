using UnityEngine;
using UnityEngine.UI;

public class NarrativePromptCloseButtonUI : MonoBehaviour
{
    [SerializeField] private Button button;

    private void Awake()
    {
        ResolveReferences();
        WireButton();
    }

    private void OnEnable()
    {
        ResolveReferences();
        WireButton();
    }

    private void ResolveReferences()
    {
        if (button == null)
            button = GetComponent<Button>();
    }

    private void WireButton()
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(ClosePrompt);
        button.onClick.AddListener(ClosePrompt);
    }

    private void ClosePrompt()
    {
        NarrativeFlowUI.CloseDiscoveryPromptForScanning();
    }
}