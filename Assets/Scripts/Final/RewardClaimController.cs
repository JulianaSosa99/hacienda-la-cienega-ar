using System;
using System.Collections;
using System.Collections.Generic;
using Firebase.Functions;
using Firebase.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardClaimController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button claimButton;

    [SerializeField] private TMP_Text buttonText;

    [SerializeField] private TMP_Text statusText;

    private const string DefaultButtonText = "Reclamar Premio";
    private const string ClaimingButtonText = "Reclamando...";
    private const string ClaimedButtonText = "Premio reclamado";
    private const string AlreadyClaimedButtonText = "Ya reclamado";

    private const string ProcessingText = "Procesando solicitud...";
    private const string WaitingAuthText = "Preparando tu sesion...";

    private Coroutine readinessRoutine;
    private bool listenerRegistered;

    private void Start()
    {
        ResolveReferences();

        if (statusText != null)
            statusText.text = string.Empty;

        SetClaimButtonInteractable(false);

        RegisterClickListener();

        StartReadinessCheck();
    }

    private void OnEnable()
    {
        ResolveReferences();
        StartReadinessCheck();
    }

    private void OnDisable()
    {
        if (readinessRoutine != null)
        {
            StopCoroutine(readinessRoutine);
            readinessRoutine = null;
        }
    }

    private IEnumerator WaitForRewardReadiness()
    {
        SetClaimButtonInteractable(false);

        if (statusText != null)
            statusText.text = WaitingAuthText;

        while (!IsRewardServiceReady())
            yield return null;

        if (statusText != null)
            statusText.text = string.Empty;

        SetClaimButtonInteractable(true);

        AppLog.Info(
            "REWARD",
            "Firebase y usuario listos para reclamar premio."
        );
    }

    private void OnDestroy()
    {
        if (claimButton != null && listenerRegistered)
            claimButton.onClick.RemoveListener(OnClaimButtonPressed);
    }

    public void Configure(Button configuredButton)
    {
        if (claimButton != null && listenerRegistered)
            claimButton.onClick.RemoveListener(OnClaimButtonPressed);

        claimButton = configuredButton;
        listenerRegistered = false;

        ResolveReferences();
        RegisterClickListener();
        StartReadinessCheck();
    }

    private async void OnClaimButtonPressed()
    {
        SetClaimButtonInteractable(false);

        if (buttonText != null)
            buttonText.text = ClaimingButtonText;

        if (statusText != null)
            statusText.text = ProcessingText;
        try
        {
            FirebaseUser user = ResolveAuthenticatedUser();

            if (user == null)
            {
                if (statusText != null)
                    statusText.text = "Inicia sesion para reclamar el premio.";

                if (buttonText != null)
                    buttonText.text = DefaultButtonText;

                StartReadinessCheck();
                return;
            }

            if (statusText != null)
                statusText.text = ProcessingText;

            await user.TokenAsync(true);

            var callable =
                FirebaseManager.Functions.GetHttpsCallable("claimReward");

            var result = await callable.CallAsync();

            if (!TryReadClaimResponse(result.Data, out bool success, out string message))
                throw new Exception("Formato de respuesta invalido.");

            if (statusText != null)
                statusText.text = message;

            if (buttonText != null)
            {
                buttonText.text = success
                    ? ClaimedButtonText
                    : AlreadyClaimedButtonText;
            }

            AppLog.Info(
                "REWARD",
                $"Respuesta Cloud Function: {message}"
            );

            SetClaimButtonInteractable(false);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);

            AppLog.Error(
                "REWARD",
                ex.ToString()
            );

            if (statusText != null)
                statusText.text = ex.Message;

            if (buttonText != null)
                buttonText.text = DefaultButtonText;

            SetClaimButtonInteractable(true);
        }
    }

    private void ResolveReferences()
    {
        if (claimButton == null)
            claimButton = GetComponent<Button>();

        if (buttonText == null && claimButton != null)
            buttonText = claimButton.GetComponentInChildren<TMP_Text>(true);
    }

    private void RegisterClickListener()
    {
        if (claimButton == null || listenerRegistered)
            return;

        claimButton.onClick.RemoveListener(OnClaimButtonPressed);
        claimButton.onClick.AddListener(OnClaimButtonPressed);
        listenerRegistered = true;
    }

    private void StartReadinessCheck()
    {
        if (!isActiveAndEnabled)
            return;

        if (readinessRoutine != null)
            StopCoroutine(readinessRoutine);

        readinessRoutine = StartCoroutine(WaitForRewardReadiness());
    }

    private bool IsRewardServiceReady()
    {
        if (!FirebaseManager.IsInitialized ||
            FirebaseManager.Auth == null ||
            FirebaseManager.Functions == null)
        {
            return false;
        }

        return ResolveAuthenticatedUser() != null;
    }

    private FirebaseUser ResolveAuthenticatedUser()
    {
        FirebaseUser user = null;

        if (FirebaseManager.Auth != null)
            user = FirebaseManager.Auth.CurrentUser;

        if (user == null && FirebaseAuth.DefaultInstance != null)
            user = FirebaseAuth.DefaultInstance.CurrentUser;

        if (user != null)
            FirebaseManager.User = user;

        return user;
    }

    private void SetClaimButtonInteractable(bool interactable)
    {
        if (claimButton != null)
            claimButton.interactable = interactable;
    }

    private static bool TryReadClaimResponse(
        object response,
        out bool success,
        out string message
    )
    {
        success = false;
        message = string.Empty;

        if (response is IDictionary<string, object> stringData)
        {
            return TryReadClaimResponseValues(
                stringData,
                out success,
                out message
            );
        }

        if (response is IDictionary<object, object> objectData)
        {
            return TryReadClaimResponseValues(
                objectData,
                out success,
                out message
            );
        }

        return false;
    }

    private static bool TryReadClaimResponseValues<TKey>(
        IDictionary<TKey, object> data,
        out bool success,
        out string message
    )
    {
        success = false;
        message = string.Empty;

        if (!TryGetValue(data, "success", out object successValue) ||
            !TryGetValue(data, "message", out object messageValue))
        {
            return false;
        }

        success = Convert.ToBoolean(successValue);
        message = messageValue?.ToString() ?? string.Empty;
        return true;
    }

    private static bool TryGetValue<TKey>(
        IDictionary<TKey, object> data,
        string key,
        out object value
    )
    {
        foreach (KeyValuePair<TKey, object> pair in data)
        {
            if (pair.Key?.ToString() == key)
            {
                value = pair.Value;
                return true;
            }
        }

        value = null;
        return false;
    }
}
