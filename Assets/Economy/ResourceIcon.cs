using UnityEngine;
using TMPro;

public class ResourceIcon : MonoBehaviour
{
    [Header("Components")]
    private SpriteRenderer spriteRenderer;
    private Camera mainCamera;
    
    [Header("Resource Data")]
    public Resource resource;
    public ResourceType resourceType;
    public TriangleData triangleData; // For natural resource display
    
    [Header("Visual Settings")]
    public float heightOffset = 50f; // Much higher above the triangle
    public float scale = 100f; // Target size in world units
    public Color tintColor = Color.white;
    public bool useBillboard = true; // true = billboard, false = flat on terrain
    
    [Header("Animation")]
    public bool enableBobbing = false; // Default to false for natural resources
    public float bobSpeed = 2f;
    public float bobHeight = 0.1f;
    public float rotationSpeed = 30f;
    
    private Vector3 basePosition;
    private float bobTime;
    
    void Start()
    {
        // Get components
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;
        
        // Set initial position
        if (resource != null)
        {
            // For effective resources, use resource position
            basePosition = resource.GetCurrentPosition() + Vector3.up * heightOffset;
            transform.position = basePosition;
            resourceType = resource.type;
        }
        else if (triangleData != null)
        {
            // For natural resources, use triangle center
            basePosition = triangleData.GetCenter() + Vector3.up * heightOffset;
            transform.position = basePosition;
        }
        
        // Create sprite renderer if it doesn't exist
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        
        // Set sprite based on resource type
        UpdateSprite();
        
        // Don't scale transform, create sprite with correct size
        transform.localScale = Vector3.one;
    }
    
    void Update()
    {
        // Billboard effect - only if enabled
        if (useBillboard && mainCamera != null)
        {
            transform.LookAt(mainCamera.transform);
            transform.Rotate(0, 180, 0); // Adjust orientation
        }
        else if (!useBillboard)
        {
            // Flat on terrain - face up (Y axis)
            transform.rotation = Quaternion.LookRotation(Vector3.up);
        }
        
        // Bobbing animation (only if enabled)
        if (enableBobbing)
        {
            bobTime += Time.deltaTime * bobSpeed;
            float bobOffset = Mathf.Sin(bobTime) * bobHeight;
            transform.position = basePosition + Vector3.up * bobOffset;
        }
        else
        {
            // Keep position static at base position
            transform.position = basePosition;
        }
        
        // Rotation animation (only if billboard)
        if (useBillboard)
        {
            transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
        }
    }
    

    

    
    public void SetResource(Resource newResource)
    {
        resource = newResource;
        if (resource != null)
        {
            resourceType = resource.type;
            tintColor = resourceType.GetColor(); // Set color automatically
            UpdateSprite();
        }
    }
    
    public void SetTriangleData(TriangleData triangle)
    {
        triangleData = triangle;
        if (triangleData != null)
        {
            resourceType = triangleData.naturalResource;
            tintColor = resourceType.GetColor(); // Set color automatically
            UpdateSprite();
        }
    }
    
    public void SetResourceType(ResourceType type)
    {
        resourceType = type;
        tintColor = resourceType.GetColor(); // Set color automatically
        UpdateSprite();
    }
    
    private void UpdateSprite()
    {
        if (spriteRenderer == null) return;
        
        // Create a simple colored circle sprite
        CreateSimpleSprite();
    }
    
    private void CreateSimpleSprite()
    {
        // Create a simple white circle texture
        int size = 64; // Fixed small size
        Texture2D texture = new Texture2D(size, size);
        
        // Fill with transparent background
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }
        
        // Draw a circle in the center
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 3f;
        
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance <= radius)
                {
                    pixels[y * size + x] = tintColor;
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        // Create sprite from texture with size/scale pixels per unit (inverse relationship)
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size / scale);
        
        // Set the sprite
        spriteRenderer.sprite = sprite;
        spriteRenderer.color = tintColor;
        
        // Make sure it's visible
        spriteRenderer.enabled = true;
    }
    
    public void SetPosition(Vector3 position)
    {
        basePosition = position + Vector3.up * heightOffset;
    }
    
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
    
    public void SetTint(Color color)
    {
        tintColor = color;
        TextMeshPro tmp = GetComponent<TextMeshPro>();
        if (tmp != null)
        {
            tmp.color = tintColor;
        }
    }
    
    public void SetScale(float newScale)
    {
        scale = newScale;
        transform.localScale = Vector3.one * scale;
    }
    
    public void SetBobbing(bool enable)
    {
        enableBobbing = enable;
    }
    
    public void SetBillboard(bool useBillboardMode)
    {
        useBillboard = useBillboardMode;
    }
    
    public void SetNaturalResourceMode()
    {
        useBillboard = false; // No billboard
        scale = 100f; // 50 unidades
        heightOffset = 5f; // Un poco más arriba del terreno
    }
    
    public void SetRealResourceMode()
    {
        useBillboard = true; // Billboard
        scale = 250f; // 100 unidades
        heightOffset = 50f; // Más alto
    }
    
    public void DestroyIcon()
    {
        Destroy(gameObject);
    }
} 