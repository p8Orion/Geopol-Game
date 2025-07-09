using System;
using UnityEngine;

[Serializable]
public class FeatureType
{
    [Header("Basic Info")]
    public int id;
    public string name;
    
    [Header("Visual Properties")]
    public GameObject meshPrefab; // Null for now, keeping default mesh generation
    public Color color = Color.white;
    
    // Static instances for common feature types
    public static readonly FeatureType None = new FeatureType { id = 0, name = "None", color = Color.clear };
    public static readonly FeatureType Road = new FeatureType { id = 1, name = "Road", color = Color.gray };
    public static readonly FeatureType Pipeline = new FeatureType { id = 2, name = "Pipeline", color = Color.blue };
    public static readonly FeatureType Canal = new FeatureType { id = 5, name = "Canal", color = Color.cyan };
    public static readonly FeatureType Bridge = new FeatureType { id = 6, name = "Bridge", color = Color.yellow };
    public static readonly FeatureType Tunnel = new FeatureType { id = 7, name = "Tunnel", color = Color.magenta };
    
    // List of all available feature types
    public static readonly FeatureType[] AllTypes = { None, Road, Pipeline, Canal, Bridge, Tunnel };
    
    public override bool Equals(object obj)
    {
        if (obj is FeatureType other)
            return id == other.id;
        return false;
    }
    
    public override int GetHashCode()
    {
        return id.GetHashCode();
    }
    
    public static bool operator ==(FeatureType a, FeatureType b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;
        return a.id == b.id;
    }
    
    public static bool operator !=(FeatureType a, FeatureType b)
    {
        return !(a == b);
    }
    
    public override string ToString()
    {
        return name;
    }
} 
