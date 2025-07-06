using UnityEngine;

[System.Serializable]
public class ResourceSaveData
{
    public ResourceType type;
    public int originTriangleId;
    public int destinationTriangleId;
    public bool isActive;
    public bool isMoving;
    public bool shouldShowIcon;
} 