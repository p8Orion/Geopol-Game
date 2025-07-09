using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(IcoSphere))]
public class TriCenterSpheres : MonoBehaviour
{
    [Header("Sphere Settings")]
    public bool showSpheres = false;
    public float sphereSize = 10f;
    public Color sphereColor = Color.gray;
    public Material sphereMaterial;
    
    [Header("Performance")]
    public bool updateInRealTime = false;
    public float updateInterval = 1.0f;
    
    private IcoSphere icoSphere;
    private List<GameObject> spheres = new List<GameObject>();
    private float lastUpdateTime;
    
    void Start()
    {
        icoSphere = GetComponent<IcoSphere>();
        if (icoSphere == null)
        {
            Debug.LogError("TriCenterSpheres: No IcoSphere component found!");
            enabled = false;
            return;
        }
        
        // Subscribe to data loaded event
        icoSphere.OnDataLoaded += OnIcoSphereDataLoaded;
        
        // Initial creation
        UpdateSpheres();
    }
    
    void OnDestroy()
    {
        if (icoSphere != null)
        {
            icoSphere.OnDataLoaded -= OnIcoSphereDataLoaded;
        }
        ClearSpheres();
    }
    
    void Update()
    {
        if (updateInRealTime && showSpheres)
        {
            if (Time.time - lastUpdateTime > updateInterval)
            {
                UpdateSpheres();
                lastUpdateTime = Time.time;
            }
        }
    }
    
    void OnIcoSphereDataLoaded()
    {
        UpdateSpheres();
    }
    
    /// <summary>
    /// Creates or updates sphere GameObjects at the center of each triangle
    /// </summary>
    public void UpdateSpheres()
    {
        if (!showSpheres)
        {
            ClearSpheres();
            return;
        }
        
        if (icoSphere?.triangleDataList == null || icoSphere.triangleDataList.Count == 0)
        {
            Debug.LogWarning("TriCenterSpheres: No triangle data available");
            return;
        }
        
        // Clear existing spheres if count doesn't match
        if (spheres.Count != icoSphere.triangleDataList.Count)
        {
            ClearSpheres();
        }
        
        // Create spheres if needed
        if (spheres.Count == 0)
        {
            CreateSpheres();
        }
        else
        {
            // Update existing sphere positions
            UpdateSpherePositions();
        }
    }
    
    void CreateSpheres()
    {
        Debug.Log($"TriCenterSpheres: Creating {icoSphere.triangleDataList.Count} spheres...");
        
        // Create default material if none is assigned
        if (sphereMaterial == null)
        {
            sphereMaterial = CreateDefaultMaterial();
        }
        
        foreach (var triangle in icoSphere.triangleDataList)
        {
            Vector3 center = (triangle.a + triangle.b + triangle.c) / 3f;
            
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = $"TriCenter_{triangle.id}";
            sphere.transform.position = center;
            sphere.transform.localScale = Vector3.one * sphereSize;
            sphere.transform.SetParent(transform);
            
            // Set material
            var renderer = sphere.GetComponent<Renderer>();
            renderer.material = sphereMaterial;
            
            spheres.Add(sphere);
        }
        
        Debug.Log($"TriCenterSpheres: Created {spheres.Count} spheres");
    }
    
    Material CreateDefaultMaterial()
    {
        // Use a simple unlit shader for predictable colors
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null)
        {
            // Fallback to Standard if Unlit/Color is not available
            shader = Shader.Find("Standard");
        }
        
        Material mat = new Material(shader);
        mat.color = sphereColor;
        mat.name = "TriCenterSpheres_DefaultMaterial";
        
        // Only add emission if using Standard shader and we want some glow
        if (shader.name.Contains("Standard"))
        {
            mat.SetFloat("_Metallic", 0.0f);
            mat.SetFloat("_Smoothness", 0.3f);
            // No emission to avoid color distortion
        }
        
        return mat;
    }
    
    void UpdateSpherePositions()
    {
        for (int i = 0; i < spheres.Count && i < icoSphere.triangleDataList.Count; i++)
        {
            var triangle = icoSphere.triangleDataList[i];
            Vector3 center = (triangle.a + triangle.b + triangle.c) / 3f;
            spheres[i].transform.position = center;
        }
    }
    
    void ClearSpheres()
    {
        foreach (var sphere in spheres)
        {
            if (sphere != null)
            {
                if (Application.isPlaying)
                    Destroy(sphere);
                else
                    DestroyImmediate(sphere);
            }
        }
        spheres.Clear();
    }
    
    /// <summary>
    /// Toggles sphere visibility
    /// </summary>
    [ContextMenu("Toggle Spheres")]
    public void ToggleSpheres()
    {
        showSpheres = !showSpheres;
        UpdateSpheres();
    }
    
    /// <summary>
    /// Shows all spheres
    /// </summary>
    [ContextMenu("Show Spheres")]
    public void ShowSpheres()
    {
        showSpheres = true;
        UpdateSpheres();
    }
    
    /// <summary>
    /// Hides all spheres
    /// </summary>
    [ContextMenu("Hide Spheres")]
    public void HideSpheres()
    {
        showSpheres = false;
        UpdateSpheres();
    }
} 