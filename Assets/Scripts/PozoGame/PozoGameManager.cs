using UnityEngine;
using TMPro;

public class PozoGameManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text feedbackText;

    public TMP_Text counterText;

    [Header("Riddle")]
    public TMP_Text titleText;
    public TMP_Text instructionText;
    public TMP_Text riddleText;

    [TextArea(4, 10)]
    public string riddleMessage;

    [TextArea(2, 5)]
    public string instructionMessage;

    [Header("Panels")]
    public GameObject gamePanel;

    [Header("Audio")]
    public AudioSource audioSource;

    public AudioClip correctSound;
    public AudioClip wrongSound;
    public AudioClip winSound;

    [Header("Gameplay")]
    public int totalCorrect = 4;

    public int helpThreshold = 3;

    public ItemSelection[] allItems;

    private int correctCount = 0;
    private int wrongCount = 0;

    private bool helpActivated = false;
    private bool gameFinished = false;

    void Start()
    {
        SetupTexts();

        UpdateCounter();

        feedbackText.text = "Descifra el acertijo...";
    }

    // =========================================
    // CONFIG TEXTS
    // =========================================

    void SetupTexts()
    {
        if (titleText != null)
        {
            titleText.text = "✦ EL PORTAL DEL POZO ✦";
        }

        if (instructionText != null)
        {
            instructionText.text = instructionMessage;
        }

        if (riddleText != null)
        {
            riddleText.text = riddleMessage;
        }
    }

    // =========================================
    // CORRECTO
    // =========================================

    public void RegisterCorrect()
    {
        if (gameFinished)
            return;

        correctCount++;

        UpdateCounter();

        feedbackText.text = "✔ El portal responde...";

        PlaySound(correctSound);

        if (correctCount >= totalCorrect)
        {
            gameFinished = true;

            Invoke(nameof(WinGame), 1.5f);
        }
    }

    // =========================================
    // INCORRECTO
    // =========================================

    public void RegisterWrong()
    {
        if (gameFinished)
            return;

        wrongCount++;

        feedbackText.text = "✖ Esto no pertenece al portal";

        PlaySound(wrongSound);

        if (!helpActivated && wrongCount >= helpThreshold)
        {
            ActivateHelp();

            helpActivated = true;
        }
    }

    // =========================================
    // AYUDA
    // =========================================

    void ActivateHelp()
    {
        foreach (ItemSelection item in allItems)
        {
            if (item != null && item.isCorrect)
            {
                item.ShowHint();
            }
        }

        feedbackText.text = "✨ El portal revela pistas...";
    }

    // =========================================
    // CONTADOR
    // =========================================

    void UpdateCounter()
    {
        if (counterText != null)
        {
            counterText.text =
                "Objetos encontrados: "
                + correctCount
                + " / "
                + totalCorrect;
        }
    }

    // =========================================
    // SONIDOS
    // =========================================

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // =========================================
    // GANAR
    // =========================================

    void WinGame()
    {
        feedbackText.text = "✨ El portal se ha abierto...";

        PlaySound(winSound);

        Invoke(nameof(CloseGame), 2f);
    }

    // =========================================
    // CERRAR
    // =========================================

    void CloseGame()
    {
        gamePanel.SetActive(false);

        PozoNarrativeManager manager =
            FindObjectOfType<PozoNarrativeManager>();

        if (manager != null)
        {
            manager.OnPozoCompleted();
        }

        UnityEngine.Debug.Log("Pozo completado");
    }
}