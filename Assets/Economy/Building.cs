using UnityEngine;

/// <summary>
/// Representa un edificio que puede recibir recursos mediante drag & drop
/// </summary>
public class Building : MonoBehaviour, IResourceAcceptor
{
    [Header("Building Properties")]
    public string buildingName = "Building";
    public BuildingType buildingType = BuildingType.Factory;
    
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
    
    // Implementación de IResourceAcceptor
    public bool CanAcceptResource(Resource resource)
    {
        if (resource == null || !resource.isActive)
            return false;
            
        // Si acepta todos los recursos, permitir
        if (acceptAllResources)
            return true;
            
        // Verificar si el tipo de recurso está en la lista de aceptados
        foreach (ResourceType acceptedType in acceptedResourceTypes)
        {
            if (resource.type == acceptedType)
                return true;
        }
        
        return false;
    }
    
    public bool AcceptResource(Resource resource)
    {
        if (!CanAcceptResource(resource))
            return false;
            
        // Establecer el destino del recurso usando el triángulo como IResourceDropPosition
        resource.SetDestination(triangle);
        
        Debug.Log($"Building {buildingName} accepted resource {resource.type}");
        return true;
    }
    
    public IResourceDropPosition GetDropPosition()
    {
        return triangle;
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
    
    public void SetAcceptedResourceTypes(ResourceType[] types)
    {
        acceptedResourceTypes = types;
        acceptAllResources = false;
    }
    
    public void SetAcceptAllResources(bool acceptAll)
    {
        acceptAllResources = acceptAll;
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
}

/// <summary>
/// Tipos de edificios disponibles
/// </summary>
public enum BuildingType
{
    Factory,
    Warehouse,
    Market,
    Port,
    Mine,
    Farm,
    PowerPlant,
    ResearchCenter
} 