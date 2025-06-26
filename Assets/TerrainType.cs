using UnityEngine;

[System.Serializable]
public class TerrainType
{
    [Header("Terrain Properties")]
    public string name = "New Terrain";
    public Material material;
    public Color previewColor = Color.white;
    
    [Header("Metadata")]
    public int id = -1; // Auto-assigned when added to terrain system
    
    public TerrainType()
    {
        name = "New Terrain";
        material = null;
        previewColor = Color.white;
        id = -1;
    }
    
    public TerrainType(string terrainName, Material terrainMaterial, Color color)
    {
        name = terrainName;
        material = terrainMaterial;
        previewColor = color;
        id = -1;
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
        return $"{name} (ID: {id})";
    }
} 