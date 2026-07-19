using UnityEngine.UI;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class SmokeGameController : MonoBehaviour
{
    public static SmokeGameController Instance;

    [Header("Referencias")]
    [SerializeField] private RectTransform areaJuego;
    [SerializeField] private UIContador uiContador;
    public SmokeWipeController smokeWipe;
    
    [SerializeField] private SmokeItem[] items;

    [Header("Configuración")]
    [SerializeField] private int erroresMaximos = 3;
    [SerializeField] private float tiempoMensaje = 3f;
   

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sonidoCorrecto;
    [SerializeField] private AudioClip sonidoError;
    [SerializeField] private AudioClip sonidoFinal;

    private int erroresActuales = 0;
    private int objetosEncontrados = 0;
    private int totalObjetosCorrectos = 0;

    private bool mostrandoPenalizacion = false;
    private bool juegoTerminado = false;

    private Vector2 startPos;
    private float maxMovement = 20f;

    

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        if (areaJuego == null)
        {
            Debug.LogError("AreaJuego no asignada");
            return;
        }

        if (items == null || items.Length == 0)
        {
            Debug.LogError("Items no asignados");
            return;
        }

        foreach (var item in items)
        {
            if (item.esCorrecto)
                totalObjetosCorrectos++;
        }

        PosicionarItems();

        if (uiContador != null)
            uiContador.Actualizar(0, totalObjetosCorrectos);
    }

    // ----------------------------
    // INPUT (PC + MÓVIL)
    // ----------------------------

    void Update()
    {
        if (juegoTerminado) return;

        // MOUSE
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
                startPos = Mouse.current.position.ReadValue();

            if (Mouse.current.leftButton.wasReleasedThisFrame)
                ProcesarInput(startPos, Mouse.current.position.ReadValue());
        }

        // TOUCH
        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;

            if (touch.press.wasPressedThisFrame)
                startPos = touch.position.ReadValue();

            if (touch.press.wasReleasedThisFrame)
                ProcesarInput(startPos, touch.position.ReadValue());
        }
    }

    void ProcesarInput(Vector2 start, Vector2 end)
    {
        float movement = Vector2.Distance(start, end);

        if (movement < maxMovement)
        {
            DetectarClick(end);
        }
    }

    // ----------------------------
    // DETECCIÓN DE CLICK
    // ----------------------------

    void DetectarClick(Vector2 screenPos)
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = screenPos;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var result in results)
        {
            SmokeItem item = result.gameObject.GetComponentInParent<SmokeItem>();

            if (item != null)
            {
                item.CheckClick(screenPos);
                return;
            }
        }
    }

    public void OnItemClicked(SmokeItem item)
    {
        if (juegoTerminado || mostrandoPenalizacion || item.yaEncontrado)
            return;

        if (item.esCorrecto)
            ProcesarCorrecto(item);
        else
            ProcesarError();
    }

   
    // LÓGICA DE JUEGO
    

    void ProcesarCorrecto(SmokeItem item)
    {
        item.yaEncontrado = true;
        objetosEncontrados++;

        if (audioSource && sonidoCorrecto)
            audioSource.PlayOneShot(sonidoCorrecto);

        if (uiContador)
            uiContador.Actualizar(objetosEncontrados, totalObjetosCorrectos);

        item.gameObject.SetActive(false);

        if (objetosEncontrados >= totalObjetosCorrectos)
            Ganar();
    }

    void ProcesarError()
    {
        erroresActuales++;

        if (audioSource && sonidoError)
            audioSource.PlayOneShot(sonidoError);

        Handheld.Vibrate();

        if (erroresActuales >= erroresMaximos)
            ActivarPenalizacion();
    }

    void ActivarPenalizacion()
    {
        mostrandoPenalizacion = true;
        StartCoroutine(Penalizacion());
        erroresActuales = 0;
    }

    IEnumerator Penalizacion()
    {
        yield return new WaitForSeconds(tiempoMensaje);
        mostrandoPenalizacion = false;
    }

   
    void Ganar()
    {
        juegoTerminado = true;

        StartCoroutine(SecuenciaVictoria());
    }

    IEnumerator SecuenciaVictoria()
    {
        if (audioSource && sonidoFinal)
            audioSource.PlayOneShot(sonidoFinal);

        yield return new WaitForSeconds(sonidoFinal.length);

        if (smokeWipe != null)
        {
            StartCoroutine(smokeWipe.FadeOutHumo(1.5f));
        }

        yield return StartCoroutine(FadeOutIncorrectos(1.5f));
        yield return new WaitForSeconds(1f);

        FindObjectOfType<JuanLassoNarrativeManager>().StartFinalNarrative();

    }


    void PosicionarItems()
    {
        float width = areaJuego.rect.width;
        float height = areaJuego.rect.height;

        float padding = 80f;

        List<Vector2> posicionesUsadas = new List<Vector2>();

        foreach (var item in items)
        {
            RectTransform rt = item.GetComponent<RectTransform>();

            float itemWidth = rt.rect.width;
            float itemHeight = rt.rect.height;

            bool posicionValida = false;
            int intentos = 0;

            Vector2 posicionFinal = Vector2.zero;

            while (!posicionValida && intentos < 100)
            {
                intentos++;

                float randomX = Random.Range(
                    -width / 2 + itemWidth / 2 + padding,
                     width / 2 - itemWidth / 2 - padding
                );

                float randomY = Random.Range(
                    -height / 2 + itemHeight / 2 + padding,
                     height / 2 - itemHeight / 2 - padding
                );

                Vector2 nuevaPos = new Vector2(randomX, randomY);

                posicionValida = true;

                // VALIDAR OVERLAP
                foreach (var posExistente in posicionesUsadas)
                {
                    float distancia = Vector2.Distance(nuevaPos, posExistente);

                    float distanciaMinima =
                        Mathf.Max(itemWidth, itemHeight) * 0.9f;

                    if (distancia < distanciaMinima)
                    {
                        posicionValida = false;
                        break;
                    }
                }

                if (posicionValida)
                {
                    posicionFinal = nuevaPos;
                    posicionesUsadas.Add(posicionFinal);
                }
            }

            rt.anchoredPosition = posicionFinal;
        }
    }

    
    IEnumerator FadeOutIncorrectos(float duracion)
    {
        float tiempo = 0f;

        List<Image> imagenes = new List<Image>();

        foreach (var item in items)
        {
            if (!item.esCorrecto)
            {
                Image img = item.GetComponent<Image>();

                if (img != null)
                    imagenes.Add(img);
            }
        }

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;

            float t = tiempo / duracion;

            foreach (var img in imagenes)
            {
                Color c = img.color;
                c.a = Mathf.Lerp(1f, 0f, t);
                img.color = c;
            }

            yield return null;
        }

        foreach (var item in items)
        {
            if (!item.esCorrecto)
            {
                item.gameObject.SetActive(false);
            }
        }
    }
}