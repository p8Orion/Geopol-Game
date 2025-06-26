using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(IcoSphere))]
public class CountryBorderRenderer : MonoBehaviour
{
    [Header("Border Settings")]
    public bool enableBorders = true;
    public float borderWidth = 0.03f;
    public float borderIntensity = 1f;
    public float borderGlow = 0f;
    [Range(0,1)]
    public float borderOffset = 0.06f;
    public bool enablePulse = false;
    public float pulseSpeed = 2.0f;
    
    [Header("Border Quality")]
    public int borderTextureResolution = 1024; // Lower resolution for better performance
    public float borderDetectionThreshold = 0.1f;
    public bool enableBorderSmoothing = true;
    public int smoothingIterations = 1; // Reduced for better performance
    
    [Header("References")]
    public Material borderMaterial;
    
    // Private fields
    [SerializeField] private Texture2D borderTexture;
    [SerializeField] private Texture2D countryIDTexture;
    [SerializeField] private Vector4[] countryColorDebugArray; // For debugging the lookup table
    private IcoSphere icoSphere;
    private MeshRenderer meshRenderer;
    private GameObject borderObject;
    private MeshRenderer borderRenderer;
    private bool bordersGenerated = false;
    private ComputeBuffer countryColorBuffer;
    
    // Reference to the source of country data
    private MapEditor mapEditor;
    
    // Optimization: Track which regions need border updates
    private HashSet<int> trianglesNeedingBorderUpdate = new HashSet<int>();
    private bool needsFullBorderRegeneration = false;
    
    private struct BorderJob {
        public TriangleData OurTriangle;
        public TriangleData NeighborTriangle;
    }
    
    void Awake()
    {
        Debug.Log("=== CountryBorderRenderer Awake() Start ===");
        
        icoSphere = GetComponent<IcoSphere>();
        Debug.Log($"CountryBorderRenderer: IcoSphere component found: {icoSphere != null}");
        
        meshRenderer = GetComponent<MeshRenderer>();
        Debug.Log($"CountryBorderRenderer: MeshRenderer component found: {meshRenderer != null}");
        
        // Create border material if not assigned
        if (borderMaterial == null)
        {
            Debug.Log("CountryBorderRenderer: No border material assigned, creating one...");
            Shader borderShader = Shader.Find("Custom/CountryBorder");
            if (borderShader != null)
            {
                borderMaterial = new Material(borderShader);
                Debug.Log("CountryBorderRenderer: Successfully created border material");
            }
            else
            {
                Debug.LogError("CountryBorderRenderer: Custom/CountryBorder shader not found! Please ensure the shader is compiled.");
                
                // Try to find any shader as fallback
                Shader fallbackShader = Shader.Find("Universal Render Pipeline/Lit");
                if (fallbackShader != null)
                {
                    borderMaterial = new Material(fallbackShader);
                    Debug.LogWarning("CountryBorderRenderer: Using fallback shader for debugging");
                }
                else
                {
                    Debug.LogError("CountryBorderRenderer: No shader found at all!");
                    return;
                }
            }
        }
        else
        {
            Debug.Log("CountryBorderRenderer: Border material already assigned");
        }
        
        // Find the MapEditor in the scene to get access to the country list
        Debug.Log("CountryBorderRenderer: Searching for MapEditor in scene...");
        mapEditor = FindObjectOfType<MapEditor>();
        if (mapEditor == null)
        {
            Debug.LogError("CountryBorderRenderer: MapEditor not found in scene! Borders cannot be colored correctly.");
        }
        else
        {
            Debug.Log($"CountryBorderRenderer: MapEditor found: {mapEditor.name}");
            Debug.Log($"CountryBorderRenderer: MapEditor has CountryList: {mapEditor.countryList != null}");
            if (mapEditor.countryList != null)
            {
                Debug.Log($"CountryBorderRenderer: CountryList has {mapEditor.countryList.countries?.Count ?? 0} countries");
            }
        }
        
        // Create border overlay object
        Debug.Log("CountryBorderRenderer: Creating border overlay...");
        CreateBorderOverlay();
        
        Debug.Log("=== CountryBorderRenderer Awake() End ===");
    }
    
    void Start()
    {
        // This method is now empty. The IcoSphere will call InitializeAndGenerateBorders directly.
    }
    
    void Update()
    {
        if (!enableBorders || borderObject == null) return;
        
        // Update border material properties
        if (borderMaterial != null)
        {
            borderMaterial.SetFloat("_BorderWidth", borderWidth);
            borderMaterial.SetFloat("_BorderIntensity", borderIntensity);
            borderMaterial.SetFloat("_BorderGlow", borderGlow);
            borderMaterial.SetFloat("_BorderPulse", enablePulse ? 1.0f : 0.0f);
            borderMaterial.SetFloat("_BorderPulseSpeed", pulseSpeed);
            borderMaterial.SetFloat("_BorderBlend", 0.5f); // Center blend
            borderMaterial.SetColor("_UnclaimedColor", new Color(0.5f, 0.5f, 0.5f, 0.3f));
        }
        
        // Show/hide border object
        borderObject.SetActive(enableBorders);
    }
    
    void CreateBorderOverlay()
    {
        Debug.Log("=== CountryBorderRenderer CreateBorderOverlay() Start ===");
        
        // Create a child object for the border overlay
        borderObject = new GameObject("CountryBorders");
        borderObject.transform.SetParent(transform);
        borderObject.transform.localPosition = Vector3.zero;
        borderObject.transform.localRotation = Quaternion.identity;
        // Revert the scale change, we will handle Z-fighting in the shader
        borderObject.transform.localScale = Vector3.one;
        
        Debug.Log($"CountryBorderRenderer: Created border object: {borderObject.name}");
        Debug.Log($"CountryBorderRenderer: Border object parent: {borderObject.transform.parent?.name}");
        Debug.Log($"CountryBorderRenderer: Border object position: {borderObject.transform.localPosition}");
        Debug.Log($"CountryBorderRenderer: Border object scale: {borderObject.transform.localScale}");
        
        // Add mesh components
        var meshFilter = borderObject.AddComponent<MeshFilter>();
        var borderMeshRenderer = borderObject.AddComponent<MeshRenderer>();
        
        Debug.Log($"CountryBorderRenderer: Added MeshFilter: {meshFilter != null}");
        Debug.Log($"CountryBorderRenderer: Added MeshRenderer: {borderMeshRenderer != null}");
        
        // Copy mesh from parent
        var parentMeshFilter = GetComponent<MeshFilter>();
        if (parentMeshFilter != null && parentMeshFilter.mesh != null)
        {
            meshFilter.mesh = parentMeshFilter.mesh;
            Debug.Log($"CountryBorderRenderer: Copied mesh with {parentMeshFilter.mesh.vertexCount} vertices");
        }
        else
        {
            Debug.LogError("CountryBorderRenderer: Parent mesh not found!");
        }
        
        // Set up renderer
        borderRenderer = borderMeshRenderer;
        Debug.Log($"CountryBorderRenderer: Border material before assignment: {borderMaterial}");
        borderRenderer.material = borderMaterial;

        // IMPORTANT: When setting renderer.material, Unity creates an instance.
        // We must update our reference to point to this new instance.
        borderMaterial = borderRenderer.material;
        
        Debug.Log($"CountryBorderRenderer: Border material after assignment: {borderRenderer.material}");
        borderRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        borderRenderer.receiveShadows = false;
        
        // Set layer to render on top
        borderObject.layer = gameObject.layer;
        
        Debug.Log($"CountryBorderRenderer: Created border overlay object. Active: {borderObject.activeInHierarchy}, Material: {borderRenderer.material}");
        Debug.Log("=== CountryBorderRenderer CreateBorderOverlay() End ===");
    }
    
    public void GenerateBorderTexture()
    {
        Debug.Log("=== CountryBorderRenderer GenerateBorderTexture() Start ===");
        
        if (icoSphere == null || icoSphere.triangleDataList.Count == 0)
        {
            Debug.LogWarning("CountryBorderRenderer: No triangle data available for border generation.");
            return;
        }
        
        Debug.Log($"CountryBorderRenderer: Generating border texture {borderTextureResolution}x{borderTextureResolution}");
        
        // Create border texture, explicitly in linear color space to prevent gamma correction on our data.
        borderTexture = new Texture2D(borderTextureResolution, borderTextureResolution, TextureFormat.RGBA32, false, true);
        borderTexture.filterMode = FilterMode.Bilinear;
        borderTexture.wrapMode = TextureWrapMode.Clamp;
        Debug.Log($"CountryBorderRenderer: Created border texture: {borderTexture != null}");
        
        // Create country ID texture, also in linear color space.
        countryIDTexture = new Texture2D(borderTextureResolution, borderTextureResolution, TextureFormat.RGBA32, false, true);
        countryIDTexture.filterMode = FilterMode.Point; // Use Point filter for sharp ID lookups
        countryIDTexture.wrapMode = TextureWrapMode.Clamp;
        Debug.Log($"CountryBorderRenderer: Created country ID texture: {countryIDTexture != null}");
        
        // Initialize with zeros
        Color[] pixels = new Color[borderTextureResolution * borderTextureResolution];
        Color[] idPixels = new Color[borderTextureResolution * borderTextureResolution];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
            idPixels[i] = Color.clear;
        }
        
        Debug.Log("CountryBorderRenderer: Initialized pixel arrays with clear colors");
        
        // Generate border data
        Debug.Log("CountryBorderRenderer: Calling GenerateBorderData...");
        GenerateBorderData(pixels, idPixels);
        
        // Apply smoothing if enabled (only to the border mask)
        if (enableBorderSmoothing)
        {
            Debug.Log("CountryBorderRenderer: Applying border smoothing...");
            pixels = SmoothBorderTexture(pixels);
        }
        
        // Apply to textures
        Debug.Log("CountryBorderRenderer: Applying pixels to textures...");
        borderTexture.SetPixels(pixels);
        borderTexture.Apply();
        countryIDTexture.SetPixels(idPixels);
        countryIDTexture.Apply();
        
        Debug.Log("CountryBorderRenderer: Textures applied successfully");
        
        // Update the color array in the material
        Debug.Log("CountryBorderRenderer: Calling UpdateCountryColorsInMaterial...");
        UpdateCountryColorsInMaterial();
        
        // Assign textures to material
        if (borderMaterial != null)
        {
            Debug.Log("CountryBorderRenderer: Assigning textures to material...");
            borderMaterial.SetTexture("_BorderTex", borderTexture);
            borderMaterial.SetTexture("_CountryIDTex", countryIDTexture);
            Debug.Log($"CountryBorderRenderer: Border texture assigned: {borderMaterial.GetTexture("_BorderTex") != null}");
            Debug.Log($"CountryBorderRenderer: Country ID texture assigned: {borderMaterial.GetTexture("_CountryIDTex") != null}");
        }
        else
        {
            Debug.LogError("CountryBorderRenderer: Border material is null! Cannot assign textures.");
        }
        
        bordersGenerated = true;
        Debug.Log("CountryBorderRenderer: Border texture and country ID texture generated successfully.");
        Debug.Log("=== CountryBorderRenderer GenerateBorderTexture() End ===");
    }
    
    void GenerateBorderData(Color[] pixels, Color[] idPixels)
    {
        Debug.Log("=== CountryBorderRenderer GenerateBorderData() Start ===");
        
        int triangleCount = icoSphere.triangleDataList.Count;

        // Step 1: Create a list of all one-sided border jobs. No deduplication.
        List<BorderJob> borderJobs = new List<BorderJob>();
        for (int i = 0; i < triangleCount; i++)
        {
            var ourTriangle = icoSphere.triangleDataList[i];
            if (ourTriangle.country == null) continue;
            
            foreach (int adjacentId in ourTriangle.adjacentTriangles)
            {
                if (adjacentId < triangleCount)
                {
                    var neighborTriangle = icoSphere.triangleDataList[adjacentId];
                    if (ourTriangle.country != neighborTriangle.country)
                    {
                        borderJobs.Add(new BorderJob { OurTriangle = ourTriangle, NeighborTriangle = neighborTriangle });
                    }
                }
            }
        }
        
        Debug.Log($"CountryBorderRenderer: Found {borderJobs.Count} one-sided border jobs to process.");
        
        if (borderJobs.Count == 0)
        {
            Debug.LogWarning("CountryBorderRenderer: No border jobs found!");
            return;
        }
        
        // Step 2: Rasterize each job as a half-border.
        int rasterizedEdges = 0;
        foreach (var job in borderJobs)
        {
            Vector3[] sharedVertices = FindSharedEdgeVertices(job.OurTriangle, job.NeighborTriangle);
            if (sharedVertices.Length == 2)
            {
                RasterizeSidedBorder(pixels, idPixels, sharedVertices[0], sharedVertices[1], job.OurTriangle);
                rasterizedEdges++;
            }
        }
        
        Debug.Log($"CountryBorderRenderer: Successfully rasterized {rasterizedEdges} half-borders.");
        Debug.Log("=== CountryBorderRenderer GenerateBorderData() End ===");
    }

    void RasterizeSidedBorder(Color[] pixels, Color[] idPixels, Vector3 v1, Vector3 v2, TriangleData ourTriangle)
    {
        // Convert 3D positions to UV coordinates
        Vector2 uv1 = Vector3ToUV(v1);
        Vector2 uv2 = Vector3ToUV(v2);

        // Calculate line direction and its perpendicular for sidedness checks
        Vector2 lineDir = (uv2 - uv1).normalized;
        Vector2 perpendicular = new Vector2(-lineDir.y, lineDir.x);

        // Determine which side our triangle's center is on relative to the edge
        Vector2 uvCenter = Vector3ToUV(ourTriangle.GetCenter());
        Vector2 edgeCenterUV = (uv1 + uv2) / 2f;
        float sideDot = Vector2.Dot(perpendicular, uvCenter - edgeCenterUV);
        float side = Mathf.Sign(sideDot); // -1 for left, 1 for right

        // Convert to pixel coordinates
        int x1 = Mathf.RoundToInt(uv1.x * borderTextureResolution);
        int y1 = Mathf.RoundToInt(uv1.y * borderTextureResolution);
        int x2 = Mathf.RoundToInt(uv2.x * borderTextureResolution);
        int y2 = Mathf.RoundToInt(uv2.y * borderTextureResolution);

        // Draw line using Bresenham's algorithm
        DrawSidedLine(pixels, idPixels, x1, y1, x2, y2, side, perpendicular, ourTriangle.country.index);
    }
    
    void DrawSidedLine(Color[] pixels, Color[] idPixels, int x1, int y1, int x2, int y2, float side, Vector2 perpendicular, int ourCountryIndex)
    {
        int dx = Mathf.Abs(x2 - x1);
        int dy = Mathf.Abs(y2 - y1);
        int sx = x1 < x2 ? 1 : -1;
        int sy = y1 < y2 ? 1 : -1;
        int err = dx - dy;
        
        int x = x1, y = y1;
        
        while (true)
        {
            // Set pixel with border width, but only on the correct side
            SetSidedBorderPixel(pixels, idPixels, x, y, side, perpendicular, ourCountryIndex);
            
            if (x == x2 && y == y2) break;
            
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y += sy;
            }
        }
    }

    void SetSidedBorderPixel(Color[] pixels, Color[] idPixels, int x, int y, float side, Vector2 perpendicular, int ourCountryIndex)
    {
        // Draw a half-width border by only drawing on the correct side of the line
        int width = Mathf.Max(1, Mathf.RoundToInt(borderWidth * borderTextureResolution * 0.1f));
        // Calculate the desired offset in pixel units. A factor of 0.5 makes an offset of 1 mean a full half-width inset.
        float pixelOffset = borderOffset * (width * 0.5f);

        for (int dx = -width; dx <= width; dx++)
        {
            for (int dy = -width; dy <= width; dy++)
            {
                // Determine which side of the line this offset pixel is on
                Vector2 offsetVec = new Vector2(dx, dy);
                float currentSide = Vector2.Dot(offsetVec, perpendicular);

                // Only draw if the pixel is on our side of the line, AND pushed inward by the desired offset.
                if (side * currentSide >= pixelOffset)
                {
                    int nx = (x + dx + borderTextureResolution) % borderTextureResolution;
                    int ny = (y + dy + borderTextureResolution) % borderTextureResolution;
                    int nIndex = ny * borderTextureResolution + nx;

                    if (nIndex >= 0 && nIndex < pixels.Length)
                    {
                        // Update border mask
                        float distance = Mathf.Sqrt(dx * dx + dy * dy);
                        float alpha = Mathf.Max(0, 1 - distance / width);
                        pixels[nIndex] = Color.Lerp(pixels[nIndex], Color.white, alpha);

                        // Update ID Texture
                        // We only need to store our country's ID now.
                        float id1_norm = (ourCountryIndex + 1) / 255.0f;
                        Color newColor = new Color(id1_norm, 0, 0, 1); // R=OurID, G=Unused, B=Unused
                        idPixels[nIndex] = Color.Lerp(idPixels[nIndex], newColor, alpha);
                    }
                }
            }
        }
    }
    
    Vector3[] FindSharedEdgeVertices(TriangleData tri1, TriangleData tri2)
    {
        Vector3[] vertices1 = { tri1.a, tri1.b, tri1.c };
        Vector3[] vertices2 = { tri2.a, tri2.b, tri2.c };
        
        List<Vector3> shared = new List<Vector3>();
        
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (Vector3.Distance(vertices1[i], vertices2[j]) < 0.001f)
                {
                    shared.Add(vertices1[i]);
                }
            }
        }
        
        return shared.ToArray();
    }
    
    Vector2 Vector3ToUV(Vector3 pos)
    {
        // Convert 3D position to spherical UV coordinates (same as IcoSphere)
        Vector3 normalized = pos.normalized;
        float u = 0.5f + Mathf.Atan2(normalized.z, normalized.x) / (2 * Mathf.PI);
        float v = 0.5f - Mathf.Asin(normalized.y) / Mathf.PI;
        return new Vector2(u, v);
    }
    
    public void RefreshBorders()
    {
        if (bordersGenerated)
        {
            GenerateBorderTexture();
        }
        else
        {
            Debug.LogWarning("CountryBorderRenderer: Borders not yet generated, calling GenerateBorderTexture()");
            GenerateBorderTexture();
        }
    }
    
    /// <summary>
    /// Configure border settings programmatically
    /// </summary>
    public void ConfigureBorders(bool enabled, float width, float intensity, float glow, bool pulse = false, float pulseSpeed = 2.0f)
    {
        enableBorders = enabled;
        borderWidth = width;
        borderIntensity = intensity;
        borderGlow = glow;
        enablePulse = pulse;
        this.pulseSpeed = pulseSpeed;
        
        // Update material properties immediately
        if (borderMaterial != null)
        {
            borderMaterial.SetFloat("_BorderWidth", borderWidth);
            borderMaterial.SetFloat("_BorderIntensity", borderIntensity);
            borderMaterial.SetFloat("_BorderGlow", borderGlow);
            borderMaterial.SetFloat("_BorderPulse", enablePulse ? 1.0f : 0.0f);
            borderMaterial.SetFloat("_BorderPulseSpeed", this.pulseSpeed);
            borderMaterial.SetFloat("_BorderBlend", 0.5f);
            borderMaterial.SetColor("_UnclaimedColor", new Color(0.5f, 0.5f, 0.5f, 0.3f));
        }
    }
    
    /// <summary>
    /// Enable or disable borders
    /// </summary>
    public void SetBordersEnabled(bool enabled)
    {
        enableBorders = enabled;
        if (borderObject != null)
        {
            borderObject.SetActive(enableBorders);
        }
    }
    
    /// <summary>
    /// Force regenerate borders (useful for debugging)
    /// </summary>
    [ContextMenu("Force Regenerate Borders")]
    public void ForceRegenerateBorders()
    {
        Debug.Log("CountryBorderRenderer: Force regenerating borders...");
        bordersGenerated = false;
        GenerateBorderTexture();
        
        // Debug material setup
        if (borderMaterial != null)
        {
            Debug.Log($"CountryBorderRenderer: Material has BorderTex: {borderMaterial.GetTexture("_BorderTex") != null}");
            Debug.Log($"CountryBorderRenderer: Material has CountryIDTex: {borderMaterial.GetTexture("_CountryIDTex") != null}");
        }
        else
        {
            Debug.LogError("CountryBorderRenderer: Border material is null!");
        }
        
        // Debug border object
        if (borderObject != null)
        {
            Debug.Log($"CountryBorderRenderer: Border object active: {borderObject.activeInHierarchy}");
            var renderer = borderObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Debug.Log($"CountryBorderRenderer: Border renderer material: {renderer.material}");
                Debug.Log($"CountryBorderRenderer: Border renderer enabled: {renderer.enabled}");
            }
        }
        else
        {
            Debug.LogError("CountryBorderRenderer: Border object is null!");
        }
    }
    
    /// <summary>
    /// Create a simple test border texture (for debugging)
    /// </summary>
    [ContextMenu("Create Test Border")]
    public void CreateTestBorder()
    {
        Debug.Log("CountryBorderRenderer: Creating test border texture...");
        
        if (borderRenderer == null || borderMaterial == null)
        {
            Debug.LogError("CountryBorderRenderer: Border renderer or material is not initialized!");
            return;
        }

        // Ensure we are using the primary border material for this test
        borderRenderer.material = borderMaterial;
        
        // Create test textures in linear space
        borderTexture = new Texture2D(256, 256, TextureFormat.RGBA32, false, true);
        countryIDTexture = new Texture2D(256, 256, TextureFormat.RGBA32, false, true);
        
        Color[] borderPixels = new Color[256 * 256];
        Color[] idPixels = new Color[256 * 256];
        
        // Use a fixed-size array to be consistent with the main function and shader
        Vector4[] testColors = new Vector4[256];
        testColors[0] = new Color(0.5f, 0.5f, 0.5f, 0.3f); // Index 0: Unclaimed
        testColors[1] = Color.red;                         // Index 1: Test Country 1
        testColors[2] = Color.blue;                        // Index 2: Test Country 2
        
        // Tell the shader there are only 3 valid colors to use for clamping
        borderMaterial.SetInt("_CountryCount", 3);
        borderMaterial.SetVectorArray("_CountryColors", testColors);
        Debug.Log("CountryBorderRenderer: Set up material with 3 test colors in a fixed-size array.");

        // Create a simple cross pattern using valid, normalized IDs
        for (int y = 0; y < 256; y++)
        {
            for (int x = 0; x < 256; x++)
            {
                int index = y * 256 + x;
                bool isVerticalBar = (x > 100 && x < 110);
                bool isHorizontalBar = (y > 150 && y < 160);

                if (isVerticalBar)
                {
                    borderPixels[index] = Color.white;
                    // Border between Unclaimed (index 0) and Red (index 1)
                    // We add 1 to the index before normalizing, so shader gets correct original index.
                    float id1_norm = (0 + 1) / 255.0f; 
                    float id2_norm = (1 + 1) / 255.0f;
                    idPixels[index] = new Color(id1_norm, id2_norm, 0.5f, 1);
                }
                else if (isHorizontalBar)
                {
                    borderPixels[index] = Color.white;
                    // Border between Red (index 1) and Blue (index 2)
                    float id1_norm = (1 + 1) / 255.0f;
                    float id2_norm = (2 + 1) / 255.0f;
                    idPixels[index] = new Color(id1_norm, id2_norm, 0.5f, 1);
                }
                else
                {
                    borderPixels[index] = Color.clear;
                    idPixels[index] = Color.clear;
                }
            }
        }
        
        borderTexture.SetPixels(borderPixels);
        borderTexture.Apply();
        countryIDTexture.SetPixels(idPixels);
        countryIDTexture.Apply();
        
        // Assign textures to material
        borderMaterial.SetTexture("_BorderTex", borderTexture);
        borderMaterial.SetTexture("_CountryIDTex", countryIDTexture);
        Debug.Log("CountryBorderRenderer: Test border textures assigned to material");
        
        bordersGenerated = true;
    }
    
    /// <summary>
    /// Check if border object is visible and properly set up
    /// </summary>
    [ContextMenu("Check Border Visibility")]
    public void CheckBorderVisibility()
    {
        Debug.Log("=== CountryBorderRenderer Visibility Check ===");
        
        if (borderObject == null)
        {
            Debug.LogError("Border object is null!");
            return;
        }
        
        Debug.Log($"Border object name: {borderObject.name}");
        Debug.Log($"Border object active in hierarchy: {borderObject.activeInHierarchy}");
        Debug.Log($"Border object active self: {borderObject.activeSelf}");
        Debug.Log($"Border object layer: {borderObject.layer}");
        Debug.Log($"Border object position: {borderObject.transform.position}");
        Debug.Log($"Border object scale: {borderObject.transform.localScale}");
        
        var renderer = borderObject.GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            Debug.LogError("Border object has no MeshRenderer!");
            return;
        }
        
        Debug.Log($"Border renderer enabled: {renderer.enabled}");
        Debug.Log($"Border renderer material: {renderer.material}");
        Debug.Log($"Border renderer shared material: {renderer.sharedMaterial}");
        Debug.Log($"Border renderer shadow casting: {renderer.shadowCastingMode}");
        Debug.Log($"Border renderer receive shadows: {renderer.receiveShadows}");
        
        var meshFilter = borderObject.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            Debug.LogError("Border object has no MeshFilter!");
            return;
        }
        
        Debug.Log($"Border mesh: {meshFilter.mesh}");
        if (meshFilter.mesh != null)
        {
            Debug.Log($"Border mesh vertex count: {meshFilter.mesh.vertexCount}");
            Debug.Log($"Border mesh triangle count: {meshFilter.mesh.triangles.Length / 3}");
        }
        
        // Check if camera can see this object
        var camera = Camera.main;
        if (camera != null)
        {
            var bounds = renderer.bounds;
            bool inFrustum = GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(camera), bounds);
            Debug.Log($"Border object in camera frustum: {inFrustum}");
            Debug.Log($"Border object bounds: {bounds}");
        }
        
        Debug.Log("=== End Visibility Check ===");
    }
    
    public void UpdateMesh(Mesh newMesh)
    {
        if (borderObject == null)
        {
            Debug.LogError("CountryBorderRenderer: Border object is null, cannot update mesh.");
            return;
        }

        var meshFilter = borderObject.GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            meshFilter.mesh = newMesh;
            Debug.Log("CountryBorderRenderer: Border overlay mesh updated successfully.");
            
            // After updating the mesh, we MUST regenerate the borders
            // because the UVs and vertex positions may have changed.
            RefreshBorders();
        }
        else
        {
            Debug.LogError("CountryBorderRenderer: MeshFilter on border object not found!");
        }
    }
    
    void OnDestroy()
    {
        if (borderTexture != null)
        {
            DestroyImmediate(borderTexture);
        }
        
        if (countryIDTexture != null)
        {
            DestroyImmediate(countryIDTexture);
        }
        
        if (borderMaterial != null)
        {
            DestroyImmediate(borderMaterial);
        }

        if (countryColorBuffer != null)
        {
            countryColorBuffer.Release();
            countryColorBuffer = null;
        }
    }

    private void HandleIcoSphereDataLoaded()
    {
        // This method is no longer used.
    }

    public void InitializeAndGenerateBorders()
    {
        Debug.Log("=== CountryBorderRenderer InitializeAndGenerateBorders() Start ===");
        
        if (icoSphere == null)
        {
            Debug.LogError("CountryBorderRenderer: IcoSphere is null!");
            return;
        }
        
        Debug.Log($"CountryBorderRenderer: IcoSphere found: {icoSphere.name}");
        
        if (icoSphere.triangleDataList == null || icoSphere.triangleDataList.Count == 0)
        {
            Debug.LogError("CountryBorderRenderer: No triangle data available!");
            return;
        }
        
        Debug.Log($"CountryBorderRenderer: Triangle data available: {icoSphere.triangleDataList.Count} triangles");
        
        if (mapEditor == null)
        {
            Debug.LogError("CountryBorderRenderer: MapEditor is null!");
            return;
        }
        
        Debug.Log($"CountryBorderRenderer: MapEditor found: {mapEditor.name}");
        Debug.Log($"CountryBorderRenderer: MapEditor has CountryList: {mapEditor.countryList != null}");
        
        int countryCount = 0;
        if (mapEditor.countryList != null)
        {
            // This is the crucial fix: After loading save data, the indices on the
            // country objects are stale. We must rebuild them before using them.
            mapEditor.countryList.RebuildCountryIndices();
            
            countryCount = mapEditor.countryList.countries?.Count ?? 0;
            Debug.Log($"CountryBorderRenderer: CountryList has {countryCount} countries");
        }
        
        Debug.Log($"CountryBorderRenderer: Found {icoSphere.triangleDataList.Count} triangles, {countryCount} countries for border generation.");
        
        // Check how many triangles have countries assigned
        int trianglesWithCountries = 0;
        for (int i = 0; i < Mathf.Min(icoSphere.triangleDataList.Count, 100); i++) // Check first 100 for performance
        {
            if (icoSphere.triangleDataList[i].country != null)
            {
                trianglesWithCountries++;
            }
        }
        Debug.Log($"CountryBorderRenderer: Sample check - {trianglesWithCountries} out of first 100 triangles have countries assigned");
        
        GenerateBorderTexture();
        
        Debug.Log("=== CountryBorderRenderer InitializeAndGenerateBorders() End ===");
    }

    void UpdateCountryColorsInMaterial()
    {
        Debug.Log("=== CountryBorderRenderer UpdateCountryColorsInMaterial() Start ===");
        
        if (mapEditor == null)
        {
            Debug.LogError("CountryBorderRenderer: MapEditor is null in UpdateCountryColorsInMaterial!");
            return;
        }
        
        if (mapEditor.countryList == null)
        {
            Debug.LogError("CountryBorderRenderer: CountryList is null in UpdateCountryColorsInMaterial!");
            return;
        }
        
        if (borderMaterial == null)
        {
            Debug.LogError("CountryBorderRenderer: Border material is null in UpdateCountryColorsInMaterial!");
            return;
        }

        var countries = mapEditor.countryList.countries;
        int count = countries.Count;
        Debug.Log($"CountryBorderRenderer: Processing {count} countries for color array");

        // Use a fixed-size array matching the shader's MAX_COUNTRIES definition
        // This avoids resizing the GPU constant buffer, which can be unreliable.
        Vector4[] colors = new Vector4[256];

        // Index 0 is for funclaimed territory
        colors[0] = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        Debug.Log("CountryBorderRenderer: Set unclaimed color at index 0");

        for (int i = 0; i < count; i++)
        {
            // Ensure we don't go out of bounds of our fixed-size array
            if (i + 1 < colors.Length)
            {
                colors[i + 1] = countries[i].color;
                //Debug.Log($"CountryBorderRenderer: Set country {i} color at index {i + 1}: {countries[i].color}");
            }
            else
            {
                Debug.LogWarning($"CountryBorderRenderer: Country {i} would exceed color array bounds, skipping");
            }
        }
        
        // We still tell the shader the *actual* number of valid countries for correct clamping.
        if (countryColorBuffer == null || countryColorBuffer.count != colors.Length)
        {
            if (countryColorBuffer != null) countryColorBuffer.Release();
            countryColorBuffer = new ComputeBuffer(colors.Length, sizeof(float) * 4);
        }
        countryColorBuffer.SetData(colors);
        
        borderMaterial.SetInt("_CountryCount", count + 1);
        borderMaterial.SetBuffer("_CountryColors", countryColorBuffer);

        // Copy data to the debug array so it can be viewed in the inspector
        if (countryColorDebugArray == null || countryColorDebugArray.Length != colors.Length)
        {
            countryColorDebugArray = new Vector4[colors.Length];
        }
        System.Array.Copy(colors, countryColorDebugArray, colors.Length);

        Debug.Log($"CountryBorderRenderer: Updated material with a fixed-size color array (256) containing {count + 1} active country colors.");
        Debug.Log("=== CountryBorderRenderer UpdateCountryColorsInMaterial() End ===");
    }

    /// <summary>
    /// Comprehensive diagnostic method to check the current state of the border system
    /// </summary>
    [ContextMenu("Diagnose Border System")]
    public void DiagnoseBorderSystem()
    {
        Debug.Log("=== CountryBorderRenderer System Diagnosis ===");
        
        // Check basic components
        Debug.Log($"IcoSphere: {(icoSphere != null ? icoSphere.name : "NULL")}");
        Debug.Log($"MapEditor: {(mapEditor != null ? mapEditor.name : "NULL")}");
        Debug.Log($"Border Material: {(borderMaterial != null ? borderMaterial.name : "NULL")}");
        Debug.Log($"Border Object: {(borderObject != null ? borderObject.name : "NULL")}");
        Debug.Log($"Border Renderer: {(borderRenderer != null ? borderRenderer.name : "NULL")}");
        
        // Check data availability
        if (icoSphere != null)
        {
            Debug.Log($"Triangle Data: {(icoSphere.triangleDataList != null ? icoSphere.triangleDataList.Count.ToString() : "NULL")} triangles");
        }
        
        if (mapEditor != null && mapEditor.countryList != null)
        {
            Debug.Log($"Country Data: {mapEditor.countryList.countries?.Count ?? 0} countries");
        }
        
        // Check textures
        Debug.Log($"Border Texture: {(borderTexture != null ? $"{borderTexture.width}x{borderTexture.height}" : "NULL")}");
        Debug.Log($"Country ID Texture: {(countryIDTexture != null ? $"{countryIDTexture.width}x{countryIDTexture.height}" : "NULL")}");
        
        // Check material properties
        if (borderMaterial != null)
        {
            Debug.Log($"Material has BorderTex: {borderMaterial.GetTexture("_BorderTex") != null}");
            Debug.Log($"Material has CountryIDTex: {borderMaterial.GetTexture("_CountryIDTex") != null}");
            Debug.Log($"Material has CountryCount: {borderMaterial.GetInt("_CountryCount")}");
            
            var colors = borderMaterial.GetVectorArray("_CountryColors");
            Debug.Log($"Material has CountryColors: {(colors != null ? colors.Length.ToString() : "NULL")} colors");
        }
        
        // Check border object state
        if (borderObject != null)
        {
            Debug.Log($"Border Object Active: {borderObject.activeInHierarchy}");
            Debug.Log($"Border Object Layer: {borderObject.layer}");
            
            var renderer = borderObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Debug.Log($"Border Renderer Enabled: {renderer.enabled}");
                Debug.Log($"Border Renderer Material: {renderer.material}");
                Debug.Log($"Border Renderer Shared Material: {renderer.sharedMaterial}");
            }
            
            var meshFilter = borderObject.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                Debug.Log($"Border Mesh: {(meshFilter.mesh != null ? $"{meshFilter.mesh.vertexCount} vertices" : "NULL")}");
            }
        }
        
        // Check state flags
        Debug.Log($"Borders Generated: {bordersGenerated}");
        Debug.Log($"Enable Borders: {enableBorders}");
        
        // Check for common issues
        if (mapEditor == null)
        {
            Debug.LogError("ISSUE: MapEditor is null - borders cannot be colored correctly");
        }
        
        if (mapEditor != null && mapEditor.countryList == null)
        {
            Debug.LogError("ISSUE: CountryList is null - borders cannot be colored correctly");
        }
        
        if (borderMaterial == null)
        {
            Debug.LogError("ISSUE: Border material is null - borders cannot be rendered");
        }
        
        if (borderObject == null)
        {
            Debug.LogError("ISSUE: Border object is null - borders cannot be rendered");
        }
        
        if (borderTexture == null)
        {
            Debug.LogError("ISSUE: Border texture is null - borders cannot be rendered");
        }
        
        if (countryIDTexture == null)
        {
            Debug.LogError("ISSUE: Country ID texture is null - borders cannot be colored");
        }
        
        if (icoSphere != null && icoSphere.triangleDataList != null && icoSphere.triangleDataList.Count == 0)
        {
            Debug.LogError("ISSUE: No triangle data available - borders cannot be generated");
        }
        
        Debug.Log("=== End Diagnosis ===");
    }

    Color[] SmoothBorderTexture(Color[] pixels)
    {
        Color[] smoothed = new Color[pixels.Length];
        System.Array.Copy(pixels, smoothed, pixels.Length);
        
        for (int iteration = 0; iteration < smoothingIterations; iteration++)
        {
            for (int y = 0; y < borderTextureResolution; y++)
            {
                for (int x = 0; x < borderTextureResolution; x++)
                {
                    int index = y * borderTextureResolution + x;
                    
                    // Sample neighbors
                    float sum = 0;
                    int count = 0;
                    
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = (x + dx + borderTextureResolution) % borderTextureResolution;
                            int ny = (y + dy + borderTextureResolution) % borderTextureResolution;
                            int nIndex = ny * borderTextureResolution + nx;
                            
                            if (nIndex >= 0 && nIndex < pixels.Length)
                            {
                                sum += pixels[nIndex].r;
                                count++;
                            }
                        }
                    }
                    
                    if (count > 0)
                    {
                        float average = sum / count;
                        smoothed[index] = new Color(average, average, average, average);
                    }
                }
            }
            
            // Swap arrays for next iteration
            var temp = pixels;
            pixels = smoothed;
            smoothed = temp;
        }
        
        return pixels;
    }
} 