using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Representa un nivel específico de un tipo de edificio
/// </summary>
[System.Serializable]
public class BuildingLevel
{
    [Header("Level Info")]
    public int level;
    public string displayName;
    
    [Header("Resources")]
    public ResourceType[] acceptedResources = new ResourceType[0];
    public ResourceType[] producedResources = new ResourceType[0];
    
    public BuildingLevel(int level, string displayName = null)
    {
        this.level = level;
        this.displayName = displayName ?? $"Level {level}";
    }
    
    /// <summary>
    /// Obtiene el nombre para mostrar del nivel
    /// </summary>
    public string GetDisplayName()
    {
        return displayName ?? $"Level {level}";
    }
    
    /// <summary>
    /// Verifica si este nivel acepta un recurso específico
    /// </summary>
    public bool AcceptsResource(ResourceType resourceType)
    {
        return acceptedResources.Contains(resourceType);
    }
    
    /// <summary>
    /// Verifica si este nivel produce un recurso específico
    /// </summary>
    public bool ProducesResource(ResourceType resourceType)
    {
        return producedResources.Contains(resourceType);
    }
}

/// <summary>
/// Clase que representa un tipo de edificio con múltiples niveles
/// </summary>
[System.Serializable]
public class BuildingType
{
    [Header("Basic Info")]
    public string name;
    public string displayName;
    
    [Header("Prefab")]
    public GameObject prefab;
    
    [Header("Levels")]
    public List<BuildingLevel> levels = new List<BuildingLevel>();
    
    // Constructor
    public BuildingType(string name, string displayName = null)
    {
        this.name = name;
        this.displayName = displayName ?? name;
    }
    
    /// <summary>
    /// Obtiene el emoji asociado al tipo de edificio
    /// </summary>
    public string GetEmoji()
    {
        switch (name.ToLower())
        {
            case "factory": return "🏭";
            case "warehouse": return "🏪";
            case "market": return "🛒";
            case "port": return "🚢";
            case "mine": return "⛏️";
            case "farm": return "🌾";
            case "powerplant": return "⚡";
            case "researchcenter": return "🔬";
            default: return "🏢";
        }
    }
    
    /// <summary>
    /// Obtiene el nombre para mostrar
    /// </summary>
    public string GetDisplayName()
    {
        return displayName ?? name;
    }
    
    /// <summary>
    /// Obtiene un nivel específico
    /// </summary>
    public BuildingLevel GetLevel(int level)
    {
        return levels.FirstOrDefault(l => l.level == level);
    }
    
    /// <summary>
    /// Obtiene el nivel máximo disponible
    /// </summary>
    public int GetMaxLevel()
    {
        return levels.Count > 0 ? levels.Max(l => l.level) : 0;
    }
    
    /// <summary>
    /// Obtiene el nivel mínimo disponible
    /// </summary>
    public int GetMinLevel()
    {
        return levels.Count > 0 ? levels.Min(l => l.level) : 0;
    }
    
    // --- Lista Estática de Tipos de Edificios con Niveles ---
    
    // Factory
    public static readonly BuildingType Factory = new BuildingType("Factory", "Factory")
    {
        levels = new List<BuildingLevel>
        {
            new BuildingLevel(1, "Basic Factory")
            {
                acceptedResources = new ResourceType[] { ResourceType.Iron, ResourceType.Gold },
                producedResources = new ResourceType[] { ResourceType.IndustrialGoods }
            },
            new BuildingLevel(2, "Advanced Factory")
            {
                acceptedResources = new ResourceType[] { ResourceType.Iron, ResourceType.Gold, ResourceType.RareEarths },
                producedResources = new ResourceType[] { ResourceType.IndustrialGoods, ResourceType.HighTech }
            },
            new BuildingLevel(3, "Automated Factory")
            {
                acceptedResources = new ResourceType[] { ResourceType.Iron, ResourceType.Gold, ResourceType.RareEarths, ResourceType.Uranium },
                producedResources = new ResourceType[] { ResourceType.IndustrialGoods, ResourceType.HighTech, ResourceType.ConsumerGoods }
            }
        }
    };
    
    // Warehouse
    public static readonly BuildingType Warehouse = new BuildingType("Warehouse", "Warehouse")
    {
        levels = new List<BuildingLevel>
        {
            new BuildingLevel(1, "Small Warehouse")
            {
                acceptedResources = new ResourceType[0], // Almacena todos
                producedResources = new ResourceType[0]
            },
            new BuildingLevel(2, "Medium Warehouse")
            {
                acceptedResources = new ResourceType[0],
                producedResources = new ResourceType[0]
            },
            new BuildingLevel(3, "Large Warehouse")
            {
                acceptedResources = new ResourceType[0],
                producedResources = new ResourceType[0]
            }
        }
    };
    
    // Market
    public static readonly BuildingType Market = new BuildingType("Market", "Market")
    {
        levels = new List<BuildingLevel>
        {
            new BuildingLevel(1, "Local Market")
            {
                acceptedResources = new ResourceType[] { ResourceType.ConsumerGoods },
                producedResources = new ResourceType[] { ResourceType.Gold }
            },
            new BuildingLevel(2, "Regional Market")
            {
                acceptedResources = new ResourceType[] { ResourceType.ConsumerGoods, ResourceType.IndustrialGoods },
                producedResources = new ResourceType[] { ResourceType.Gold }
            },
            new BuildingLevel(3, "International Market")
            {
                acceptedResources = new ResourceType[] { ResourceType.ConsumerGoods, ResourceType.IndustrialGoods, ResourceType.HighTech },
                producedResources = new ResourceType[] { ResourceType.Gold }
            }
        }
    };
    
    // Mine
    public static readonly BuildingType Mine = new BuildingType("Mine", "Mine")
    {
        levels = new List<BuildingLevel>
        {
            new BuildingLevel(1, "Surface Mine")
            {
                acceptedResources = new ResourceType[0], // Extrae del terreno
                producedResources = new ResourceType[] { ResourceType.Iron, ResourceType.Gold }
            },
            new BuildingLevel(2, "Deep Mine")
            {
                acceptedResources = new ResourceType[0],
                producedResources = new ResourceType[] { ResourceType.Iron, ResourceType.Gold, ResourceType.Uranium }
            },
            new BuildingLevel(3, "Advanced Mine")
            {
                acceptedResources = new ResourceType[0],
                producedResources = new ResourceType[] { ResourceType.Iron, ResourceType.Gold, ResourceType.Uranium, ResourceType.RareEarths }
            }
        }
    };
    
    // Farm
    public static readonly BuildingType Farm = new BuildingType("Farm", "Farm")
    {
        levels = new List<BuildingLevel>
        {
            new BuildingLevel(1, "Small Farm")
            {
                acceptedResources = new ResourceType[] { ResourceType.FreshWater },
                producedResources = new ResourceType[] { ResourceType.Cereal }
            },
            new BuildingLevel(2, "Commercial Farm")
            {
                acceptedResources = new ResourceType[] { ResourceType.FreshWater },
                producedResources = new ResourceType[] { ResourceType.Cereal }
            },
            new BuildingLevel(3, "Industrial Farm")
            {
                acceptedResources = new ResourceType[] { ResourceType.FreshWater },
                producedResources = new ResourceType[] { ResourceType.Cereal }
            }
        }
    };
    
    // Power Plant
    public static readonly BuildingType PowerPlant = new BuildingType("PowerPlant", "Power Plant")
    {
        levels = new List<BuildingLevel>
        {
            new BuildingLevel(1, "Coal Plant")
            {
                acceptedResources = new ResourceType[] { ResourceType.Hydrocarbons },
                producedResources = new ResourceType[] { ResourceType.Electricity }
            },
            new BuildingLevel(2, "Nuclear Plant")
            {
                acceptedResources = new ResourceType[] { ResourceType.Uranium },
                producedResources = new ResourceType[] { ResourceType.Electricity }
            },
            new BuildingLevel(3, "Fusion Plant")
            {
                acceptedResources = new ResourceType[] { ResourceType.Uranium, ResourceType.RareEarths },
                producedResources = new ResourceType[] { ResourceType.Electricity }
            }
        }
    };
    
    // Research Center
    public static readonly BuildingType ResearchCenter = new BuildingType("ResearchCenter", "Research Center")
    {
        levels = new List<BuildingLevel>
        {
            new BuildingLevel(1, "Basic Lab")
            {
                acceptedResources = new ResourceType[] { ResourceType.HighTech, ResourceType.RareEarths },
                producedResources = new ResourceType[] { ResourceType.HighTech }
            },
            new BuildingLevel(2, "Advanced Lab")
            {
                acceptedResources = new ResourceType[] { ResourceType.HighTech, ResourceType.RareEarths, ResourceType.Uranium },
                producedResources = new ResourceType[] { ResourceType.HighTech }
            },
            new BuildingLevel(3, "Research Institute")
            {
                acceptedResources = new ResourceType[] { ResourceType.HighTech, ResourceType.RareEarths, ResourceType.Uranium, ResourceType.Gold },
                producedResources = new ResourceType[] { ResourceType.HighTech }
            }
        }
    };
    
    // --- Métodos Estáticos para Acceder a la Lista ---
    
    /// <summary>
    /// Obtiene todos los tipos de edificios disponibles
    /// </summary>
    public static List<BuildingType> GetAllBuildingTypes()
    {
        return new List<BuildingType>
        {
            Factory,
            Warehouse,
            Market,
            Mine,
            Farm,
            PowerPlant,
            ResearchCenter
        };
    }
    
    /// <summary>
    /// Obtiene un tipo de edificio por nombre
    /// </summary>
    public static BuildingType GetByName(string name)
    {
        var allTypes = GetAllBuildingTypes();
        return allTypes.FirstOrDefault(bt => bt.name == name);
    }
    
    /// <summary>
    /// Obtiene un tipo de edificio por índice
    /// </summary>
    public static BuildingType GetByIndex(int index)
    {
        var allTypes = GetAllBuildingTypes();
        if (index >= 0 && index < allTypes.Count)
        {
            return allTypes[index];
        }
        return null;
    }
    
    /// <summary>
    /// Obtiene el índice de un tipo de edificio
    /// </summary>
    public static int GetIndex(BuildingType buildingType)
    {
        var allTypes = GetAllBuildingTypes();
        return allTypes.IndexOf(buildingType);
    }
    
    /// <summary>
    /// Obtiene la cantidad total de tipos de edificios
    /// </summary>
    public static int GetCount()
    {
        return GetAllBuildingTypes().Count;
    }
    
    // --- Operadores para compatibilidad con enum ---
    
    public static implicit operator string(BuildingType buildingType)
    {
        return buildingType?.name ?? "";
    }
    
    public static bool operator ==(BuildingType a, BuildingType b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;
        return a.name == b.name;
    }
    
    public static bool operator !=(BuildingType a, BuildingType b)
    {
        return !(a == b);
    }
    
    public override bool Equals(object obj)
    {
        if (obj is BuildingType other)
        {
            return this == other;
        }
        return false;
    }
    
    public override int GetHashCode()
    {
        return name?.GetHashCode() ?? 0;
    }
    
    public override string ToString()
    {
        return GetDisplayName();
    }
} 