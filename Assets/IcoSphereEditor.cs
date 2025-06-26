using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(IcoSphere))]
public class IcoSphereEditor : Editor
{
    private IcoSphere icoSphere;
    private bool showTerrainTypes = true;
    private Vector2 terrainTypesScroll;

    void OnEnable()
    {
        icoSphere = (IcoSphere)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Terrain Type Management", EditorStyles.boldLabel);

        // Terrain Types Section
        showTerrainTypes = EditorGUILayout.Foldout(showTerrainTypes, $"Terrain Types ({icoSphere.terrainTypes.Count})");
        if (showTerrainTypes)
        {
            terrainTypesScroll = EditorGUILayout.BeginScrollView(terrainTypesScroll, GUILayout.Height(200));
            
            for (int i = 0; i < icoSphere.terrainTypes.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                
                var terrainType = icoSphere.terrainTypes[i];
                
                // ID
                EditorGUILayout.LabelField($"ID: {i}", GUILayout.Width(40));
                
                // Name
                terrainType.name = EditorGUILayout.TextField(terrainType.name, GUILayout.Width(120));
                
                // Material
                terrainType.material = (Material)EditorGUILayout.ObjectField(terrainType.material, typeof(Material), false, GUILayout.Width(120));
                
                // Preview Color
                terrainType.previewColor = EditorGUILayout.ColorField(terrainType.previewColor, GUILayout.Width(50));
                
                // Remove button
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    icoSphere.terrainTypes.RemoveAt(i);
                    EditorUtility.SetDirty(icoSphere);
                    break;
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
            
            // Add new terrain type button
            if (GUILayout.Button("Add Terrain Type"))
            {
                var newTerrainType = new TerrainType();
                newTerrainType.id = icoSphere.terrainTypes.Count;
                icoSphere.terrainTypes.Add(newTerrainType);
                EditorUtility.SetDirty(icoSphere);
            }
        }

        EditorGUILayout.Space();
        
        // Quick Actions
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Regenerate Splat Maps"))
        {
            icoSphere.CreateAndApplyNewSplatMaterial();
        }
        
        if (GUILayout.Button("Generate New Terrain"))
        {
            icoSphere.Generate();
        }
        
        if (GUILayout.Button("Initialize Terrain Types"))
        {
            // Call the private method via reflection
            var method = typeof(IcoSphere).GetMethod("InitializeTerrainTypes", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(icoSphere, null);
                EditorUtility.SetDirty(icoSphere);
            }
        }
    }
} 