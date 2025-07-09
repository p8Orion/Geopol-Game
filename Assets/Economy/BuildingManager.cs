using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manager para crear y gestionar edificios en el mundo
/// </summary>
public class BuildingManager : MonoBehaviour
{
    [Header("Building Settings")]
    public float buildingHeight = 1f;
    
    private List<Building> activeBuildings = new List<Building>();
    

    
    /// <summary>
    /// Crea un edificio en el triángulo especificado
    /// </summary>
    /// <param name="triangle">El triángulo donde crear el edificio</param>
    /// <param name="buildingType">El tipo de edificio a crear</param>
    /// <param name="ownerCountry">El país propietario del edificio (null = usar el país del triángulo)</param>
    /// <param name="level">El nivel del edificio (por defecto 1)</param>
    /// <returns>El edificio creado, o null si falló</returns>
    public Building CreateBuilding(TriangleData triangle, BuildingType buildingType, Country ownerCountry = null, int level = 1)
    {
        if (triangle == null)
        {
            Debug.LogError("Cannot create building: triangle is null");
            return null;
        }
        
        // Verificar que el tipo de edificio sea válido
        if (buildingType == null)
        {
            Debug.LogError("Cannot create building: buildingType is null");
            return null;
        }
        
        // Verificar que el nivel sea válido
        var buildingLevel = buildingType.GetLevel(level);
        if (buildingLevel == null)
        {
            Debug.LogError($"Invalid level {level} for building type: {buildingType.name}");
            return null;
        }
        
        // Crear el GameObject del edificio (sin prefab visual)
        Vector3 position = triangle.GetCenter();
        position.y += buildingHeight; // Usar la altura por defecto del BuildingManager
        
        // Determinar el país propietario
        Country finalOwnerCountry = ownerCountry ?? triangle.country;
        string countryName = finalOwnerCountry != null ? finalOwnerCountry.name : "Unknown";
        
        GameObject buildingGO = new GameObject($"{buildingType.name}_{level}_{countryName}_{triangle.id}");
        buildingGO.transform.position = position;
        buildingGO.transform.SetParent(transform);
        
        Building building = buildingGO.AddComponent<Building>();
        
        if (building == null)
        {
            Debug.LogError("Failed to add Building component!");
            Destroy(buildingGO);
            return null;
        }
        
        // Configurar el edificio usando la nueva lógica
        building.SetTriangle(triangle);
        building.SetCountry(finalOwnerCountry); // Establecer el país propietario explícitamente
        building.Initialize(buildingType, level);
        
        // Agregar a la lista de edificios activos
        activeBuildings.Add(building);
        
        Debug.Log($"Created {buildingType.name} Level {level} building on triangle {triangle.id}, owned by {countryName}");
        return building;
    }
    
    /// <summary>
    /// Obtiene todos los edificios activos
    /// </summary>
    public List<Building> GetActiveBuildings()
    {
        return new List<Building>(activeBuildings);
    }
    
    /// <summary>
    /// Obtiene todos los edificios activos que tienen un triángulo asignado
    /// </summary>
    public List<Building> GetActiveBuildingsWithTriangles()
    {
        return activeBuildings.Where(building => building != null && building.triangle != null).ToList();
    }
    
    /// <summary>
    /// Obtiene edificios de un tipo específico
    /// </summary>
    public List<Building> GetBuildingsByType(BuildingType buildingType)
    {
        List<Building> result = new List<Building>();
        foreach (Building building in activeBuildings)
        {
            if (building.buildingType == buildingType)
            {
                result.Add(building);
            }
        }
        return result;
    }
    
    /// <summary>
    /// Obtiene todos los tipos de edificios disponibles
    /// </summary>
    public BuildingType[] GetAvailableBuildingTypes()
    {
        return BuildingType.GetAllBuildingTypes().ToArray();
    }
    
    /// <summary>
    /// Obtiene un tipo de edificio por nombre
    /// </summary>
    public BuildingType GetBuildingTypeByName(string name)
    {
        return BuildingType.GetByName(name);
    }
    
    /// <summary>
    /// Obtiene un tipo de edificio por índice
    /// </summary>
    public BuildingType GetBuildingTypeByIndex(int index)
    {
        return BuildingType.GetByIndex(index);
    }
    
    /// <summary>
    /// Obtiene edificios de un tipo y nivel específicos
    /// </summary>
    public List<Building> GetBuildingsByTypeAndLevel(BuildingType buildingType, int level)
    {
        List<Building> result = new List<Building>();
        foreach (Building building in activeBuildings)
        {
            if (building.buildingType == buildingType && building.buildingLevel == level)
            {
                result.Add(building);
            }
        }
        return result;
    }
    
    /// <summary>
    /// Obtiene edificios de un nivel específico
    /// </summary>
    public List<Building> GetBuildingsByLevel(int level)
    {
        List<Building> result = new List<Building>();
        foreach (Building building in activeBuildings)
        {
            if (building.buildingLevel == level)
            {
                result.Add(building);
            }
        }
        return result;
    }
    
    /// <summary>
    /// Obtiene el nivel máximo de un tipo de edificio
    /// </summary>
    public int GetMaxLevel(BuildingType buildingType)
    {
        return buildingType?.GetMaxLevel() ?? 0;
    }
    
    /// <summary>
    /// Obtiene el nivel mínimo de un tipo de edificio
    /// </summary>
    public int GetMinLevel(BuildingType buildingType)
    {
        return buildingType?.GetMinLevel() ?? 0;
    }
    
    /// <summary>
    /// Destruye un edificio
    /// </summary>
    public void DestroyBuilding(Building building)
    {
        if (building != null && activeBuildings.Contains(building))
        {
            // Destroy visual representation first
            building.DestroyVisual();
            
            activeBuildings.Remove(building);
            Destroy(building.gameObject);
            Debug.Log($"Destroyed building: {building.buildingName}");
        }
    }
    
    /// <summary>
    /// Destruye todos los edificios
    /// </summary>
    public void DestroyAllBuildings()
    {
        for (int i = activeBuildings.Count - 1; i >= 0; i--)
        {
            DestroyBuilding(activeBuildings[i]);
        }
    }
} 