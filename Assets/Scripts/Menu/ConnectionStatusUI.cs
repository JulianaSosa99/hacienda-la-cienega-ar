using TMPro;
using UnityEngine;

public class ConnectionStatusUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup panel;
    [SerializeField] private TMP_Text messageText;

    [Header("Text")]
    [SerializeField] private string offlineMessage = "Sin conexion a internet. Algunas funciones pueden tardar o no estar disponibles.";

    [Header("Check")]
    [SerializeField] private float checkInterval = 2f;
    [SerializeField] private bool showOnlyWhenOffline = true;

    private float nextCheckTime;
    private bool lastOffline;

    private void Awake()
    {
        ResolveReferences();
        UpdateState(true);
    }

    private void Update()
    {
        if (Time.unscaledTime < nextCheckTime)
            return;

        nextCheckTime = Time.unscaledTime + checkInterval;
        UpdateState(false);
    }

    public void UpdateState(bool force)
    {
        bool offline = Application.internetReachability == NetworkReachability.NotReachable;

        if (!force && offline == lastOffline)
            return;

        lastOffline = offline;

        if (messageText != null)
            messageText.text = offlineMessage;

        SetVisible(!showOnlyWhenOffline || offline);

        if (offline)
            AppLog.Warning("NETWORK", "Dispositivo sin conexion a internet");
    }

    private void ResolveReferences()
    {
        if (panel == null)
            panel = GetComponent<CanvasGroup>();

        if (panel == null)
            panel = gameObject.AddComponent<CanvasGroup>();

        if (messageText == null)
            messageText = GetComponentInChildren<TMP_Text>(true);
    }

    private void SetVisible(bool visible)
    {
        if (panel == null)
            return;

        panel.alpha = visible ? 1f : 0f;
        panel.interactable = visible;
        panel.blocksRaycasts = false;
    }
}