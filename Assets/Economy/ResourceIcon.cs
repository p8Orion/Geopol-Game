using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResourceIcon : WorldUIElement
{
    [Header("Resource Data")]
    public Resource resource;
    public ResourceType resourceType;
    public TriangleData triangleData; // For natural resource display
    
    [Header("Resource Size Settings")]
    public float naturalResourceBaseSize = 16f; // Tamaño base para recursos naturales
    public float realResourceBaseSize = 32f; // Tamaño base para recursos reales
    
    [Header("Bobbing Animation")]
    public bool bobbingEnabled = false;
    public float bobbingSpeed = 2f;
    public float bobbingAmount = 5f;
    private float bobbingTime;

    // Propiedades para abstraer la lógica de tipo de recurso
    private bool IsNaturalResource => triangleData != null;
    private bool IsRealResource => resource != null && triangleData == null;
    
    protected override void OnUpdate()
    {
        // Update bobbing animation
        if (bobbingEnabled)
        {
            bobbingTime += Time.deltaTime * bobbingSpeed;
        }
    }
    
    private void ApplyResourceStyleLogic()
    {
        // Aplicar estilo basado en el tipo de recurso actual
        if (IsNaturalResource)
        {
            ApplyNaturalResourceStyle();
        }
        else if (IsRealResource)
        {
            ApplyRealResourceStyle();
        }
    }
    
  
    protected override Vector3 GetWorldPosition()
    {
        Vector3 basePosition = triangleData != null
            ? triangleData.GetCenter()
            : (resource != null ? resource.GetCurrentPosition() : Vector3.zero);
            
        // Apply bobbing offset if enabled
        if (bobbingEnabled)
        {
            float bobbingOffset = Mathf.Sin(bobbingTime) * bobbingAmount;
            basePosition.y += bobbingOffset;
        }

        return basePosition;
    }
    
    protected override void OnStart()
    {
        UpdateSprite();
    }
    
    protected override ZoomLevel minZoomLevel
    {
        get
        {
            return triangleData != null ? ZoomLevel.Close : ZoomLevel.Ground;
        }
    }
    
    protected override ZoomLevel maxZoomLevel
    {
        get
        {
            return triangleData != null ? ZoomLevel.Medium : ZoomLevel.Far;
        }
    }
    
    protected override float baseSize
    {
        get
        {
            return triangleData != null ? naturalResourceBaseSize : realResourceBaseSize;
        }
    }

    public void SetTriangleData(TriangleData triangle)
    {
        triangleData = triangle;
        if (triangleData != null)
        {
            resourceType = triangleData.naturalResource;
            tintColor = resourceType.GetColor();
            UpdateSprite();
            ApplyResourceStyleLogic();
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
            ApplyResourceStyleLogic();
        }
    }

    public void SetResourceType(ResourceType type)
    {
        resourceType = type;
        tintColor = resourceType.GetColor();
        UpdateSprite();
        ApplyResourceStyleLogic();
    }

    private void UpdateSprite()
    {
        if (image == null) return;
        string iconName = GetIconNameForResourceType(resourceType);
        Sprite iconSprite = Resources.Load<Sprite>($"Icons/{iconName}");
        if (iconSprite != null)
        {
            image.sprite = iconSprite;
        }
        else
        {
            CreateSimpleSprite();
        }
        
        // Aplicar estilos primero
        ApplyResourceStyleLogic();
        

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
    }

    public void DestroyIcon()
    {
        DestroyElement();
    }

    private void ApplyNaturalResourceStyle()
    {
        // Estilo para recursos naturales (fijos en el terreno)
        if (image != null)
        {
            Color naturalColor = tintColor;
            naturalColor.a = 0.7f; // Más transparente
            naturalColor *= 0.7f; // Más apagado
            image.color = naturalColor;
            
            // Asegurar que el Image tenga transparencia habilitada
            image.raycastTarget = false;
        }
        SetSize(baseSize * 0.8f); // Más pequeños
        bobbingEnabled = false; // Deshabilitar bobbing para recursos naturales
    }

    private void ApplyRealResourceStyle()
    {
        // Estilo para recursos reales (en movimiento)
        if (image != null)
        {
            image.color = tintColor;
        }
        SetSize(baseSize * 1.5f); // Más grandes
        bobbingEnabled = true; // Habilitar bobbing para recursos reales
    }

    

} 