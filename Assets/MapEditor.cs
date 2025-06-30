using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor;

public enum EditorMode
{
    Terrain,
    Country
    // Future modes like Borders, Rivers, etc., can be added here.
}

public class MapEditor : MonoBehaviour
{
    [Header("Editor Mode")]
    public EditorMode currentMode = EditorMode.Terrain;

    [Header("Editor Resources")]
    public Shader terrainPreviewShader;

    [Header("Editor References")]
    public IcoSphere icoSphere;
    public IDPicker idPicker;
    public Camera editorCamera;
    public TriangleDataSaver triangleDataSaver;

    [Header("Brush Settings")]
    public int selectedTerrainType = 0;
    public float brushSize = 200.0f; 
    public float brushFalloff = 0.5f;
    public bool useFalloff = true;

    [Header("Country Settings")]
    public Country selectedCountry = null;
    public CountryList countryList = new CountryList();
    [Header("Country Painting Settings")]
    public bool onlyPaintOverUnclaimed = true; // Only paint over triangles with no country assigned
    public float countryPreviewAlpha = 0.5f; // Alpha for country preview overlay

    [Header("Visual Feedback")]
    public bool showBrushPreview = true;
    public Color brushPreviewColor = new Color(1, 1, 0, 0.3f);
    
    [Header("Editor State")]
    public bool useNewInputSystem = true;
    
    // --- Private State ---
    private bool isEditing = false;
    private bool isPainting = false;
    private Vector3 lastPaintPosition;
    private Dictionary<int, int> originalTerrainTypes = new Dictionary<int, int>();
    private GameObject brushPreviewInstance;
    private string statusMessage = "Ready. Press E to toggle.";
    private bool isDirty = false; // To track if we have un-applied changes

    // --- Materials ---
    // The main splat map material is no longer cached here.
    // It will be recreated from scratch on Apply.
    private Dictionary<EditorMode, Material> previewMaterials = new Dictionary<EditorMode, Material>();
    
    // --- Preview Colors ---
    // Colors are now dynamically read from materials instead of being hardcoded
    private Color[] terrainPreviewColors = new Color[0]; // Will be populated dynamically
    
    // --- Preview Mode ---
    public enum PreviewMode
    {
        TerrainType,
        Country,
        TriangleID,
        AdjacencyCount,
        Area,
        Latitude,
        Longitude
    }
    
    [Header("Preview Settings")]
    public PreviewMode currentPreviewMode = PreviewMode.TerrainType;

    // --- Input System ---
    private Keyboard keyboard;
    private Mouse mouse;
    private readonly Key[] digitKeys = { Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };

    [Header("UI Settings")]
    private Rect editorWindowRect;
    private Vector2 editorScrollPosition;
    
    // --- Unity Methods ---
    void Awake()
    {
        // Auto-find components if not assigned
        if (icoSphere == null) icoSphere = UnityEngine.Object.FindFirstObjectByType<IcoSphere>();
        if (idPicker == null) idPicker = UnityEngine.Object.FindFirstObjectByType<IDPicker>();
        if (editorCamera == null) editorCamera = Camera.main;
        if (triangleDataSaver == null) triangleDataSaver = UnityEngine.Object.FindFirstObjectByType<TriangleDataSaver>();

        // Initialize preview materials
        InitializePreviewMaterials();
        
        // Check if save data was loaded on startup
        CheckIfSaveDataWasLoaded();
    }

    void OnEnable()
    {
        // Initialize the new input system if enabled
        if (useNewInputSystem)
        {
            keyboard = InputSystem.GetDevice<Keyboard>();
            mouse = InputSystem.GetDevice<Mouse>();
        }

        // Initialize UI window rectangle
        editorWindowRect = new Rect(10, 10, 400, 900); // Increased from 600 to 900 (50% taller)

        // Setup brush preview
        if (brushPreviewInstance == null)
        {
            CreateBrushPreview();
        }
    }

    void OnDisable()
    {
        // Clean up brush preview
        if (brushPreviewInstance != null)
        {
            Destroy(brushPreviewInstance);
        }
    }

    void Update()
    {
        // Only run when in play mode
        if (!Application.isPlaying) return;

        HandleInput();

        if (isEditing && showBrushPreview)
        {
            UpdateBrushPreview();
        }
    }

    void OnGUI()
    {
        if (!isEditing || !Application.isPlaying) return;

        // Draw the main editor window
        editorWindowRect = GUILayout.Window(0, editorWindowRect, DrawEditorWindow, "Map Editor");
    }

    void OnDrawGizmos()
    {
        if (!isEditing || currentPreviewMode != PreviewMode.Country) return;
        
        // Only draw gizmos in editor mode (Scene view)
        #if UNITY_EDITOR
        // Draw country dots in the center of each triangle
        if (icoSphere != null && icoSphere.triangleDataList != null)
        {
            foreach (var triangle in icoSphere.triangleDataList)
            {
                if (triangle.country != null)
                {
                    Vector3 center = triangle.GetCenter();
                    
                    // Draw country dot
                    Gizmos.color = triangle.country.color;
                    Gizmos.DrawSphere(center, 0.1f); // Small dot in triangle center
                    
                    // Draw country name
                    if (UnityEditor.SceneView.lastActiveSceneView != null)
                    {
                        GUIStyle labelStyle = new GUIStyle();
                        labelStyle.fontSize = 8;
                        labelStyle.normal.textColor = Color.white;
                        UnityEditor.Handles.Label(center, triangle.country.name, labelStyle);
                    }
                }
            }
        }
        #endif
    }

    void OnRenderObject()
    {
        if (!isEditing || currentPreviewMode != PreviewMode.Country) 
        {
            return;
        }
        if (!Application.isPlaying) return; // Only draw in play mode
        
        // Draw country outlines in Game view during play mode
        if (icoSphere != null && icoSphere.triangleDataList != null)
        {
            int countryCount = 0;
            foreach (var triangle in icoSphere.triangleDataList)
            {
                if (triangle.country != null)
                {
                    countryCount++;
                }
            }
             
            // Draw triangle outlines for countries
            DrawCountryOutlines();
            
            // Draw preview overlay for selected country when painting
            if (isPainting)
            {
                DrawCountryPreviewOverlay();
            }
        }
        else
        {
            Debug.Log("OnRenderObject: icoSphere or triangleDataList is null");
        }
    }
    
    void DrawCountryOutlines()
    {
        // Create a simple unlit shader for drawing lines
        if (lineMaterial == null)
        {
            CreateLineMaterial();
        }
        
        if (lineMaterial != null)
        {
            lineMaterial.SetPass(0);
            
            // Draw smaller triangles inside each triangle to show country ownership
            GL.Begin(GL.LINES);
            foreach (var triangle in icoSphere.triangleDataList)
            {
                if (triangle.country != null)
                {
                    Color countryColor = triangle.country.color;
                    
                    // Debug: Check if color is valid (not black, white, or transparent)
                    if (countryColor.r == 0 && countryColor.g == 0 && countryColor.b == 0)
                    {
                        Debug.LogWarning($"MapEditor: Country '{triangle.country.name}' has black color, using default");
                        countryColor = Color.white;
                    }
                    else if (countryColor.r == 1 && countryColor.g == 1 && countryColor.b == 1)
                    {
                        // If it's still white (default), initialize a random color
                        triangle.country.InitializeRandomColor();
                        countryColor = triangle.country.color;
                    }
                    
                    GL.Color(countryColor);
                    
                    // Calculate center of the triangle
                    Vector3 center = (triangle.a + triangle.b + triangle.c) / 3f;
                    
                    // Calculate smaller triangle vertices (50% smaller)
                    float scale = 0.5f;
                    Vector3 smallA = center + (triangle.a - center) * scale;
                    Vector3 smallB = center + (triangle.b - center) * scale;
                    Vector3 smallC = center + (triangle.c - center) * scale;
                    
                    // Draw the smaller triangle outline
                    GL.Vertex(smallA);
                    GL.Vertex(smallB);
                    
                    GL.Vertex(smallB);
                    GL.Vertex(smallC);
                    
                    GL.Vertex(smallC);
                    GL.Vertex(smallA);
                }
            }
            GL.End();
        }
    }
    
    private Material lineMaterial;
    
    void CreateLineMaterial()
    {
        Shader lineShader = Shader.Find("Custom/Line");
        if (lineShader == null)
        {
            Debug.LogError("Custom/Line shader not found! Please create it in Assets/Shaders/LineShader.shader");
            return;
        }
        lineMaterial = new Material(lineShader);
    }

    // --- UI Drawing ---

    void DrawEditorWindow(int windowId)
    {
        // Allow the window to be dragged
        GUI.DragWindow(new Rect(0, 0, 10000, 20));

        editorScrollPosition = GUILayout.BeginScrollView(editorScrollPosition);

        // --- Editor Mode Section ---
        GUILayout.Label("Editor Mode", EditorStyles.boldLabel);
        // This could be replaced with a dropdown or buttons in the future
        GUILayout.Label($"Current Mode: {currentMode}");
        
        // Add mode switching buttons
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Terrain Mode"))
        {
            SwitchToMode(EditorMode.Terrain);
        }
        if (GUILayout.Button("Country Mode"))
        {
            SwitchToMode(EditorMode.Country);
        }
        GUILayout.EndHorizontal();
        
        // --- Preview Mode Section ---
        GUILayout.Label("Preview Mode", EditorStyles.boldLabel);
        GUILayout.Label($"Current Preview: {currentPreviewMode}");
        if (GUILayout.Button("Cycle Preview Mode"))
        {
            CyclePreviewMode();
        }
        
        // Quick access buttons for common preview modes
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Terrain"))
        {
            currentPreviewMode = PreviewMode.TerrainType;
            RefreshMeshColors();
            UpdateStatus("Switched to Terrain preview mode");
        }
        if (GUILayout.Button("Country"))
        {
            currentPreviewMode = PreviewMode.Country;
            RefreshMeshColors();
            UpdateStatus("Switched to Country preview mode");
        }
        GUILayout.EndHorizontal();
        
        GUILayout.Space(10);
        
        // --- File Section ---
        GUILayout.Label("File", EditorStyles.boldLabel);
        
        // Add a button to apply changes
        if (isDirty)
        {
            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("Apply Changes"))
            {
                ApplyChangesAndRebuildMap();
            }
            GUI.backgroundColor = Color.white;
        }

        if (GUILayout.Button("Save Map")) SaveMap();
        if (GUILayout.Button("Load Map")) LoadMap();
        if (GUILayout.Button("Undo Last Paint")) Undo();
        
        GUILayout.Space(10);
        
        // --- Brush Settings Section ---
        GUILayout.Label("Brush Settings", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Brush Size: {brushSize:F1}", GUILayout.Width(100));
        brushSize = GUILayout.HorizontalSlider(brushSize, 10.0f, 1000.0f);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Smaller [-")) DecreaseBrushSize();
        if (GUILayout.Button("Larger ]+")) IncreaseBrushSize();
        GUILayout.EndHorizontal();
        
        useFalloff = GUILayout.Toggle(useFalloff, "Enable Brush Falloff");
        if (useFalloff)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Falloff: {brushFalloff:F2}", GUILayout.Width(100));
            brushFalloff = GUILayout.HorizontalSlider(brushFalloff, 0.1f, 1.0f);
            GUILayout.EndHorizontal();
        }
        
        GUILayout.Space(10);

        // --- Country Selection Section (only show in Country mode) ---
        if (currentMode == EditorMode.Country)
        {
            GUILayout.Label("Country Selection", EditorStyles.boldLabel);
            
            // Create new country button
            if (GUILayout.Button("Create New Country"))
            {
                CreateNewCountry();
            }
            
            // Country painting settings
            GUILayout.Space(5);
            onlyPaintOverUnclaimed = GUILayout.Toggle(onlyPaintOverUnclaimed, "Only Paint Over Unclaimed Areas");
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Preview Alpha:", GUILayout.Width(100));
            countryPreviewAlpha = GUILayout.HorizontalSlider(countryPreviewAlpha, 0.1f, 1.0f);
            GUILayout.Label($"{countryPreviewAlpha:F2}", GUILayout.Width(40));
            GUILayout.EndHorizontal();
            
            // Country selection dropdown
            if (countryList.countries.Count > 0)
            {
                GUILayout.Label($"Selected Country: {(selectedCountry != null ? selectedCountry.name : "None (Erase)")}");
                
                // Add "None" option for erasing
                GUILayout.BeginHorizontal();
                GUI.backgroundColor = Color.gray;
                GUILayout.Box("", GUILayout.Width(20), GUILayout.Height(20));
                GUI.backgroundColor = Color.white;
                
                if (GUILayout.Button("None (Erase)", GUILayout.ExpandWidth(true)))
                {
                    SelectCountry(null);
                }
                GUILayout.EndHorizontal();
                
                // Show country list with selection buttons
                for (int i = 0; i < countryList.countries.Count; i++)
                {
                    var country = countryList.countries[i];
                    GUILayout.BeginHorizontal();
                    
                    // Color preview
                    GUI.backgroundColor = country.color;
                    GUILayout.Box("", GUILayout.Width(20), GUILayout.Height(20));
                    GUI.backgroundColor = Color.white;
                    
                    // Selection button
                    if (GUILayout.Button(country.name, GUILayout.ExpandWidth(true)))
                    {
                        SelectCountry(country);
                    }
                    
                    // Remove button
                    if (GUILayout.Button("X", GUILayout.Width(25)))
                    {
                        RemoveCountry(country);
                    }
                    
                    GUILayout.EndHorizontal();
                }
            }
            else
            {
                GUILayout.Label("No countries available. Create one to start editing.");
            }
        }

        // --- Terrain Selection Section ---
        GUILayout.Label("Terrain Type", EditorStyles.boldLabel);
        GUILayout.Label($"Selected: {selectedTerrainType} (Press 0-9)");

        if (icoSphere != null && icoSphere.terrainTypes.Count > 0)
        {
            int buttonSize = 40;
            int buttonsPerRow = 5;
            
            for (int i = 0; i < icoSphere.terrainTypes.Count; i++)
            {
                if (i % buttonsPerRow == 0)
                {
                    if (i > 0) GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }

                var terrainType = icoSphere.terrainTypes[i];
                Texture2D preview = terrainType.GetTexture() ?? AssetPreview.GetAssetPreview(terrainType.material) ?? Texture2D.whiteTexture;

                GUI.backgroundColor = (i == selectedTerrainType) ? Color.green : Color.white;
                if (GUILayout.Button(new GUIContent(preview, $"Select {terrainType.name} (ID: {i})"), GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
                {
                    SelectTerrainType(i);
                }
            }
            GUILayout.EndHorizontal();
        }
        
        GUILayout.Space(10);

        // --- Visual Feedback Section ---
        GUILayout.Label("Visual Feedback", EditorStyles.boldLabel);
        showBrushPreview = GUILayout.Toggle(showBrushPreview, "Show Brush Preview");
        
        GUILayout.Space(10);

        // --- Status Message ---
        GUILayout.Label($"Status: {statusMessage}", EditorStyles.wordWrappedLabel);
        
        GUILayout.EndScrollView();
    }
    
    // --- Public API ---

    /// <summary>
    /// Switches to the specified editor mode and updates the material accordingly
    /// </summary>
    public void SwitchToMode(EditorMode newMode)
    {
        if (currentMode == newMode) return;
        
        currentMode = newMode;
        
        if (isEditing)
        {
            // If we're currently editing, update the material immediately
            var renderer = icoSphere.GetComponent<MeshRenderer>();
            
            if (newMode == EditorMode.Country)
            {
                // For country mode, use splatmap material
                if (previewMaterials[EditorMode.Country] == null)
                {
                    icoSphere.CreateAndApplyNewSplatMaterial();
                    previewMaterials[EditorMode.Country] = renderer.material;
                }
                else
                {
                    renderer.material = previewMaterials[EditorMode.Country];
                }
                
                // Clear vertex colors for splatmap material
                var mesh = icoSphere.GetComponent<MeshFilter>().mesh;
                if (mesh != null && mesh.colors.Length > 0)
                {
                    var colors = new Color[mesh.vertexCount];
                    for(int i = 0; i < colors.Length; i++)
                    {
                        colors[i] = Color.white;
                    }
                    mesh.colors = colors;
                }
                
                UpdateStatus("Switched to Country editing mode with textured terrain.");
            }
            else
            {
                // For terrain mode, use simple preview material
                if (previewMaterials.ContainsKey(EditorMode.Terrain))
                {
                    renderer.material = previewMaterials[EditorMode.Terrain];
                    RefreshMeshColors();
                    UpdateStatus("Switched to Terrain editing mode.");
                }
            }
        }
        else
        {
            UpdateStatus($"Switched to {newMode} mode.");
        }
    }

    [ContextMenu("Toggle Map Editor")]
    public void ToggleEditor()
    {
        isEditing = !isEditing;
        UpdateStatus(isEditing ? "Editor enabled." : "Editor disabled. Press E to toggle.");

        if (isEditing)
        {
            // Refresh terrain preview colors when enabling editor
            UpdateTerrainPreviewColors();
        }
        else
        {
            // When disabling editor, restore the original splatmap material
            if (icoSphere != null)
            {
                icoSphere.CreateAndApplyNewSplatMaterial();
                UpdateStatus("Restored original splatmap material.");
            }
        }

        if (brushPreviewInstance != null)
        {
            brushPreviewInstance.SetActive(isEditing && showBrushPreview);
        }
    }

    // --- Input Handling ---

    public bool IsMouseOverGUI()
    {
        if (!isEditing) return false;
        
        Vector2 mousePos = useNewInputSystem ? mouse.position.ReadValue() : (Vector2)Input.mousePosition;
        mousePos.y = Screen.height - mousePos.y; // Invert Y for GUI space
        
        return editorWindowRect.Contains(mousePos);
    }
    
    void HandleInput()
    {
        bool togglePressed = useNewInputSystem ? (keyboard != null && keyboard.eKey.wasPressedThisFrame) : Input.GetKeyDown(KeyCode.E);

        if (togglePressed)
        {
            ToggleEditor();
        }
        
        if (!isEditing) return;
        
        if (IsMouseOverGUI())
        {
            if (isPainting) StopPainting(); // Stop painting if mouse enters UI
            return;
        }

        bool startPaint = useNewInputSystem ? (mouse != null && mouse.leftButton.wasPressedThisFrame) : Input.GetMouseButtonDown(0);
        bool painting = useNewInputSystem ? (mouse != null && mouse.leftButton.isPressed) : Input.GetMouseButton(0);
        bool endPaint = useNewInputSystem ? (mouse != null && mouse.leftButton.wasReleasedThisFrame) : Input.GetMouseButtonUp(0);

        if (startPaint) StartPainting();
        else if (painting) ContinuePainting();
        else if (endPaint) StopPainting();

        bool decreaseBrush = useNewInputSystem ? (keyboard != null && keyboard.leftBracketKey.wasPressedThisFrame) : Input.GetKeyDown(KeyCode.LeftBracket);
        bool increaseBrush = useNewInputSystem ? (keyboard != null && keyboard.rightBracketKey.wasPressedThisFrame) : Input.GetKeyDown(KeyCode.RightBracket);

        if (decreaseBrush) DecreaseBrushSize();
        if (increaseBrush) IncreaseBrushSize();
        
        for (int i = 0; i < digitKeys.Length; i++)
        {
            bool keypressed = useNewInputSystem ? (keyboard != null && keyboard[digitKeys[i]].wasPressedThisFrame) : Input.GetKeyDown(KeyCode.Alpha0 + i);
            if (keypressed)
            {
                SelectTerrainType(i);
            }
        }
    }

    // --- Painting Logic ---
    
    void StartPainting()
    {
        isPainting = true;
        originalTerrainTypes.Clear();

        // Ensure preview materials are initialized
        if (!previewMaterials.ContainsKey(currentMode))
        {
            InitializePreviewMaterials();
        }

        // Switch to the correct preview material for the current mode
        var renderer = icoSphere.GetComponent<MeshRenderer>();
        
        if (currentMode == EditorMode.Country)
        {
            // For country mode, use the existing splatmap material to show textured terrain
            // Country borders will be drawn separately in OnRenderObject
            if (previewMaterials[EditorMode.Country] == null)
            {
                // Just use the current material, don't regenerate the splat map
                previewMaterials[EditorMode.Country] = renderer.material;
                Debug.Log("MapEditor: Using existing splatmap material for country preview mode");
            }
            else
            {
                renderer.material = previewMaterials[EditorMode.Country];
            }
            
            // Clear vertex colors to white since splatmap material doesn't use them
            var mesh = icoSphere.GetComponent<MeshFilter>().mesh;
            if (mesh != null && mesh.colors.Length > 0)
            {
                var colors = new Color[mesh.vertexCount];
                for(int i = 0; i < colors.Length; i++)
                {
                    colors[i] = Color.white;
                }
                mesh.colors = colors;
            }
            
            UpdateStatus("Switched to Country preview mode with textured terrain.");
        }
        else
        {
            // For terrain mode, use the simple preview material
            if (previewMaterials.ContainsKey(currentMode))
            {
                var previewMat = previewMaterials[currentMode];
                if (renderer.material != previewMat)
                {
                    renderer.material = previewMat;
                    // Refresh ALL mesh colors with preview colors when entering preview mode
                    RefreshMeshColors();
                    UpdateStatus($"Switched to {currentMode} preview mode.");
                }
            }
            else
            {
                UpdateStatus($"Error: No preview material for {currentMode} mode.");
                isPainting = false;
                return;
            }
        }

        // Auto-switch to country preview mode when in country editing mode
        if (currentMode == EditorMode.Country && currentPreviewMode != PreviewMode.Country)
        {
            currentPreviewMode = PreviewMode.Country;
            UpdateStatus("Auto-switched to country preview mode.");
        }

        Vector3 pos = GetPaintPosition();
        if (pos != Vector3.zero)
        {
            PaintAtPosition(pos);
        }
    }

    void ContinuePainting()
    {
        if (!isPainting) return;
        
        Vector3 pos = GetPaintPosition();
        if (pos != Vector3.zero && Vector3.Distance(pos, lastPaintPosition) > 0.1f)
        {
             PaintAtPosition(pos);
        }
    }

    void StopPainting()
    {
        if (!isPainting) return;
        isPainting = false;
        UpdateStatus($"Finished painting. Modified {originalTerrainTypes.Count} unique triangles. Click 'Apply' to finalize.");
    }
    
    Vector3 GetPaintPosition()
    {
        if (idPicker == null)
        {
            UpdateStatus("Error: IDPicker not found!");
            return Vector3.zero;
        }

        int triangleId = idPicker.GetSelectedTriangleID();
        if (triangleId != -1 && triangleId < icoSphere.triangleDataList.Count)
        {
            var tri = icoSphere.triangleDataList[triangleId];
            Vector3 center = (tri.a + tri.b + tri.c) / 3f;
            return center;
        }
        
        return Vector3.zero; // Return zero if no valid triangle is hit
    }

    void PaintAtPosition(Vector3 position)
    {
        if (icoSphere == null || icoSphere.triangleDataList == null || position == Vector3.zero) return;
        
        var mesh = icoSphere.GetComponent<MeshFilter>().mesh;
        var colors = mesh.colors;
        int paintedCount = 0;
        int inRadiusCount = 0;
        float brushRadiusSquared = brushSize * brushSize;
        bool firstPainted = true; // Debug flag
        
        for (int i = 0; i < icoSphere.triangleDataList.Count; i++)
        {
            var triangle = icoSphere.triangleDataList[i];
            Vector3 triangleCenter = (triangle.a + triangle.b + triangle.c) / 3f;
            float distanceSquared = Vector3.SqrMagnitude(triangleCenter - position);
            
            if (distanceSquared <= brushRadiusSquared)
            {
                inRadiusCount++;
                float strength = 1f;
                if (useFalloff)
                {
                    float distance = Mathf.Sqrt(distanceSquared);
                    strength = 1f - Mathf.Clamp01(distance / brushSize);
                    strength = Mathf.Pow(strength, brushFalloff * 2);
                }
                
                if (strength > 0.1f)
                {
                    // Branch logic based on the current editor mode
                    switch (currentMode)
                    {
                        case EditorMode.Terrain:
                            if (firstPainted) {
                                Debug.Log($"[DEBUG] BEFORE PAINT: Triangle {i} terrain was {icoSphere.triangleDataList[i].terrainType}");
                            }
                            PaintTerrain(i, colors);
                            if (firstPainted) {
                                Debug.Log($"[DEBUG] AFTER PAINT: Triangle {i} terrain is now {icoSphere.triangleDataList[i].terrainType}");
                                firstPainted = false;
                            }
                            break;
                        case EditorMode.Country:
                            PaintCountry(i, colors);
                            break;
                        // Future modes like PaintBorders would be called here
                    }
                    paintedCount++;
                    isDirty = true;
                }
            }
        }
        
        if (paintedCount > 0)
        {
            Debug.Log($"MapEditor: Found {inRadiusCount} triangles in brush radius. Painted {paintedCount}.");
            
            // Only apply vertex colors for terrain mode (country mode uses splatmap material)
            if (currentMode == EditorMode.Terrain)
            {
                mesh.colors = colors; // Apply new colors to the mesh
            }
            
            lastPaintPosition = position; // Update the last painted position
        }
    }

    /// <summary>
    /// Handles the logic for changing terrain data and setting preview colors.
    /// </summary>
    private void PaintTerrain(int triangleIndex, Color[] colors)
    {
        var triangle = icoSphere.triangleDataList[triangleIndex];

        if (!originalTerrainTypes.ContainsKey(triangleIndex))
        {
            originalTerrainTypes[triangleIndex] = triangle.terrainType;
        }
        triangle.terrainType = selectedTerrainType;
        
        // Set preview color directly on the mesh vertices
        int baseVertexIndex = triangleIndex * 3;
        Color previewColor = GetPreviewColorForTriangle(triangleIndex);
        
        if (baseVertexIndex + 2 < colors.Length)
        {
            colors[baseVertexIndex] = previewColor;
            colors[baseVertexIndex + 1] = previewColor;
            colors[baseVertexIndex + 2] = previewColor;
        }
    }

    // --- Brush & UI Helpers ---

    void CreateBrushPreview()
    {
        if (idPicker == null)
        {
            Debug.LogError("MapEditor: IDPicker is not assigned. Cannot create brush preview or paint.");
            return;
        }

        var previewObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        previewObject.name = "BrushPreview";
        Destroy(previewObject.GetComponent<Collider>()); // No need for collider
        
        var renderer = previewObject.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Legacy Shaders/Transparent/Diffuse"));
        renderer.material.color = brushPreviewColor;

        brushPreviewInstance = previewObject;
        brushPreviewInstance.SetActive(false);
    }
     
    void UpdateBrushPreview()
    {
        if (brushPreviewInstance == null) return;
        
        Vector3 paintPosition = GetPaintPosition();
        bool hasPosition = paintPosition != Vector3.zero;

        brushPreviewInstance.SetActive(isEditing && hasPosition);

        if (hasPosition)
        {
             brushPreviewInstance.transform.position = paintPosition;
             brushPreviewInstance.transform.localScale = Vector3.one * brushSize * 2f;
        }
    }
    
    public void SelectTerrainType(int terrainType)
    {
        if (icoSphere == null) return;
        selectedTerrainType = Mathf.Clamp(terrainType, 0, icoSphere.terrainTypes.Count - 1);
        UpdateStatus($"Selected terrain type {selectedTerrainType}.");
    }

    public void IncreaseBrushSize()
    {
        brushSize = Mathf.Min(brushSize + 50.0f, 1000.0f);
        UpdateStatus($"Brush size: {brushSize:F1}");
    }
     
    public void DecreaseBrushSize()
    {
        brushSize = Mathf.Max(brushSize - 50.0f, 10.0f);
        UpdateStatus($"Brush size: {brushSize:F1}");
    }

    public void Undo()
    {
        if (originalTerrainTypes.Count == 0)
        {
            UpdateStatus("Nothing to undo.");
            return;
        }

        // This undo logic is currently specific to terrain.
        // A more robust undo system would be needed for multiple edit modes.
        switch(currentMode)
        {
            case EditorMode.Terrain:
                var mesh = icoSphere.GetComponent<MeshFilter>().mesh;
                var colors = mesh.colors;
                
                foreach (var entry in originalTerrainTypes)
                {
                    if (entry.Key < icoSphere.triangleDataList.Count)
                    {
                        icoSphere.triangleDataList[entry.Key].terrainType = entry.Value;
                        
                        // Restore original triangle ID colors
                        int baseVertexIndex = entry.Key * 3;
                        if (baseVertexIndex + 2 < colors.Length)
                        {
                            Color originalColor = new Color(
                                ((entry.Key & 0xFF) / 255.0f),
                                (((entry.Key >> 8) & 0xFF) / 255.0f),
                                (((entry.Key >> 16) & 0xFF) / 255.0f),
                                1.0f
                            );
                            colors[baseVertexIndex] = originalColor;
                            colors[baseVertexIndex + 1] = originalColor;
                            colors[baseVertexIndex + 2] = originalColor;
                        }
                    }
                }
                
                mesh.colors = colors;
                break;
                
            case EditorMode.Country:
                // For country mode, we don't modify vertex colors since we're using the splatmap material
                // Just restore the country assignments
                foreach (var entry in originalTerrainTypes)
                {
                    if (entry.Key < icoSphere.triangleDataList.Count)
                    {
                        var triangle = icoSphere.triangleDataList[entry.Key];
                        
                        // Restore original country assignment
                        if (entry.Value == -1)
                        {
                            triangle.RemoveFromCountry();
                        }
                        else if (entry.Value >= 0 && entry.Value < countryList.countries.Count)
                        {
                            triangle.AssignToCountry(countryList.countries[entry.Value]);
                        }
                    }
                }
                
                // Update country borders after undoing
                icoSphere.UpdateCountryBorders();
                break;
        }
        
        UpdateStatus($"Undid last paint operation ({originalTerrainTypes.Count} triangles).");
        originalTerrainTypes.Clear();
        isDirty = false;
    }

    // --- Data Management ---
    
    public void SaveMap()
    {
        if (isDirty)
        {
            ApplyChangesAndRebuildMap();
            UpdateStatus("Applied pending changes before saving.");
        }

        if (triangleDataSaver == null)
        {
            UpdateStatus("Error: No TriangleDataSaver found!");
            return;
        }
        triangleDataSaver.SaveTriangleData();
        UpdateStatus($"Map saved successfully.");
    }
     
    public void LoadMap()
    {
        if (triangleDataSaver == null)
        {
            UpdateStatus("Error: No TriangleDataSaver found!");
            return;
        }
        triangleDataSaver.LoadTriangleData();
        
        // Refresh mesh colors after loading
        RefreshMeshColors();
        
        UpdateStatus($"Loaded map.");
    }

    void UpdateStatus(string message)
    {
        statusMessage = message;
        Debug.Log($"MapEditor: {message}");
    }

    public void ApplyChangesAndRebuildMap()
    {
        UpdateStatus("Applying changes and rebuilding map...");
        
        // This single method now handles creating and applying a new, fully configured material.
        icoSphere.CreateAndApplyNewSplatMaterial();
        
        // Clear vertex colors on the mesh to white, as the new material doesn't use them.
        var mesh = icoSphere.GetComponent<MeshFilter>().mesh;
        if (mesh != null && mesh.colors.Length > 0)
        {
            var colors = new Color[mesh.vertexCount];
            for(int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.white;
            }
            mesh.colors = colors;
        }

        // Ensure all triangle data is properly synchronized with countries
        if (icoSphere.triangleDataList != null)
        {
            foreach (var triangle in icoSphere.triangleDataList)
            {
                // This ensures the bidirectional relationship is maintained
                if (triangle.country != null && !triangle.country.territory.Contains(triangle))
                {
                    triangle.country.AddTriangleInternal(triangle);
                }
            }
        }

        // Update country borders after applying changes
        icoSphere.UpdateCountryBorders();

        isDirty = false;
        UpdateStatus("Changes applied successfully!");
    }

    void RestoreTriangleIDColors()
    {
        if (icoSphere == null || icoSphere.triangleDataList == null) return;
        
        var mesh = icoSphere.GetComponent<MeshFilter>().mesh;
        var colors = mesh.colors;
        
        for (int i = 0; i < icoSphere.triangleDataList.Count; i++)
        {
            Color originalColor = new Color(
                ((i & 0xFF) / 255.0f),
                (((i >> 8) & 0xFF) / 255.0f),
                (((i >> 16) & 0xFF) / 255.0f),
                1.0f
            );
            int baseVertexIndex = i * 3;
            if (baseVertexIndex + 2 < colors.Length)
            {
                colors[baseVertexIndex] = originalColor;
                colors[baseVertexIndex + 1] = originalColor;
                colors[baseVertexIndex + 2] = originalColor;
            }
        }
        
        mesh.colors = colors;
    }

    Color GetPreviewColorForTriangle(int triangleIndex)
    {
        if (icoSphere == null || triangleIndex >= icoSphere.triangleDataList.Count) 
            return Color.black;
            
        var triangle = icoSphere.triangleDataList[triangleIndex];
        
        switch (currentPreviewMode)
        {
            case PreviewMode.TerrainType:
                int terrainType = triangle.terrainType;
                if (terrainType < terrainPreviewColors.Length)
                    return terrainPreviewColors[terrainType];
                return Color.magenta; // Unknown terrain type
                
            case PreviewMode.Country:
                // Show terrain colors (country dots will be drawn separately in OnDrawGizmos)
                int countryTerrainType = triangle.terrainType;
                if (countryTerrainType < terrainPreviewColors.Length)
                    return terrainPreviewColors[countryTerrainType];
                return Color.magenta; // Unknown terrain type
                
            case PreviewMode.TriangleID:
                // Use triangle ID to generate a color
                return new Color(
                    (triangleIndex & 0xFF) / 255f,
                    ((triangleIndex >> 8) & 0xFF) / 255f,
                    ((triangleIndex >> 16) & 0xFF) / 255f,
                    1f
                );
                
            case PreviewMode.AdjacencyCount:
                // Color based on number of adjacent triangles
                float adjacencyRatio = triangle.adjacentTriangles.Count / 10f; // Normalize to 0-1
                return new Color(adjacencyRatio, 1f - adjacencyRatio, 0f, 1f);
                
            case PreviewMode.Area:
                // Color based on triangle area
                var (ab, bc, ca) = triangle.GetSideLengths();
                float area = (ab + bc + ca) / 3f; // Average side length as area proxy
                float areaRatio = Mathf.Clamp01(area / 100f); // Normalize
                return new Color(areaRatio, 0f, 1f - areaRatio, 1f);
                
            case PreviewMode.Latitude:
                // Color based on latitude (Y coordinate)
                var center = triangle.GetCenter();
                float lat = Mathf.Asin(center.y) * Mathf.Rad2Deg;
                float latRatio = (lat + 90f) / 180f; // Normalize -90 to +90
                return new Color(latRatio, 0f, 1f - latRatio, 1f);
                
            case PreviewMode.Longitude:
                // Color based on longitude (X/Z coordinates)
                var center2 = triangle.GetCenter();
                float lon = Mathf.Atan2(center2.z, center2.x) * Mathf.Rad2Deg;
                float lonRatio = (lon + 180f) / 360f; // Normalize -180 to +180
                return new Color(0f, lonRatio, 1f - lonRatio, 1f);
                
            default:
                return Color.white;
        }
    }

    void CyclePreviewMode()
    {
        currentPreviewMode = (PreviewMode)((int)(currentPreviewMode + 1) % Enum.GetNames(typeof(PreviewMode)).Length);
        RefreshMeshColors(); // Refresh the mesh colors with new preview mode
        UpdateStatus($"Preview mode changed to {currentPreviewMode}");
    }

    void RefreshMeshColors()
    {
        if (icoSphere == null || icoSphere.triangleDataList == null) return;
        
        var mesh = icoSphere.GetComponent<MeshFilter>().mesh;
        var colors = mesh.colors;
        
        for (int i = 0; i < icoSphere.triangleDataList.Count; i++)
        {
            Color previewColor = GetPreviewColorForTriangle(i);
            int baseVertexIndex = i * 3;
            if (baseVertexIndex + 2 < colors.Length)
            {
                colors[baseVertexIndex] = previewColor;
                colors[baseVertexIndex + 1] = previewColor;
                colors[baseVertexIndex + 2] = previewColor;
            }
        }
        
        mesh.colors = colors;
    }

    void InitializePreviewMaterials()
    {
        // Initialize preview materials with simple shader for terrain mode
        var simpleShader = Shader.Find("Unlit/URP_TerrainPreview_Simple");
        if (simpleShader != null)
        {
            var previewMat = new Material(simpleShader);
            previewMaterials[EditorMode.Terrain] = previewMat;
            Debug.Log("MapEditor: Initialized simple preview material for terrain mode");
        }
        else
        {
            Debug.LogError("MapEditor: Simple preview shader not found. Terrain preview will not work.");
        }
        
        // For country mode, we'll use the splatmap material to show textured terrain
        // This will be created dynamically when needed in StartPainting
        previewMaterials[EditorMode.Country] = null; // Will be set dynamically
        
        // Initialize terrain preview colors from materials
        UpdateTerrainPreviewColors();
    }
    
    void UpdateTerrainPreviewColors()
    {
        if (icoSphere == null || icoSphere.terrainTypes == null)
        {
            terrainPreviewColors = new Color[0];
            return;
        }
        
        int terrainTypeCount = icoSphere.terrainTypes.Count;
        terrainPreviewColors = new Color[terrainTypeCount];
        
        for (int i = 0; i < terrainTypeCount; i++)
        {
            var terrainType = icoSphere.terrainTypes[i];
            if (terrainType != null)
            {
                // Use the terrain type's preview color (the one you set in the inspector)
                terrainPreviewColors[i] = terrainType.previewColor;
            }
            else
            {
                // Fallback to magenta for missing terrain types
                terrainPreviewColors[i] = Color.magenta;
            }
        }
        
        Debug.Log($"MapEditor: Updated terrain preview colors from {terrainTypeCount} terrain types");
    }

    void CheckIfSaveDataWasLoaded()
    {
        if (triangleDataSaver == null) return;
        
        if (triangleDataSaver.HasSavedData())
        {
            UpdateStatus("Save data was loaded on startup.");
        }
        else
        {
            UpdateStatus("No save data found on startup.");
        }
    }

    // --- Country Management ---

    /// <summary>
    /// Creates a new country and adds it to the available countries list
    /// </summary>
    public void CreateNewCountry()
    {
        Country newCountry = countryList.CreateCountry();
        selectedCountry = newCountry;
        UpdateStatus($"Created new country: {newCountry.name}");
    }

    /// <summary>
    /// Removes a country and unclaims all its territory
    /// </summary>
    public void RemoveCountry(Country country)
    {
        if (country == null) return;

        countryList.RemoveCountry(country);

        // Clear selection if this was the selected country
        if (selectedCountry == country)
        {
            selectedCountry = countryList.countries.Count > 0 ? countryList.countries[0] : null;
        }

        UpdateStatus($"Removed country: {country.name}");
    }

    /// <summary>
    /// Selects a country for editing
    /// </summary>
    public void SelectCountry(Country country)
    {
        selectedCountry = country;
        if (country != null)
        {
            UpdateStatus($"Selected country: {country.name}");
        }
        else
        {
            UpdateStatus("Selected: None (Erase mode) - Click to remove countries from triangles");
        }
    }

    /// <summary>
    /// Handles painting logic for country assignment
    /// </summary>
    private void PaintCountry(int triangleIndex, Color[] colors)
    {
        var triangle = icoSphere.triangleDataList[triangleIndex];

        // Check if we should only paint over unclaimed triangles
        if (onlyPaintOverUnclaimed && triangle.country != null)
        {
            // Skip this triangle if it already has a country assigned
            return;
        }

        // Store original country for undo
        if (!originalTerrainTypes.ContainsKey(triangleIndex))
        {
            // We'll use the terrain types dictionary to store original country IDs
            // -1 means no country, otherwise it's the index in availableCountries
            int originalCountryIndex = -1;
            if (triangle.country != null)
            {
                originalCountryIndex = countryList.GetCountryIndex(triangle.country);
            }
            originalTerrainTypes[triangleIndex] = originalCountryIndex;
        }

        // Assign to selected country (null means remove from country)
        triangle.AssignToCountry(selectedCountry);
        
        // Note: In country mode, we don't modify vertex colors since we're using the splatmap material
        // Country borders are drawn dynamically in OnRenderObject, so no need to update borders here
    }

    void DrawCountryPreviewOverlay()
    {
        if (lastPaintPosition == Vector3.zero) return;

        // Create a simple unlit shader for drawing filled triangles
        if (lineMaterial == null)
        {
            CreateLineMaterial();
        }
        
        if (lineMaterial != null)
        {
            lineMaterial.SetPass(0);
            
            // Draw filled triangles for all triangles that would be affected by the brush
            GL.Begin(GL.TRIANGLES);
            
            float brushRadiusSquared = brushSize * brushSize;
            
            foreach (var triangle in icoSphere.triangleDataList)
            {
                Vector3 triangleCenter = (triangle.a + triangle.b + triangle.c) / 3f;
                float distanceSquared = Vector3.SqrMagnitude(triangleCenter - lastPaintPosition);
                
                if (distanceSquared <= brushRadiusSquared)
                {
                    // Check if we should only paint over unclaimed triangles
                    if (onlyPaintOverUnclaimed && triangle.country != null)
                    {
                        continue; // Skip this triangle
                    }
                    
                    // Calculate brush falloff
                    float strength = 1f;
                    if (useFalloff)
                    {
                        float distance = Mathf.Sqrt(distanceSquared);
                        strength = 1f - Mathf.Clamp01(distance / brushSize);
                        strength = Mathf.Pow(strength, brushFalloff * 2);
                    }
                    
                    if (strength > 0.1f)
                    {
                        Color previewColor;
                        
                        if (selectedCountry != null)
                        {
                            // Use selected country color with configured alpha
                            previewColor = new Color(
                                selectedCountry.color.r,
                                selectedCountry.color.g,
                                selectedCountry.color.b,
                                countryPreviewAlpha * strength
                            );
                        }
                        else
                        {
                            // Erasing mode - show red overlay
                            previewColor = new Color(1f, 0f, 0f, countryPreviewAlpha * strength);
                        }
                        
                        GL.Color(previewColor);
                        GL.Vertex(triangle.a);
                        GL.Vertex(triangle.b);
                        GL.Vertex(triangle.c);
                    }
                }
            }
            
            GL.End();
        }
    }
}