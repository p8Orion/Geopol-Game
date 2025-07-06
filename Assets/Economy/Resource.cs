using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Resource : MonoBehaviour 
{
    [Header("Resource Properties")]
    public ResourceType type;
    public Country owner;

    [Header("Location")]
    public TriangleData origin;
    public TriangleData destination;
    
    [Header("Route")]
    public List<Vector3> waypoints = new();
    public bool isActive = true;

    [Header("Visual")]
    public ResourceIcon iconInstance;
    public bool isMoving = true;
    public bool shouldShowIcon = true;
    
    [Header("Selection")]
    public bool isSelected = false;
    public bool isSelectable = true;

    // Constructor removed - MonoBehaviour cannot have custom constructors
    // Use ResourceManager.CreateResource() instead
    
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
        if (waypoints.Count < 2) return origin != null ? origin.GetCenter() : Vector3.zero;
        
        // For now, just return origin position
        return origin != null ? origin.GetCenter() : Vector3.zero;
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
            // Update visibility based on conditions
            bool shouldBeVisible = shouldShowIcon && isActive;
            iconInstance.SetVisible(shouldBeVisible);
        }
    }
    
    private void CreateIcon()
    {
        // Create a GameObject for the icon
        GameObject iconGO = new GameObject($"ResourceIcon_{type}");
        
        // Add ResourceIcon component
        ResourceIcon icon = iconGO.AddComponent<ResourceIcon>();
        
        // Set the resource reference first
        icon.SetResource(this);
        
        // Ensure the icon is properly initialized
        if (icon != null)
        {
            // Store reference
            iconInstance = icon;
            
            // Set initial visibility
            icon.SetVisible(shouldShowIcon && isActive);
            
            Debug.Log($"ResourceIcon created successfully for {type} at position {GetCurrentPosition()}");
        }

    }
    
    public void SetSelected(bool selected)
    {
        if (!isSelectable) return;
        
        isSelected = selected;
        UpdateVisual();
    }
    
    public void ToggleSelection()
    {
        SetSelected(!isSelected);
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
    
    public void SetSelectable(bool selectable)
    {
        isSelectable = selectable;
        if (!selectable && isSelected)
        {
            SetSelected(false);
        }
    }

    
    public override string ToString()
    {
        return $"{type.GetEmoji()} {type.GetDisplayName()}";
    }
} 