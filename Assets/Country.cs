using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Country
{
    [Header("Country Information")]
    public string name;
    public Color color;
    public int index = -1;
    
    [Header("Territory")]
    [NonSerialized]
    public List<TriangleData> territory = new();
    
    [Header("Statistics")]
    public float totalArea;
    public int triangleCount;
    
    public Country()
    {
        name = "New Country";
        color = Color.white; // Default color, will be set to random later
        territory = new List<TriangleData>();
    }
    
    public Country(string countryName, Color countryColor)
    {
        name = countryName;
        color = countryColor;
        territory = new List<TriangleData>();
    }
    
    /// <summary>
    /// Initializes the country with a random color if it's still using the default
    /// Call this from Awake() or Start() to avoid threading issues
    /// </summary>
    public void InitializeRandomColor()
    {
        if (color == Color.white && name == "New Country")
        {
            color = GetRandomColor();
        }
    }
    
    /// <summary>
    /// Adds a triangle to this country's territory
    /// </summary>
    public void AddTriangle(TriangleData triangle)
    {
        if (triangle != null && !territory.Contains(triangle))
        {
            territory.Add(triangle);
            UpdateStatistics();
        }
    }
    
    /// <summary>
    /// Removes a triangle from this country's territory
    /// </summary>
    public void RemoveTriangle(TriangleData triangle)
    {
        if (territory.Remove(triangle))
        {
            UpdateStatistics();
        }
    }
    
    /// <summary>
    /// Adds a triangle to this country's territory without triggering the triangle's assignment logic
    /// Use this when you want to manage the relationship from the country side
    /// </summary>
    public void AddTriangleInternal(TriangleData triangle)
    {
        if (triangle != null && !territory.Contains(triangle))
        {
            territory.Add(triangle);
            triangle.country = this;
            UpdateStatistics();
        }
    }
    
    /// <summary>
    /// Removes a triangle from this country's territory without triggering the triangle's removal logic
    /// Use this when you want to manage the relationship from the country side
    /// </summary>
    public void RemoveTriangleInternal(TriangleData triangle)
    {
        if (territory.Remove(triangle))
        {
            triangle.country = null;
            UpdateStatistics();
        }
    }
    
    /// <summary>
    /// Adds multiple triangles to this country's territory
    /// </summary>
    public void AddTriangles(List<TriangleData> triangles)
    {
        foreach (var triangle in triangles)
        {
            if (triangle != null && !territory.Contains(triangle))
            {
                territory.Add(triangle);
            }
        }
        UpdateStatistics();
    }
    
    /// <summary>
    /// Clears all territory and resets statistics
    /// </summary>
    public void ClearTerritory()
    {
        territory.Clear();
        UpdateStatistics();
    }
    
    /// <summary>
    /// Updates the country's statistics based on current territory
    /// </summary>
    private void UpdateStatistics()
    {
        triangleCount = territory.Count;
        totalArea = CalculateTotalArea();
    }
    
    /// <summary>
    /// Calculates the total area of the country's territory
    /// </summary>
    private float CalculateTotalArea()
    {
        float area = 0f;
        foreach (var triangle in territory)
        {
            area += CalculateTriangleArea(triangle);
        }
        return area;
    }
    
    /// <summary>
    /// Calculates the area of a single triangle on a sphere
    /// </summary>
    private float CalculateTriangleArea(TriangleData triangle)
    {
        // Using spherical triangle area formula
        var (ab, bc, ca) = triangle.GetSideLengths();
        
        // Convert side lengths to angles (assuming unit sphere)
        float angleA = Mathf.Acos((Mathf.Cos(bc) - Mathf.Cos(ab) * Mathf.Cos(ca)) / (Mathf.Sin(ab) * Mathf.Sin(ca)));
        float angleB = Mathf.Acos((Mathf.Cos(ca) - Mathf.Cos(ab) * Mathf.Cos(bc)) / (Mathf.Sin(ab) * Mathf.Sin(bc)));
        float angleC = Mathf.Acos((Mathf.Cos(ab) - Mathf.Cos(bc) * Mathf.Cos(ca)) / (Mathf.Sin(bc) * Mathf.Sin(ca)));
        
        // Spherical excess (sum of angles - π)
        float sphericalExcess = angleA + angleB + angleC - Mathf.PI;
        
        // Area = R² * spherical excess (where R is the radius)
        return sphericalExcess;
    }
    
    /// <summary>
    /// Gets the center point of the country's territory
    /// </summary>
    public Vector3 GetTerritoryCenter()
    {
        if (territory.Count == 0)
            return Vector3.zero;
            
        Vector3 center = Vector3.zero;
        foreach (var triangle in territory)
        {
            center += triangle.GetCenter();
        }
        return center / territory.Count;
    }
    
    /// <summary>
    /// Gets the geographic center (latitude/longitude) of the country
    /// </summary>
    public (float latitude, float longitude) GetGeographicCenter()
    {
        Vector3 center = GetTerritoryCenter();
        return TriangleData.Vector3ToLatLon(center);
    }
    
    /// <summary>
    /// Checks if a triangle is adjacent to any triangle in this country's territory
    /// </summary>
    public bool IsAdjacentToTerritory(TriangleData triangle)
    {
        foreach (var territoryTriangle in territory)
        {
            if (territoryTriangle.adjacentTriangles.Contains(triangle.id))
            {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Gets all triangles that are adjacent to this country's territory
    /// </summary>
    public List<TriangleData> GetAdjacentTriangles(List<TriangleData> allTriangles)
    {
        HashSet<int> adjacentIds = new HashSet<int>();
        
        foreach (var territoryTriangle in territory)
        {
            foreach (int adjacentId in territoryTriangle.adjacentTriangles)
            {
                if (adjacentId < allTriangles.Count)
                {
                    var adjacentTriangle = allTriangles[adjacentId];
                    if (!territory.Contains(adjacentTriangle))
                    {
                        adjacentIds.Add(adjacentId);
                    }
                }
            }
        }
        
        List<TriangleData> adjacentTriangles = new List<TriangleData>();
        foreach (int id in adjacentIds)
        {
            if (id < allTriangles.Count)
            {
                adjacentTriangles.Add(allTriangles[id]);
            }
        }
        
        return adjacentTriangles;
    }
    
    /// <summary>
    /// Generates a random color for the country
    /// </summary>
    private Color GetRandomColor()
    {
        return new Color(
            UnityEngine.Random.Range(0.2f, 1.0f),
            UnityEngine.Random.Range(0.2f, 1.0f),
            UnityEngine.Random.Range(0.2f, 1.0f),
            1.0f
        );
    }
    
    /// <summary>
    /// Gets a string representation of the country
    /// </summary>
    public override string ToString()
    {
        var (lat, lon) = GetGeographicCenter();
        return $"Country: {name}\n" +
               $"Color: {color}\n" +
               $"Territory: {triangleCount} triangles\n" +
               $"Area: {totalArea:F2} km²\n" +
               $"Center: {lat:F2}°, {lon:F2}°";
    }
} 