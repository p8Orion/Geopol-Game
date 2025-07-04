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
    
    
    public Resource CreateResource(ResourceType type, TriangleData originTriangle, TriangleData destinationTriangle = null)
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
        resource.origin = originTriangle;
        resource.destination = destinationTriangle ?? originTriangle;
        
        // Initialize route as direct path
        resource.waypoints.Clear();
        resource.waypoints.Add(originTriangle.GetCenter());
        resource.waypoints.Add((destinationTriangle ?? originTriangle).GetCenter());
        
        activeResources.Add(resource);
        
        return resource;
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
    

} 