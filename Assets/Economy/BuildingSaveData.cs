using UnityEngine;

[System.Serializable]
public class BuildingSaveData
{
    public string uniqueId; // Building's unique identifier
    public string buildingTypeName; // Name of the building type
    public int buildingLevel; // Level of the building
    public int triangleId; // ID of the triangle where the building is located
    public string countryName; // Name of the country that owns the building
} 