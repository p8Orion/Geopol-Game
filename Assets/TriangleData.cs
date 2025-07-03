using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TriangleData
{
    public int id;
    public Vector3 a, b, c;
    public int terrainType;
    public float colorR, colorG, colorB;
    
    [Header("Natural Resources")]
    public ResourceType naturalResource = ResourceType.Cereal;

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
}
