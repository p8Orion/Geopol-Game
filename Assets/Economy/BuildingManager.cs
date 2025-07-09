using UnityEngine;
using System.Collections.Generic;

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
    /// <param name="level">El nivel del edificio (por defecto 1)</param>
    /// <returns>El edificio creado, o null si falló</returns>
    public Building CreateBuilding(TriangleData triangle, BuildingType buildingType, int level = 1)
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
        
        // Obtener el prefab del tipo de edificio
        GameObject prefab = buildingType.prefab;
        if (prefab == null)
        {
            Debug.LogError($"No prefab found for building type: {buildingType.name}");
            return null;
        }
        
        // Crear el edificio
        Vector3 position = triangle.GetCenter();
        position.y += buildingHeight; // Usar la altura por defecto del BuildingManager
        
        GameObject buildingGO = Instantiate(prefab, position, Quaternion.identity, transform);
        Building building = buildingGO.GetComponent<Building>();
        
        if (building == null)
        {
            Debug.LogError($"Building prefab {prefab.name} does not have Building component!");
            Destroy(buildingGO);
            return null;
        }
        
        // Configurar el edificio
        building.SetTriangle(triangle);
        building.buildingType = buildingType;
        building.buildingLevel = level;
        
        // Crear nombre con país, tipo y nivel
        string countryName = triangle.country != null ? triangle.country.name : "Unclaimed";
        building.buildingName = $"{countryName}_{buildingType.name}_L{level}_{triangle.id}";
        
        // Agregar a la lista de edificios activos
        activeBuildings.Add(building);
        
        Debug.Log($"Created {buildingType.name} Level {level} building on triangle {triangle.id}");
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