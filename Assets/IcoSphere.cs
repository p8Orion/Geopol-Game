using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor;
using System.Linq;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class IcoSphere : MonoBehaviour
{
    [Header("Ico Sphere Settings")]
    public float triEdgeLengthKm = 150f;
    public float sphereCircumferenceKm = 40075f;
    public List<TerrainType> terrainTypes = new();

    [Header("Splat Map Settings")]
    public int splatMapResolution = 1024;
    public float borderNoiseStrength = 0.4f; // How much to break up triangle borders. Higher values mean more mixing.
    public float borderNoiseScale = 0.25f; // The scale of the border noise. Smaller values create larger patches.
    public int borderDepth = 5; // How many pixels deep the border effect should be.
    public float tilingScale = 30.0f;
    public int terrainCount = 12; // Number of terrain types to use (max 12)
    public bool enableBlur = true; // Whether to apply blur to splat maps
    public int blurRadius = 1; // Radius of the blur (1 = 3x3, 2 = 5x5, etc.)
    [Header("Ocean Border Settings")]
    public List<int> oceanTerrainIDs = new List<int> { 11, 10 }; // IDs of ocean terrain types (default: 10 for TerrainTypeEnum.Ocean)
    public bool excludeOceanFromBorderNoise = false; // Whether to exclude ocean borders from noise generation
    public bool excludeOceanFromBorderBlur = true; // Whether to exclude ocean borders from blur generation
    [Header("Coastal Variation Settings")]
    public bool enableCoastalVariation = true; // Whether to vary coastal noise patterns
    public float coastalVariationScale = 0.05f; // Scale of coastal variation (smaller = larger regions)
    public float smoothCoastThreshold = 0.6f; // Threshold for smooth vs rough coasts (0-1)

    [Header("Debug Settings")]
    public bool showGizmos = true; 
    public float gizmoScale = 0.1f;
    public Color gizmoColor = Color.yellow;
    public int mainLabelSize = 14;
    public int neighborLabelSize = 12;
    public float neighborLabelOffset = 0.33f; // How far along the line to place the label (0-1)

    public Camera idCamera;
    public LayerMask idLayer;
    public Material idMaterial;

    private Mesh mesh;
    public float radius; // Made public for TriangleDataSaver
    public int subdivisions; // Made public for TriangleDataSaver

    public List<TriangleData> triangleDataList = new();
    private List<int> triangleToDataIndex = new();
    
    // Event to notify other components that data is ready
    public event System.Action OnDataLoaded;
    
    // Dictionary to track edges and their associated triangles
    private Dictionary<Edge, List<int>> edgeToTriangles = new();

    // Public property to access edgeToTriangles
    public Dictionary<Edge, List<int>> EdgeToTriangles => edgeToTriangles;

    private GameObject idSphere;  // Reference to the ID sphere GameObject

    private KoppenTerrainMapper koppenMapper;
    
    // New border system
    private BorderManager borderManager;
    
    // Ocean wave effect system
    private OceanWaveEffect oceanWaveEffect;

    Texture2D[] splatMaps = new Texture2D[3];  // 3 splat maps for 12 terrain types

    // --- Secondary Materials System ---
    private List<Material> secondaryMaterials = new List<Material>();

    /// <summary>
    /// Registers a secondary material to be rendered after the main terrain material.
    /// </summary>
    public void RegisterSecondaryMaterial(Material mat)
    {
        if (mat != null && !secondaryMaterials.Contains(mat))
            secondaryMaterials.Add(mat);
        ApplyMaterials();
    }

    /// <summary>
    /// Unregisters a secondary material.
    /// </summary>
    public void UnregisterSecondaryMaterial(Material mat)
    {
        if (mat != null && secondaryMaterials.Contains(mat))
            secondaryMaterials.Remove(mat);
        ApplyMaterials();
    }

    /// <summary>
    /// Applies the main terrain material and all registered secondary materials to the MeshRenderer.
    /// </summary>
    public void ApplyMaterials()
    {
        var renderer = GetComponent<MeshRenderer>();
        if (renderer == null) return;
        var mats = new List<Material>();
        if (mainTerrainMaterial != null)
            mats.Add(mainTerrainMaterial);
        mats.AddRange(secondaryMaterials);
        renderer.materials = mats.ToArray();
    }

    // Guarda el material principal para el sistema de refresh
    private Material mainTerrainMaterial;

    // Helper method to check if a terrain type is ocean for noise exclusion
    private bool IsOceanTerrainForNoise(int terrainType)
    {
        return excludeOceanFromBorderNoise && oceanTerrainIDs.Contains(terrainType);
    }
    
    // Helper method to check if a terrain type is ocean for blur exclusion
    private bool IsOceanTerrainForBlur(int terrainType)
    {
        return excludeOceanFromBorderBlur && oceanTerrainIDs.Contains(terrainType);
    }

    // Backward compatibility property for existing code
    public List<Material> terrainMaterials
    {
        get
        {
            var materials = new List<Material>();
            foreach (var terrainType in terrainTypes)
            {
                materials.Add(terrainType.material);
            }
            return materials;
        }
        set
        {
            // Convert materials to terrain types
            terrainTypes.Clear();
            for (int i = 0; i < value.Count; i++)
            {
                var terrainType = new TerrainType($"Terrain {i}", value[i], GetDefaultTerrainColor(i));
                terrainType.id = i;
                terrainTypes.Add(terrainType);
            }
        }
    }

    // Helper class to represent edges
    public class Edge
    {
        public Vector3 a, b;
        
        public Edge(Vector3 a, Vector3 b)
        {
            // Ensure consistent ordering of vertices
            if (a.x < b.x || (a.x == b.x && a.y < b.y) || (a.x == b.x && a.y == b.y && a.z < b.z))
            {
                this.a = a;
                this.b = b;
            }
            else
            {
                this.a = b;
                this.b = a;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is Edge other)
            {
                return Vector3.Distance(a, other.a) < 0.0001f && Vector3.Distance(b, other.b) < 0.0001f;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return a.GetHashCode() ^ b.GetHashCode();
        }
    }

    void OnDrawGizmos()
    {
        if (!showGizmos || triangleDataList == null) return;

        // Get the scene view camera
        Camera sceneCamera = UnityEditor.SceneView.lastActiveSceneView?.camera;
        if (sceneCamera == null) return;

        foreach (var triangle in triangleDataList)
        {
            // Calculate triangle center and normal
            Vector3 center = (triangle.a + triangle.b + triangle.c) / 3f;
            Vector3 normal = Vector3.Cross(triangle.b - triangle.a, triangle.c - triangle.a).normalized;
            
            // Check if triangle is facing the camera
            Vector3 viewDir = (sceneCamera.transform.position - center).normalized;
            if (Vector3.Dot(normal, viewDir) <= 0) continue; // Skip back-facing triangles
            
            // Draw ID and RGB values in one label
            GUIStyle labelStyle = new GUIStyle();
            labelStyle.fontSize = mainLabelSize;
            labelStyle.normal.textColor = Color.white;
            int rawR = Mathf.RoundToInt(triangle.colorR * 255f);
            int rawG = Mathf.RoundToInt(triangle.colorG * 255f);
            int rawB = Mathf.RoundToInt(triangle.colorB * 255f);
            UnityEditor.Handles.Label(center, $"{triangle.id}\n {rawR},{rawG},{rawB}", labelStyle);

            // Draw connections to adjacent triangles
            Gizmos.color = Color.blue;
            foreach (var adjacentId in triangle.adjacentTriangles)
            {
                if (adjacentId < triangleDataList.Count)
                {
                    var adjacent = triangleDataList[adjacentId];
                    Vector3 adjacentCenter = (adjacent.a + adjacent.b + adjacent.c) / 3f;
                    Vector3 adjacentNormal = Vector3.Cross(adjacent.b - adjacent.a, adjacent.c - adjacent.a).normalized;
                    
                    // Only draw connection if adjacent triangle is also front-facing
                    if (Vector3.Dot(adjacentNormal, viewDir) > 0)
                    {
                        Gizmos.DrawLine(center, adjacentCenter);
                        
                        // Draw neighbor ID along the connection line
                        Vector3 labelPos = Vector3.Lerp(center, adjacentCenter, neighborLabelOffset);
                        GUIStyle neighborStyle = new GUIStyle();
                        neighborStyle.fontSize = neighborLabelSize;
                        neighborStyle.normal.textColor = Color.white;
                        UnityEditor.Handles.Label(labelPos, adjacentId.ToString(), neighborStyle);
                    }
                }
            }
        }
    }

    void Start()
    {
        if (!TryGetComponent<MeshCollider>(out var collider))
            gameObject.AddComponent<MeshCollider>();

        // Auto-assign KoppenTerrainMapper if not set
        koppenMapper = KoppenTerrainMapper.Instance;

        // Initialize terrain types with IDs
        InitializeTerrainTypes();

        // Initialize ID material if not set
        if (idMaterial == null)
        {
            Shader idShader = Shader.Find("Custom/TriangleID");
            if (idShader != null)
            {
                idMaterial = new Material(idShader);
            }
            else
            {
                Debug.LogError("Shader 'Shaders/TriangleID' not found. Please create it manually.");
                return;
            }
        }

        // Initialize new border system
        InitializeBorderManager();
        
        // Initialize ocean wave effect system
        InitializeOceanWaveEffect();

        // Check if save data exists and load it instead of generating new data
        if (TryLoadExistingSaveData())
        {
            Debug.Log("IcoSphere: Loaded existing save data. Triggering border generation.");
            borderManager?.GenerateAllBorders();
        }
        else
        {
            Debug.Log("IcoSphere: No save data found, generating new data from Koppen.");
            Generate();
        }
    }

    /// <summary>
    /// Initializes terrain types with proper IDs and default names if needed
    /// </summary>
    private void InitializeTerrainTypes()
    {
        // If terrainTypes is empty, initialize it with default terrain types in enum order
        if (terrainTypes.Count == 0)
        {
            InitializeDefaultTerrainTypes();
        }
        
        for (int i = 0; i < terrainTypes.Count; i++)
        {
            var terrainType = terrainTypes[i];
            terrainType.id = i;
            
            // Set default name if not set
            if (string.IsNullOrEmpty(terrainType.name) || terrainType.name == "New Terrain")
            {
                terrainType.name = $"Terrain {i}";
            }
            
            // Set default color if material is null
            if (terrainType.material == null)
            {
                terrainType.previewColor = GetDefaultTerrainColor(i);
            }
        }
        
        Debug.Log($"Initialized {terrainTypes.Count} terrain types");
    }
    
    /// <summary>
    /// Initializes the terrainTypes list with default terrain types in the correct enum order
    /// </summary>
    private void InitializeDefaultTerrainTypes()
    {
        terrainTypes.Clear();
        
        // Create terrain types in the same order as TerrainTypeEnum
        terrainTypes.Add(new TerrainType("Unknown", null, Color.gray, TerrainTypeEnum.Unknown));
        terrainTypes.Add(new TerrainType("Bosque Tropical", null, Color.green, TerrainTypeEnum.BosqueTropical));
        terrainTypes.Add(new TerrainType("Sabana", null, Color.yellow, TerrainTypeEnum.Sabana));
        terrainTypes.Add(new TerrainType("Desierto", null, Color.brown, TerrainTypeEnum.Desierto));
        terrainTypes.Add(new TerrainType("Estepa", null, Color.orange, TerrainTypeEnum.Estepa));
        terrainTypes.Add(new TerrainType("Bosque Templado", null, Color.cyan, TerrainTypeEnum.BosqueTemplado));
        terrainTypes.Add(new TerrainType("Llanura", null, Color.magenta, TerrainTypeEnum.Llanura));
        terrainTypes.Add(new TerrainType("Bosque Boreal", null, Color.blue, TerrainTypeEnum.BosqueBoreal));
        terrainTypes.Add(new TerrainType("Tundra", null, Color.white, TerrainTypeEnum.Tundra));
        terrainTypes.Add(new TerrainType("Hielo", null, Color.white, TerrainTypeEnum.Hielo));
        terrainTypes.Add(new TerrainType("Ocean", null, Color.blue, TerrainTypeEnum.Ocean));
        
        Debug.Log("Initialized default terrain types in enum order");
    }
    
    /// <summary>
    /// Returns a default color for terrain types based on index
    /// </summary>
    private Color GetDefaultTerrainColor(int index)
    {
        Color[] defaultColors = {
            Color.green,      // Grass
            Color.brown,      // Desert
            Color.blue,       // Ocean
            Color.white,      // Ice
            Color.gray,       // Mountain
            Color.yellow,     // Steppe
            Color.cyan,       // Tundra
            Color.magenta,    // Tropical Forest
            Color.red,        // Savanna
            Color.orange,     // Forest
            Color.purple,     // Unknown
            Color.black       // Extra
        };
        
        return defaultColors[index % defaultColors.Length];
    }

    /// <summary>
    /// Initializes the new border manager system
    /// </summary>
    private void InitializeBorderManager()
    {
        // Add BorderManager if it doesn't exist
        borderManager = GetComponent<BorderManager>();
        if (borderManager == null)
        {
            borderManager = gameObject.AddComponent<BorderManager>();
            Debug.Log("IcoSphere: Added BorderManager component.");
        }
    }
    
    /// <summary>
    /// Initializes the ocean wave effect system
    /// </summary>
    private void InitializeOceanWaveEffect()
    {
        // Add OceanWaveEffect if it doesn't exist
        oceanWaveEffect = GetComponent<OceanWaveEffect>();
        if (oceanWaveEffect == null)
        {
            oceanWaveEffect = gameObject.AddComponent<OceanWaveEffect>();
            Debug.Log("IcoSphere: Added OceanWaveEffect component.");
        }
    }

    /// <summary>
    /// Attempts to load existing save data. Returns true if successful, false if no save data exists.
    /// </summary>
    private bool TryLoadExistingSaveData()
    {
        // Find TriangleDataSaver component
        var triangleDataSaver = UnityEngine.Object.FindFirstObjectByType<TriangleDataSaver>();
        if (triangleDataSaver == null)
        {
            Debug.LogWarning("IcoSphere: No TriangleDataSaver found in scene. Cannot load save data.");
            return false;
        }
        
        // Check if save data exists
        if (!triangleDataSaver.HasSavedData())
        {
            return false;
        }
        
        try
        {
            // Use the existing LoadTriangleData method
            triangleDataSaver.LoadTriangleData();
            
            // Apply the splat material (same as MapEditor does)
            CreateAndApplyNewSplatMaterial();
            
            // Create ID clone if needed
            if (idCamera != null)
            {
                CreateIDClone();
            }
            
            Debug.Log("IcoSphere: Successfully loaded save data and configured material.");
            
            // Notify listeners that data is ready
            OnDataLoaded?.Invoke();
            
            return true;
        }
        catch (System.Exception e)
        { 
            Debug.LogError($"IcoSphere: Failed to load save data: {e.Message}");
            
            // Check if this is a corruption-related error
            if (e.Message.Contains("End of Stream") || 
                e.Message.Contains("Serialization") || 
                e.Message.Contains("corrupted"))
            {
                Debug.LogWarning("IcoSphere: Save file appears to be corrupted. Attempting to regenerate map...");
                
                // Try to delete the corrupted save and regenerate
                try
                {
                    triangleDataSaver.DeleteCorruptedSaveAndRegenerate();
                    Debug.Log("IcoSphere: Successfully regenerated map after corrupted save file.");
                    return false; // Return false so the normal generation flow continues
                }
                catch (System.Exception regenError)
                {
                    Debug.LogError($"IcoSphere: Failed to regenerate map: {regenError.Message}");
                    // Fall through to normal generation
                }
            }
            
            return false;
        }
    }

    public void CreateAndApplyNewSplatMaterial()
    {
        var terrainShader = Shader.Find("Custom/TerrainSplatMap12");
        if (terrainShader == null)
        {
            Debug.LogError("Custom/TerrainSplatMap12 shader not found! Please create it in Assets/Shaders/TerrainSplatMap12.shader");
            return;
        }
        var newMaterial = new Material(terrainShader);

        // Generate splat maps from the current triangleDataList
        GenerateSplatMap(newMaterial);

        // Assign all terrain textures and parameters
        SetupTerrainMaterial(newMaterial);

        // Set as main material and refresh all
        mainTerrainMaterial = newMaterial;
        ApplyMaterials();
        Debug.Log("IcoSphere: Created and applied a new splat material (with secondary materials if any).");

        if (oceanWaveEffect != null)
        {
            oceanWaveEffect.ApplyWaveMaskToMaterial(mainTerrainMaterial);
        }
    }

    public void Generate()
    {
        radius = sphereCircumferenceKm / (2 * Mathf.PI);
        subdivisions = EstimateSubdivisions(radius, triEdgeLengthKm);
        Debug.Log($"IcoSphere: Generating with {subdivisions} subdivisions, radius: {radius:F2}km, target edge length: {triEdgeLengthKm:F2}km");

        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        GetComponent<MeshFilter>().mesh = mesh;

        MeshData data = GenerateIcoSphere(subdivisions);
        // --- Unique vertices per triangle (like ID mesh) ---
        int triangleCount = triangleDataList.Count;
        Vector3[] newVerts = new Vector3[triangleCount * 3];
        Vector2[] newUVs = new Vector2[triangleCount * 3];
        Vector3[] newNormals = new Vector3[triangleCount * 3]; // Smoothed normals
        Color[] newColors = new Color[triangleCount * 3];
        int[] newTris = new int[triangleCount * 3];
        for (int i = 0; i < triangleCount; i++)
        {
            var tri = triangleDataList[i];
            int baseIdx = i * 3;
            
            // Vertices
            newVerts[baseIdx + 0] = tri.a;
            newVerts[baseIdx + 1] = tri.b;
            newVerts[baseIdx + 2] = tri.c;

            // Smoothed Normals (the key to making the sphere look round)
            newNormals[baseIdx + 0] = tri.a.normalized;
            newNormals[baseIdx + 1] = tri.b.normalized;
            newNormals[baseIdx + 2] = tri.c.normalized;

            // Spherical UVs based on actual 3D positions
            newUVs[baseIdx + 0] = Vector3ToUV(tri.a);
            newUVs[baseIdx + 1] = Vector3ToUV(tri.b);
            newUVs[baseIdx + 2] = Vector3ToUV(tri.c);
            
            // Triangle ID as color
            Color color = new Color(
                ((i & 0xFF) / 255.0f),
                (((i >> 8) & 0xFF) / 255.0f),
                (((i >> 16) & 0xFF) / 255.0f),
                1.0f
            );
            newColors[baseIdx + 0] = color;
            newColors[baseIdx + 1] = color;
            newColors[baseIdx + 2] = color;
            newTris[baseIdx + 0] = baseIdx + 0;
            newTris[baseIdx + 1] = baseIdx + 1;
            newTris[baseIdx + 2] = baseIdx + 2;
        }
        mesh.vertices = newVerts;
        mesh.uv = newUVs;
        mesh.normals = newNormals; // Use the new smoothed normals
        mesh.colors = newColors;
        mesh.triangles = newTris;
        // mesh.RecalculateNormals(); // <-- DO NOT USE: This creates the "boxy" flat-shaded look.

        // Update mesh collider
        if (TryGetComponent<MeshCollider>(out var collider))
        {
            collider.sharedMesh = mesh;
        }

        // Restore adjacency info
        CalculateAdjacency(data);

        if (idCamera != null)
        {
            CreateIDClone();
        }

        // --- SPLAT MAP WORKFLOW ---
        var terrainShader = Shader.Find("Custom/TerrainSplatMap12");
        if (terrainShader == null)
        {
            Debug.LogError("Custom/TerrainSplatMap12 shader not found! Please create it in Assets/Shaders/TerrainSplatMap12.shader");
            return;
        }
        Debug.Log($"Found shader: {terrainShader.name}");
        var terrainMaterial = new Material(terrainShader);

        // Generate splat maps from triangle data
        GenerateSplatMap(terrainMaterial);

        // Assign terrain textures to material (up to 12)
        SetupTerrainMaterial(terrainMaterial);

        GetComponent<MeshRenderer>().material = terrainMaterial;
        Debug.Log($"Material assigned to renderer: {terrainMaterial.name}");
        
        // Data is now ready, trigger border generation for new maps.
        Debug.Log("IcoSphere: New map generated. Triggering border generation.");
        borderManager?.GenerateAllBorders();
    }

    public void SetupTerrainMaterial(Material terrainMaterial)
    {
        // Assign terrain textures to material (up to 12)
        Debug.Log($"Assigning {Mathf.Min(terrainTypes.Count, 12)} terrain textures to material");
        for (int i = 0; i < Mathf.Min(terrainTypes.Count, 12); i++)
        {
            var terrainType = terrainTypes[i];
            Texture2D tex2D = terrainType.GetTexture();

            if (tex2D != null)
            {
                terrainMaterial.SetTexture($"_TerrainTex{i + 1}", tex2D);
                Debug.Log($"Assigned texture {i + 1}: {tex2D.name} (Size: {tex2D.width}x{tex2D.height})");
            }
            else
            {
                // Create a solid color texture if no texture is available
                Color color = terrainType.GetBaseColor();

                Texture2D colorTex = new Texture2D(1024, 1024, TextureFormat.RGBA32, false);
                Color[] pixels = new Color[1024 * 1024];
                for (int p = 0; p < pixels.Length; p++) pixels[p] = color;
                colorTex.SetPixels(pixels);
                colorTex.Apply();
                terrainMaterial.SetTexture($"_TerrainTex{i + 1}", colorTex);
                Debug.Log($"Created color texture {i + 1}: {color} (no texture found in material)");
            }
        }

        // Set splat map parameters
        terrainMaterial.SetFloat("_TilingScale", tilingScale);
        terrainMaterial.SetFloat("_TerrainCount", terrainCount);

        Debug.Log($"Setting material parameters - TerrainCount: {terrainCount}");
    }

    void CreateIDClone()
    {
        if (idSphere != null) return;  // Don't create if it already exists

        GameObject clone = new GameObject("IDSphere");
        clone.transform.SetPositionAndRotation(transform.position, transform.rotation);
        clone.layer = Mathf.RoundToInt(Mathf.Log(idLayer.value, 2));
        idSphere = clone;

        var meshFilter = clone.AddComponent<MeshFilter>();
        var meshRenderer = clone.AddComponent<MeshRenderer>();

        Mesh idMesh = new Mesh();
        idMesh.name = "ID_Mesh";
        idMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        // Build the ID mesh directly from triangleDataList
        int triCount = triangleDataList.Count;
        var newVerts = new Vector3[triCount * 3];
        var newTris = new int[triCount * 3];
        var colors = new Color[triCount * 3];

        for (int i = 0; i < triCount; i++)
        {
            var tri = triangleDataList[i];
            int baseIdx = i * 3;
            newVerts[baseIdx + 0] = tri.a;
            newVerts[baseIdx + 1] = tri.b;
            newVerts[baseIdx + 2] = tri.c;
            newTris[baseIdx + 0] = baseIdx + 0;
            newTris[baseIdx + 1] = baseIdx + 1;
            newTris[baseIdx + 2] = baseIdx + 2;
            Color color = new Color(
                ((i & 0xFF) / 255.0f),
                (((i >> 8) & 0xFF) / 255.0f),
                (((i >> 16) & 0xFF) / 255.0f),
                1.0f
            );
            colors[baseIdx + 0] = color;
            colors[baseIdx + 1] = color;
            colors[baseIdx + 2] = color;
        }

        idMesh.vertices = newVerts;
        idMesh.triangles = newTris;
        idMesh.colors = colors;
        idMesh.RecalculateNormals();

        meshFilter.mesh = idMesh;
        meshRenderer.material = idMaterial;
    }

    int EstimateSubdivisions(float radius, float triEdgeLength)
    {
        float triArea = (3f * Mathf.Sqrt(3f) / 2f) * triEdgeLength * triEdgeLength;
        float sphereArea = 4f * Mathf.PI * radius * radius;
        int approxTriangles = Mathf.CeilToInt(sphereArea / triArea);
        return Mathf.Clamp((int)Mathf.Log(approxTriangles / 20f, 4), 0, 6);
    }

    MeshData GenerateIcoSphere(int level)
    {
        MeshData meshData = new();
        triangleDataList.Clear();
        triangleToDataIndex.Clear();
        edgeToTriangles.Clear();

        float t = (1f + Mathf.Sqrt(5f)) / 2f;

        // 1. Global unique vertex list
        List<Vector3> verts = new()
        {
            new Vector3(-1,  t,  0), new Vector3( 1,  t,  0), new Vector3(-1, -t,  0), new Vector3( 1, -t,  0),
            new Vector3( 0, -1,  t), new Vector3( 0,  1,  t), new Vector3( 0, -1, -t), new Vector3( 0,  1, -t),
            new Vector3( t,  0, -1), new Vector3( t,  0,  1), new Vector3(-t,  0, -1), new Vector3(-t,  0,  1)
        };
        for (int i = 0; i < verts.Count; i++)
            verts[i] = verts[i].normalized * radius;

        int[] faces = {
            0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11,
            1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
            3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9,
            4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1
        };

        // Dictionary to cache midpoints and avoid duplicate vertices
        Dictionary<long, int> midpointCache = new();

        for (int i = 0; i < faces.Length; i += 3)
        {
            SubdivideTriangleUnique(faces[i], faces[i + 1], faces[i + 2], level, verts, midpointCache, meshData);
        }

        meshData.uniqueVertices = verts;
        return meshData;
    }

    // Subdivide using unique vertex list
    void SubdivideTriangleUnique(int i1, int i2, int i3, int depth, List<Vector3> verts, Dictionary<long, int> midpointCache, MeshData data)
    {
        if (depth == 0)
        {
            AddTriangleUnique(i1, i2, i3, verts, data);
            return;
        }
        int a = GetOrCreateMidpoint(i1, i2, verts, midpointCache);
        int b = GetOrCreateMidpoint(i2, i3, verts, midpointCache);
        int c = GetOrCreateMidpoint(i3, i1, verts, midpointCache);
        SubdivideTriangleUnique(i1, a, c, depth - 1, verts, midpointCache, data);
        SubdivideTriangleUnique(a, i2, b, depth - 1, verts, midpointCache, data);
        SubdivideTriangleUnique(c, b, i3, depth - 1, verts, midpointCache, data);
        SubdivideTriangleUnique(a, b, c, depth - 1, verts, midpointCache, data);
    }

    // Get or create midpoint vertex, return its index
    int GetOrCreateMidpoint(int i1, int i2, List<Vector3> verts, Dictionary<long, int> cache)
    {
        long key = ((long)Mathf.Min(i1, i2) << 32) | (uint)Mathf.Max(i1, i2);
        if (cache.TryGetValue(key, out int idx)) return idx;
        Vector3 midpoint = ((verts[i1] + verts[i2]) * 0.5f).normalized * radius;
        verts.Add(midpoint);
        int newIdx = verts.Count - 1;
        cache[key] = newIdx;
        return newIdx;
    }

    // Add triangle by indices, keep terrain/submesh and triangleDataList logic
    void AddTriangleUnique(int i1, int i2, int i3, List<Vector3> verts, MeshData data)
    {
        int terrainType = 0;
        if (koppenMapper != null)
        {
            Vector3 center = (verts[i1] + verts[i2] + verts[i3]) / 3f;
            center.Normalize();
            var (lat, lon) = TriangleData.Vector3ToLatLon(center);
            var terrain = koppenMapper.GetTerrainFromLatLon(lat, lon);
            terrainType = (int)terrain;
            if (terrainType < 0 || terrainType >= terrainTypes.Count)
                terrainType = 0;
        }
        else
        {
            terrainType = 0;
        }
        if (!data.submeshTriangles.ContainsKey(terrainType))
            data.submeshTriangles[terrainType] = new List<int>();
        data.submeshTriangles[terrainType].Add(i1);
        data.submeshTriangles[terrainType].Add(i2);
        data.submeshTriangles[terrainType].Add(i3);
        int triangleIndex = triangleDataList.Count;
        triangleToDataIndex.Add(triangleIndex);
        var triangle = new TriangleData
        {
            a = verts[i1],
            b = verts[i2],
            c = verts[i3],
            terrainType = terrainType,
            id = triangleIndex,
            colorR = (triangleIndex & 0xFF) / 255.0f,
            colorG = ((triangleIndex >> 8) & 0xFF) / 255.0f,
            colorB = ((triangleIndex >> 16) & 0xFF) / 255.0f
        };
        triangleDataList.Add(triangle);
    }

    // Efficiently fill adjacentTriangles and vertexAdjacentTriangles for each TriangleData
    void CalculateAdjacency(MeshData data)
    {
        // Map from edge (min,max) to triangle indices
        var edgeToTriangles = new Dictionary<(int,int), List<int>>();
        // Map from vertex index to triangle indices
        var vertexToTriangles = new Dictionary<int, List<int>>();
        
        // Build a list of all triangles with their indices
        var triangleIndices = new List<(int a, int b, int c)>();
        foreach (var tri in triangleDataList)
        {
            int i1 = data.uniqueVertices.IndexOf(tri.a);
            int i2 = data.uniqueVertices.IndexOf(tri.b);
            int i3 = data.uniqueVertices.IndexOf(tri.c);
            triangleIndices.Add((i1, i2, i3));
        }
        
        // Fill edgeToTriangles and vertexToTriangles
        for (int t = 0; t < triangleIndices.Count; t++)
        {
            var (a, b, c) = triangleIndices[t];
            
            // Add edges
            foreach (var edge in new[]{ (Mathf.Min(a,b), Mathf.Max(a,b)), (Mathf.Min(b,c), Mathf.Max(b,c)), (Mathf.Min(c,a), Mathf.Max(c,a)) })
            {
                if (!edgeToTriangles.ContainsKey(edge)) edgeToTriangles[edge] = new List<int>();
                edgeToTriangles[edge].Add(t);
            }
            
            // Add vertices
            foreach (var vertex in new[]{ a, b, c })
            {
                if (!vertexToTriangles.ContainsKey(vertex)) vertexToTriangles[vertex] = new List<int>();
                vertexToTriangles[vertex].Add(t);
            }
        }
        
        // Assign both edge and vertex adjacents
        for (int t = 0; t < triangleIndices.Count; t++)
        {
            var (a, b, c) = triangleIndices[t];
            var triData = triangleDataList[t];
            
            // Clear existing adjacency data
            triData.adjacentTriangles.Clear();
            triData.vertexAdjacentTriangles.Clear();
            
            // Calculate edge adjacency (triangles sharing an edge)
            foreach (var edge in new[]{ (Mathf.Min(a,b), Mathf.Max(a,b)), (Mathf.Min(b,c), Mathf.Max(b,c)), (Mathf.Min(c,a), Mathf.Max(c,a)) })
            {
                foreach (var neighbor in edgeToTriangles[edge])
                {
                    if (neighbor != t)
                        triData.adjacentTriangles.Add(neighbor);
                }
            }
            
            // Calculate vertex adjacency (triangles sharing any vertex)
            foreach (var vertex in new[]{ a, b, c })
            {
                foreach (var neighbor in vertexToTriangles[vertex])
                {
                    if (neighbor != t)
                        triData.vertexAdjacentTriangles.Add(neighbor);
                }
            }
        }
    }

    class MeshData
    {
        public List<Vector3> uniqueVertices = new();
        public Dictionary<int, List<int>> submeshTriangles = new();
    }

    void OnDestroy()
    {
        // Destroy the ID sphere if it exists
        if (idSphere != null)
        {
            if (Application.isPlaying)
            {
                Destroy(idSphere);
            }
            else
            {
                DestroyImmediate(idSphere);
            }
        }
    }

    public void GenerateSplatMap(Material terrainMaterial)
    {
        int triangleCount = triangleDataList.Count;
        
        // Create 3 splat map textures for 12 terrain types
        for (int mapIndex = 0; mapIndex < 3; mapIndex++)
        {
            splatMaps[mapIndex] = new Texture2D(splatMapResolution, splatMapResolution, TextureFormat.RGBA32, false, true);
            // Use Bilinear filtering to re-enable the GPU's automatic smoothing.
            splatMaps[mapIndex].filterMode = FilterMode.Bilinear;
            splatMaps[mapIndex].wrapMode = TextureWrapMode.Clamp;
        }
        
        // Initialize all splat maps with zeros
        Color[] pixels1 = new Color[splatMapResolution * splatMapResolution];
        Color[] pixels2 = new Color[splatMapResolution * splatMapResolution];
        Color[] pixels3 = new Color[splatMapResolution * splatMapResolution];
        
        for (int i = 0; i < pixels1.Length; i++)
        {
            pixels1[i] = new Color(0, 0, 0, 0); // Explicitly set alpha to 0
            pixels2[i] = new Color(0, 0, 0, 0);
            pixels3[i] = new Color(0, 0, 0, 0);
        }
        
        // Generate splat maps from triangle data
        Debug.Log($"Generating 3 splat maps {splatMapResolution}x{splatMapResolution} from {triangleCount} triangles");
        
        // Count terrain types for debugging
        Dictionary<int, int> terrainTypeCount = new Dictionary<int, int>();
        
        // First pass: count terrain types and find unique ones
        HashSet<int> uniqueTerrainTypes = new HashSet<int>();
        for (int triIndex = 0; triIndex < triangleCount; triIndex++)
        {
            var tri = triangleDataList[triIndex];
            uniqueTerrainTypes.Add(tri.terrainType);
            
            if (!terrainTypeCount.ContainsKey(tri.terrainType))
                terrainTypeCount[tri.terrainType] = 0;
            terrainTypeCount[tri.terrainType]++;
        }
        
        Dictionary<int, (int mapIndex, int channel)> terrainToSplat = new Dictionary<int, (int, int)>();
        
        // Create mapping for ALL possible terrain types (0-10), not just the ones found in data
        // This ensures the mapping stays consistent even if some terrain types don't appear
        for (int terrainType = 0; terrainType < terrainTypes.Count; terrainType++)
        {
            if (terrainType >= 12) break; // Max 12 terrain types supported
            
            int mapIndex = terrainType / 4;  // Which splat map (0-2)
            int channel = terrainType % 4;   // Which channel in that map (0-3 for RGBA)
            terrainToSplat[terrainType] = (mapIndex, channel);
        }
        
        Debug.Log($"Mapping {terrainToSplat.Count} terrain types (including unused ones) to maintain correct indices.");
        
        // --- Triangle Rasterization Step ---
        // A robust method to prevent blend issues at the rasterization stage.
        // 1. Create a map of which terrain "owns" each pixel.
        int[] terrainOwner = new int[splatMapResolution * splatMapResolution];
        for (int i = 0; i < terrainOwner.Length; i++)
        {
            terrainOwner[i] = -1; // -1 means no owner
        }
        
        for (int triIndex = 0; triIndex < triangleCount; triIndex++)
        {
            var tri = triangleDataList[triIndex];
            Vector2 uvA = Vector3ToUV(tri.a);
            Vector2 uvB = Vector3ToUV(tri.b);
            Vector2 uvC = Vector3ToUV(tri.c);
            
            DrawTriangleWithOwner(terrainOwner, uvA, uvB, uvC, tri.terrainType, splatMapResolution);
        }
        
        // --- NEW: Post-process to create "patchy" borders ---
        if (borderNoiseStrength > 0)
        {
            int[] patchedOwner = new int[terrainOwner.Length];
            System.Array.Copy(terrainOwner, patchedOwner, terrainOwner.Length);

            for (int y = 0; y < splatMapResolution; y++)
            {
                for (int x = 0; x < splatMapResolution; x++)
                {
                    int centerIndex = y * splatMapResolution + x;
                    int centerOwner = terrainOwner[centerIndex];
                    
                    if (centerOwner == -1) continue; // Skip unassigned pixels
                    
                    // 1. Detect if the pixel is near a border using the specified depth.
                    bool isBorder = false;
                    for (int j = -borderDepth; j <= borderDepth; j++)
                    {
                        for (int i = -borderDepth; i <= borderDepth; i++)
                        {
                            if (i == 0 && j == 0) continue;
                            int nx = x + i;
                            int ny = y + j;

                            if (nx < 0) nx += splatMapResolution;
                            if (nx >= splatMapResolution) nx -= splatMapResolution;
                            if (ny < 0) ny += splatMapResolution;
                            if (ny >= splatMapResolution) ny -= splatMapResolution;
                            
                            int neighborOwner = terrainOwner[ny * splatMapResolution + nx];
                            if (neighborOwner != -1 && neighborOwner != centerOwner)
                            {
                                isBorder = true;
                                goto FoundBorder; // Exit the loops as soon as we confirm it's a border.
                            }
                        }
                    }
                    FoundBorder:;

                    if (isBorder)
                    {
                        // Check if either the center terrain or any neighbor is ocean - if so, skip noise
                        bool shouldSkipNoise = IsOceanTerrainForNoise(centerOwner);
                        
                        // Check neighbors for ocean terrain
                        for (int j = -1; j <= 1 && !shouldSkipNoise; j++)
                        {
                            for (int i = -1; i <= 1 && !shouldSkipNoise; i++)
                            {
                                if (i == 0 && j == 0) continue;
                                int nx = x + i;
                                int ny = y + j;

                                if (nx < 0) nx += splatMapResolution;
                                if (nx >= splatMapResolution) nx -= splatMapResolution;
                                if (ny < 0) ny += splatMapResolution;
                                if (ny >= splatMapResolution) ny -= splatMapResolution;
                                
                                int neighborOwner = terrainOwner[ny * splatMapResolution + nx];
                                if (neighborOwner != -1 && IsOceanTerrainForNoise(neighborOwner))
                                {
                                    shouldSkipNoise = true;
                                    break;
                                }
                            }
                        }
                        
                        // If ocean is involved, skip the noise generation
                        if (shouldSkipNoise) continue;
                        
                        // 2. If it's a border pixel, collect its *immediate* neighbors to determine what it can switch to.
                        List<int> neighborOwners = new List<int>();
                        for (int j = -1; j <= 1; j++)
                        {
                            for (int i = -1; i <= 1; i++)
                            {
                                if (i == 0 && j == 0) continue;
                                int nx = x + i;
                                int ny = y + j;

                                if (nx < 0) nx += splatMapResolution;
                                if (nx >= splatMapResolution) nx -= splatMapResolution;
                                if (ny < 0) ny += splatMapResolution;
                                if (ny >= splatMapResolution) ny -= splatMapResolution;
                                
                                int neighborOwner = terrainOwner[ny * splatMapResolution + nx];
                                if (neighborOwner != -1 && !neighborOwners.Contains(neighborOwner))
                                {
                                    neighborOwners.Add(neighborOwner);
                                }
                            }
                        }
                        
                        // 3. Apply noise to see if this pixel should be "flipped".
                        float noise = Mathf.PerlinNoise(x * borderNoiseScale , y * borderNoiseScale );
                        float noise2 = Mathf.PerlinNoise(x * borderNoiseScale * 2.5f + 100, y * borderNoiseScale * 2.5f + 100) * 0.5f;
                        float noise3 = Mathf.PerlinNoise(x * borderNoiseScale * 8.0f + 200, y * borderNoiseScale * 8.0f + 200) * 0.25f;
                        float noise4 = Mathf.PerlinNoise(x * borderNoiseScale * 16.0f + 300, y * borderNoiseScale * 16.0f + 300) * 0.125f;
                        float finalNoise = (noise + noise2 + noise3 + noise4) / 1.875f;

                        // Apply coastal variation if enabled
                        if (enableCoastalVariation)
                        {
                            // Generate regional variation pattern
                            float regionalVariation = Mathf.PerlinNoise(x * coastalVariationScale, y * coastalVariationScale);
                            
                            // If this region is "smooth coast", reduce the noise strength
                            if (regionalVariation > smoothCoastThreshold)
                            {
                                // Reduce noise strength for smooth coasts (less "chispitas")
                                finalNoise = Mathf.Lerp(finalNoise, 1.0f, 0.7f);
                            }
                        }

                        if (finalNoise < borderNoiseStrength)
                        {
                            List<int> candidates = neighborOwners.FindAll(owner => owner != centerOwner);
                            if (candidates.Count > 0)
                            {
                                int hash = (x * 1619 + y * 31337) % candidates.Count;
                                patchedOwner[y * splatMapResolution + x] = candidates[hash];
                            }
                        }
                    }
                }
            }
            terrainOwner = patchedOwner;
        }

        // 2. Generate the splat maps based on the final ownership data.
        for(int i = 0; i < terrainOwner.Length; i++)
        {
            int ownerType = terrainOwner[i];
            if (ownerType != -1 && terrainToSplat.ContainsKey(ownerType))
            {
                var (mapIndex, channel) = terrainToSplat[ownerType];
                Color color = new Color(0, 0, 0, 0); // Ensure all channels start at 0
                switch(channel)
                {
                    case 0: color.r = 1f; break;
                    case 1: color.g = 1f; break;
                    case 2: color.b = 1f; break;
                    case 3: color.a = 1f; break;
                }

                if (mapIndex == 0) pixels1[i] = color;
                else if (mapIndex == 1) pixels2[i] = color;
                else if (mapIndex == 2) pixels3[i] = color;
            }
        }
        
        // --- Apply blurring to splat maps ---
        if (enableBlur)
        {
            Debug.Log($"Applying border-only blur to splat maps with radius {blurRadius}...");
            pixels1 = BlurTextureBordersOnly(pixels1, splatMapResolution, blurRadius, terrainOwner);
            pixels2 = BlurTextureBordersOnly(pixels2, splatMapResolution, blurRadius, terrainOwner);
            pixels3 = BlurTextureBordersOnly(pixels3, splatMapResolution, blurRadius, terrainOwner);
        }
        
        // --- Post-processing Step ---
        for (int mapIndex = 0; mapIndex < 3; mapIndex++)
        {
            Color[] pixels;
            if (mapIndex == 0) pixels = pixels1;
            else if (mapIndex == 1) pixels = pixels2;
            else pixels = pixels3;
            
            splatMaps[mapIndex].SetPixels(pixels);
            splatMaps[mapIndex].Apply();
        }
        
        // Assign splat maps to material
        terrainMaterial.SetTexture("_SplatMap1", splatMaps[0]);
        terrainMaterial.SetTexture("_SplatMap2", splatMaps[1]);
        terrainMaterial.SetTexture("_SplatMap3", splatMaps[2]);
        
        // Debug terrain type distribution
        Debug.Log("Terrain type distribution:");
        foreach (var kvp in terrainTypeCount)
        {
            var (mapIndex, channel) = terrainToSplat[kvp.Key];
            string channelName = channel == 0 ? "R" : channel == 1 ? "G" : channel == 2 ? "B" : "A";
            Debug.Log($"Type {kvp.Key} (SplatMap{mapIndex + 1}.{channelName}): {kvp.Value} triangles");
        }
        
        Debug.Log($"3 splat maps generated{(enableBlur ? $" and blurred (radius: {blurRadius})" : "")}: {splatMapResolution}x{splatMapResolution}");
        
        // Update ocean wave effect with the new terrain data
        if (oceanWaveEffect != null && oceanTerrainIDs.Count > 0)
        {
            oceanWaveEffect.UpdateWaveMask(terrainOwner, splatMapResolution, oceanTerrainIDs[0]); // Use first ocean ID for now
        }
    }
    
    Vector2 Vector3ToUV(Vector3 pos)
    {
        // Convert 3D position to spherical UV coordinates
        Vector3 normalized = pos.normalized;
        float u = 0.5f + Mathf.Atan2(normalized.z, normalized.x) / (2 * Mathf.PI);
        float v = 0.5f - Mathf.Asin(normalized.y) / Mathf.PI;
        return new Vector2(u, v);
    }
    
    float CalculateTriangleArea(Vector2 a, Vector2 b, Vector2 c)
    {
        // Calculate area of triangle in UV space
        return Mathf.Abs((b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y)) * 0.5f;
    }

    void DrawTriangleWithOwner(int[] owner, Vector2 v1, Vector2 v2, Vector2 v3, int terrainType, int resolution)
    {
        float u_min = Mathf.Min(v1.x, v2.x, v3.x);
        float u_max = Mathf.Max(v1.x, v2.x, v3.x);

        if (u_max - u_min > 0.8f)
        {
            Vector2 p1_left = v1.x > 0.5f ? new Vector2(v1.x - 1.0f, v1.y) : v1;
            Vector2 p2_left = v2.x > 0.5f ? new Vector2(v2.x - 1.0f, v2.y) : v2;
            Vector2 p3_left = v3.x > 0.5f ? new Vector2(v3.x - 1.0f, v3.y) : v3;
            RasterizeTriangleOwner(owner, p1_left, p2_left, p3_left, terrainType, resolution);
            
            Vector2 p1_right = v1.x < 0.5f ? new Vector2(v1.x + 1.0f, v1.y) : v1;
            Vector2 p2_right = v2.x < 0.5f ? new Vector2(v2.x + 1.0f, v2.y) : v2;
            Vector2 p3_right = v3.x < 0.5f ? new Vector2(v3.x + 1.0f, v3.y) : v3;
            RasterizeTriangleOwner(owner, p1_right, p2_right, p3_right, terrainType, resolution);
        }
        else
        {
            RasterizeTriangleOwner(owner, v1, v2, v3, terrainType, resolution);
        }
    }

    void RasterizeTriangleOwner(int[] owner, Vector2 v1, Vector2 v2, Vector2 v3, int terrainType, int resolution)
    {
        // Scale UV coordinates to pixel space
        var p1 = new Vector2(v1.x * resolution, v1.y * resolution);
        var p2 = new Vector2(v2.x * resolution, v2.y * resolution);
        var p3 = new Vector2(v3.x * resolution, v3.y * resolution);
        
        // Bounding box for the triangle
        int minX = (int)Mathf.Min(p1.x, p2.x, p3.x);
        int maxX = (int)Mathf.Max(p1.x, p2.x, p3.x);
        int minY = (int)Mathf.Min(p1.y, p2.y, p3.y);
        int maxY = (int)Mathf.Max(p1.y, p2.y, p3.y);

        // Iterate over every pixel in the bounding box
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                // Barycentric coordinate calculation
                float w0 = ((p2.y - p3.y) * (x - p3.x) + (p3.x - p2.x) * (y - p3.y)) /
                           ((p2.y - p3.y) * (p1.x - p3.x) + (p3.x - p2.x) * (p1.y - p3.y));
                
                float w1 = ((p3.y - p1.y) * (x - p3.x) + (p1.x - p3.x) * (y - p3.y)) /
                           ((p2.y - p3.y) * (p1.x - p3.x) + (p3.x - p2.x) * (p1.y - p3.y));

                float w2 = 1.0f - w0 - w1;

                // If the pixel is inside the triangle (or on its edges)
                if (w0 >= 0 && w1 >= 0 && w2 >= 0)
                {
                    // Clamp to make sure we don't write outside the owner array
                    int clampedY = Mathf.Clamp(y, 0, resolution - 1);
                    int clampedX = Mathf.Clamp(x, 0, resolution - 1);
                    int pixelIndex = clampedY * resolution + clampedX;

                    if (pixelIndex >= 0 && pixelIndex < owner.Length)
                    {
                        if (owner[pixelIndex] == -1)
                        {
                            owner[pixelIndex] = terrainType;
                        }
                    }
                }
            }
        }
    }


    Color[] BlurTextureBordersOnly(Color[] pixels, int resolution, int blurRadius, int[] terrainOwner)
    {
        Color[] blurred = new Color[pixels.Length];
        bool[] isBorderPixel = new bool[pixels.Length];
        
        // First pass: detect border pixels
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int centerIndex = y * resolution + x;
                int centerOwner = terrainOwner[centerIndex];
                
                if (centerOwner == -1) continue; // Skip unassigned pixels
                
                // 1. Detect if the pixel is near a border using the specified depth.
                bool isBorder = false;
                bool involvesOcean = IsOceanTerrainForBlur(centerOwner);
                
                for (int j = -borderDepth; j <= borderDepth; j++)
                {
                    for (int i = -borderDepth; i <= borderDepth; i++)
                    {
                        if (i == 0 && j == 0) continue;
                        int nx = x + i;
                        int ny = y + j;

                        if (nx < 0) nx += resolution;
                        if (nx >= resolution) nx -= resolution;
                        if (ny < 0) ny += resolution;
                        if (ny >= resolution) ny -= resolution;
                        
                        int neighborOwner = terrainOwner[ny * resolution + nx];
                        if (neighborOwner != -1 && neighborOwner != centerOwner)
                        {
                            // Check if either terrain is ocean
                            if (IsOceanTerrainForBlur(neighborOwner))
                            {
                                involvesOcean = true;
                            }
                            isBorder = true;
                            goto FoundBorder; // Exit the loops as soon as we confirm it's a border.
                        }
                    }
                }
                FoundBorder:;
                
                // Only mark as border pixel if it doesn't involve ocean
                isBorderPixel[centerIndex] = isBorder && !involvesOcean;
            }
        }
        
        // Second pass: apply blur only to border pixels and their immediate neighbors
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int pixelIndex = y * resolution + x;
                
                // Check if this pixel or any nearby pixel is a border
                bool shouldBlur = false;
                for (int dy = -blurRadius; dy <= blurRadius; dy++)
                {
                    for (int dx = -blurRadius; dx <= blurRadius; dx++)
                    {
                        int nx = x + dx;
                        int ny = y + dy;
                        
                        // Handle wrapping
                        if (nx < 0) nx += resolution;
                        if (nx >= resolution) nx -= resolution;
                        if (ny < 0) ny += resolution;
                        if (ny >= resolution) ny -= resolution;
                        
                        int neighborIndex = ny * resolution + nx;
                        if (neighborIndex >= 0 && neighborIndex < isBorderPixel.Length && isBorderPixel[neighborIndex])
                        {
                            shouldBlur = true;
                            break;
                        }
                    }
                    if (shouldBlur) break;
                }
                
                if (shouldBlur)
                {
                    // Apply blur to this pixel
                    Vector4 sum = Vector4.zero;
                    int kernelSize = blurRadius * 2 + 1;
                    float kernelWeight = 1.0f / (kernelSize * kernelSize);
                    
                    for (int ky = -blurRadius; ky <= blurRadius; ky++)
                    {
                        for (int kx = -blurRadius; kx <= blurRadius; kx++)
                        {
                            int sampleX = x + kx;
                            int sampleY = y + ky;
                            
                            // Handle wrapping for seamless textures
                            if (sampleX < 0) sampleX += resolution;
                            if (sampleX >= resolution) sampleX -= resolution;
                            if (sampleY < 0) sampleY += resolution;
                            if (sampleY >= resolution) sampleY -= resolution;
                            
                            int index = sampleY * resolution + sampleX;
                            sum += (Vector4)pixels[index] * kernelWeight;
                        }
                    }
                    
                    blurred[pixelIndex] = (Color)sum;
                }
                else
                {
                    // Keep the original pixel value for non-border areas
                    blurred[pixelIndex] = pixels[pixelIndex];
                }
            }
        }
        
        return blurred;
    }

    public void LoadTriangleData(List<TriangleData> loadedTriangles)
    {
        if (loadedTriangles == null || loadedTriangles.Count == 0)
        {
            Debug.LogError("IcoSphere: No triangle data provided to load!");
            return;
        }

        // Clear existing data
        triangleDataList.Clear();
        triangleToDataIndex.Clear();
        edgeToTriangles.Clear();

        // Add loaded data
        triangleDataList.AddRange(loadedTriangles);
        for (int i = 0; i < triangleDataList.Count; i++)
        {
            triangleToDataIndex.Add(i);
        }
        
        // Rebuild the mesh with the new data
        RebuildMeshFromTriangleData();
        
        // --- FIX ---
        // After loading, we must regenerate the splat map to reflect the loaded terrain data.
        var renderer = GetComponent<MeshRenderer>();
        if (renderer != null && renderer.material != null)
        {
            GenerateSplatMap(renderer.material);
            Debug.Log("IcoSphere: Regenerated splat maps after loading new triangle data.");
        }
        else
        {
            Debug.LogWarning("IcoSphere: Could not regenerate splat maps after loading. Renderer or material not found.");
        }
        
        // Update country borders after loading data
        UpdateCountryBorders();
    }

    private void RebuildMeshFromTriangleData()
    {
        if (triangleDataList.Count == 0) return;

        // Reconstruct unique vertex list to calculate adjacency after loading.
        var uniqueVertices = new List<Vector3>();
        var vertexMap = new Dictionary<Vector3, int>(); // To quickly check for duplicates

        foreach (var tri in triangleDataList)
        {
            if (!vertexMap.ContainsKey(tri.a)) { vertexMap[tri.a] = uniqueVertices.Count; uniqueVertices.Add(tri.a); }
            if (!vertexMap.ContainsKey(tri.b)) { vertexMap[tri.b] = uniqueVertices.Count; uniqueVertices.Add(tri.b); }
            if (!vertexMap.ContainsKey(tri.c)) { vertexMap[tri.c] = uniqueVertices.Count; uniqueVertices.Add(tri.c); }
        }
        var newMeshData = new MeshData { uniqueVertices = uniqueVertices };

        // Create new mesh
        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        GetComponent<MeshFilter>().mesh = mesh;

        // Build mesh data from triangle list
        int triangleCount = triangleDataList.Count;
        Vector3[] newVerts = new Vector3[triangleCount * 3];
        Vector2[] newUVs = new Vector2[triangleCount * 3];
        Vector3[] newNormals = new Vector3[triangleCount * 3];
        Color[] newColors = new Color[triangleCount * 3];
        int[] newTris = new int[triangleCount * 3];

        for (int i = 0; i < triangleCount; i++)
        {
            var tri = triangleDataList[i];
            int baseIdx = i * 3;
            
            // Vertices
            newVerts[baseIdx + 0] = tri.a;
            newVerts[baseIdx + 1] = tri.b;
            newVerts[baseIdx + 2] = tri.c;

            // Smoothed Normals
            newNormals[baseIdx + 0] = tri.a.normalized;
            newNormals[baseIdx + 1] = tri.b.normalized;
            newNormals[baseIdx + 2] = tri.c.normalized;

            // Spherical UVs based on actual 3D positions
            newUVs[baseIdx + 0] = Vector3ToUV(tri.a);
            newUVs[baseIdx + 1] = Vector3ToUV(tri.b);
            newUVs[baseIdx + 2] = Vector3ToUV(tri.c);
            
            // Triangle ID as color
            Color color = new Color(
                ((i & 0xFF) / 255.0f),
                (((i >> 8) & 0xFF) / 255.0f),
                (((i >> 16) & 0xFF) / 255.0f),
                1.0f
            );
            newColors[baseIdx + 0] = color;
            newColors[baseIdx + 1] = color;
            newColors[baseIdx + 2] = color;
            newTris[baseIdx + 0] = baseIdx + 0;
            newTris[baseIdx + 1] = baseIdx + 1;
            newTris[baseIdx + 2] = baseIdx + 2;
        }

        mesh.vertices = newVerts;
        mesh.uv = newUVs;
        mesh.normals = newNormals;
        mesh.colors = newColors;
        mesh.triangles = newTris;

        // Update mesh collider
        if (TryGetComponent<MeshCollider>(out var collider))
        {
            collider.sharedMesh = mesh;
        }
        
        // Notify the border renderer of the new mesh
        if (borderManager != null)
        {
            borderManager.UpdateMesh(mesh);
        }

        // Restore adjacency info, which is lost during serialization
        CalculateAdjacency(newMeshData);
    }
    
    /// <summary>
    /// Updates the country borders when country assignments change
    /// </summary>
    public void UpdateCountryBorders()
    {
        if (borderManager != null)
        {
            borderManager.GenerateAllBorders();
        }
    }

    /// <summary>
    /// Devuelve el material principal de terreno actualmente asignado.
    /// </summary>
    public Material GetMainTerrainMaterial()
    {
        return mainTerrainMaterial;
    }
}
