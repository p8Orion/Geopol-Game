using UnityEngine;

/// <summary>
/// Enum that defines the terrain type categories
/// </summary>
public enum TerrainTypeEnum
{
    Unknown,
    BosqueTropical,
    Sabana,
    Desierto,
    Estepa,
    BosqueTemplado,
    Llanura,
    BosqueBoreal,
    Tundra,
    Hielo,
    Ocean
}

[System.Serializable]
public class TerrainType
{
    
    [Header("Terrain Properties")]
    public string name = "New Terrain";
    public Material material;
    public Color previewColor = Color.white;
    
    [Header("Metadata")]
    public int id = -1; // Auto-assigned when added to terrain system
    public TerrainTypeEnum terrainType = TerrainTypeEnum.Unknown; // Terrain type classification
    
    public TerrainType()
    {
        name = "New Terrain";
        material = null;
        previewColor = Color.white;
        id = -1;
        terrainType = TerrainTypeEnum.Unknown;
    }
    
    public TerrainType(string terrainName, Material terrainMaterial, Color color)
    {
        name = terrainName;
        material = terrainMaterial;
        previewColor = color;
        id = -1;
        terrainType = TerrainTypeEnum.Unknown;
    }
    
    public TerrainType(string terrainName, Material terrainMaterial, Color color, TerrainTypeEnum terrainType)
    {
        name = terrainName;
        material = terrainMaterial;
        previewColor = color;
        id = -1;
        this.terrainType = terrainType;
    }
    
    public Texture2D GetTexture()
    {
        if (material == null) return null;
        
        Texture tex = material.mainTexture;
        if (tex == null)
            tex = material.GetTexture("_BaseMap");
            
        return tex as Texture2D;
    }
    
    public Color GetBaseColor()
    {
        if (material == null) return previewColor;
        
        if (material.HasProperty("_BaseColor"))
            return material.GetColor("_BaseColor");
        else if (material.HasProperty("_Color"))
            return material.GetColor("_Color");
        else
            return previewColor;
    }
    
    public override string ToString()
    {
        return $"{name} (ID: {id}, Type: {terrainType})";
    }
} 