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
    [SerializeField] private string uniqueId;
    
    [Header("Location")]
    public TriangleData triangle;
    
    [Header("Ownership")]
    public Country country;
    
    [Header("Resource Acceptance")]
    public ResourceType[] acceptedResourceTypes = new ResourceType[0];
    public bool acceptAllResources = false;
    
    [Header("Visual")]
    public GameObject buildingModel;
    public Material buildingMaterial;
    
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
        
        Debug.Log($"Building {buildingName} accepted resource {resource.type}");
        return true;
    }
    // Métodos adicionales del edificio
    public void SetTriangle(TriangleData newTriangle)
    {
        triangle = newTriangle;
        if (triangle != null)
        {
            transform.position = triangle.GetCenter();
            
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
        Debug.Log($"Building {buildingName} is now owned by {newCountry?.name ?? "No Country"}");
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
}

 


