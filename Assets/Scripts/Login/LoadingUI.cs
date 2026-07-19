using UnityEngine;

public class LoadingUI : MonoBehaviour
{
    public static LoadingUI Instance;

    [SerializeField] private GameObject loadingPanel;

    private void Awake()
    {
        Instance = this;

        Hide();
    }

    public void Show()
    {
        loadingPanel.SetActive(true);
    }

    public void Hide()
    {
        loadingPanel.SetActive(false);
    }
}