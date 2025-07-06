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
    public float naturalResourceBaseSize = 12f; // Tamaño base para recursos naturales
    public float realResourceBaseSize = 24f; // Tamaño base para recursos reales
    
    [Header("Bobbing Animation")]
    public bool bobbingEnabled = false;
    public float bobbingSpeed = 2f;
    public float bobbingAmount = 5f;
    private float bobbingTime;

    [Header("Selection")]
    public bool isSelected = false;
    public bool isSelectable = true;
    
    [Header("Shader Settings")]
    public Material resourceIconMaterial;

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
        
        // Aplicar estilo de selección
        ApplySelectionStyle();
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
        SetupResourceSpecifics();
        SetupShader();
        UpdateSprite();
    }
    
    private void SetupResourceSpecifics()
    {
        // Los recursos naturales no son clickeables
        if (IsNaturalResource)
        {
            SetClickable(false);
            isSelectable = false;
        }
        else if (IsRealResource)
        {
            SetClickable(true);
            isSelectable = true;
        }
    }
    

    
    protected override OrbitalCamera.ZoomLevel minZoomLevel
    {
        get
        {
            return triangleData != null ? OrbitalCamera.ZoomLevel.Ground : OrbitalCamera.ZoomLevel.Ground;
        }
    }
    
    protected override OrbitalCamera.ZoomLevel maxZoomLevel
    {
        get
        {
            return triangleData != null ? OrbitalCamera.ZoomLevel.Medium : OrbitalCamera.ZoomLevel.VeryFar;
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
            SetupResourceSpecifics();
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
            SetupResourceSpecifics();
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
        
        // Aplicar estilos después de cargar el sprite
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
        bobbingEnabled = false; // Deshabilitar bobbing para recursos naturales
        UpdateShaderProperties();
    }

    private void ApplyRealResourceStyle()
    {
        // Estilo para recursos reales (en movimiento)
        bobbingEnabled = true; // Habilitar bobbing para recursos reales
        UpdateShaderProperties();
    }
    
    private void SetupShader()
    {
        if (resourceIconMaterial == null)
        {
            // Cargar el material del shader si no está asignado
            resourceIconMaterial = new Material(Shader.Find("Custom/ResourceIcon"));
        }
        
        if (image != null && resourceIconMaterial != null)
        {
            image.material = resourceIconMaterial;
        }
    }
    
    private void UpdateShaderProperties()
    {
        if (image != null && resourceIconMaterial != null)
        {
            // Actualizar propiedades del shader directamente en el material
            resourceIconMaterial.SetColor("_Color", tintColor);
            resourceIconMaterial.SetFloat("_Brightness", isSelected ? 1.2f : 1.0f);
            resourceIconMaterial.SetFloat("_PulseSpeed", isSelected ? 5.0f : 0.0f);
            resourceIconMaterial.SetFloat("_PulseAmount", isSelected ? 0.5f : 0.05f);
            resourceIconMaterial.SetColor("_OutlineColor", Color.black);
            resourceIconMaterial.SetFloat("_OutlineWidth", IsNaturalResource ? 0.00f : 0.05f);
            resourceIconMaterial.SetFloat("_GlowIntensity", isSelected ? 0.00f : 0.00f);
            resourceIconMaterial.SetColor("_GlowColor", tintColor);
            
            // Aplicar opacity y saturation según el tipo de recurso
            if (IsNaturalResource)
            {
                resourceIconMaterial.SetFloat("_Opacity", 0.6f);
                resourceIconMaterial.SetFloat("_Saturation", 0.6f);
            }
            else if (IsRealResource)
            {
                resourceIconMaterial.SetFloat("_Opacity", 1.0f);
                resourceIconMaterial.SetFloat("_Saturation", 1.0f);
            }
        }
    }
    
    private void ApplySelectionStyle()
    {
        UpdateShaderProperties();
    }
    
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        ApplySelectionStyle();
        
        // Comunicar con el Resource si existe
        if (resource != null)
        {
            resource.isSelected = selected;
        }
        
        Debug.Log($"ResourceIcon {(selected ? "selected" : "deselected")} for {resourceType}");
    }
    
    public void ToggleSelection()
    {
        SetSelected(!isSelected);
    }
    
    protected override void OnClicked()
    {
        // Solo manejar clicks para recursos reales
        if (IsRealResource && resource != null && resource.isSelectable && resource.isActive && isSelectable)
        {
            ToggleSelection();
        }
    }

    

} 