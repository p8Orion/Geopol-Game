using UnityEngine;
using UnityEditor;

[RequireComponent(typeof(IcoSphere))]
public class SplatMapVisualizer : MonoBehaviour
{
    [Header("Visualization Settings")]
    public bool showSplatMaps = false;
    public float displayScale = 0.5f;
    public Vector2 displayOffset = Vector2.zero;
    public bool showAllMaps = true;
    
    private IcoSphere icoSphere;
    private Texture2D[] splatMaps = new Texture2D[3];
    private Material debugMaterial;
    
    void Start()
    {
        icoSphere = GetComponent<IcoSphere>();
    }
    
    void OnGUI()
    {
        if (!showSplatMaps || icoSphere == null) return;
        
        // Get the splat maps from the IcoSphere
        var renderer = GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            splatMaps[0] = renderer.material.GetTexture("_SplatMap1") as Texture2D;
            splatMaps[1] = renderer.material.GetTexture("_SplatMap2") as Texture2D;
            splatMaps[2] = renderer.material.GetTexture("_SplatMap3") as Texture2D;
        }
        
        // Debug info
        int validMaps = 0;
        for (int i = 0; i < 3; i++)
        {
            if (splatMaps[i] != null) validMaps++;
        }
        
        // Show debug info
        GUILayout.BeginArea(new Rect(10, 10, 300, 100));
        GUILayout.Label($"Splat Maps Found: {validMaps}/3");
        GUILayout.Label($"Show All Maps: {showAllMaps}");
        for (int i = 0; i < 3; i++)
        {
            GUILayout.Label($"Map {i + 1}: {(splatMaps[i] != null ? "OK" : "NULL")}");
        }
        
        if (GUILayout.Button("Force Regenerate"))
        {
            RegenerateSplatMaps();
        }
        
        if (GUILayout.Button("Toggle Show All"))
        {
            showAllMaps = !showAllMaps;
        }
        GUILayout.EndArea();
        
        if (splatMaps[0] == null) return;
        
        // Calculate display rects
        float width = splatMaps[0].width * displayScale;
        float height = splatMaps[0].height * displayScale;
        
        if (showAllMaps)
        {
            // Show all 3 splat maps in a horizontal row
            for (int i = 0; i < 3; i++)
            {
                if (splatMaps[i] != null)
                {
                    Rect displayRect = new Rect(
                        displayOffset.x + i * (width + 10), 
                        displayOffset.y + 120, // Move down to avoid debug info
                        width, 
                        height
                    );
                    
                    // Draw the splat map
                    GUI.DrawTexture(displayRect, splatMaps[i]);
                    
                    // Draw border
                    GUI.color = Color.white;
                    GUI.Box(displayRect, "");
                    
                    // Draw info
                    GUI.Label(new Rect(displayRect.x, displayRect.y - 20, width, 20), 
                             $"Splat Map {i + 1}: {splatMaps[i].width}x{splatMaps[i].height}");
                }
                else
                {
                    // Draw placeholder for missing map
                    Rect displayRect = new Rect(
                        displayOffset.x + i * (width + 10), 
                        displayOffset.y + 120,
                        width, 
                        height
                    );
                    
                    GUI.color = Color.red;
                    GUI.Box(displayRect, "NULL");
                    GUI.color = Color.white;
                    
                    GUI.Label(new Rect(displayRect.x, displayRect.y - 20, width, 20), 
                             $"Splat Map {i + 1}: NULL");
                }
            }
        }
        else
        {
            // Show only the first splat map
            Rect displayRect = new Rect(displayOffset.x, displayOffset.y + 120, width, height);
            
            // Draw the splat map
            GUI.DrawTexture(displayRect, splatMaps[0]);
            
            // Draw border
            GUI.color = Color.white;
            GUI.Box(displayRect, "");
            
            // Draw info
            GUI.Label(new Rect(displayOffset.x, displayOffset.y + 100, width, 20), 
                     $"Splat Map 1: {splatMaps[0].width}x{splatMaps[0].height}");
        }
    }
    
    [ContextMenu("Regenerate Splat Maps")]
    public void RegenerateSplatMaps()
    {
        if (icoSphere != null)
        {
            // Regenerate splat maps from existing triangle data without regenerating terrain data
            icoSphere.CreateAndApplyNewSplatMaterial();
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showSplatMaps) return;
        
        // Draw a gizmo to show where the splat maps are being displayed
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 2f);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(SplatMapVisualizer))]
public class SplatMapVisualizerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        SplatMapVisualizer visualizer = (SplatMapVisualizer)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug Controls", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Regenerate Splat Maps"))
        {
            visualizer.RegenerateSplatMaps();
        }
        
        if (GUILayout.Button("Toggle Splat Maps Display"))
        {
            visualizer.showSplatMaps = !visualizer.showSplatMaps;
        }
        
        if (GUILayout.Button("Toggle Show All Maps"))
        {
            visualizer.showAllMaps = !visualizer.showAllMaps;
        }
    }
}
#endif 