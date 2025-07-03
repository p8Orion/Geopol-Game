using UnityEngine;

[System.Serializable]
public enum ResourceType
{
    // Special
    None,               // No resource
    
    // Basic resources
    Cereal,             // 🌾
    Fish,               // 🐟
    FreshWater,         // 💧
    Iron,               // 🪨
    IndustrialGoods,    // ⚙️
    ConsumerGoods,      // 🛍️
    NavalMaterials,     // ⚓
    HighTech,           // 🔬
    Hydrocarbons,       // 🛢️
    Electricity,        // ⚡
    RareEarths,         // 🏔️
    Uranium,            // ☢️
    Gold                // 🪙
}

public static class ResourceTypeExtensions
{
    public static string GetEmoji(this ResourceType resourceType)
    {
        return resourceType switch
        {
            ResourceType.None => "❌",
            ResourceType.Cereal => "🌾",
            ResourceType.Fish => "🐟",
            ResourceType.FreshWater => "💧",
            ResourceType.Iron => "🪨",
            ResourceType.IndustrialGoods => "⚙️",
            ResourceType.ConsumerGoods => "🛍️",
            ResourceType.NavalMaterials => "⚓",
            ResourceType.HighTech => "🔬",
            ResourceType.Hydrocarbons => "🛢️",
            ResourceType.Electricity => "⚡",
            ResourceType.RareEarths => "🏔️",
            ResourceType.Uranium => "☢️",
            ResourceType.Gold => "🪙",
            _ => "❓"
        };
    }
    
    public static string GetDisplayName(this ResourceType resourceType)
    {
        return resourceType switch
        {
            ResourceType.None => "No Resource",
            ResourceType.Cereal => "Cereal",
            ResourceType.Fish => "Fish",
            ResourceType.FreshWater => "Fresh Water",
            ResourceType.Iron => "Iron",
            ResourceType.IndustrialGoods => "Industrial Goods",
            ResourceType.ConsumerGoods => "Consumer Goods",
            ResourceType.NavalMaterials => "Naval Materials",
            ResourceType.HighTech => "High Technology",
            ResourceType.Hydrocarbons => "Hydrocarbons",
            ResourceType.Electricity => "Electricity",
            ResourceType.RareEarths => "Rare Earths",
            ResourceType.Uranium => "Uranium",
            ResourceType.Gold => "Gold",
            _ => "Unknown"
        };
    }
    
    public static bool IsNaturalResource(this ResourceType resourceType)
    {
        return resourceType switch
        {
            ResourceType.None => false,
            ResourceType.Cereal => true,
            ResourceType.Fish => true,
            ResourceType.FreshWater => true,
            ResourceType.Iron => true,
            ResourceType.IndustrialGoods => false, // Processed from iron
            ResourceType.ConsumerGoods => false, // Processed from industrial goods
            ResourceType.NavalMaterials => false, // Processed from iron/industrial
            ResourceType.HighTech => false, // Processed from rare earths + industrial
            ResourceType.Hydrocarbons => true,
            ResourceType.Electricity => false, // Generated from various sources
            ResourceType.RareEarths => true,
            ResourceType.Uranium => true,
            ResourceType.Gold => true,
            _ => false
        };
    }
} 