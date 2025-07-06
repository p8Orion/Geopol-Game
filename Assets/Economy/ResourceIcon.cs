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
    
    [Header("Selection Outline")]
    public Color selectionOutlineColor = Color.black;
    public float outlineScale = 1.1f;
    public float outlineOffset = 0.1f;
    private Image outlineImage;

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
        CreateOutlineImage();
        SetupResourceSpecifics();
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
    
    private void CreateOutlineImage()
    {
        // Crear GameObject hijo para el outline
        GameObject outlineGO = new GameObject("SelectionOutline");
        outlineGO.transform.SetParent(transform);
        outlineGO.transform.localPosition = Vector3.zero;
        outlineGO.transform.localRotation = Quaternion.identity;
        outlineGO.transform.localScale = Vector3.one * outlineScale;
        
        // Agregar Image component para el outline
        outlineImage = outlineGO.AddComponent<Image>();
        outlineImage.color = selectionOutlineColor;
        outlineImage.raycastTarget = false;
        
        // Configurar el RectTransform del outline
        RectTransform outlineRect = outlineImage.rectTransform;
        outlineRect.anchorMin = new Vector2(0.5f, 0.5f);
        outlineRect.anchorMax = new Vector2(0.5f, 0.5f);
        outlineRect.anchoredPosition = Vector2.zero;
        
        // Usar el mismo tamaño que el icono principal
        if (rectTransform != null)
        {
            outlineRect.sizeDelta = rectTransform.sizeDelta;
        }
        else
        {
            outlineRect.sizeDelta = new Vector2(baseSize, baseSize);
        }
        
        // Asegurar que el outline esté detrás usando sorting order
        Canvas outlineCanvas = outlineGO.GetComponent<Canvas>();
        if (outlineCanvas == null)
        {
            outlineCanvas = outlineGO.AddComponent<Canvas>();
        }
        outlineCanvas.overrideSorting = true;
        outlineCanvas.sortingOrder = -1; // Dibujar antes que el icono principal
        
        // Inicialmente oculto
        outlineImage.enabled = false;
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
            // Actualizar también el sprite del outline
            if (outlineImage != null)
            {
                outlineImage.sprite = iconSprite;
            }
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
        
        // Actualizar también el sprite del outline
        if (outlineImage != null)
        {
            outlineImage.sprite = sprite;
        }
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
    
    private void ApplySelectionStyle()
    {
        if (outlineImage != null)
        {
            outlineImage.enabled = isSelected;
        }
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