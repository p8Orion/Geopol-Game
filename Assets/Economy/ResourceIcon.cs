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
    
    [Header("Visual Settings")]
    public float heightOffset = 0.5f; // Height above the triangle
    public float scale = 1f;
    public Color tintColor = Color.white;
    
    [Header("Animation")]
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
            basePosition = resource.GetCurrentPosition() + Vector3.up * heightOffset;
            transform.position = basePosition;
            resourceType = resource.type;
        }
        
        // Set sprite based on resource type
        UpdateSprite();
    }
    
    void Update()
    {
        // Billboard effect - always face camera
        if (mainCamera != null)
        {
            transform.LookAt(mainCamera.transform);
            transform.Rotate(0, 180, 0); // Adjust orientation
        }
        
        // Bobbing animation
        bobTime += Time.deltaTime * bobSpeed;
        float bobOffset = Mathf.Sin(bobTime) * bobHeight;
        transform.position = basePosition + Vector3.up * bobOffset;
        
        // Rotation animation
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
    }
    
    public void SetResource(Resource newResource)
    {
        resource = newResource;
        if (resource != null)
        {
            resourceType = resource.type;
            UpdateSprite();
        }
    }
    
    public void SetResourceType(ResourceType type)
    {
        resourceType = type;
        UpdateSprite();
    }
    
    private void UpdateSprite()
    {
        if (spriteRenderer == null) return;
        
        // Get emoji as sprite
        string emoji = resourceType.GetEmoji();
        
        // For now, we'll use a simple approach with TextMeshPro
        // In a real implementation, you'd want to use a sprite atlas with emoji textures
        CreateEmojiSprite(emoji);
    }
    
    private void CreateEmojiSprite(string emoji)
    {
        // Create a TextMeshPro component for emoji display
        TextMeshPro tmp = GetComponent<TextMeshPro>();
        if (tmp == null)
        {
            tmp = gameObject.AddComponent<TextMeshPro>();
        }
        
        // Configure TextMeshPro for emoji display
        tmp.text = emoji;
        tmp.fontSize = 2f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = tintColor;
        
        // Disable the SpriteRenderer since we're using TextMeshPro
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
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
    
    public void DestroyIcon()
    {
        Destroy(gameObject);
    }
} 