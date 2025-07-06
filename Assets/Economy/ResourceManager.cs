using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResourceManager : MonoBehaviour
{
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
    
    void Awake()
    {
        // Find CanvasMundo component
        CanvasMundo canvasMundo = FindFirstObjectByType<CanvasMundo>();
        if (canvasMundo != null)
        {
            worldCanvas = canvasMundo.GetCanvas();
        }
        
        if (worldCanvas == null)
        {
            Debug.LogError("CanvasMundo not found! Please add CanvasMundo component to a GameObject.");
            return;
        }
        
        ConfigureCanvasMundo();
    }
    
    void Start()
    {
        // Start is now empty since we moved initialization to Awake
    }
    
    private void ConfigureCanvasMundo()
    {
        // Set render mode
        worldCanvas.renderMode = RenderMode.ScreenSpaceCamera;
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
    
    
    public Resource CreateResource(ResourceType type, TriangleData originTriangle, TriangleData destinationTriangle = null, Country owner = null)
    {
        // Ensure worldCanvas is available
        if (worldCanvas == null)
        {
            CanvasMundo canvasMundo = FindFirstObjectByType<CanvasMundo>();
            if (canvasMundo != null)
            {
                worldCanvas = canvasMundo.GetCanvas();
            }
            
            if (worldCanvas == null)
            {
                Debug.LogError("ResourceManager: CanvasMundo not found! Cannot create resource.");
                return null;
            }
        }
        
        // Create GameObject for the resource as child of CanvasMundo
        GameObject resourceGO = new GameObject($"Resource_{type}");
        resourceGO.transform.SetParent(worldCanvas.transform, false);
        
        // Add Resource component
        Resource resource = resourceGO.AddComponent<Resource>();
        
        // Initialize the resource properties
        resource.type = type;
        resource.owner = owner ?? originTriangle.country;
        resource.origin = originTriangle;
        resource.destination = destinationTriangle ?? originTriangle;
        

        
        activeResources.Add(resource);
        
        // Add resource to triangle lists
        if (!originTriangle.resourcesOriginatingFrom.Contains(resource))
        {
            originTriangle.resourcesOriginatingFrom.Add(resource);
        }
        
        if (destinationTriangle != null && destinationTriangle != originTriangle)
        {
            if (!destinationTriangle.resourcesDestinedTo.Contains(resource))
            {
                destinationTriangle.resourcesDestinedTo.Add(resource);
            }
        }
        else
        {
            // If no destination or same as origin, add to origin's destined list
            if (!originTriangle.resourcesDestinedTo.Contains(resource))
            {
                originTriangle.resourcesDestinedTo.Add(resource);
            }
        }
        
        return resource;
    }

    
    public void RemoveResource(Resource resource)
    {
        if (activeResources.Contains(resource))
        {
            activeResources.Remove(resource);
        }
        
        // Remove resource from triangle lists
        if (resource != null)
        {
            if (resource.origin != null)
            {
                resource.origin.resourcesOriginatingFrom.Remove(resource);
                resource.origin.resourcesDestinedTo.Remove(resource);
            }
            
            if (resource.destination != null && resource.destination != resource.origin)
            {
                resource.destination.resourcesDestinedTo.Remove(resource);
            }
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
            
            // Update resource visual
            resource.UpdateVisual();
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
    
    /// <summary>
    /// Returns all active resources for serialization
    /// </summary>
    public List<Resource> GetAllActiveResources()
    {
        return new List<Resource>(activeResources);
    }
    
    /// <summary>
    /// Returns the count of active resources
    /// </summary>
    public int GetActiveResourceCount()
    {
        return activeResources.Count;
    }
    
    /// <summary>
    /// Returns a list of all resource types currently active
    /// </summary>
    public List<ResourceType> GetActiveResourceTypes()
    {
        var types = new List<ResourceType>();
        foreach (var resource in activeResources)
        {
            if (resource != null && !types.Contains(resource.type))
            {
                types.Add(resource.type);
            }
        }
        return types;
    }
    
    /// <summary>
    /// Returns a summary of active resources for debugging
    /// </summary>
    public string GetResourceSummary()
    {
        var summary = $"Active Resources: {activeResources.Count}\n";
        var typeCounts = new Dictionary<ResourceType, int>();
        
        foreach (var resource in activeResources)
        {
            if (resource != null)
            {
                if (!typeCounts.ContainsKey(resource.type))
                    typeCounts[resource.type] = 0;
                typeCounts[resource.type]++;
            }
        }
        
        foreach (var kvp in typeCounts)
        {
            summary += $"- {kvp.Key.GetDisplayName()}: {kvp.Value}\n";
        }
        
        return summary;
    }
    
    /// <summary>
    /// Rebuilds auxiliary data structures after loading resources
    /// This method can be called after resources are restored to repopulate any missing data structures
    /// </summary>
    public void RebuildAuxiliaryDataStructures()
    {
        Debug.Log("ResourceManager: Rebuilding auxiliary data structures...");
        
        // Clear existing auxiliary data
        foreach (var resource in activeResources)
        {
            if (resource != null && resource.origin != null)
            {
                // Clear existing lists
                resource.origin.resourcesOriginatingFrom.Clear();
                resource.origin.resourcesDestinedTo.Clear();
                
                if (resource.destination != null)
                {
                    resource.destination.resourcesOriginatingFrom.Clear();
                    resource.destination.resourcesDestinedTo.Clear();
                }
            }
        }
        
        // Rebuild auxiliary data
        int rebuiltCount = 0;
        foreach (var resource in activeResources)
        {
            if (resource != null && resource.origin != null)
            {
                // Add to origin's originating list
                if (!resource.origin.resourcesOriginatingFrom.Contains(resource))
                {
                    resource.origin.resourcesOriginatingFrom.Add(resource);
                }
                
                // Add to destination's destined list
                if (resource.destination != null)
                {
                    if (!resource.destination.resourcesDestinedTo.Contains(resource))
                    {
                        resource.destination.resourcesDestinedTo.Add(resource);
                    }
                }
                else
                {
                    // If no destination, add to origin's destined list
                    if (!resource.origin.resourcesDestinedTo.Contains(resource))
                    {
                        resource.origin.resourcesDestinedTo.Add(resource);
                    }
                }
                
                rebuiltCount++;
            }
        }
        
        Debug.Log($"ResourceManager: Rebuilt auxiliary data for {rebuiltCount} resources");
    }
    

} 