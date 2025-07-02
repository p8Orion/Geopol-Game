using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;

public class DistanceTester : MonoBehaviour
{
    [Header("Distance Tester Settings")]
    public DistanceCalculator distanceCalculator;
    public IDPicker idPicker;
    public Text distanceText; // UI Text to display distance info
    
    [Header("Selection Settings")]
    public Color startTriangleColor = Color.green;
    public Color endTriangleColor = Color.red;
    public float selectionMarkerSize = 100f;
    public Material startMarkerMaterial;
    public Material endMarkerMaterial;
    
    [Header("Path Visualization Settings")]
    public bool showPath = true;
    public Color pathColor = Color.red;
    public float pathWidth = 50f;
    public float pathHeight = 50f;
    
    public enum TerrainFilter
    {
        Both,
        LandOnly,
        WaterOnly
    }
    
    [Header("Path Filtering Settings")]
    
    public TerrainFilter terrainFilter = TerrainFilter.Both;
    public bool sameCountryOnly = false;
    
    private int selectedStartTriangle = -1;
    private int selectedEndTriangle = -1;
    private GameObject startMarker;
    private GameObject endMarker;
    private List<GameObject> pathVisualizers = new List<GameObject>();
    private List<int> currentPath = new List<int>();
    
    // GUI settings
    private bool showGUI = true;
    private Rect guiRect = new Rect(Screen.width - 250, 10, 240, 150);
    
    void Start()
    {
        // Find components if not assigned
        if (distanceCalculator == null)
        {
            distanceCalculator = UnityEngine.Object.FindFirstObjectByType<DistanceCalculator>();
        }
        
        if (idPicker == null)
        {
            idPicker = UnityEngine.Object.FindFirstObjectByType<IDPicker>();
        }
        
        // Create marker materials if not assigned
        if (startMarkerMaterial == null)
        {
            startMarkerMaterial = new Material(Shader.Find("Unlit/Color"));
            startMarkerMaterial.color = startTriangleColor;
        }
        
        if (endMarkerMaterial == null)
        {
            endMarkerMaterial = new Material(Shader.Find("Unlit/Color"));
            endMarkerMaterial.color = endTriangleColor;
        }
        
        // Hide distance text initially
        if (distanceText != null)
        {
            distanceText.gameObject.SetActive(false);
        }
    }
    
    void Update()
    {
        if (Mouse.current == null) return;
        
        // Left click to select start triangle
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            SelectStartTriangle();
        }
        
        // Right click to select end triangle
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            SelectEndTriangle();
        }
        
        // Middle click or space to clear selection
        if (Mouse.current.middleButton.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            ClearSelection();
        }
    }
    
    void OnGUI()
    {
        if (!showGUI) return;
        
        // Update GUI position for screen size changes
        guiRect.x = Screen.width - 250;
        
        GUILayout.BeginArea(guiRect);
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("Distance Calculator Settings", GUI.skin.box);
        
        // Terrain filter
        GUILayout.BeginVertical();
        GUILayout.Label("Terrain:");
        var newTerrainFilter = (TerrainFilter)GUILayout.SelectionGrid((int)terrainFilter, 
            new string[] { "Both", "Land Only", "Water Only" }, 3);
        if (newTerrainFilter != terrainFilter)
        {
            terrainFilter = newTerrainFilter;
            if (distanceCalculator != null)
            {
                distanceCalculator.SetTerrainFilter((DistanceCalculator.TerrainFilter)terrainFilter);
            }
        }
        GUILayout.EndVertical();
        
        // Same country filter
        GUILayout.BeginHorizontal();
        GUILayout.Label("Country:", GUILayout.Width(80));
        var newSameCountryOnly = GUILayout.Toggle(sameCountryOnly, "Same Country Only", GUILayout.Width(150));
        if (newSameCountryOnly != sameCountryOnly)
        {
            sameCountryOnly = newSameCountryOnly;
            if (distanceCalculator != null)
            {
                distanceCalculator.SetCountryFilter(sameCountryOnly);
            }
        }
        GUILayout.EndHorizontal();
        
        // Clear button
        if (GUILayout.Button("Clear Selection"))
        {
            ClearSelection();
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
    
    void SelectStartTriangle()
    {
        int triangleId = GetTriangleUnderMouse();
        if (triangleId != -1)
        {
            selectedStartTriangle = triangleId;
            CreateStartMarker(triangleId);
            Debug.Log($"DistanceTester: Selected start triangle: {triangleId}");
            
            // If we have both triangles selected, calculate distance
            if (selectedEndTriangle != -1)
            {
                CalculateAndDisplayDistance();
            }
        }
    }
    
    void SelectEndTriangle()
    {
        int triangleId = GetTriangleUnderMouse();
        if (triangleId != -1)
        {
            selectedEndTriangle = triangleId;
            CreateEndMarker(triangleId);
            Debug.Log($"DistanceTester: Selected end triangle: {triangleId}");
            
            // If we have both triangles selected, calculate distance
            if (selectedStartTriangle != -1)
            {
                CalculateAndDisplayDistance();
            }
        }
    }
    
    int GetTriangleUnderMouse()
    {
        if (idPicker != null)
        {
            return idPicker.GetSelectedTriangleID();
        }
        return -1;
    }
    
    void CalculateAndDisplayDistance()
    {
        if (distanceCalculator == null) return;
        
        // Sync filters with DistanceCalculator
        distanceCalculator.SetTerrainFilter((DistanceCalculator.TerrainFilter)terrainFilter);
        distanceCalculator.SetCountryFilter(sameCountryOnly);
        
        // Calculate distance
        List<int> path;
        int distance = distanceCalculator.CalculateDistance(selectedStartTriangle, selectedEndTriangle, out path);
        
        if (distance == -1)
        {
            Debug.LogWarning($"DistanceTester: No path found between triangles {selectedStartTriangle} and {selectedEndTriangle}");
            return;
        }
        
        currentPath = path;
        
        // Visualize path
        if (showPath && path.Count > 1)
        {
            CreatePathVisualization(path);
        }
        
        // Update UI text
        if (distanceText != null)
        {
            string pathString = string.Join(" → ", path);
            distanceText.text = $"Distance: {distance} hops\nPath: {pathString}";
            distanceText.color = Color.white;
            distanceText.gameObject.SetActive(true);
            
            // Position text near mouse
            Vector2 mousePos = Mouse.current.position.ReadValue();
            distanceText.transform.position = mousePos + new Vector2(20, 60);
        }
        
                Debug.Log($"DistanceTester: Distance from {selectedStartTriangle} to {selectedEndTriangle}: {distance} hops");
        Debug.Log($"Path: {string.Join(" -> ", path)}");
    }
    
    /// <summary>
    /// Creates visual representation of the path
    /// </summary>
    private void CreatePathVisualization(List<int> path)
    {
        // Clear previous path visualization
        ClearPathVisualization();
        
        Debug.Log($"DistanceTester: Creating path visualization for {path.Count} triangles");
        
        var icoSphere = UnityEngine.Object.FindFirstObjectByType<IcoSphere>();
        if (icoSphere == null || icoSphere.triangleDataList == null) return;
        
        for (int i = 0; i < path.Count - 1; i++)
        {
            int currentTri = path[i];
            int nextTri = path[i + 1];
            
            if (currentTri < icoSphere.triangleDataList.Count && nextTri < icoSphere.triangleDataList.Count)
            {
                var currentTriangle = icoSphere.triangleDataList[currentTri];
                var nextTriangle = icoSphere.triangleDataList[nextTri];
                
                // Get centers of both triangles
                Vector3 currentCenter = currentTriangle.GetCenter();
                Vector3 nextCenter = nextTriangle.GetCenter();
                
                // Create line renderer
                GameObject lineObj = new GameObject($"PathLine_{i}");
                lineObj.transform.SetParent(transform);
                
                LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();
                lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
                lineRenderer.material.color = pathColor;
                lineRenderer.startWidth = pathWidth;
                lineRenderer.endWidth = pathWidth;
                lineRenderer.positionCount = 2;
                lineRenderer.useWorldSpace = true;
                lineRenderer.sortingOrder = 1000;
                
                // Apply height offset for visibility
                Vector3 offsetA = currentCenter.normalized * pathHeight;
                Vector3 offsetB = nextCenter.normalized * pathHeight;
                lineRenderer.SetPosition(0, currentCenter + offsetA);
                lineRenderer.SetPosition(1, nextCenter + offsetB);
                
                pathVisualizers.Add(lineObj);
            }
        }
    }
    
    /// <summary>
    /// Clears the current path visualization
    /// </summary>
    private void ClearPathVisualization()
    {
        foreach (var visualizer in pathVisualizers)
        {
            if (visualizer != null)
            {
                if (Application.isPlaying)
                    Destroy(visualizer);
                else
                    DestroyImmediate(visualizer);
            }
        }
        pathVisualizers.Clear();
    }
    

    
    void CreateStartMarker(int triangleId)
    {
        // Remove previous marker
        if (startMarker != null)
        {
            if (Application.isPlaying)
                Destroy(startMarker);
            else
                DestroyImmediate(startMarker);
        }
        
        // Create new marker
        startMarker = CreateTriangleMarker(triangleId, startTriangleColor, "StartMarker");
        if (startMarker != null)
        {
            var renderer = startMarker.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = startMarkerMaterial;
            }
        }
    }
    
    void CreateEndMarker(int triangleId)
    {
        // Remove previous marker
        if (endMarker != null)
        {
            if (Application.isPlaying)
                Destroy(endMarker);
            else
                DestroyImmediate(endMarker);
        }
        
        // Create new marker
        endMarker = CreateTriangleMarker(triangleId, endTriangleColor, "EndMarker");
        if (endMarker != null)
        {
            var renderer = endMarker.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = endMarkerMaterial;
            }
        }
    }
    
    GameObject CreateTriangleMarker(int triangleId, Color color, string name)
    {
        var icoSphere = UnityEngine.Object.FindFirstObjectByType<IcoSphere>();
        if (icoSphere == null || triangleId < 0 || triangleId >= icoSphere.triangleDataList.Count)
        {
            return null;
        }
        
        var triangle = icoSphere.triangleDataList[triangleId];
        Vector3 center = triangle.GetCenter();
        
        // Create a small sphere at the triangle center
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = name;
        marker.layer = LayerMask.NameToLayer("Default"); // Ensure it's on default layer
        
        // Move marker slightly outward to avoid Z-fighting
        Vector3 offset = center.normalized * 0.02f;
        marker.transform.position = center + offset;
        marker.transform.localScale = Vector3.one * selectionMarkerSize;
        marker.transform.SetParent(transform);
        
        // Set material
        var renderer = marker.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = new Material(Shader.Find("Unlit/Color"));
            renderer.material.color = color;
        }
        
        return marker;
    }
    
    void ClearSelection()
    {
        selectedStartTriangle = -1;
        selectedEndTriangle = -1;
        
        // Remove markers
        if (startMarker != null)
        {
            if (Application.isPlaying)
                Destroy(startMarker);
            else
                DestroyImmediate(startMarker);
            startMarker = null;
        }
        
        if (endMarker != null)
        {
            if (Application.isPlaying)
                Destroy(endMarker);
            else
                DestroyImmediate(endMarker);
            endMarker = null;
        }
        
        // Clear path visualization
        ClearPathVisualization();
        currentPath.Clear();
        
        // Hide distance text
        if (distanceText != null)
        {
            distanceText.gameObject.SetActive(false);
        }
        
        Debug.Log("DistanceTester: Selection cleared");
    }
    
    /// <summary>
    /// Gets the currently selected start triangle ID
    /// </summary>
    public int GetSelectedStartTriangle()
    {
        return selectedStartTriangle;
    }
    
    /// <summary>
    /// Gets the currently selected end triangle ID
    /// </summary>
    public int GetSelectedEndTriangle()
    {
        return selectedEndTriangle;
    }
    
    /// <summary>
    /// Manually set start triangle (useful for testing)
    /// </summary>
    public void SetStartTriangle(int triangleId)
    {
        selectedStartTriangle = triangleId;
        CreateStartMarker(triangleId);
        
        if (selectedEndTriangle != -1)
        {
            CalculateAndDisplayDistance();
        }
    }
    
    /// <summary>
    /// Manually set end triangle (useful for testing)
    /// </summary>
    public void SetEndTriangle(int triangleId)
    {
        selectedEndTriangle = triangleId;
        CreateEndMarker(triangleId);
        
        if (selectedStartTriangle != -1)
        {
            CalculateAndDisplayDistance();
        }
    }
    
    void OnDestroy()
    {
        ClearSelection();
    }
} 