using UnityEngine;

[RequireComponent(typeof(IcoSphere))]
public class TerrainDebugController : MonoBehaviour
{
    [Header("Debug Controls")]
    public bool showDebugGUI = true;
    public int debugMode = 0;
    public bool forceRegenerate = false;
    
    private IcoSphere icoSphere;
    private Material currentMaterial;
    
    void Start()
    {
        icoSphere = GetComponent<IcoSphere>();
    }
    
    void Update()
    {
        if (forceRegenerate)
        {
            forceRegenerate = false;
            RegenerateTerrain();
        }
        
        // Update debug mode
        var renderer = GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            currentMaterial = renderer.material;
            if (currentMaterial.HasProperty("_DebugMode"))
            {
                currentMaterial.SetFloat("_DebugMode", debugMode);
            }
        }
    }
    
    [ContextMenu("Regenerate Terrain")]
    public void RegenerateTerrain()
    {
        if (icoSphere != null)
        {
            icoSphere.Generate();
        }
    }
    
    void OnGUI()
    {
        if (!showDebugGUI) return;
        
        GUILayout.BeginArea(new Rect(10, 250, 300, 300));
        GUILayout.Label("Terrain Debug Controller", GUI.skin.box);
        
        if (GUILayout.Button("Regenerate Terrain"))
        {
            RegenerateTerrain();
        }
        
        GUILayout.Space(10);
        GUILayout.Label("Debug Mode:");
        GUILayout.Label("0 = Splat Map 1");
        GUILayout.Label("1 = Terrain Texture 1");
        GUILayout.Label("2 = Terrain Texture 2");
        GUILayout.Label("3 = Terrain Texture 3");
        
        GUILayout.Space(10);
        debugMode = (int)GUILayout.HorizontalSlider(debugMode, 0, 3);
        GUILayout.Label($"Current Mode: {debugMode}");
        
        if (icoSphere != null)
        {
            GUILayout.Space(10);
            GUILayout.Label($"Terrain Types: {icoSphere.terrainTypes.Count}");
            GUILayout.Label($"Triangles: {icoSphere.triangleDataList.Count}");
        }
        
        GUILayout.EndArea();
    }
} 