using UnityEngine;

public class IntroExperienceUI : MonoBehaviour
{
    [Header("Pantallas")]
    [SerializeField] private GameObject introPanel;
    [SerializeField] private GameObject menuPanel;

    [Header("Marco")]
    [SerializeField] private GameObject logoHacienda;
    [SerializeField] private GameObject memoriesTitle;

    private void Start()
    {
        ShowIntro();
    }

    public void ShowIntro()
    {
        if (ExplorationProgressBarUI.Instance != null)
            ExplorationProgressBarUI.Instance.Hide();

        if (introPanel != null)
            introPanel.SetActive(true);

        if (menuPanel != null)
            menuPanel.SetActive(false);

        if (logoHacienda != null)
            logoHacienda.SetActive(true);

        if (memoriesTitle != null)
            memoriesTitle.SetActive(false);
    }

    public void OpenMenu()
    {
        if (ExplorationProgressBarUI.Instance != null)
            ExplorationProgressBarUI.Instance.Hide();

        if (introPanel != null)
            introPanel.SetActive(false);

        if (menuPanel != null)
            menuPanel.SetActive(true);
    }
}
