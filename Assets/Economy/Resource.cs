using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Resource
{
    [Header("Resource Properties")]
    public ResourceType type;
    public float productionRate = 1f; // Resources per minute
    
    [Header("Location")]
    public Vector3 origin;
    public Vector3 destination;
    
    [Header("Route")]
    public List<Vector3> waypoints = new();
    public bool isActive = true;
    public float routeProgress = 0f; // 0 to 1, represents position along route
    
    [Header("Visual")]
    public ResourceIcon iconInstance;
    public bool isMoving = true;
    public bool shouldShowIcon = true;
    
    [Header("Timing")]
    public float spawnTime;
    public float lastUpdateTime;
    
    public Resource(ResourceType resourceType, Vector3 startPos, Vector3 endPos)
    {
        type = resourceType;
        origin = startPos;
        destination = endPos;
        spawnTime = Time.time;
        lastUpdateTime = Time.time;
        
        // Initialize route as direct path
        waypoints.Add(origin);
        waypoints.Add(destination);
    }
    
    public void AddWaypoint(Vector3 waypoint, bool permanent = false)
    {
        // Find the best position to insert the waypoint
        int insertIndex = waypoints.Count - 1; // Before the destination
        waypoints.Insert(insertIndex, waypoint);
    }
    
    public void RemoveWaypoint(int index)
    {
        if (index > 0 && index < waypoints.Count - 1) // Don't remove origin or destination
        {
            waypoints.RemoveAt(index);
        }
    }
    
    public Vector3 GetCurrentPosition()
    {
        if (waypoints.Count < 2) return origin;
        
        // Calculate total route length
        float totalLength = 0f;
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            totalLength += Vector3.Distance(waypoints[i], waypoints[i + 1]);
        }
        
        // Calculate current position based on progress
        float targetDistance = totalLength * routeProgress;
        float currentDistance = 0f;
        
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            float segmentLength = Vector3.Distance(waypoints[i], waypoints[i + 1]);
            if (currentDistance + segmentLength >= targetDistance)
            {
                // We're in this segment
                float segmentProgress = (targetDistance - currentDistance) / segmentLength;
                return Vector3.Lerp(waypoints[i], waypoints[i + 1], segmentProgress);
            }
            currentDistance += segmentLength;
        }
        
        return destination;
    }
    
    public void UpdateProgress(float deltaTime)
    {
        if (!isActive || !isMoving) return;
        
        // Move along route based on production rate
        float speed = productionRate / 60f; // Convert per minute to per second
        routeProgress += speed * deltaTime;
        
        if (routeProgress >= 1f)
        {
            routeProgress = 1f;
            isMoving = false;
        }
        
        lastUpdateTime = Time.time;
        
        // Update visual representation
        UpdateVisual();
    }
    
    public void UpdateVisual()
    {
        // Create icon if it doesn't exist and we should show it
        if (iconInstance == null && shouldShowIcon && isActive)
        {
            CreateIcon();
        }
        
        if (iconInstance != null)
        {
            // Update position
            iconInstance.SetPosition(GetCurrentPosition());
            
            // Update visibility based on conditions
            bool shouldBeVisible = shouldShowIcon && isActive;
            iconInstance.SetVisible(shouldBeVisible);
        }
    }
    
    private void CreateIcon()
    {
        // Create a GameObject for the icon
        GameObject iconGO = new GameObject($"ResourceIcon_{type}");
        iconGO.transform.position = GetCurrentPosition();
        
        // Add ResourceIcon component
        ResourceIcon icon = iconGO.AddComponent<ResourceIcon>();
        icon.SetResource(this);
        icon.SetTint(type.GetColor());
        
        // Store reference
        iconInstance = icon;
    }
    
    public bool HasReachedDestination()
    {
        return routeProgress >= 1f;
    }
    
    public void Deactivate()
    {
        isActive = false;
        isMoving = false;
        UpdateVisual();
    }
    
    public void Reactivate()
    {
        isActive = true;
        isMoving = true;
        UpdateVisual();
    }
    
    public void SetIconVisible(bool visible)
    {
        shouldShowIcon = visible;
        UpdateVisual();
    }
    
    public float GetRouteLength()
    {
        float length = 0f;
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            length += Vector3.Distance(waypoints[i], waypoints[i + 1]);
        }
        return length;
    }
    
    public override string ToString()
    {
        return $"{type.GetEmoji()} {type.GetDisplayName()} - Progress: {routeProgress:P0}";
    }
} 