using UnityEngine;
using UnityEngine.UI;

public abstract class WorldUIElement : MonoBehaviour
{
    [Header("Components")]
    protected RectTransform rectTransform;
    protected Camera mainCamera;
    protected RectTransform canvasRect;
    protected Canvas canvasMundo;
    protected Image image;

    [Header("Visual Settings")]
    public Color tintColor = Color.white;
    
    [Header("Zoom Fade")]
    public float fadeDistance = 300f; // Distancia de transición para el fade

    // Slots fijos para diferentes rangos de zoom
    [Header("Zoom Slots")]
    public static float planetLevel = 7000f; // Máximo acercamiento (tope de zoom)
    public static float closeZoomMax = 8200f;
    public static float mediumZoomMax = 9500f; // Centro - tamaño base
    public static float farZoomMax = 10000f;
    public static float veryFarZoomMax = 15000f;

    public enum ZoomLevel
    {
        None,
        Ground, // Máximo acercamiento
        Close,
        Medium, // Centro - tamaño base
        Far,
        VeryFar
    }
    
    // Las clases derivadas deben definir estos valores
    protected abstract ZoomLevel minZoomLevel { get; }
    protected abstract ZoomLevel maxZoomLevel { get; }
    protected abstract float baseSize { get; }

    protected void Start()
    {
        SetupCanvas();
        SetupComponents();
        OnStart();
    }

    private void SetupCanvas()
    {
        // Buscar o crear CanvasMundo
        GameObject canvasGO = GameObject.Find("CanvasMundo");
        if (canvasGO == null)
        {
            canvasGO = new GameObject("CanvasMundo");
            canvasMundo = canvasGO.AddComponent<Canvas>();
            canvasMundo.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasMundo.worldCamera = Camera.main;
            canvasMundo.planeDistance = 1f;
            
            // Configurar blending para transparencia
            var canvasGroup = canvasGO.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            
            RectTransform rect = canvasGO.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            Debug.Log("WorldUIElement: CanvasMundo creado y configurado como Screen Space - Camera con transparencia.");
        }
        else
        {
            canvasMundo = canvasGO.GetComponent<Canvas>();
            // Asegurar que tenga la configuración correcta para transparencia
            if (canvasMundo.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                canvasMundo.renderMode = RenderMode.ScreenSpaceCamera;
                canvasMundo.worldCamera = Camera.main;
                canvasMundo.planeDistance = 1f;
            }
        }
        
        // Hacerse hijo del canvas
        transform.SetParent(canvasMundo.transform, false);
        canvasRect = canvasMundo.GetComponent<RectTransform>();
    }

    private void SetupComponents()
    {
        // Asegurarse de tener RectTransform
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
            rectTransform = gameObject.AddComponent<RectTransform>();

        image = GetComponent<Image>();
        if (image == null)
            image = gameObject.AddComponent<Image>();
        
        // Configurar Image para transparencia
        image.raycastTarget = false;
        image.material = null;
        
        rectTransform.sizeDelta = new Vector2(baseSize, baseSize);
        mainCamera = Camera.main;
    }

    protected void Update()
    {
        if (mainCamera == null || rectTransform == null || canvasRect == null) return;
        
        Vector3 worldPos = GetWorldPosition();
        
        // Calcular distancia de la cámara al centro del planeta (0,0,0)
        float cameraDistanceFromCenter = Vector3.Distance(Vector3.zero, mainCamera.transform.position);
        
        // Determinar visibilidad y tamaño basado en la distancia de la cámara al centro
        bool shouldBeVisible = IsInZoomRange(cameraDistanceFromCenter);
        float dynamicSize = baseSize * GetSizeMultiplier(cameraDistanceFromCenter);
        Debug.Log("dynamicSize: " + dynamicSize);
        
        // Aplicar fade basado en zoom
        if (image != null)
        {
            float alpha = CalculateZoomAlpha(cameraDistanceFromCenter);
            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }
        
        // Siempre procesar posición y tamaño, pero solo mostrar si es visible
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
        Vector2 anchoredPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPos, mainCamera, out anchoredPos);
        
        rectTransform.anchoredPosition = anchoredPos;
        
        // Aplicar tamaño dinámico
        SetSize(dynamicSize);
        
        OnUpdate();
    }


    
    protected bool IsInZoomRange(float distance)
    {
        float minDistance = GetDistanceForZoomLevel(minZoomLevel);
        float maxDistance = GetDistanceForZoomLevel(maxZoomLevel);
        return distance >= minDistance && distance <= maxDistance;
    }
    
    protected float GetSizeMultiplier(float distance)
    {
        // Fórmula normalizada: (distance - ground) / (medium - ground)
        return 1 / ((distance - GetDistanceForZoomLevel(ZoomLevel.Ground)) / 
               (GetDistanceForZoomLevel(ZoomLevel.Medium) - GetDistanceForZoomLevel(ZoomLevel.Ground)));
    }
    
    private float GetDistanceForZoomLevel(ZoomLevel level)
    {
        switch (level)
        {
            case ZoomLevel.Ground: return planetLevel;
            case ZoomLevel.Close: return closeZoomMax;
            case ZoomLevel.Medium: return mediumZoomMax;
            case ZoomLevel.Far: return farZoomMax;
            case ZoomLevel.VeryFar: return veryFarZoomMax;
            default: return 0f;
        }
    }
    
    private float CalculateZoomAlpha(float distance)
    {
        // Obtener los umbrales de distancia
        float minDistance = GetDistanceForZoomLevel(minZoomLevel);
        float maxDistance = GetDistanceForZoomLevel(maxZoomLevel);
        
        // Calcular alpha basado en la distancia
        float alpha = 1f;
        
        // Si está por debajo del umbral mínimo, hacer fade out
        if (distance < minDistance)
        {
            alpha = Mathf.Lerp(0f, 1f, (distance - (minDistance - fadeDistance)) / fadeDistance);
        }
        // Si está por encima del umbral máximo, hacer fade out
        else if (distance > maxDistance)
        {
            alpha = Mathf.Lerp(1f, 0f, (distance - maxDistance) / fadeDistance);
        }
        
        return Mathf.Clamp01(alpha);
    }

    public void SetSize(float newSize)
    {
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(newSize, newSize);
        }
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    public void SetTint(Color color)
    {
        tintColor = color;
        if (image != null)
        {
            image.color = tintColor;
        }
    }

    public void DestroyElement()
    {
        Destroy(gameObject);
    }

    /// <summary>
    /// Gets the current zoom level based on distance to camera
    /// </summary>
    public static ZoomLevel GetCurrentZoomLevel(float distance)
    {
        if (distance <= planetLevel)
            return ZoomLevel.Ground;
        else if (distance <= closeZoomMax)
            return ZoomLevel.Close;
        else if (distance <= mediumZoomMax)
            return ZoomLevel.Medium;
        else if (distance <= farZoomMax)
            return ZoomLevel.Far;
        else if (distance <= veryFarZoomMax)
            return ZoomLevel.VeryFar;
        else
            return ZoomLevel.None;
    }

    // Métodos abstractos que las clases derivadas deben implementar
    protected abstract Vector3 GetWorldPosition();
    protected abstract void OnStart();
    protected abstract void OnUpdate();
} 