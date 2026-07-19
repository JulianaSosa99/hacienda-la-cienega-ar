using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum CheeseType
{
    MuyTierno,
    Tierno,
    Semicurado,
    Maduro,
    MuyMaduro
}

public class CheeseGameController : MonoBehaviour
{

    [Header("Orders")]
    [SerializeField] private TMP_Text leftOrderText;
    [SerializeField] private TMP_Text rightOrderText;

    [Header("Cheese Press")]
    [SerializeField] private Image cheeseLeft;
    [SerializeField] private Image cheeseRight;

    [SerializeField] private RectTransform pressTopLeft;
    [SerializeField] private RectTransform pressTopRight;

    [Header("Pressure Bars")]
    [SerializeField] private Image leftPressureBar;
    [SerializeField] private Image rightPressureBar;

    [Header("Pressure Labels")]
    [SerializeField] private TMP_Text leftPressureLabel;
    [SerializeField] private TMP_Text rightPressureLabel;

    [Header("Press Button")]
    [SerializeField] private Button pressButton;

    [SerializeField] private Image pressButtonImage;

    [SerializeField] private Sprite normalButtonSprite;
    [SerializeField] private Sprite pressedButtonSprite;

    [SerializeField] private float pressDistance = 45f;

    [SerializeField] private float pressDuration = 0.25f;

    [Header("Feedback")]
    [SerializeField] private TMP_Text feedbackText;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private AudioClip leverSound;
    [SerializeField] private AudioClip pressSound;
    [SerializeField] private AudioClip successSound;
    [SerializeField] private AudioClip failSound;

    [Header("Cheese Sprites")]
    [SerializeField] private Sprite verySoftCheese;
    [SerializeField] private Sprite softCheese;
    [SerializeField] private Sprite semiCuredCheese;
    [SerializeField] private Sprite matureCheese;
    [SerializeField] private Sprite veryMatureCheese;

    [Header("Left Pressure Bar Sprites")]
    [SerializeField] private Sprite leftPressureLevel1;
    [SerializeField] private Sprite leftPressureLevel2;
    [SerializeField] private Sprite leftPressureLevel3;
    [SerializeField] private Sprite leftPressureLevel4;
    [SerializeField] private Sprite leftPressureLevel5;

    [Header("Right Pressure Bar Sprites")]
    [SerializeField] private Sprite rightPressureLevel1;
    [SerializeField] private Sprite rightPressureLevel2;
    [SerializeField] private Sprite rightPressureLevel3;
    [SerializeField] private Sprite rightPressureLevel4;
    [SerializeField] private Sprite rightPressureLevel5;

    private int leftLevel = 1;
    private int rightLevel = 1;

    private CheeseType leftOrder;
    private CheeseType rightOrder;

    private bool gameCompleted = false;

    // NARRATIVE
    
    [SerializeField] private GameObject gamePanel;

    [SerializeField] private GuaguaNarrativeManager narrativeManager;

    private bool completionNotified = false;

    private const int MinLevel = 1;
    private const int MaxLevel = 5;

    private void Start()
    {
        GenerateOrders();

        UpdatePressureUI();

        cheeseLeft.sprite = verySoftCheese;
        cheeseRight.sprite = verySoftCheese;

        pressButtonImage.sprite = normalButtonSprite;

        feedbackText.text =
            "Ajusta las prensas para cumplir el pedido";
    }

    // ORDERS

    private void GenerateOrders()
    {
        leftOrder = (CheeseType)Random.Range(0, 5);

        do
        {
            rightOrder = (CheeseType)Random.Range(0, 5);
        }
        while (rightOrder == leftOrder);

        leftOrderText.text =
            GetCheeseName(GetTargetLevel(leftOrder));

        rightOrderText.text =
            GetCheeseName(GetTargetLevel(rightOrder));
    }

    // PRESSURE UI

    private void UpdatePressureUI()
    {
        // Barras
        leftPressureBar.sprite =
            GetLeftPressureBarSprite(leftLevel);

        rightPressureBar.sprite =
            GetRightPressureBarSprite(rightLevel);

        // Texto
        leftPressureLabel.text =
            GetCheeseName(leftLevel);

        rightPressureLabel.text =
            GetCheeseName(rightLevel);
    }

    // CHEESE NAMES

    private string GetCheeseName(int level)
    {
        switch (level)
        {
            case 1:
                return "Muy Tierno";

            case 2:
                return "Tierno";

            case 3:
                return "Semicurado";

            case 4:
                return "Maduro";

            case 5:
                return "Muy Maduro";

            default:
                return "";
        }
    }

    // LEFT PRESSURE BAR

    private Sprite GetLeftPressureBarSprite(int level)
    {
        switch (level)
        {
            case 1:
                return leftPressureLevel1;

            case 2:
                return leftPressureLevel2;

            case 3:
                return leftPressureLevel3;

            case 4:
                return leftPressureLevel4;

            case 5:
                return leftPressureLevel5;

            default:
                return leftPressureLevel1;
        }
    }

    // RIGHT PRESSURE BAR

    private Sprite GetRightPressureBarSprite(int level)
    {
        switch (level)
        {
            case 1:
                return rightPressureLevel1;

            case 2:
                return rightPressureLevel2;

            case 3:
                return rightPressureLevel3;

            case 4:
                return rightPressureLevel4;

            case 5:
                return rightPressureLevel5;

            default:
                return rightPressureLevel1;
        }
    }

    // CHEESE SPRITES

    private Sprite GetCheeseSprite(int level)
    {
        switch (level)
        {
            case 1:
                return verySoftCheese;

            case 2:
                return softCheese;

            case 3:
                return semiCuredCheese;

            case 4:
                return matureCheese;

            case 5:
                return veryMatureCheese;

            default:
                return verySoftCheese;
        }
    }

    // TARGET LEVEL

    private int GetTargetLevel(CheeseType cheeseType)
    {
        switch (cheeseType)
        {
            case CheeseType.MuyTierno:
                return 1;

            case CheeseType.Tierno:
                return 2;

            case CheeseType.Semicurado:
                return 3;

            case CheeseType.Maduro:
                return 4;

            case CheeseType.MuyMaduro:
                return 5;

            default:
                return 1;
        }
    }

    // LEFT CONTROLS

    public void IncreaseLeft()
    {
        if (gameCompleted)
            return;

        if (leftLevel >= MaxLevel)
            return;

        leftLevel++;

        PlaySound(leverSound);

        UpdatePressureUI();
    }

    public void DecreaseLeft()
    {
        if (gameCompleted)
            return;

        if (leftLevel <= MinLevel)
            return;

        leftLevel--;

        PlaySound(leverSound);

        UpdatePressureUI();
    }

    // RIGHT CONTROLS

    public void IncreaseRight()
    {
        if (gameCompleted)
            return;

        if (rightLevel >= MaxLevel)
            return;

        rightLevel++;

        PlaySound(leverSound);

        UpdatePressureUI();
    }

    public void DecreaseRight()
    {
        if (gameCompleted)
            return;

        if (rightLevel <= MinLevel)
            return;

        rightLevel--;

        PlaySound(leverSound);

        UpdatePressureUI();
    }

    // AUDIO

    private void PlaySound(AudioClip clip)
    {
        if (audioSource == null)
            return;

        if (clip == null)
            return;

        audioSource.PlayOneShot(clip);
    }

    // PRESS BUTTON
    
    public void PressCheese()
    {
        if (gameCompleted)
            return;

        pressButton.interactable = false;

        StartCoroutine(ProcessPress());
    }

    private IEnumerator ProcessPress()
    {
        pressButtonImage.sprite = pressedButtonSprite;

        pressButton.interactable = false;

        PlaySound(pressSound);

        yield return StartCoroutine(PressDown());

        cheeseLeft.enabled = false;
        cheeseRight.enabled = false;

        yield return new WaitForSeconds(0.15f);

        cheeseLeft.sprite =
            GetCheeseSprite(leftLevel);

        cheeseRight.sprite =
            GetCheeseSprite(rightLevel);

        cheeseLeft.enabled = true;
        cheeseRight.enabled = true;

        yield return StartCoroutine(PressUp());

        pressButtonImage.sprite = normalButtonSprite;

        pressButton.interactable = true;

        CheckWin();
    }

    private IEnumerator PressDown()
    {
        Vector2 leftStart = pressTopLeft.anchoredPosition;
        Vector2 rightStart = pressTopRight.anchoredPosition;

        Vector2 leftEnd =
            leftStart + Vector2.down * pressDistance;

        Vector2 rightEnd =
            rightStart + Vector2.down * pressDistance;

        float timer = 0f;

        while (timer < pressDuration)
        {
            timer += Time.deltaTime;

            float t = timer / pressDuration;

            pressTopLeft.anchoredPosition =
                Vector2.Lerp(leftStart, leftEnd, t);

            pressTopRight.anchoredPosition =
                Vector2.Lerp(rightStart, rightEnd, t);

            yield return null;
        }

        pressTopLeft.anchoredPosition = leftEnd;
        pressTopRight.anchoredPosition = rightEnd;
    }

    private IEnumerator PressUp()
    {
        Vector2 leftEnd = pressTopLeft.anchoredPosition;
        Vector2 rightEnd = pressTopRight.anchoredPosition;

        Vector2 leftStart =
            leftEnd + Vector2.up * pressDistance;

        Vector2 rightStart =
            rightEnd + Vector2.up * pressDistance;

        float timer = 0f;

        while (timer < pressDuration)
        {
            timer += Time.deltaTime;

            float t = timer / pressDuration;

            pressTopLeft.anchoredPosition =
                Vector2.Lerp(leftEnd, leftStart, t);

            pressTopRight.anchoredPosition =
                Vector2.Lerp(rightEnd, rightStart, t);

            yield return null;
        }

        pressTopLeft.anchoredPosition = leftStart;
        pressTopRight.anchoredPosition = rightStart;
    }

    private IEnumerator PressAnimation()
    {
        Vector2 leftStart =
            pressTopLeft.anchoredPosition;

        Vector2 rightStart =
            pressTopRight.anchoredPosition;

        Vector2 leftEnd =
            leftStart + Vector2.down * pressDistance;

        Vector2 rightEnd =
            rightStart + Vector2.down * pressDistance;

        float timer = 0;

        while (timer < pressDuration)
        {
            timer += Time.deltaTime;

            float t = timer / pressDuration;

            pressTopLeft.anchoredPosition =
                Vector2.Lerp(leftStart, leftEnd, t);

            pressTopRight.anchoredPosition =
                Vector2.Lerp(rightStart, rightEnd, t);

            yield return null;
        }

        yield return new WaitForSeconds(0.2f);

        timer = 0;

        while (timer < pressDuration)
        {
            timer += Time.deltaTime;

            float t = timer / pressDuration;

            pressTopLeft.anchoredPosition =
                Vector2.Lerp(leftEnd, leftStart, t);

            pressTopRight.anchoredPosition =
                Vector2.Lerp(rightEnd, rightStart, t);

            yield return null;
        }

        pressTopLeft.anchoredPosition =
            leftStart;

        pressTopRight.anchoredPosition =
            rightStart;
    }

    // WIN CHECK
    
    private void CheckWin()
    {
        bool leftCorrect =
            leftLevel == GetTargetLevel(leftOrder);

        bool rightCorrect =
            rightLevel == GetTargetLevel(rightOrder);

        if (leftCorrect && rightCorrect)
        {
            gameCompleted = true;

            feedbackText.text =
                "¡Excelente trabajo!\nLos quesos fueron prensados correctamente.";

            PlaySound(successSound);

            StartCoroutine(FinishGame());

            return;
        }

        feedbackText.text =
            "El resultado no coincide\ncon el pedido.";

        PlaySound(failSound);
    }

    private IEnumerator FinishGame()
    {
        yield return new WaitForSeconds(3f);

        CloseGame();
    }

    private void CloseGame()
    {
        if (gamePanel != null)
        {
            gamePanel.SetActive(false);
        }

        NotifyNarrativeCompleted();
    }

    private void NotifyNarrativeCompleted()
    {
        if (completionNotified)
            return;

        completionNotified = true;

        if (narrativeManager == null)
        {
            narrativeManager = FindObjectOfType<GuaguaNarrativeManager>();
        }

        if (narrativeManager != null)
        {
            narrativeManager.OnGameCompleted();
            return;
        }

        if (ExplorationManager.Instance != null)
        {
            ExplorationManager.Instance.RegisterMinigameCompleted(
                ExplorationIds.GUAGUA_MAYOR
            );
        }

        NarrativeFlowUI.ExitMinigameToNarrative();
    }
}