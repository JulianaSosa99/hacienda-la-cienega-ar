using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class SmokeWipeController : MonoBehaviour
{
    public Image smokeImage;

    private Texture2D texture;
    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = smokeImage.rectTransform;

        Texture2D originalTexture = smokeImage.sprite.texture;

        texture = new Texture2D(originalTexture.width, originalTexture.height, TextureFormat.RGBA32, false);
        texture.SetPixels(originalTexture.GetPixels());

       
        Color[] pixels = texture.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i].a = 1f;
        }

        texture.SetPixels(pixels);
        texture.Apply();

        smokeImage.sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );
    }

    void Update()
    {
        bool presionado = false;
        Vector2 inputPos = Vector2.zero;

         // MOUSE
        
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            presionado = true;
            inputPos = Mouse.current.position.ReadValue();
        }

        
        // TOUCH
        
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            presionado = true;
            inputPos = Touchscreen.current.primaryTouch.position.ReadValue();
        }

        if (!presionado) return;

        Vector2 localPoint;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            inputPos,
            smokeImage.canvas.worldCamera,
            out localPoint
        );

        Draw(localPoint);
    }

    void Draw(Vector2 localPoint)
    {
        int x = (int)((localPoint.x + rectTransform.rect.width / 2) / rectTransform.rect.width * texture.width);
        int y = (int)((localPoint.y + rectTransform.rect.height / 2) / rectTransform.rect.height * texture.height);

        int radius = 65;

        for (int i = -radius; i <= radius; i++)
        {
            for (int j = -radius; j <= radius; j++)
            {
                if (i * i + j * j <= radius * radius)
                {
                    int px = x + i;
                    int py = y + j;

                    if (px >= 0 && px < texture.width && py >= 0 && py < texture.height)
                    {
                        Color color = texture.GetPixel(px, py);
                        color.a = 0f;
                        texture.SetPixel(px, py, color);
                    }
                }
            }
        }

        texture.Apply();
    }

    // DETECCIÓN DE HUMO
    public bool EstaCubiertoPorHumo(Vector2 screenPos)
    {
        Vector2 localPoint;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            screenPos,
            null,
            out localPoint
        );

        Rect rect = rectTransform.rect;

        float normalizedX = (localPoint.x - rect.xMin) / rect.width;
        float normalizedY = (localPoint.y - rect.yMin) / rect.height;

        int px = Mathf.Clamp(Mathf.FloorToInt(normalizedX * texture.width), 0, texture.width - 1);
        int py = Mathf.Clamp(Mathf.FloorToInt(normalizedY * texture.height), 0, texture.height - 1);

        float alpha = texture.GetPixel(px, py).a;

        Debug.Log("Alpha directo: " + alpha);

        
        return alpha > 0.2f;
    }

    public bool EstaCubiertoPorHumoEnRect(RectTransform rectItem)
    {
        int puntosCubiertos = 0;

        Vector2 centro = rectItem.TransformPoint(rectItem.rect.center);

        Vector2 arriba = rectItem.TransformPoint(
            new Vector2(0, rectItem.rect.height * 0.25f)
        );

        Vector2 abajo = rectItem.TransformPoint(
            new Vector2(0, -rectItem.rect.height * 0.25f)
        );

        Vector2 izquierda = rectItem.TransformPoint(
            new Vector2(-rectItem.rect.width * 0.25f, 0)
        );

        Vector2 derecha = rectItem.TransformPoint(
            new Vector2(rectItem.rect.width * 0.25f, 0)
        );

        Vector2[] puntos =
        {
        centro,
        arriba,
        abajo,
        izquierda,
        derecha
    };

        foreach (var p in puntos)
        {
            if (EstaCubiertoPorHumo(p))
                puntosCubiertos++;
        }

        return puntosCubiertos >= 3;
    }

    public IEnumerator FadeOutHumo(float duracion)
    {
        float tiempo = 0f;

        Color[] originalPixels = texture.GetPixels();

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;

            float t = tiempo / duracion;

            Color[] nuevosPixels = new Color[originalPixels.Length];

            for (int i = 0; i < originalPixels.Length; i++)
            {
                Color c = originalPixels[i];
                c.a = Mathf.Lerp(originalPixels[i].a, 0f, t);
                nuevosPixels[i] = c;
            }

            texture.SetPixels(nuevosPixels);
            texture.Apply();

            yield return null;
        }
    }
}