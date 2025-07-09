using UnityEngine;
using System;

/// <summary>
/// Representa un edificio que puede recibir recursos mediante drag & drop
/// </summary>
public class Building : MonoBehaviour, IResourceAcceptor
{
    [Header("Building Properties")]
    public string buildingName = "Building";
    public BuildingType buildingType;
    public int buildingLevel = 1;
    
    [Header("Unique ID")]
    [SerializeField] public string uniqueId;
    
    [Header("Location")]
    public TriangleData triangle;
    
    [Header("Ownership")]
    public Country country;
    
    [Header("Resource Acceptance")]
    public ResourceType[] acceptedResourceTypes = new ResourceType[0];
    public bool acceptAllResources = false;
    
    [Header("Visual Representation")]
    [System.NonSerialized]
    public GameObject visualInstance; // The actual visual representation created from BuildingLevel prefab
    
    // Note: The visual representation is created dynamically from the BuildingLevel prefab
    // This allows each building level to have its own unique model and materials
    // The visualInstance contains the actual MeshRenderer and materials
    
    private void Start()
    {
        // Generar ID único si no existe
        if (string.IsNullOrEmpty(uniqueId))
        {
            GenerateUniqueId();
        }
        
        // Inicializar tipo de edificio por defecto si no está asignado
        if (buildingType == null)
        {
            buildingType = BuildingType.Factory;
        }
        
        // Validar que el nivel sea válido
        if (buildingType != null)
        {
            var level = buildingType.GetLevel(buildingLevel);
            if (level == null)
            {
                Debug.LogWarning($"Building {buildingName}: Invalid level {buildingLevel}, using level 1");
                buildingLevel = 1;
            }
        }
        
        // Validar que tenga un triángulo asignado
        if (triangle == null)
        {
            Debug.LogError($"Building {buildingName} has no triangle assigned!");
        }
        
        // Posicionar el edificio en el centro del triángulo
        if (triangle != null)
        {
            transform.position = triangle.GetCenter();
            
            // Si no se asignó un país, usar el del triángulo
            if (country == null)
            {
                country = triangle.country;
            }
        }
        
        // Create visual representation if building type is set
        if (buildingType != null && buildingType.prefab != null)
        {
            CreateVisual();
        }
    }
    
    /// <summary>
    /// Genera un ID único para este building usando "B" + GUID
    /// </summary>
    private void GenerateUniqueId()
    {
        uniqueId = "B" + Guid.NewGuid().ToString("N"); // N para quitar los guiones
    }
    
    /// <summary>
    /// Implementación de la propiedad id de IResourceAcceptor
    /// </summary>
    public int id
    {
        get
        {
            // Convertir el string ID a un hash numérico para compatibilidad
            return uniqueId.GetHashCode();
        }
    }
    
    // Implementación de IResourceAcceptor
    public bool CanAcceptResource(Resource resource)
    {
        if (resource == null || !resource.isActive || buildingType == null)
            return false;
            
        var level = buildingType.GetLevel(buildingLevel);
        if (level == null) return false;
        
        // Si no tiene recursos aceptados, acepta todos (como warehouse)
        if (level.acceptedResources.Length == 0)
            return true;
            
        // Verificar si el tipo de recurso está en la lista de recursos aceptados
        return level.AcceptsResource(resource.type);
    }
    
    public bool AcceptResource(Resource resource)
    {
        if (!CanAcceptResource(resource))
            return false;
            
        // Establecer el destino del recurso usando el triángulo como IResourceDropPosition
        resource.SetDestination(this);
        
        // Crear ruta de logística
        CreateLogisticsRoute(resource);
        
        Debug.Log($"Building {buildingName} accepted resource {resource.type}");
        return true;
    }
    
    /// <summary>
    /// Crea una ruta de logística cuando se acepta un resource
    /// </summary>
    /// <param name="resource">El resource que se está aceptando</param>
    private void CreateLogisticsRoute(Resource resource)
    {
        if (resource == null || resource.origin == null)
        {
            Debug.LogWarning("No se puede crear ruta: resource o origin son null");
            return;
        }
        
        // Verificar que el resource no esté ya usado
        if (resource.isUsed)
        {
            Debug.LogWarning($"No se puede crear ruta: el resource {resource.type} ya está siendo usado");
            return;
        }
        
        // Buscar el RouteManager en la escena
        RouteManager routeManager = FindObjectOfType<RouteManager>();
        if (routeManager == null)
        {
            Debug.LogWarning("No se encontró RouteManager en la escena");
            return;
        }
        
        // Crear la ruta usando el triángulo de origen del resource y este building como destino
        Route newRoute = routeManager.CreateRoute(resource.origin, this, RouteType.Land, resource);
        
        if (newRoute != null)
        {
            // Actualizar la referencia de la ruta en el resource y marcarlo como usado
            resource.associatedRoute = newRoute;
            resource.isUsed = true;
            resource.NotifyIconUpdate(); // Actualizar visualización del ícono
            Debug.Log($"Ruta de logística creada: {resource.origin.id} -> {buildingName}");
        }
    }
    // Métodos adicionales del edificio
    public void SetTriangle(TriangleData newTriangle)
    {
        triangle = newTriangle;
        if (triangle != null)
        {
            transform.position = triangle.GetCenter();
            
            // Reposition visual instance if it exists
            if (visualInstance != null)
            {
                visualInstance.transform.position = triangle.GetCenter();
            }
            
            // Actualizar el país si no se asignó manualmente
            if (country == null)
            {
                country = triangle.country;
            }
        }
    }
    
    /// <summary>
    /// Obtiene el nivel actual del edificio
    /// </summary>
    public BuildingLevel GetCurrentLevel()
    {
        return buildingType?.GetLevel(buildingLevel);
    }
    
    /// <summary>
    /// Verifica si el edificio acepta un recurso específico
    /// </summary>
    public bool AcceptsResource(ResourceType resourceType)
    {
        var level = GetCurrentLevel();
        return level?.AcceptsResource(resourceType) ?? false;
    }
    
    /// <summary>
    /// Verifica si el edificio produce un recurso específico
    /// </summary>
    public bool ProducesResource(ResourceType resourceType)
    {
        var level = GetCurrentLevel();
        return level?.ProducesResource(resourceType) ?? false;
    }
    
    /// <summary>
    /// Obtiene los recursos que acepta el edificio
    /// </summary>
    public ResourceType[] GetAcceptedResources()
    {
        var level = GetCurrentLevel();
        return level?.acceptedResources ?? new ResourceType[0];
    }
    
    /// <summary>
    /// Obtiene los recursos que produce el edificio
    /// </summary>
    public ResourceType[] GetProducedResources()
    {
        var level = GetCurrentLevel();
        return level?.producedResources ?? new ResourceType[0];
    }
    
    /// <summary>
    /// Establece el país propietario del edificio
    /// </summary>
    public void SetCountry(Country newCountry)
    {
        country = newCountry;
        
        // Update building name to reflect new ownership
        if (buildingType != null)
        {
            string countryName = country != null ? country.name : "Unclaimed";
            buildingName = $"{countryName}_{buildingType.name}_L{buildingLevel}_{triangle?.id ?? 0}";
        }
        
        // Update visual color based on country ownership
        UpdateVisualColor();
        
        Debug.Log($"Building {buildingName} is now owned by {newCountry?.name ?? "No Country"}");
    }
    
    /// <summary>
    /// Updates the visual color based on the country ownership
    /// </summary>
    private void UpdateVisualColor()
    {
        if (visualInstance == null) return;
        
        var meshRenderer = GetMeshRenderer();
        if (meshRenderer == null) return;
        
        Color targetColor = Color.white; // Default color
        
        if (country != null)
        {
            // Use country color with some transparency to show ownership
            targetColor = new Color(
                country.color.r,
                country.color.g, 
                country.color.b,
                0.8f // Slight transparency
            );
        }
        else
        {
            // Unclaimed buildings use a neutral gray
            targetColor = new Color(0.7f, 0.7f, 0.7f, 0.6f);
        }
        
        // Apply color to all materials
        Material[] materials = meshRenderer.materials;
        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] != null)
            {
                // Create a new material instance to avoid affecting other buildings
                Material newMaterial = new Material(materials[i]);
                newMaterial.color = targetColor;
                materials[i] = newMaterial;
            }
        }
        
        meshRenderer.materials = materials;
    }
    
    /// <summary>
    /// Forces the visual color to update based on current country ownership
    /// </summary>
    public void RefreshVisualColor()
    {
        UpdateVisualColor();
    }
    
    /// <summary>
    /// Obtiene el nombre del país propietario
    /// </summary>
    public string GetCountryName()
    {
        return country != null ? country.name : "Unclaimed";
    }
    
    /// <summary>
    /// Verifica si el edificio pertenece a un país específico
    /// </summary>
    public bool IsOwnedBy(Country targetCountry)
    {
        return country == targetCountry;
    }
    
    /// <summary>
    /// Verifica si el edificio está reclamado por algún país
    /// </summary>
    public bool IsClaimed()
    {
        return country != null;
    }

    public TriangleData GetTriangle()
    {
        return triangle;
    }
    
    /// <summary>
    /// Creates the visual representation of the building using the prefab from BuildingType
    /// </summary>
    public void CreateVisual()
    {
        // Destroy existing visual if any
        DestroyVisual();
        
        if (buildingType == null)
        {
            Debug.LogError($"Building {buildingName}: No building type assigned");
            return;
        }
        
        // Get the specific level
        var level = buildingType.GetLevel(buildingLevel);
        if (level == null)
        {
            Debug.LogError($"Building {buildingName}: Invalid level {buildingLevel} for building type {buildingType.name}");
            return;
        }
        
        // Get the prefab for this specific level
        GameObject levelPrefab = level.GetPrefab();
        if (levelPrefab == null)
        {
            Debug.LogError($"Building {buildingName}: No prefab found for {buildingType.name} level {buildingLevel}");
            return;
        }
        
        // Create the visual instance from the level-specific prefab
        visualInstance = Instantiate(levelPrefab, transform);
        visualInstance.name = $"Visual_{buildingName}_Level{buildingLevel}";
        
        // Position the visual at the center of the triangle
        if (triangle != null)
        {
            visualInstance.transform.position = triangle.GetCenter();
        }
        
        // Verify that the visual instance has the required components
        var meshRenderer = visualInstance.GetComponent<MeshRenderer>();
        var meshFilter = visualInstance.GetComponent<MeshFilter>();
        
        if (meshRenderer == null)
        {
            Debug.LogWarning($"Building {buildingName}: Visual instance has no MeshRenderer component");
        }
        
        if (meshFilter == null)
        {
            Debug.LogWarning($"Building {buildingName}: Visual instance has no MeshFilter component");
        }
        
        if (meshFilter != null && meshFilter.mesh == null)
        {
            Debug.LogWarning($"Building {buildingName}: Visual instance has no mesh assigned");
        }
        
        // Update the visual color based on country ownership
        UpdateVisualColor();
        
        Debug.Log($"Building {buildingName}: Created visual for {buildingType.name} Level {buildingLevel}");
    }
    
    /// <summary>
    /// Destroys the visual representation of the building
    /// </summary>
    public void DestroyVisual()
    {
        if (visualInstance != null)
        {
            UnityEngine.Object.DestroyImmediate(visualInstance);
            visualInstance = null;
        }
    }
    
    /// <summary>
    /// Updates the building type and level, recreating the visual if necessary
    /// </summary>
    public void UpdateBuildingType(BuildingType newBuildingType, int newLevel = 1)
    {
        bool needsVisualUpdate = (buildingType != newBuildingType || buildingLevel != newLevel);
        
        buildingType = newBuildingType;
        buildingLevel = newLevel;
        
        // Update building name
        if (buildingType != null)
        {
            string countryName = country != null ? country.name : "Unclaimed";
            buildingName = $"{countryName}_{buildingType.name}_L{newLevel}_{triangle?.id ?? 0}";
        }
        
        // Recreate visual if building type or level changed
        if (needsVisualUpdate)
        {
            CreateVisual();
        }
    }
    
    /// <summary>
    /// Initializes the building with type and level, creating the visual representation
    /// </summary>
    public void Initialize(BuildingType newBuildingType, int newLevel = 1)
    {
        UpdateBuildingType(newBuildingType, newLevel);
        CreateVisual();
    }
    
    /// <summary>
    /// Gets the mesh renderer of the building's visual representation
    /// </summary>
    public MeshRenderer GetMeshRenderer()
    {
        if (visualInstance == null) return null;
        return visualInstance.GetComponent<MeshRenderer>();
    }
    
    /// <summary>
    /// Gets the mesh filter of the building's visual representation
    /// </summary>
    public MeshFilter GetMeshFilter()
    {
        if (visualInstance == null) return null;
        return visualInstance.GetComponent<MeshFilter>();
    }
    
    /// <summary>
    /// Gets the mesh of the building's visual representation
    /// </summary>
    public Mesh GetMesh()
    {
        var meshFilter = GetMeshFilter();
        return meshFilter != null ? meshFilter.mesh : null;
    }
    
    /// <summary>
    /// Gets the material of the building's visual representation
    /// </summary>
    public Material GetMaterial()
    {
        var meshRenderer = GetMeshRenderer();
        return meshRenderer != null ? meshRenderer.material : null;
    }
    
    /// <summary>
    /// Gets all materials of the building's visual representation (for multi-material objects)
    /// </summary>
    public Material[] GetMaterials()
    {
        var meshRenderer = GetMeshRenderer();
        return meshRenderer != null ? meshRenderer.materials : new Material[0];
    }
    
    /// <summary>
    /// Sets the material of the building's visual representation
    /// </summary>
    public void SetMaterial(Material newMaterial)
    {
        var meshRenderer = GetMeshRenderer();
        if (meshRenderer != null)
        {
            meshRenderer.material = newMaterial;
        }
    }
    
    /// <summary>
    /// Sets all materials of the building's visual representation
    /// </summary>
    public void SetMaterials(Material[] newMaterials)
    {
        var meshRenderer = GetMeshRenderer();
        if (meshRenderer != null)
        {
            meshRenderer.materials = newMaterials;
        }
    }
    
    /// <summary>
    /// Gets the bounds of the building's visual representation
    /// </summary>
    public Bounds GetBounds()
    {
        if (visualInstance == null) return new Bounds();
        
        var renderer = visualInstance.GetComponent<Renderer>();
        return renderer != null ? renderer.bounds : new Bounds();
    }
    
    /// <summary>
    /// Checks if the building has a valid visual representation
    /// </summary>
    public bool HasVisual()
    {
        return visualInstance != null;
    }
    
    /// <summary>
    /// Gets information about the building's visual representation
    /// </summary>
    public string GetVisualInfo()
    {
        if (visualInstance == null)
        {
            return "No visual representation";
        }
        
        var meshRenderer = GetMeshRenderer();
        var meshFilter = GetMeshFilter();
        var mesh = GetMesh();
        var material = GetMaterial();
        
        string info = $"Visual: {visualInstance.name}\n";
        
        if (meshFilter != null)
        {
            info += $"MeshFilter: {(meshFilter.mesh != null ? "Has mesh" : "No mesh")}\n";
            if (mesh != null)
            {
                info += $"Mesh: {mesh.name} ({mesh.vertexCount} vertices, {mesh.triangles.Length / 3} triangles)\n";
            }
        }
        else
        {
            info += "MeshFilter: Missing\n";
        }
        
        if (meshRenderer != null)
        {
            info += $"MeshRenderer: Has {(meshRenderer.materials.Length)} material(s)\n";
            if (material != null)
            {
                info += $"Material: {material.name}\n";
            }
        }
        else
        {
            info += "MeshRenderer: Missing\n";
        }
        
        return info;
    }
}

 


