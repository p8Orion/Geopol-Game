using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class FeatureRenderer : MonoBehaviour
{
    [Header("Feature Rendering")]
    public Material featureLineMaterial;
    public float lineWidth = 5f;
    public float lineIntensity = 0.8f;
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    
    private Dictionary<string, GameObject> featureSegments = new Dictionary<string, GameObject>();
    private Transform featureParent;
    
    void Awake()
    {
        // Create parent for all feature segments
        featureParent = new GameObject("FeatureSegments").transform;
        featureParent.SetParent(transform);
        featureParent.localPosition = Vector3.zero;
        featureParent.localRotation = Quaternion.identity;
        featureParent.localScale = Vector3.one;
    }
    
    /// <summary>
    /// Called when a triangle's features change - recalculates all connected segments
    /// </summary>
    public void OnTriangleFeaturesChanged(TriangleData triangle)
    {
        // Get all features on this triangle
        var features = new List<(FeatureType type, int level)>();
        for (int i = 0; i < triangle.featureTypes.Count; i++)
        {
            features.Add((triangle.featureTypes[i], triangle.featureLevels[i]));
        }
        
        // For each feature, check adjacent triangles and create/update segments
        foreach (var (featureType, level) in features)
        {
            UpdateFeatureSegments(triangle, featureType, level);
        }
        
        // Also check if we need to remove segments (when features are removed)
        RemoveOrphanedSegments();
    }
    
    /// <summary>
    /// Updates feature segments for a specific feature type on a triangle
    /// </summary>
    private void UpdateFeatureSegments(TriangleData triangle, FeatureType featureType, int level)
    {
        // Check each adjacent triangle
        foreach (int adjacentId in triangle.adjacentTriangles)
        {
            if (adjacentId >= 0 && adjacentId < triangle.adjacentTriangles.Count)
            {
                var adjacentTriangle = GetTriangleById(adjacentId);
                if (adjacentTriangle != null)
                {
                    // Check if adjacent triangle has the same feature
                    if (adjacentTriangle.HasFeature(featureType))
                    {
                        // Create or update the segment between these triangles
                        CreateOrUpdateFeatureSegment(triangle, adjacentTriangle, featureType, level);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Creates or updates a feature segment between two triangles
    /// </summary>
    private void CreateOrUpdateFeatureSegment(TriangleData triA, TriangleData triB, FeatureType featureType, int level)
    {
        string segmentKey = GetFeatureSegmentKey(triA.id, triB.id, featureType);
        
        // Check if segment already exists
        if (featureSegments.ContainsKey(segmentKey))
        {
            // Update existing segment
            UpdateFeatureSegment(segmentKey, triA, triB, featureType, level);
        }
        else
        {
            // Create new segment
            CreateFeatureSegment(segmentKey, triA, triB, featureType, level);
        }
    }
    
    /// <summary>
    /// Creates a new feature segment GameObject
    /// </summary>
    private void CreateFeatureSegment(string segmentKey, TriangleData triA, TriangleData triB, FeatureType featureType, int level)
    {
        var segmentObject = new GameObject($"Feature_{featureType}_{triA.id}_to_{triB.id}");
        segmentObject.transform.SetParent(featureParent);
        segmentObject.transform.localPosition = Vector3.zero;
        segmentObject.transform.localRotation = Quaternion.identity;
        segmentObject.transform.localScale = Vector3.one;
        
        // Add mesh components
        var meshFilter = segmentObject.AddComponent<MeshFilter>();
        var renderer = segmentObject.AddComponent<MeshRenderer>();
        
        // Create material instance
        var material = new Material(featureLineMaterial);
        renderer.material = material;
        
        // Configure renderer
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingOrder = 2; // Render above borders
        
        // Store reference
        featureSegments[segmentKey] = segmentObject;
        
        // Generate mesh
        UpdateFeatureSegmentMesh(segmentKey, triA, triB, featureType, level);
        
        if (showDebugInfo)
        {
            Debug.Log($"FeatureRenderer: Created segment {segmentKey}");
        }
    }
    
    /// <summary>
    /// Updates an existing feature segment
    /// </summary>
    private void UpdateFeatureSegment(string segmentKey, TriangleData triA, TriangleData triB, FeatureType featureType, int level)
    {
        UpdateFeatureSegmentMesh(segmentKey, triA, triB, featureType, level);
        
        if (showDebugInfo)
        {
            Debug.Log($"FeatureRenderer: Updated segment {segmentKey}");
        }
    }
    
    /// <summary>
    /// Updates the mesh for a feature segment
    /// </summary>
    private void UpdateFeatureSegmentMesh(string segmentKey, TriangleData triA, TriangleData triB, FeatureType featureType, int level)
    {
        if (!featureSegments.ContainsKey(segmentKey)) return;
        
        var segmentObject = featureSegments[segmentKey];
        var meshFilter = segmentObject.GetComponent<MeshFilter>();
        var renderer = segmentObject.GetComponent<MeshRenderer>();
        
        // Generate line between triangle centers
        Vector3 centerA = triA.GetCenter();
        Vector3 centerB = triB.GetCenter();
        
        // Create a simple line mesh
        var mesh = new Mesh();
        mesh.name = $"FeatureMesh_{featureType}_{triA.id}_to_{triB.id}";
        
        // Calculate line direction and perpendicular for thickness
        Vector3 direction = (centerB - centerA).normalized;
        Vector3 center = (centerA + centerB) * 0.5f;
        Vector3 radial = (center - Vector3.zero).normalized;
        Vector3 perpendicular = Vector3.Cross(direction, radial).normalized;
        
        float thickness = lineWidth * (level / 5f); // Scale thickness by level
        
        // Create vertices for thick line
        var vertices = new Vector3[]
        {
            centerA + perpendicular * thickness,
            centerA - perpendicular * thickness,
            centerB + perpendicular * thickness,
            centerB - perpendicular * thickness
        };
        
        var colors = new Color[]
        {
            GetFeatureColor(featureType),
            GetFeatureColor(featureType),
            GetFeatureColor(featureType),
            GetFeatureColor(featureType)
        };
        
        var triangles = new int[]
        {
            0, 2, 1,
            2, 3, 1
        };
        
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        
        meshFilter.mesh = mesh;
        
        // Update material properties
        var material = renderer.material;
        material.SetFloat("_LineWidth", thickness);
        material.SetFloat("_LineIntensity", lineIntensity);
        material.SetColor("_FeatureColor", GetFeatureColor(featureType));
    }
    
    /// <summary>
    /// Removes segments that no longer have valid features
    /// </summary>
    private void RemoveOrphanedSegments()
    {
        var segmentsToRemove = new List<string>();
        
        foreach (var kvp in featureSegments)
        {
            string segmentKey = kvp.Key;
            var segmentObject = kvp.Value;
            
            // Parse segment key to get triangle IDs and feature type
            var (triAId, triBId, featureType) = ParseFeatureSegmentKey(segmentKey);
            
            // Check if both triangles still have this feature
            var triA = GetTriangleById(triAId);
            var triB = GetTriangleById(triBId);
            
            bool shouldRemove = false;
            
            if (triA == null || triB == null)
            {
                shouldRemove = true;
            }
            else if (!triA.HasFeature(featureType) || !triB.HasFeature(featureType))
            {
                shouldRemove = true;
            }
            
            if (shouldRemove)
            {
                segmentsToRemove.Add(segmentKey);
            }
        }
        
        // Remove orphaned segments
        foreach (string segmentKey in segmentsToRemove)
        {
            if (featureSegments.ContainsKey(segmentKey))
            {
                var segmentObject = featureSegments[segmentKey];
                if (Application.isPlaying)
                    Destroy(segmentObject);
                else
                    DestroyImmediate(segmentObject);
                featureSegments.Remove(segmentKey);
                
                if (showDebugInfo)
                {
                    Debug.Log($"FeatureRenderer: Removed orphaned segment {segmentKey}");
                }
            }
        }
    }
    
    /// <summary>
    /// Gets a unique key for a feature segment
    /// </summary>
    private string GetFeatureSegmentKey(int triAId, int triBId, FeatureType featureType)
    {
        // Ensure consistent ordering
        if (triAId > triBId)
        {
            return $"{triBId}_{triAId}_{featureType}";
        }
        return $"{triAId}_{triBId}_{featureType}";
    }
    
    /// <summary>
    /// Parses a feature segment key back to its components
    /// </summary>
    private (int triAId, int triBId, FeatureType featureType) ParseFeatureSegmentKey(string segmentKey)
    {
        string[] parts = segmentKey.Split('_');
        if (parts.Length >= 3)
        {
            int triAId = int.Parse(parts[0]);
            int triBId = int.Parse(parts[1]);
            FeatureType featureType = (FeatureType)System.Enum.Parse(typeof(FeatureType), parts[2]);
            return (triAId, triBId, featureType);
        }
        return (-1, -1, FeatureType.None);
    }
    
    /// <summary>
    /// Gets the color for a feature type
    /// </summary>
    private Color GetFeatureColor(FeatureType featureType)
    {
        switch (featureType)
        {
            case FeatureType.Road:
                return Color.gray;
            case FeatureType.Pipeline:
                return Color.cyan;
            case FeatureType.Canal:
                return Color.blue;
            case FeatureType.Bridge:
                return Color.brown;
            case FeatureType.Tunnel:
                return Color.black;
            case FeatureType.None:
            default:
                return Color.white;
        }
    }
    
    /// <summary>
    /// Gets a triangle by ID (you'll need to implement this based on your data structure)
    /// </summary>
    private TriangleData GetTriangleById(int id)
    {
        // This should be implemented based on how you store triangle data
        // For now, assuming you have access to the IcoSphere
        var icoSphere = FindFirstObjectByType<IcoSphere>();
        if (icoSphere != null && icoSphere.triangleDataList != null && id >= 0 && id < icoSphere.triangleDataList.Count)
        {
            return icoSphere.triangleDataList[id];
        }
        return null;
    }
    
    /// <summary>
    /// Clears all feature segments
    /// </summary>
    public void ClearAllSegments()
    {
        foreach (var kvp in featureSegments)
        {
            if (Application.isPlaying)
                Destroy(kvp.Value);
            else
                DestroyImmediate(kvp.Value);
        }
        featureSegments.Clear();
    }
    
    /// <summary>
    /// Rebuilds all feature segments (call this after loading save data)
    /// </summary>
    public void RebuildAllSegments()
    {
        ClearAllSegments();
        
        var icoSphere = FindFirstObjectByType<IcoSphere>();
        if (icoSphere != null && icoSphere.triangleDataList != null)
        {
            foreach (var triangle in icoSphere.triangleDataList)
            {
                OnTriangleFeaturesChanged(triangle);
            }
        }
    }
} 