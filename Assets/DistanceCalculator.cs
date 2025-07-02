using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class DistanceCalculator : MonoBehaviour
{
    [Header("Distance Calculator Settings")]
    public bool showPath = true;
    public Color pathColor = Color.red;

    [Header("Debug Settings")]
    public bool showPathInfo = true;
    public bool showTriangleLabels = true;
    
    public enum TerrainFilter
    {
        Both,
        LandOnly,
        WaterOnly
    }
    
    [Header("Path Filtering")]
    
    public TerrainFilter terrainFilter = TerrainFilter.Both;
    public bool sameCountryOnly = false;
    public bool strictVertexAdjacency = false; // If true, all triangles sharing a vertex must be valid
    
    private IcoSphere icoSphere;
    private List<int> currentPath = new List<int>();
    
    void Start()
    {
        // Find IcoSphere if not assigned
        if (icoSphere == null)
        {
            icoSphere = UnityEngine.Object.FindFirstObjectByType<IcoSphere>();
        }
    }
    
    /// <summary>
    /// Calculates the shortest path distance between two triangles using BFS with vertex adjacency
    /// </summary>
    /// <param name="startTriangleId">Starting triangle ID</param>
    /// <param name="endTriangleId">Ending triangle ID</param>
    /// <param name="path">Output parameter for the path (list of triangle IDs)</param>
    /// <returns>Distance in number of triangle hops, or -1 if no path exists</returns>
    public int CalculateDistance(int startTriangleId, int endTriangleId, out List<int> path)
    {
        path = new List<int>();
        
        if (icoSphere == null || icoSphere.triangleDataList == null)
        {
            Debug.LogError("DistanceCalculator: IcoSphere or triangle data not available");
            return -1;
        }
        
        if (startTriangleId < 0 || startTriangleId >= icoSphere.triangleDataList.Count ||
            endTriangleId < 0 || endTriangleId >= icoSphere.triangleDataList.Count)
        {
            Debug.LogError($"DistanceCalculator: Invalid triangle IDs: {startTriangleId} or {endTriangleId}");
            return -1;
        }
        
        if (startTriangleId == endTriangleId)
        {
            path.Add(startTriangleId);
            return 0;
        }
        
        // Check if path is valid with current filters
        if (!IsPathValidWithFilters(startTriangleId, endTriangleId))
        {
            Debug.LogWarning($"DistanceCalculator: Path from {startTriangleId} to {endTriangleId} is not valid with current filters!");
            return -1;
        }
        
        // BFS to find shortest path using vertex adjacency with filters
        Queue<int> queue = new Queue<int>();
        Dictionary<int, int> distance = new Dictionary<int, int>();
        Dictionary<int, int> previous = new Dictionary<int, int>();
        HashSet<int> visited = new HashSet<int>();
        
        queue.Enqueue(startTriangleId);
        distance[startTriangleId] = 0;
        visited.Add(startTriangleId);
        
        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            
            // Check all vertex-adjacent triangles that pass the filters
            var currentTriangle = icoSphere.triangleDataList[current];
            foreach (int neighbor in currentTriangle.vertexAdjacentTriangles)
            {
                if (!visited.Contains(neighbor) && IsTriangleValidForPath(neighbor, startTriangleId))
                {
                    // Check strict vertex adjacency for the transition from current to neighbor
                    if (strictVertexAdjacency && !checkStrictVertexAdjacency(current, neighbor, startTriangleId))
                    {
                        continue; // Skip this neighbor but don't mark as visited
                    }
                    
                    visited.Add(neighbor);
                    distance[neighbor] = distance[current] + 1;
                    previous[neighbor] = current;
                    queue.Enqueue(neighbor);
                    
                    // Found the target
                    if (neighbor == endTriangleId)
                    {
                        // Reconstruct path
                        int node = endTriangleId;
                        while (node != startTriangleId)
                        {
                            path.Insert(0, node);
                            node = previous[node];
                        }
                        path.Insert(0, startTriangleId);
                        
                        return distance[endTriangleId];
                    }
                }
            }
        }
        
        // No path found
        return -1;
    }
    
    /// <summary>
    /// Checks if a triangle is valid for pathfinding with current filters
    /// </summary>
    private bool IsTriangleValidForPath(int triangleId, int startTriangleId)
    {
        if (triangleId < 0 || triangleId >= icoSphere.triangleDataList.Count) return false;
        
        var triangle = icoSphere.triangleDataList[triangleId];
        var startTriangle = icoSphere.triangleDataList[startTriangleId];
        
        // Check terrain filter
        if (!IsTerrainValid(triangle.terrainType)) return false;
        
        // Check country filter
        if (sameCountryOnly && triangle.country != startTriangle.country) return false;
        
        return true;
    }
    
    /// <summary>
    /// Checks if all triangles sharing the vertex between two triangles are valid
    /// </summary>
    private bool checkStrictVertexAdjacency(int fromTriangleId, int toTriangleId, int startTriangleId)
    {
        var fromTriangle = icoSphere.triangleDataList[fromTriangleId];
        var toTriangle = icoSphere.triangleDataList[toTriangleId];
        var startTriangle = icoSphere.triangleDataList[startTriangleId];
        
        // Find the intersection of vertex-adjacent triangles (the triangles that share the vertex)
        var sharedTriangles = fromTriangle.adjacentTriangles.Intersect(toTriangle.vertexAdjacentTriangles);

        // Check if majority of triangles sharing this vertex are valid
        int validCount = 0;
        int totalCount = 0;
        
        foreach (int triangleId in sharedTriangles)
        {
            Debug.Log($"Checking triangle {triangleId}");
            if (triangleId < 0 || triangleId >= icoSphere.triangleDataList.Count) continue;
            
            totalCount++;
            var triangle = icoSphere.triangleDataList[triangleId];
            
            bool isValid = true;
            
            // Check terrain filter
            if (!IsTerrainValid(triangle.terrainType)) isValid = false;
            
            // Check country filter
            if (sameCountryOnly && triangle.country != startTriangle.country) {
                isValid = false;
            }
            
            if (isValid) validCount++;
        }
        
        // If only 1 shared triangle, it must be valid
        if (totalCount == 1) return validCount == 1;
        
        // For multiple triangles, majority must be valid (at least half)
        return validCount >= (totalCount + 1) / 2;
    }
    
    /// <summary>
    /// Checks if a path between two triangles is valid with current filters
    /// </summary>
    private bool IsPathValidWithFilters(int startTriangleId, int endTriangleId)
    {
        if (startTriangleId < 0 || startTriangleId >= icoSphere.triangleDataList.Count ||
            endTriangleId < 0 || endTriangleId >= icoSphere.triangleDataList.Count)
        {
            return false;
        }
        
        var startTriangle = icoSphere.triangleDataList[startTriangleId];
        var endTriangle = icoSphere.triangleDataList[endTriangleId];
        
        // Check terrain filter
        if (!IsTerrainValid(startTriangle.terrainType) || !IsTerrainValid(endTriangle.terrainType))
        {
            return false;
        }
        
        // Check country filter
        if (sameCountryOnly && startTriangle.country != endTriangle.country)
        {
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Checks if a terrain type is valid according to current terrain filter
    /// </summary>
    private bool IsTerrainValid(int terrainType)
    {
        if (icoSphere == null) return true;
        
        bool isWater = icoSphere.oceanTerrainIDs.Contains(terrainType);
        
        switch (terrainFilter)
        {
            case TerrainFilter.LandOnly:
                return !isWater;
            case TerrainFilter.WaterOnly:
                return isWater;
            case TerrainFilter.Both:
            default:
                return true;
        }
    }
    
    /// <summary>
    /// Calculates distance without returning the path (faster)
    /// </summary>
    public int CalculateDistance(int startTriangleId, int endTriangleId)
    {
        List<int> dummyPath;
        return CalculateDistance(startTriangleId, endTriangleId, out dummyPath);
    }
    
    /// <summary>
    /// Gets the current path as a list of triangle IDs
    /// </summary>
    public List<int> GetCurrentPath()
    {
        return new List<int>(currentPath);
    }
    
    /// <summary>
    /// Gets the distance of the current path
    /// </summary>
    public int GetCurrentPathDistance()
    {
        return currentPath.Count > 0 ? currentPath.Count - 1 : 0;
    }
    
    /// <summary>
    /// Sets the terrain filter for pathfinding
    /// </summary>
    public void SetTerrainFilter(TerrainFilter filter)
    {
        terrainFilter = filter;
    }
    
    /// <summary>
    /// Sets the country filter for pathfinding
    /// </summary>
    public void SetCountryFilter(bool sameCountryOnly)
    {
        this.sameCountryOnly = sameCountryOnly;
    }
    
    /// <summary>
    /// Sets the strict vertex adjacency filter for pathfinding
    /// </summary>
    public void SetStrictVertexAdjacency(bool strictVertexAdjacency)
    {
        this.strictVertexAdjacency = strictVertexAdjacency;
    }
} 