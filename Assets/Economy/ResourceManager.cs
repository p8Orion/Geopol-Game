using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResourceManager : MonoBehaviour
{
    [Header("Resource Icon Prefab")]
    public GameObject resourceIconPrefab;
    
    [Header("Canvas")]
    public Canvas worldCanvas; // Assign CanvasMundo here
    
    [Header("Active Resources")]
    public List<Resource> activeResources = new();
    
    [Header("Visual Settings")]
    public float defaultSize = 1f;
    public Color defaultTint = Color.white;
    
    [Header("Performance")]
    public float updateInterval = 0.1f; // Update every 100ms
    
    private float lastUpdateTime;
    
    void Start()
    {
        // Find CanvasMundo if not assigned
        if (worldCanvas == null)
        {
            worldCanvas = GameObject.Find("CanvasMundo")?.GetComponent<Canvas>();
            if (worldCanvas == null)
            {
                Debug.LogError("CanvasMundo not found! Please create a Canvas named 'CanvasMundo' in Screen Space - Overlay mode.");
                return;
            }
        }
        ConfigureCanvasMundo();
        // Create default prefab if none assigned
        if (resourceIconPrefab == null)
        {
            CreateDefaultPrefab();
        }
    }
    
    private void ConfigureCanvasMundo()
    {
        // Set render mode
        worldCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Set RectTransform to cover the whole screen
        RectTransform rect = worldCanvas.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        Debug.Log("CanvasMundo configured as Screen Space - Overlay and full screen.");
    }
    
    void Update()
    {
        // Update at intervals for performance
        if (Time.time - lastUpdateTime < updateInterval) return;
        lastUpdateTime = Time.time;
        
        UpdateAllResources();
    }
    
    public Resource CreateResource(ResourceType type, Vector3 origin, Vector3 destination, float productionRate = 1f)
    {
        Resource resource = new Resource(type, origin, destination)
        {
            productionRate = productionRate
        };
        
        activeResources.Add(resource);
        CreateResourceIcon(resource);
        
        return resource;
    }
    
    public void CreateResourceIcon(Resource resource)
    {
        if (resourceIconPrefab == null || worldCanvas == null) return;
        
        // Create icon instance
        GameObject iconObject = Instantiate(resourceIconPrefab, worldCanvas.transform);
        ResourceIcon icon = iconObject.GetComponent<ResourceIcon>();
        
        if (icon != null)
        {
            icon.SetResource(resource);
            // El color se maneja automáticamente en SetResource()
            
            // Link to resource
            resource.iconInstance = icon;
        }
    }
    
    public void RemoveResource(Resource resource)
    {
        if (activeResources.Contains(resource))
        {
            activeResources.Remove(resource);
        }
        
        // Resource handles its own icon destruction
        if (resource != null && resource.iconInstance != null)
        {
            resource.iconInstance.DestroyIcon();
            resource.iconInstance = null;
        }
    }
    
    public void UpdateAllResources()
    {
        float deltaTime = Time.deltaTime;
        
        // Update all resources
        for (int i = activeResources.Count - 1; i >= 0; i--)
        {
            Resource resource = activeResources[i];
            
            if (resource == null)
            {
                activeResources.RemoveAt(i);
                continue;
            }
            
            // Update resource progress (this will also update visual)
            resource.UpdateProgress(deltaTime);
            
            // Remove if reached destination
            if (resource.HasReachedDestination())
            {
                RemoveResource(resource);
            }
        }
    }
    
    public void ClearAllResources()
    {
        // Remove all resources (they handle their own icons)
        for (int i = activeResources.Count - 1; i >= 0; i--)
        {
            RemoveResource(activeResources[i]);
        }
    }
    
    public List<Resource> GetResourcesByType(ResourceType type)
    {
        List<Resource> result = new();
        foreach (Resource resource in activeResources)
        {
            if (resource.type == type)
            {
                result.Add(resource);
            }
        }
        return result;
    }
    
    public List<Resource> GetResourcesAtPosition(Vector3 position, float radius = 1f)
    {
        List<Resource> result = new();
        foreach (Resource resource in activeResources)
        {
            if (Vector3.Distance(resource.GetCurrentPosition(), position) <= radius)
            {
                result.Add(resource);
            }
        }
        return result;
    }
    
    private void CreateDefaultPrefab()
    {
        // Create a simple prefab with ResourceIcon component
        GameObject prefab = new GameObject("ResourceIconPrefab");
        prefab.AddComponent<ResourceIcon>();
        
        // Add UI components (will be configured by ResourceIcon)
        prefab.AddComponent<RectTransform>();
        prefab.AddComponent<CanvasRenderer>();
        prefab.AddComponent<Image>();
        
        // Save as prefab (this is just for reference - you'd need to create the actual prefab in Unity)
        resourceIconPrefab = prefab;
        
        Debug.Log("Created default ResourceIcon prefab. Consider creating a proper prefab in Unity.");
    }
    
    // Debug methods
    public void SpawnTestResource(ResourceType type, Vector3 position)
    {
        Vector3 destination = position + Random.insideUnitSphere * 5f;
        destination.y = position.y; // Keep on same height
        
        CreateResource(type, position, destination, Random.Range(0.5f, 2f));
    }
    
    void OnDrawGizmos()
    {
        // Draw resource paths in scene view
        Gizmos.color = Color.yellow;
        foreach (Resource resource in activeResources)
        {
            if (resource == null) continue;
            
            Vector3 currentPos = resource.GetCurrentPosition();
            Gizmos.DrawWireSphere(currentPos, 0.1f);
            
            // Draw route
            for (int i = 0; i < resource.waypoints.Count - 1; i++)
            {
                Gizmos.DrawLine(resource.waypoints[i], resource.waypoints[i + 1]);
            }
        }
    }
} 