using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class TriangleData : IResourceDropPosition
{
    public int id;
    public Vector3 a, b, c;
    public int terrainType;
    public float colorR, colorG, colorB;
    
    [Header("Natural Resources")]
    public ResourceType naturalResource = ResourceType.None; // None means no resource assigned
    
    [System.NonSerialized]
    public ResourceIcon resourceIcon; // Visual representation of the resource

    [Header("Resource Origins/Destinations")]
    [System.NonSerialized]
    public List<Resource> resourcesOriginatingFrom = new();
    [System.NonSerialized]
    public List<Resource> resourcesDestinedTo = new();

    [Header("Country Assignment")]
    [NonSerialized]
    public Country country;
    
    // Adjacent triangles (sharing an edge) - using HashSet for performance and uniqueness
    [System.NonSerialized] // Don't serialize HashSet directly
    public HashSet<int> adjacentTriangles = new();

    // Adjacent triangles (sharing a vertex) - using HashSet for performance and uniqueness
    [System.NonSerialized] // Don't serialize HashSet directly
    public HashSet<int> vertexAdjacentTriangles = new();

    // Convert a Vector3 point on a sphere to latitude and longitude
    public static (float latitude, float longitude) Vector3ToLatLon(Vector3 point)
    {
        // Normalize the point to ensure it's on the unit sphere
        Vector3 normalized = point.normalized;
        
        // Calculate latitude (angle from equator)
        float latitude = Mathf.Round(Mathf.Asin(normalized.y) * Mathf.Rad2Deg);
        
        // Calculate longitude (angle around equator) 
        float longitude = Mathf.Round(Mathf.Atan2(normalized.z, normalized.x) * Mathf.Rad2Deg);
        return (latitude, longitude);
    }

    // Get lat/lon coordinates for all three points of the triangle
    public (float lat, float lon)[] GetLatLonCoordinates()
    {
        return new[]
        {
            Vector3ToLatLon(a),
            Vector3ToLatLon(b),
            Vector3ToLatLon(c)
        };
    }

    // Get the center point of the triangle
    public Vector3 GetCenter()
    {
        return (a + b + c) / 3f;
    }

    // Calculate the normal vector of the triangle
    public Vector3 GetNormal()
    {
        Vector3 side1 = b - a;
        Vector3 side2 = c - a;
        return Vector3.Cross(side1, side2).normalized;
    }

    // Calculate the three side lengths of the triangle
    public (float ab, float bc, float ca) GetSideLengths()
    {
        float ab = Vector3.Distance(a, b);
        float bc = Vector3.Distance(b, c);
        float ca = Vector3.Distance(c, a);
        return (ab, bc, ca);
    }
    
    /// <summary>
    /// Assigns this triangle to a country
    /// </summary>
    public void AssignToCountry(Country newCountry)
    {
        // Remove from previous country if any
        if (country != null)
        {
            country.RemoveTriangleInternal(this);
        }
        
        // Assign to new country
        country = newCountry;
        
        // Add to new country's territory
        if (country != null)
        {
            country.AddTriangleInternal(this);
        }
    }
    
    /// <summary>
    /// Removes this triangle from its current country
    /// </summary>
    public void RemoveFromCountry()
    {
        if (country != null)
        {
            country.RemoveTriangleInternal(this);
            country = null;
        }
    }
    
    /// <summary>
    /// Gets the name of the country this triangle belongs to
    /// </summary>
    public string GetCountryName()
    {
        return country != null ? country.name : "Unclaimed";
    }
    
    /// <summary>
    /// Checks if this triangle belongs to any country
    /// </summary>
    public bool IsClaimed()
    {
        return country != null;
    }
    
    /// <summary>
    /// Gets the color of the country this triangle belongs to
    /// </summary>
    public Color GetCountryColor()
    {
        return country != null ? country.color : Color.gray;
    }
    
    /// <summary>
    /// Checks if this triangle has a resource assigned
    /// </summary>
    public bool HasResource()
    {
        return naturalResource != ResourceType.None;
    }
    
    /// <summary>
    /// Sets the natural resource for this triangle and creates/updates the visual icon
    /// </summary>
    public void SetNaturalResource(ResourceType resourceType)
    {
        // Si es el mismo recurso, no hacer nada
        if (naturalResource == resourceType)
        {
            return;
        }
        
        // Destroy existing icon if changing resource
        if (resourceIcon != null)
        {
            resourceIcon.DestroyIcon();
            resourceIcon = null;
        }
        
        naturalResource = resourceType;
        UpdateResourceIcon();
    }
    
    /// <summary>
    /// Creates or updates the resource icon based on the current naturalResource
    /// </summary>
    public void UpdateResourceIcon()
    {
        // Destroy existing icon if no resource
        if (naturalResource == ResourceType.None)
        {
            if (resourceIcon != null)
            {
                resourceIcon.DestroyIcon();
                resourceIcon = null;
            }
            return;
        }
        
        // Verificar si el icono existe y es válido
        if (resourceIcon != null && resourceIcon.gameObject != null)
        {
            // Update existing icon
            resourceIcon.SetResourceType(naturalResource);
        }
        else
        {
            // Create new icon if needed
            CreateResourceIcon();
        }
    }
    
    /// <summary>
    /// Regenerates the resource icon after loading from save
    /// </summary>
    public void RegenerateResourceIcon()
    {
        // Destroy any existing icon first
        if (resourceIcon != null)
        {
            resourceIcon.DestroyIcon();
            resourceIcon = null;
        }
        
        // Create new icon if there's a natural resource
        if (naturalResource != ResourceType.None)
        {
            CreateResourceIcon();
        }
    }
    
    /// <summary>
    /// Creates a new ResourceIcon for this triangle
    /// </summary>
    private void CreateResourceIcon()
    {
        // Verificar si ya existe un icono para este triángulo y destruirlo
        if (resourceIcon != null)
        {
            resourceIcon.DestroyIcon();
            resourceIcon = null;
        }
        
        // Buscar si ya existe un GameObject con el nombre esperado y destruirlo
        string expectedName = $"ResourceIcon_{id}_{naturalResource}";
        GameObject existingIcon = GameObject.Find(expectedName);
        if (existingIcon != null)
        {
            UnityEngine.Object.DestroyImmediate(existingIcon);
        }
        
        GameObject iconGO = new GameObject(expectedName);
        ResourceIcon icon = iconGO.AddComponent<ResourceIcon>();
        icon.SetTriangleData(this);
        resourceIcon = icon;
    }

    public override string ToString()
    {
        var center = GetCenter();
        var (lat, lon) = Vector3ToLatLon(center);
        var (ab, bc, ca) = GetSideLengths();
        string countryInfo = country != null ? $"Country: {country.name}" : "Country: Unclaimed";
        
        return $"Triangle {id}\n" +
               $"Terrain: {terrainType}\n" +
               $"{lat:F2}°, {lon:F2}°\n" +
               $"Sides: {ab:F2}, {bc:F2}, {ca:F2}\n" +
               $"{countryInfo}\n" +
               $"Edge Adjacent: {string.Join(", ", adjacentTriangles)}\n" +
               $"Vertex Adjacent: {string.Join(", ", vertexAdjacentTriangles)}";
    }
    
    // Implementación de IResourceDropPosition
    public Vector3 GetWorldPosition()
    {
        return GetCenter();
    }
    
    public string GetDropPositionName()
    {
        return $"Triangle_{id}";
    }
    
    
    public bool IsAvailable()
    {
        return true;
    }

}
