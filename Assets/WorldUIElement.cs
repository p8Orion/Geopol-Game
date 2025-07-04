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
    
    // Las clases derivadas deben definir estos valores
    protected abstract OrbitalCamera.ZoomLevel minZoomLevel { get; }
    protected abstract OrbitalCamera.ZoomLevel maxZoomLevel { get; }
    protected abstract float baseSize { get; }

    protected void Start()
    {
        SetupCanvas();
        SetupComponents();
        OnStart();
    }

    private void SetupCanvas()
    {
        // Find CanvasMundo component
        CanvasMundo canvasMundoComponent = FindFirstObjectByType<CanvasMundo>();
        if (canvasMundoComponent != null)
        {
            canvasMundo = canvasMundoComponent.GetCanvas();
        }
        else
        {
            // Fallback to direct search (for backward compatibility)
            GameObject canvasGO = GameObject.Find("CanvasMundo");
            canvasMundo = canvasGO.GetComponent<Canvas>();
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
        float minDistance = OrbitalCamera.GetDistanceForZoomLevel(minZoomLevel);
        float maxDistance = OrbitalCamera.GetDistanceForZoomLevel(maxZoomLevel);
        return distance >= minDistance && distance <= maxDistance;
    }
    
    protected float GetSizeMultiplier(float distance)
    {
        // Fórmula normalizada: (distance - ground) / (medium - ground)
        return 1 / ((distance - OrbitalCamera.GetDistanceForZoomLevel(OrbitalCamera.ZoomLevel.Ground)) / 
               (OrbitalCamera.GetDistanceForZoomLevel(OrbitalCamera.ZoomLevel.Medium) - OrbitalCamera.GetDistanceForZoomLevel(OrbitalCamera.ZoomLevel.Ground)));
    }
    

    
    private float CalculateZoomAlpha(float distance)
    {
        // Obtener los umbrales de distancia
        float minDistance = OrbitalCamera.GetDistanceForZoomLevel(minZoomLevel);
        float maxDistance = OrbitalCamera.GetDistanceForZoomLevel(maxZoomLevel);
        
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



    // Métodos abstractos que las clases derivadas deben implementar
    protected abstract Vector3 GetWorldPosition();
    protected abstract void OnStart();
    protected abstract void OnUpdate();
} 