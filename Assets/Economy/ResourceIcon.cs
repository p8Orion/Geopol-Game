using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ResourceIcon : WorldUIElement, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
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
    
    [Header("Drag & Drop")]
    public bool isDragging = false;
    public Vector3 dragOffset;
    public GameObject dragPreview;
    
    [Header("Shader Settings")]
    public Material resourceIconMaterial;

    // Propiedades para abstraer la lógica de tipo de recurso
    private bool IsNaturalResource => triangleData != null;
    private bool IsRealResource => resource != null && triangleData == null;
    
    // Referencias para drag & drop
    private IDPicker idPicker;
    
    protected override void OnUpdate()
    {
        // Update bobbing animation
        if (bobbingEnabled)
        {
            bobbingTime += Time.deltaTime * bobbingSpeed;
        }
        
        // Update drag preview position
        if (isDragging && dragPreview != null)
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            dragPreview.transform.position = mouseWorldPos + dragOffset;
        }
    }
    
    private Vector3 GetMouseWorldPosition()
    {
        if (Camera.main == null) return Vector3.zero;
        
        // Usar el nuevo Input System
        Vector3 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector3.zero;
        mousePos.z = 10f; // Distance from camera
        return Camera.main.ScreenToWorldPoint(mousePos);
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
        
        // Find references for drag & drop
        if (idPicker == null)
            idPicker = UnityEngine.Object.FindFirstObjectByType<IDPicker>();
            
        // Ensure we have a GraphicRaycaster for drag & drop to work
        EnsureGraphicRaycaster();
    }
    
    private void EnsureGraphicRaycaster()
    {
        // Check if we have a GraphicRaycaster in the hierarchy
        GraphicRaycaster raycaster = GetComponentInParent<GraphicRaycaster>();
        if (raycaster == null)
        {
            Debug.LogWarning("ResourceIcon: No GraphicRaycaster found in parent hierarchy. Drag & drop may not work.");
        }
        else
        {
            Debug.Log("ResourceIcon: GraphicRaycaster found, drag & drop should work.");
        }
        
        // Ensure we have an Image component for drag & drop
        if (image == null)
        {
            Debug.LogError("ResourceIcon: No Image component found! Drag & drop will not work.");
        }
        else
        {
            Debug.Log("ResourceIcon: Image component found, drag & drop should work.");
        }
    }
    
    private void SetupResourceSpecifics()
    {
        // Los recursos naturales no son clickeables ni draggables
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
            resourceIconMaterial.SetFloat("_PulseSpeed", isSelected ? 2.0f : 0.0f);
            resourceIconMaterial.SetFloat("_PulseAmount", isSelected ? 0.7f : 0.00f);
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
                resourceIconMaterial.SetFloat("_Opacity", 0.85f);
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

    // --- Drag & Drop Implementation ---
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        // Solo permitir drag para recursos reales
        if (!IsRealResource || resource == null || !resource.isActive || !resource.isSelectable)
        {
            Debug.Log($"Drag blocked: IsRealResource={IsRealResource}, resource={resource}, isActive={resource?.isActive}, isSelectable={resource?.isSelectable}");
            return;
        }
        
        isDragging = true;
        
        // Calcular offset basado en la posición del mouse en el mundo
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        dragOffset = transform.position - mouseWorldPos;
        
        // Crear preview de drag
        CreateDragPreview();
        
        Debug.Log($"Started dragging resource {resource.type} from triangle {resource.origin?.id}");
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        
        // El preview se actualiza en OnUpdate()
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        
        // Intentar hacer drop en un triángulo
        TriangleData targetTriangle = GetTriangleUnderMouse();
        Debug.Log($"EndDrag: triangleId={targetTriangle?.id}");
        
        if (targetTriangle != null)
        {
            resource.SetDestination(targetTriangle);
        }
        else
        {
            Debug.Log("No valid triangle found for drop");
        }
        
        // Limpiar drag
        CleanupDrag();
        
        Debug.Log($"Ended dragging resource {resource.type}");
    }
    
    public void OnDrop(PointerEventData eventData)
    {
        // Los ResourceIcon no reciben drops, solo los hacen
    }
    
    private TriangleData GetTriangleUnderMouse()
    {
        if (idPicker == null) 
        {
            Debug.LogWarning("IDPicker not found for drag & drop");
            return null;
        }
        return idPicker.GetSelectedTriangle();
    }
        
    private void CreateDragPreview()
    {
        if (dragPreview != null) return;
        
        // Crear un preview visual del recurso siendo arrastrado
        dragPreview = new GameObject($"DragPreview_{resourceType}");
        
        // Crear un sprite renderer para el preview
        SpriteRenderer previewRenderer = dragPreview.AddComponent<SpriteRenderer>();
        previewRenderer.sprite = image.sprite;
        previewRenderer.color = tintColor;
        previewRenderer.sortingOrder = 1000; // Asegurar que esté por encima
        
        // Hacer el preview semi-transparente
        Color previewColor = tintColor;
        previewColor.a = 0.7f;
        previewRenderer.color = previewColor;
        
        // Escalar el preview
        dragPreview.transform.localScale = Vector3.one * 0.5f;
        
        Debug.Log("Created drag preview");
    }
    
    private void CleanupDrag()
    {
        isDragging = false;
        
        if (dragPreview != null)
        {
            Destroy(dragPreview);
            dragPreview = null;
        }
    }
    
    void OnDestroy()
    {
        CleanupDrag();
    }
} 