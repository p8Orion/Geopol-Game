using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResourceIcon : MonoBehaviour
{
    [Header("Components")]
    private Image image;
    private RectTransform rectTransform;
    private Camera mainCamera;
    private RectTransform canvasRect;
    private Canvas canvasMundo;
    
    [Header("Resource Data")]
    public Resource resource;
    public ResourceType resourceType;
    public TriangleData triangleData; // For natural resource display
    
    [Header("Visual Settings")]
    public float size = 24f; // Size in UI units
    public Color tintColor = Color.white;

    [Header("Bobbing Animation")]
public bool bobbingEnabled = false;
public float bobbingSpeed = 2f;
public float bobbingAmount = 5f;
private float bobbingTime;
    
    private void Start()
    {
        // Buscar o crear CanvasMundo
        GameObject canvasGO = GameObject.Find("CanvasMundo");
        if (canvasGO == null)
        {
            canvasGO = new GameObject("CanvasMundo");
            canvasMundo = canvasGO.AddComponent<Canvas>();
            canvasMundo.renderMode = RenderMode.ScreenSpaceOverlay;
            RectTransform rect = canvasGO.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            Debug.Log("ResourceIcon: CanvasMundo creado y configurado como Screen Space - Overlay.");
        }
        else
        {
            canvasMundo = canvasGO.GetComponent<Canvas>();
        }
        // Hacerse hijo del canvas
        transform.SetParent(canvasMundo.transform, false);
        canvasRect = canvasMundo.GetComponent<RectTransform>();

        // Asegurarse de tener RectTransform
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
            rectTransform = gameObject.AddComponent<RectTransform>();

        image = GetComponent<Image>();
        if (image == null)
            image = gameObject.AddComponent<Image>();
        rectTransform.sizeDelta = new Vector2(size, size);
        mainCamera = Camera.main;
        UpdateSprite();
    }

    private void Update()
    {
        if (mainCamera == null || rectTransform == null || canvasRect == null) return;
        
        // Update bobbing animation
        if (bobbingEnabled)
        {
            bobbingTime += Time.deltaTime * bobbingSpeed;
        }
        
        Vector3 worldPos = triangleData != null
            ? triangleData.GetCenter()
            : (resource != null ? resource.GetCurrentPosition() : Vector3.zero);
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
        Vector2 anchoredPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPos, null, out anchoredPos);
        
        // Apply bobbing offset only if enabled
        if (bobbingEnabled)
        {
            float bobbingOffset = Mathf.Sin(bobbingTime) * bobbingAmount;
            anchoredPos.y += bobbingOffset;
        }
        
        rectTransform.anchoredPosition = anchoredPos;
    }

    public void SetTriangleData(TriangleData triangle)
    {
        triangleData = triangle;
        if (triangleData != null)
        {
            resourceType = triangleData.naturalResource;
            tintColor = resourceType.GetColor();
            UpdateSprite();
            ApplyNaturalResourceStyle();
        }
    }

    public void SetResource(Resource newResource)
    {
        resource = newResource;
        if (resource != null)
        {
            resourceType = resource.type;
            tintColor = resourceType.GetColor();
            UpdateSprite();
            ApplyRealResourceStyle();
        }
    }

    public void SetResourceType(ResourceType type)
    {
        resourceType = type;
        tintColor = resourceType.GetColor();
        UpdateSprite();
    }

    private void UpdateSprite()
    {
        if (image == null) return;
        string iconName = GetIconNameForResourceType(resourceType);
        Sprite iconSprite = Resources.Load<Sprite>($"Icons/{iconName}");
        if (iconSprite != null)
        {
            image.sprite = iconSprite;
            image.color = tintColor;
        }
        else
        {
            CreateSimpleSprite();
        }
    }

    private string GetIconNameForResourceType(ResourceType resourceType)
    {
        if (resourceType == ResourceType.None)
            return "cardboard-box";
        return resourceType.ToString();
    }

    private void CreateSimpleSprite()
    {
        int size = 64;
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 3f;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                if (Vector2.Distance(new Vector2(x, y), center) <= radius)
                    pixels[y * size + x] = tintColor;
        texture.SetPixels(pixels);
        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 1f);
        image.sprite = sprite;
        image.color = tintColor;
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

    public void SetSize(float newSize)
    {
        size = newSize;
        rectTransform.sizeDelta = new Vector2(size, size);
    }

    public void DestroyIcon()
    {
        Destroy(gameObject);
    }

    private void ApplyNaturalResourceStyle()
    {
        // Estilo para recursos naturales (fijos en el terreno)
        if (image != null)
        {
            Color naturalColor = tintColor;
            naturalColor.a = 0.6f; // Más transparente
            naturalColor *= 0.7f; // Más apagado
            image.color = naturalColor;
        }
        SetSize(size); // Un poco más pequeños
    }

    private void ApplyRealResourceStyle()
    {
        // Estilo para recursos reales (en movimiento)
        if (image != null)
        {
            image.color = tintColor;
        }
        SetSize(size * 1.5f); // Un poco más grandes
        bobbingEnabled = true; // Habilitar bobbing para recursos reales
    }
} 