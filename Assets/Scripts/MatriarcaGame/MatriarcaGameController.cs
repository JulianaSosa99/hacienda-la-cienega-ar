using System.Collections;
using TMPro;
using UnityEngine;

public class MatriarcaGameController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Dial dial;
    

    [Header("Configuración")]
    [SerializeField] private float validationDelay = 2f;

    [Header("Selección")]
    [SerializeField] private RectTransform selectionRing;
    [SerializeField] private RectTransform slot1Target;
    [SerializeField] private RectTransform slot2Target;
    [SerializeField] private RectTransform slot3Target;

    private int selectedSlot = 0;

    [Header("Combination UI")]
    [SerializeField] private TextMeshProUGUI number1;
    [SerializeField] private TextMeshProUGUI number2;
    [SerializeField] private TextMeshProUGUI number3;

    [Header("Combinación Correcta")]
    [SerializeField] private int[] correctCombination = new int[3] { 4, 7, 1 };

    [Header("Audio")]
    [SerializeField] private AudioClip winSound;
    [SerializeField] private AudioClip wrongSound;

    private AudioSource audioSource;

    private readonly int[] enteredCombination = new int[3];

    private bool[] slotEdited = new bool[3];

    private bool isCompleted = false;

    private Coroutine validationCoroutine;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        ClearCombinationUI();

        selectionRing.position = slot1Target.position;

        dial.OnNumberChanged += HandleDialNumberChanged;
    }

    private void OnDestroy()
    {
        if (dial != null)
        {
            dial.OnNumberChanged -= HandleDialNumberChanged;
        }
    }

    private void HandleDialNumberChanged(int number)
    {
        if (isCompleted)
            return;

        switch (selectedSlot)
        {
            case 0:
                number1.text = number.ToString();
                enteredCombination[0] = number;
                slotEdited[0] = true;
                break;

            case 1:
                number2.text = number.ToString();
                enteredCombination[1] = number;
                slotEdited[1] = true;
                break;

            case 2:
                number3.text = number.ToString();
                enteredCombination[2] = number;
                slotEdited[2] = true;
                break;
        }

        Debug.Log($"Slot {selectedSlot + 1} actualizado a {number}");

        StartValidationTimer();
    }

    private void StartValidationTimer()
    {
        if (!AllSlotsEdited())
            return;

        if (validationCoroutine != null)
        {
            StopCoroutine(validationCoroutine);
        }

        validationCoroutine = StartCoroutine(ValidationRoutine());
    }

    private IEnumerator ValidationRoutine()
    {
        yield return new WaitForSeconds(validationDelay);

        if (isCompleted)
            yield break;

        if (!AllSlotsEdited())
            yield break;

        Debug.Log("Validando combinación...");

        CheckCurrentCombination();
    }

    private bool AllSlotsEdited()
    {
        return
            slotEdited[0] &&
            slotEdited[1] &&
            slotEdited[2];
    }

    private void CheckCurrentCombination()
    {
        if (isCompleted)
            return;

        Debug.Log(
            $"Ingresada: {enteredCombination[0]}-{enteredCombination[1]}-{enteredCombination[2]}"
        );

        Debug.Log(
            $"Correcta: {correctCombination[0]}-{correctCombination[1]}-{correctCombination[2]}"
        );

        bool success = true;

        for (int i = 0; i < correctCombination.Length; i++)
        {
            if (enteredCombination[i] != correctCombination[i])
            {
                success = false;
                break;
            }
        }

        if (success)
        {
            Debug.Log("COMBINACION CORRECTA");

            if (audioSource != null && winSound != null)
            {
                audioSource.PlayOneShot(winSound);
            }

            isCompleted = true;

            dial.LockDial();

            OnCombinationSuccess();
        }
        else
        {
            Debug.Log("CLAVE INCORRECTA");

#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif

            if (audioSource != null && wrongSound != null)
            {
                audioSource.PlayOneShot(wrongSound);
            }

            ResetCombination();
        }
    }

    public void SelectSlot1()
    {
        if (isCompleted)
            return;

        selectedSlot = 0;
        selectionRing.position = slot1Target.position;

        Debug.Log("Slot 1 seleccionado");
    }

    public void SelectSlot2()
    {
        if (isCompleted)
            return;

        selectedSlot = 1;
        selectionRing.position = slot2Target.position;

        Debug.Log("Slot 2 seleccionado");
    }

    public void SelectSlot3()
    {
        if (isCompleted)
            return;

        selectedSlot = 2;
        selectionRing.position = slot3Target.position;

        Debug.Log("Slot 3 seleccionado");
    }

    public void ResetCombination()
    {
        if (isCompleted)
            return;

        for (int i = 0; i < enteredCombination.Length; i++)
        {
            enteredCombination[i] = 0;
        }

        for (int i = 0; i < slotEdited.Length; i++)
        {
            slotEdited[i] = false;
        }

        ClearCombinationUI();

        Debug.Log("Combinación reiniciada.");
    }

    private void ClearCombinationUI()
    {
        number1.text = "";
        number2.text = "";
        number3.text = "";
    }

    private void OnCombinationSuccess()
    {
        Debug.Log("MINIJUEGO COMPLETADO");

        StartCoroutine(CombinationSuccessRoutine());
    }

    private IEnumerator CombinationSuccessRoutine()
    {
        if (winSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(winSound);

            yield return new WaitForSeconds(winSound.length);
        }

        MatriarcaNarrativeManager manager =
            FindObjectOfType<MatriarcaNarrativeManager>();

        if (manager != null)
        {
            manager.OnGameCompleted();
        }
    }
}