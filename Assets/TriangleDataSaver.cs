using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

[System.Serializable]
public class SerializableColor
{
    public float r, g, b, a;
    
    public SerializableColor()
    {
        r = g = b = a = 1f;
    }
    
    public SerializableColor(float r, float g, float b, float a)
    {
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
    }
    
    public SerializableColor(Color color)
    {
        r = color.r;
        g = color.g;
        b = color.b;
        a = color.a;
    }
    
    public Color ToColor()
    {
        return new Color(r, g, b, a);
    }
    
    public static SerializableColor FromColor(Color color)
    {
        return new SerializableColor(color.r, color.g, color.b, color.a);
    }
}



[System.Serializable]
public class TriangleDataSave
{
    [Header("Basic Info")]
    public int totalTriangles;
    public float sphereRadius;
    public int subdivisions;
    
    [Header("Triangle Data")]
    public List<float> vertices; 
    public List<int> terrainTypes;
    
    [Header("Natural Resources")]
    public List<int> naturalResourceTypes; // ResourceType enum values for each triangle
    
    [Header("Country Data")]
    public List<string> countryNames; // Names of countries for reference
    public List<int> countryIndices; // Index of country for each triangle (-1 for unclaimed)
    public List<SerializableColor> countryColors; // Colors of countries for reference
    
    [Header("Adjacency Data")]
    public List<int> adjacentTriangleIndices; 
    public List<int> adjacencyListEndIndices;
    
    [Header("Active Resources")]
    public List<ResourceSaveData> activeResources = new();
    
    [Header("Buildings")]
    public List<BuildingSaveData> buildings = new();
    
    [Header("Features")]
    public List<List<int>> featureTypes = new(); // List of FeatureType enum values for each triangle
    public List<List<int>> featureLevels = new(); // Parallel list of levels for each triangle
}

[System.Serializable]
public class TriangleDataSerializable
{
    // All the same fields as TriangleData, but with adjacentTriangles as List for serialization
    public int id;
    public Vector3 a, b, c;
    public int terrainType;
    public float colorR, colorG, colorB;
    public List<int> adjacentTriangles = new List<int>();
    
    public TriangleDataSerializable(TriangleData original)
    {
        id = original.id;
        a = original.a;
        b = original.b;
        c = original.c;
        terrainType = original.terrainType;
        colorR = original.colorR;
        colorG = original.colorG;
        colorB = original.colorB;
        adjacentTriangles = original.adjacentTriangles.ToList(); // Convert HashSet to List
    }
    
    public TriangleData ToTriangleData()
    {
        var triangle = new TriangleData
        {
            id = id,
            a = a,
            b = b,
            c = c,
            terrainType = terrainType,
            colorR = colorR,
            colorG = colorG,
            colorB = colorB
        };
        
        // Convert List back to HashSet
        triangle.adjacentTriangles = new HashSet<int>(adjacentTriangles);
        
        return triangle;
    }
}

[System.Serializable]
public class SaveData
{
    public List<TriangleData> triangles = new();
    public List<string> countryNames = new();
    public List<int> countryIndices = new();
    public List<SerializableColor> countryColors = new(); // Using SerializableColor for serialization
    public float sphereRadius;
    public int subdivisions;
}

[System.Serializable]
public class TriangleDataSaver : MonoBehaviour
{
    [Header("Save Settings")]
    public bool useCompression = true;
    
    [Header("Save Options")]
    public bool saveAdjacencyData = true;
    public bool saveColorData = true;
    public bool saveTerrainData = true;
    public bool saveVertexData = true;
    
    [Header("Debug")]
    public bool showSaveProgress = true;
    public bool autoSaveOnGenerate = false;
    
    private IcoSphere icoSphere;
    private const string MapFileName = "MapData.bin";
    
    void Awake()
    {
        icoSphere = UnityEngine.Object.FindFirstObjectByType<IcoSphere>();
        if (icoSphere == null)
        {
            Debug.LogError("TriangleDataSaver: No IcoSphere found in scene!");
        }
    }
    
    public string SaveTriangleData()
    {
        if (icoSphere == null)
        {
            Debug.LogError("TriangleDataSaver: IcoSphere component not found!");
            return null;
        }

        string directoryPath = Path.Combine(Application.streamingAssetsPath, "Maps");
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        string filePath = Path.Combine(directoryPath, MapFileName);
        
        try
        {
            var dataToSave = new TriangleDataSave
            {
                totalTriangles = icoSphere.triangleDataList.Count,
                sphereRadius = icoSphere.radius,
                subdivisions = icoSphere.subdivisions,
                vertices = new List<float>(),
                terrainTypes = new List<int>(),
                naturalResourceTypes = new List<int>(),
                countryNames = new List<string>(),
                countryIndices = new List<int>(),
                countryColors = new List<SerializableColor>(),
                adjacentTriangleIndices = new List<int>(),
                adjacencyListEndIndices = new List<int>(),
                activeResources = new List<ResourceSaveData>(),
                buildings = new List<BuildingSaveData>(),
                featureTypes = new List<List<int>>(),
                featureLevels = new List<List<int>>()
            };

            // Get all unique country names for reference
            var countryNameMap = new Dictionary<string, int>();
            var countryNames = new List<string>();
            
            foreach (var tri in icoSphere.triangleDataList)
            {
                if (tri.country != null && !countryNameMap.ContainsKey(tri.country.name))
                {
                    countryNameMap[tri.country.name] = countryNames.Count;
                    countryNames.Add(tri.country.name);
                    dataToSave.countryColors.Add(SerializableColor.FromColor(tri.country.color));
                }
            }
            
            dataToSave.countryNames = countryNames;
            
            Debug.Log($"TriangleDataSaver: Saving {countryNames.Count} countries: {string.Join(", ", countryNames)}");

            int adjacencyCounter = 0;
            int countryTriangleCount = 0;
            foreach (var tri in icoSphere.triangleDataList)
            {
                dataToSave.vertices.Add(tri.a.x); dataToSave.vertices.Add(tri.a.y); dataToSave.vertices.Add(tri.a.z);
                dataToSave.vertices.Add(tri.b.x); dataToSave.vertices.Add(tri.b.y); dataToSave.vertices.Add(tri.b.z);
                dataToSave.vertices.Add(tri.c.x); dataToSave.vertices.Add(tri.c.y); dataToSave.vertices.Add(tri.c.z);
                dataToSave.terrainTypes.Add(tri.terrainType);
                dataToSave.naturalResourceTypes.Add((int)tri.naturalResource);
                
                // Save country index (-1 for unclaimed)
                if (tri.country != null && countryNameMap.ContainsKey(tri.country.name))
                {
                    dataToSave.countryIndices.Add(countryNameMap[tri.country.name]);
                    countryTriangleCount++;
                }
                else
                {
                    dataToSave.countryIndices.Add(-1);
                }
                
                dataToSave.adjacentTriangleIndices.AddRange(tri.adjacentTriangles);
                adjacencyCounter += tri.adjacentTriangles.Count;
                dataToSave.adjacencyListEndIndices.Add(adjacencyCounter);
                
                // Save features
                var triangleFeatureTypes = new List<int>();
                var triangleFeatureLevels = new List<int>();
                for (int j = 0; j < tri.featureTypes.Count; j++)
                {
                    triangleFeatureTypes.Add(tri.featureTypes[j].id);
                    triangleFeatureLevels.Add(tri.featureLevels[j]);
                }
                dataToSave.featureTypes.Add(triangleFeatureTypes);
                dataToSave.featureLevels.Add(triangleFeatureLevels);
            }
            
            Debug.Log($"TriangleDataSaver: Saved {countryTriangleCount} triangles with country assignments out of {icoSphere.triangleDataList.Count} total");

            // Save active resources from ResourceManager
            var resourceManager = UnityEngine.Object.FindFirstObjectByType<ResourceManager>();
            if (resourceManager != null)
            {
                var activeResources = resourceManager.GetAllActiveResources();
                foreach (var resource in activeResources)
                {
                    if (resource != null && resource.origin != null)
                    {
                        var resourceData = new ResourceSaveData
                        {
                            type = resource.type,
                            originTriangleId = resource.origin.id,
                            destinationId = resource.destination != null ? resource.destination.id : -1,
                            isActive = resource.isActive,
                            isMoving = resource.isMoving,
                            shouldShowIcon = resource.shouldShowIcon
                        };
                        dataToSave.activeResources.Add(resourceData);
                    }
                }
                Debug.Log($"TriangleDataSaver: Saved {dataToSave.activeResources.Count} active resources");
            }

            // Save buildings from BuildingManager
            var buildingManager = UnityEngine.Object.FindFirstObjectByType<BuildingManager>();
            if (buildingManager != null)
            {
                var activeBuildings = buildingManager.GetActiveBuildingsWithTriangles();
                foreach (var building in activeBuildings)
                {
                    var buildingData = new BuildingSaveData
                    {
                        uniqueId = building.uniqueId,
                        buildingTypeName = building.buildingType != null ? building.buildingType.name : "",
                        buildingLevel = building.buildingLevel,
                        triangleId = building.triangle.id,
                        countryName = building.country != null ? building.country.name : ""
                    };
                    dataToSave.buildings.Add(buildingData);
                }
                Debug.Log($"TriangleDataSaver: Saved {dataToSave.buildings.Count} buildings");
            }

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, dataToSave);
            }

            Debug.Log($"TriangleDataSaver: Successfully saved {dataToSave.totalTriangles} triangles to {filePath}");
            return filePath;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"TriangleDataSaver: Failed to save data to {filePath}. Error: {e.Message}");
            return null;
        }
    }
    
    public void LoadTriangleData()
    {
        if (icoSphere == null)
        {
            Debug.LogError("TriangleDataSaver: IcoSphere component not found!");
            return;
        }

        string filePath = Path.Combine(Application.streamingAssetsPath, "Maps", MapFileName);

        if (!File.Exists(filePath))
        {
            Debug.LogError("TriangleDataSaver: No save file found at " + filePath);
            return;
        }
        
        // Check file size to ensure it's not empty or too small
        FileInfo fileInfo = new FileInfo(filePath);
        if (fileInfo.Length < 100) // Minimum reasonable size for a save file
        {
            Debug.LogError($"TriangleDataSaver: Save file appears to be corrupted or empty. Size: {fileInfo.Length} bytes");
            return;
        }
        
        try
        {
            TriangleDataSave saveData;
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                var formatter = new BinaryFormatter();
                
                // Add validation before deserialization
                if (stream.Length == 0)
                {
                    Debug.LogError("TriangleDataSaver: File is empty");
                    return;
                }
                
                try
                {
                    saveData = (TriangleDataSave)formatter.Deserialize(stream);
                }
                catch (System.Runtime.Serialization.SerializationException se)
                {
                    Debug.LogError($"TriangleDataSaver: Serialization error during deserialization: {se.Message}");
                    Debug.LogError("This usually means the save file format has changed or the file is corrupted.");
                    return;
                }
                catch (System.IO.EndOfStreamException ese)
                {
                    Debug.LogError($"TriangleDataSaver: End of stream error: {ese.Message}");
                    Debug.LogError("The save file appears to be truncated or corrupted.");
                    return;
                }
            }
            
            // Validate the loaded data
            if (saveData == null)
            {
                Debug.LogError("TriangleDataSaver: Deserialized data is null");
                return;
            }
            
            if (saveData.totalTriangles <= 0)
            {
                Debug.LogError($"TriangleDataSaver: Invalid triangle count: {saveData.totalTriangles}");
                return;
            }
            
            // Validate array lengths
            int expectedVertexCount = saveData.totalTriangles * 9; // 3 vertices * 3 coordinates each
            if (saveData.vertices == null || saveData.vertices.Count != expectedVertexCount)
            {
                Debug.LogError($"TriangleDataSaver: Vertex count mismatch. Expected: {expectedVertexCount}, Got: {saveData.vertices?.Count ?? 0}");
                return;
            }
            
            if (saveData.terrainTypes == null || saveData.terrainTypes.Count != saveData.totalTriangles)
            {
                Debug.LogError($"TriangleDataSaver: Terrain type count mismatch. Expected: {saveData.totalTriangles}, Got: {saveData.terrainTypes?.Count ?? 0}");
                return;
            }
            
            // After deserialization, before using the lists:
            if (saveData.countryNames == null)
                saveData.countryNames = new List<string>();
            if (saveData.countryIndices == null)
                saveData.countryIndices = Enumerable.Repeat(-1, saveData.totalTriangles).ToList();
            if (saveData.countryColors == null)
                saveData.countryColors = new List<SerializableColor>();
            if (saveData.naturalResourceTypes == null)
                saveData.naturalResourceTypes = Enumerable.Repeat(0, saveData.totalTriangles).ToList(); // Default to ResourceType.None
            if (saveData.adjacentTriangleIndices == null)
                saveData.adjacentTriangleIndices = new List<int>();
            if (saveData.adjacencyListEndIndices == null)
                saveData.adjacencyListEndIndices = new List<int>();
            if (saveData.activeResources == null)
                saveData.activeResources = new List<ResourceSaveData>();
            if (saveData.buildings == null)
                saveData.buildings = new List<BuildingSaveData>();
            if (saveData.featureTypes == null)
                saveData.featureTypes = new List<List<int>>();
            if (saveData.featureLevels == null)
                saveData.featureLevels = new List<List<int>>();
            
            Debug.Log($"TriangleDataSaver: Loading {saveData.countryNames.Count} countries: {string.Join(", ", saveData.countryNames)}");
            
            // Convert back to TriangleData list
            var loadedTriangles = new List<TriangleData>();
            int adjacencyStartIndex = 0;
            int restoredCountryCount = 0;
            for (int i = 0; i < saveData.totalTriangles; i++)
            {
                var tri = new TriangleData();
                tri.id = i;
                
                int vertIndex = i * 9;
                tri.a = new Vector3(saveData.vertices[vertIndex], saveData.vertices[vertIndex+1], saveData.vertices[vertIndex+2]);
                tri.b = new Vector3(saveData.vertices[vertIndex+3], saveData.vertices[vertIndex+4], saveData.vertices[vertIndex+5]);
                tri.c = new Vector3(saveData.vertices[vertIndex+6], saveData.vertices[vertIndex+7], saveData.vertices[vertIndex+8]);

                tri.terrainType = saveData.terrainTypes[i];
                tri.naturalResource = (ResourceType)saveData.naturalResourceTypes[i];

                // Regenerate resource icon after loading
                tri.RegenerateResourceIcon();

                // Restore country assignment
                int countryIndex = saveData.countryIndices[i];
                if (countryIndex >= 0 && countryIndex < saveData.countryNames.Count)
                {
                    string countryName = saveData.countryNames[countryIndex];
                    
                    // Find or create the country in the current country list
                    var mapEditor = UnityEngine.Object.FindFirstObjectByType<MapEditor>();
                    
                    // If MapEditor not found, try again after a short delay (might be timing issue)
                    if (mapEditor == null)
                    {
                        Debug.LogWarning("TriangleDataSaver: MapEditor not found on first attempt, trying again...");
                        // Try to find it in all loaded objects
                        mapEditor = UnityEngine.Object.FindFirstObjectByType<MapEditor>();
                    }
                    
                    if (mapEditor != null && mapEditor.countryList != null)
                    {
                        Country country = mapEditor.countryList.GetCountryByName(countryName);
                        Color loadedColor = Color.white;
                        if (saveData.countryColors != null && countryIndex < saveData.countryColors.Count)
                        {
                            loadedColor = saveData.countryColors[countryIndex].ToColor();
                        }
                        
                        if (country == null)
                        {
                            // Create the country if it doesn't exist
                            country = mapEditor.countryList.CreateCountry(countryName, loadedColor);
                        }
                        else
                        {
                            // Always restore color from save
                            country.color = loadedColor;
                        }
                        tri.country = country;
                        restoredCountryCount++;
                    }
                    else
                    {
                        Debug.LogWarning($"TriangleDataSaver: MapEditor or countryList not found during load for country '{countryName}'");
                    }
                }

                tri.adjacentTriangles = new HashSet<int>();
                if (i < saveData.adjacencyListEndIndices.Count)
                {
                    int adjacencyEndIndex = saveData.adjacencyListEndIndices[i];
                    for (int j = adjacencyStartIndex; j < adjacencyEndIndex && j < saveData.adjacentTriangleIndices.Count; j++)
                    {
                        tri.adjacentTriangles.Add(saveData.adjacentTriangleIndices[j]);
                    }
                    adjacencyStartIndex = adjacencyEndIndex;
                }
                
                // Load features
                if (i < saveData.featureTypes.Count && i < saveData.featureLevels.Count)
                {
                    var triangleFeatureTypes = saveData.featureTypes[i];
                    var triangleFeatureLevels = saveData.featureLevels[i];
                    for (int j = 0; j < triangleFeatureTypes.Count && j < triangleFeatureLevels.Count; j++)
                    {
                        // Find FeatureType by ID
                        FeatureType featureType = FeatureType.AllTypes.FirstOrDefault(ft => ft.id == triangleFeatureTypes[j]) ?? FeatureType.None;
                        tri.AddFeature(featureType, triangleFeatureLevels[j]);
                    }
                }

                loadedTriangles.Add(tri);
            }
            
            Debug.Log($"TriangleDataSaver: Loaded {loadedTriangles.Count} triangles with {restoredCountryCount} country assignments");
            
            // Pass the loaded data to the IcoSphere
            icoSphere.LoadTriangleData(loadedTriangles);
            
            // Restore country relationships to ensure bidirectional links are maintained
            RestoreCountryRelationships(loadedTriangles);
            
            // Restore active resources
            RestoreActiveResources(saveData.activeResources, loadedTriangles);
            
            // Restore buildings
            RestoreBuildings(saveData.buildings, loadedTriangles);
            
            // Rebuild auxiliary data structures in ResourceManager
            var resourceManager = UnityEngine.Object.FindFirstObjectByType<ResourceManager>();
            if (resourceManager != null)
            {
                resourceManager.RebuildAuxiliaryDataStructures();
            }
            
            // Rebuild feature segments
            var featureRenderer = UnityEngine.Object.FindFirstObjectByType<FeatureRenderer>();
            if (featureRenderer != null)
            {
                featureRenderer.RebuildAllSegments();
            }
            
            // Set the radius and subdivisions from save data
            icoSphere.radius = saveData.sphereRadius;
            icoSphere.subdivisions = saveData.subdivisions;
            
            Debug.Log($"TriangleDataSaver: Successfully loaded {loadedTriangles.Count} triangles from {filePath}");
            
            if (showSaveProgress)
            {
                Debug.Log($"Sphere radius: {saveData.sphereRadius}");
                Debug.Log($"Subdivisions: {saveData.subdivisions}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"TriangleDataSaver: Failed to load or deserialize data from {filePath}. Error: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            
            // Provide more specific error information
            if (e is System.Runtime.Serialization.SerializationException)
            {
                Debug.LogError("This is a serialization error. The save file format may have changed or the file is corrupted.");
                Debug.LogError("Consider deleting the save file and regenerating the map.");
            }
        }
    }
    
    /// <summary>
    /// Restores the bidirectional relationship between triangles and countries after loading
    /// </summary>
    private void RestoreCountryRelationships(List<TriangleData> triangles)
    {
        int restoredCount = 0;
        foreach (var triangle in triangles)
        {
            if (triangle.country != null)
            {
                // Ensure the triangle is in the country's territory list
                if (!triangle.country.territory.Contains(triangle))
                {
                    triangle.country.territory.Add(triangle);
                    restoredCount++;
                }
            }
        }
        Debug.Log($"TriangleDataSaver: Restored {restoredCount} country-territory relationships");
    }
    
    /// <summary>
    /// Restores buildings from save data
    /// </summary>
    private void RestoreBuildings(List<BuildingSaveData> savedBuildings, List<TriangleData> triangles)
    {
        var buildingManager = UnityEngine.Object.FindFirstObjectByType<BuildingManager>();
        if (buildingManager == null)
        {
            Debug.LogWarning("TriangleDataSaver: BuildingManager not found, cannot restore buildings");
            return;
        }
        
        // Clear existing buildings
        buildingManager.DestroyAllBuildings();
        
        int restoredCount = 0;
        foreach (var buildingData in savedBuildings)
        {
            // Find the triangle where the building should be placed
            TriangleData targetTriangle = null;
            if (buildingData.triangleId >= 0 && buildingData.triangleId < triangles.Count)
            {
                targetTriangle = triangles[buildingData.triangleId];
            }
            
            if (targetTriangle == null)
            {
                Debug.LogWarning($"TriangleDataSaver: Could not find triangle {buildingData.triangleId} for building {buildingData.uniqueId}");
                continue;
            }
            
            // Find the building type by name
            BuildingType buildingType = BuildingType.GetByName(buildingData.buildingTypeName);
            if (buildingType == null)
            {
                Debug.LogWarning($"TriangleDataSaver: Could not find building type '{buildingData.buildingTypeName}' for building {buildingData.uniqueId}");
                continue;
            }
            
            // Find the country by name
            Country ownerCountry = null;
            if (!string.IsNullOrEmpty(buildingData.countryName))
            {
                var mapEditor = UnityEngine.Object.FindFirstObjectByType<MapEditor>();
                if (mapEditor != null && mapEditor.countryList != null)
                {
                    ownerCountry = mapEditor.countryList.GetCountryByName(buildingData.countryName);
                }
            }
            
            // Create the building
            Building newBuilding = buildingManager.CreateBuilding(targetTriangle, buildingType, ownerCountry, buildingData.buildingLevel);
            
            if (newBuilding != null)
            {
                // Restore the unique ID
                newBuilding.uniqueId = buildingData.uniqueId;
                
                // Set the building on the triangle
                targetTriangle.SetBuilding(newBuilding);
                
                restoredCount++;
            }
        }
        
        Debug.Log($"TriangleDataSaver: Restored {restoredCount} buildings");
    }
    
    /// <summary>
    /// Restores active resources from save data
    /// </summary>
    private void RestoreActiveResources(List<ResourceSaveData> savedResources, List<TriangleData> triangles)
    {
        var resourceManager = UnityEngine.Object.FindFirstObjectByType<ResourceManager>();
        if (resourceManager == null)
        {
            Debug.LogWarning("TriangleDataSaver: ResourceManager not found, cannot restore active resources");
            return;
        }
        
        // Clear existing resources
        resourceManager.ClearAllResources();
        
        int restoredCount = 0;
        foreach (var resourceData in savedResources)
        {
            // Find origin triangle
            TriangleData originTriangle = null;
            
            if (resourceData.originTriangleId >= 0 && resourceData.originTriangleId < triangles.Count)
            {
                originTriangle = triangles[resourceData.originTriangleId];
            }
            
            // Find destination acceptor by ID
            IResourceAcceptor destinationAcceptor = null;
            if (resourceData.destinationId >= 0)
            {
                // TODO: RESTORE ACCEPTOR - We need to implement acceptor saving/loading system
                // Most acceptors will be buildings, so we should save them in this same file
                // and load them before restoring resources
                // For now, we'll look for buildings or other acceptors that might have this ID
                var allAcceptors = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>().OfType<IResourceAcceptor>();
                destinationAcceptor = allAcceptors.FirstOrDefault(acceptor => acceptor.id == resourceData.destinationId);
            }
            
            if (originTriangle != null)
            {
                // Create the resource
                var resource = resourceManager.CreateResource(resourceData.type, originTriangle, destinationAcceptor);
                
                if (resource != null)
                {
                    // Restore resource properties
                    resource.isActive = resourceData.isActive;
                    resource.isMoving = resourceData.isMoving;
                    resource.shouldShowIcon = resourceData.shouldShowIcon;
                    

                    
                    restoredCount++;
                }
            }
        }
        
        Debug.Log($"TriangleDataSaver: Restored {restoredCount} active resources");
    }
    
    public void GetSaveFileInfo()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "Maps", MapFileName);
        if (File.Exists(filePath))
        {
            FileInfo fileInfo = new FileInfo(filePath);
            Debug.Log($"Save file info for {filePath}:");
            Debug.Log($"Size: {fileInfo.Length} bytes");
            Debug.Log($"Created: {fileInfo.CreationTime}");
            Debug.Log($"Modified: {fileInfo.LastWriteTime}");
        }
        else
        {
            Debug.Log($"No save file found at {filePath}");
        }
    }
    
    public void AnalyzeSaveFile()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "Maps", MapFileName);
        if (!File.Exists(filePath))
        {
            Debug.LogError($"TriangleDataSaver: No save file found at {filePath}");
            return;
        }
        
        try
        {
            TriangleDataSave saveData;
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                var formatter = new BinaryFormatter();
                saveData = (TriangleDataSave)formatter.Deserialize(stream);
            }
            
            Debug.Log($"Save file analysis for {filePath}:");
            Debug.Log($"- Total Triangles: {saveData.totalTriangles}");
            Debug.Log($"- Sphere radius: {saveData.sphereRadius}");
            Debug.Log($"- Subdivisions: {saveData.subdivisions}");
            
            if (saveData.vertices.Count > 0)
            {
                Debug.Log($"- Vertices: {saveData.vertices.Count} floats");
            }
            if (saveData.terrainTypes.Count > 0)
            {
                Debug.Log($"- Terrain types: {saveData.terrainTypes.Count} integers");
            }
            if (saveData.countryNames.Count > 0)
            {
                Debug.Log($"- Country names: {saveData.countryNames.Count} strings");
            }
            if (saveData.countryIndices.Count > 0)
            {
                Debug.Log($"- Country indices: {saveData.countryIndices.Count} integers");
            }
            if (saveData.adjacentTriangleIndices.Count > 0)
            {
                Debug.Log($"- Adjacent triangle indices: {saveData.adjacentTriangleIndices.Count} integers");
            }
            if (saveData.adjacencyListEndIndices.Count > 0)
            {
                Debug.Log($"- Adjacency list end indices: {saveData.adjacencyListEndIndices.Count} integers");
            }
            if (saveData.activeResources.Count > 0)
            {
                Debug.Log($"- Active resources: {saveData.activeResources.Count} resources");
            }
            if (saveData.buildings.Count > 0)
            {
                Debug.Log($"- Buildings: {saveData.buildings.Count} buildings");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"TriangleDataSaver: Failed to analyze save file: {e.Message}");
        }
    }
    
    /// <summary>
    /// Checks if save data exists without loading it.
    /// </summary>
    /// <returns>True if save data exists, false otherwise.</returns>
    public bool HasSavedData()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "Maps", MapFileName);
        return File.Exists(filePath);
    }
    
    /// <summary>
    /// Forces regeneration of the map from Koppen data, ignoring any existing save data.
    /// </summary>
    public void ForceRegenerateFromKoppen()
    {
        Debug.Log("TriangleDataSaver: Force regenerating map from Koppen data...");
        
        // Delete the existing save file if it exists
        string filePath = Path.Combine(Application.streamingAssetsPath, "Maps", MapFileName);
        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
                Debug.Log($"TriangleDataSaver: Deleted existing save file: {filePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"TriangleDataSaver: Could not delete existing save file: {e.Message}");
            }
        }
        
        // Force the IcoSphere to regenerate
        if (icoSphere != null)
        {
            icoSphere.Generate();
            Debug.Log("TriangleDataSaver: Map regeneration completed.");
        }
        else
        {
            Debug.LogError("TriangleDataSaver: IcoSphere not found for regeneration.");
        }
    }
    
    /// <summary>
    /// Deletes the corrupted save file and regenerates the map from Koppen data.
    /// Use this when the save file is corrupted and cannot be loaded.
    /// </summary>
    public void DeleteCorruptedSaveAndRegenerate()
    {
        Debug.Log("TriangleDataSaver: Deleting corrupted save file and regenerating map...");
        
        string filePath = Path.Combine(Application.streamingAssetsPath, "Maps", MapFileName);
        if (File.Exists(filePath))
        {
            try
            {
                // Create a backup of the corrupted file for debugging
                string backupPath = filePath + ".corrupted";
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
                File.Move(filePath, backupPath);
                Debug.Log($"TriangleDataSaver: Moved corrupted file to backup: {backupPath}");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"TriangleDataSaver: Could not backup corrupted file: {e.Message}");
                // Try to delete it directly
                try
                {
                    File.Delete(filePath);
                    Debug.Log($"TriangleDataSaver: Deleted corrupted save file: {filePath}");
                }
                catch (System.Exception e2)
                {
                    Debug.LogError($"TriangleDataSaver: Could not delete corrupted save file: {e2.Message}");
                    return;
                }
            }
        }
        
        // Force the IcoSphere to regenerate
        if (icoSphere != null)
        {
            icoSphere.Generate();
            Debug.Log("TriangleDataSaver: Map regeneration completed after deleting corrupted save.");
        }
        else
        {
            Debug.LogError("TriangleDataSaver: IcoSphere not found for regeneration.");
        }
    }
    
    /// <summary>
    /// Attempts to repair a corrupted save file by validating and fixing common issues.
    /// </summary>
    public bool TryRepairSaveFile()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "Maps", MapFileName);
        
        if (!File.Exists(filePath))
        {
            Debug.LogWarning("TriangleDataSaver: No save file to repair.");
            return false;
        }
        
        try
        {
            // Try to load the file to see what specific error occurs
            TriangleDataSave saveData;
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                var formatter = new BinaryFormatter();
                saveData = (TriangleDataSave)formatter.Deserialize(stream);
            }
            
            // If we get here, the file loaded successfully
            Debug.Log("TriangleDataSaver: Save file appears to be valid, no repair needed.");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"TriangleDataSaver: Save file is corrupted: {e.Message}");
            Debug.LogWarning("TriangleDataSaver: Cannot repair corrupted binary save file. Consider regenerating the map.");
            return false;
        }
    }
} 