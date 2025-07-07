using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manager para crear y gestionar edificios en el mundo
/// </summary>
public class BuildingManager : MonoBehaviour
{
    [Header("Building Prefabs")]
    public GameObject factoryPrefab;
    public GameObject warehousePrefab;
    public GameObject marketPrefab;
    
    [Header("Building Settings")]
    public float buildingHeight = 1f;
    
    private List<Building> activeBuildings = new List<Building>();
    
    /// <summary>
    /// Crea un edificio en el triángulo especificado
    /// </summary>
    /// <param name="triangle">El triángulo donde crear el edificio</param>
    /// <param name="buildingType">El tipo de edificio a crear</param>
    /// <returns>El edificio creado, o null si falló</returns>
    public Building CreateBuilding(TriangleData triangle, BuildingType buildingType)
    {
        if (triangle == null)
        {
            Debug.LogError("Cannot create building: triangle is null");
            return null;
        }
        
        // Seleccionar el prefab según el tipo
        GameObject prefab = GetPrefabForBuildingType(buildingType);
        if (prefab == null)
        {
            Debug.LogError($"No prefab found for building type: {buildingType}");
            return null;
        }
        
        // Crear el edificio
        Vector3 position = triangle.GetCenter();
        position.y += buildingHeight; // Elevar un poco el edificio
        
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
        
        // Crear nombre con país y tipo
        string countryName = triangle.country != null ? triangle.country.name : "Unclaimed";
        building.buildingName = $"{countryName}_{buildingType}_{triangle.id}";
        
        // Configurar qué recursos acepta según el tipo
        ConfigureBuildingResourceAcceptance(building, buildingType);
        
        // Agregar a la lista de edificios activos
        activeBuildings.Add(building);
        
        Debug.Log($"Created {buildingType} building on triangle {triangle.id}");
        return building;
    }
    
    private GameObject GetPrefabForBuildingType(BuildingType buildingType)
    {
        switch (buildingType)
        {
            case BuildingType.Factory:
                return factoryPrefab;
            case BuildingType.Warehouse:
                return warehousePrefab;
            case BuildingType.Market:
                return marketPrefab;
            default:
                Debug.LogWarning($"No prefab configured for building type: {buildingType}");
                return factoryPrefab; // Fallback
        }
    }
    
    private void ConfigureBuildingResourceAcceptance(Building building, BuildingType buildingType)
    {
        switch (buildingType)
        {
            case BuildingType.Factory:
                // Las fábricas aceptan materias primas
                building.SetAcceptedResourceTypes(new ResourceType[] 
                { 
                    ResourceType.Iron, 
                    ResourceType.Gold, 
                    ResourceType.Uranium,
                    ResourceType.RareEarths 
                });
                break;
                
            case BuildingType.Warehouse:
                // Los almacenes aceptan todos los recursos
                building.SetAcceptAllResources(true);
                break;
                
            case BuildingType.Market:
                // Los mercados aceptan bienes de consumo
                building.SetAcceptedResourceTypes(new ResourceType[] 
                { 
                    ResourceType.ConsumerGoods,
                    ResourceType.IndustrialGoods,
                    ResourceType.HighTech
                });
                break;
                
            default:
                // Por defecto, aceptar todos los recursos
                building.SetAcceptAllResources(true);
                break;
        }
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