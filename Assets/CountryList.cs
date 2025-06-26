using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class CountryList
{
    [Header("Available Countries")]
    public List<Country> countries = new List<Country>();
    
    [Header("Default Countries")]
    public bool useDefaultCountries = true;
    
    public CountryList()
    {
        if (useDefaultCountries)
        {
            CreateDefaultCountries();
        }
    }
    
    /// <summary>
    /// Creates a set of default countries for the world
    /// </summary>
    private void CreateDefaultCountries()
    {
        countries.Clear();
        
        // Major world countries with realistic colors
        countries.Add(new Country("United States", new Color(0.2f, 0.4f, 0.8f))); // Blue
        countries.Add(new Country("Canada", new Color(0.8f, 0.2f, 0.2f))); // Red
        countries.Add(new Country("Mexico", new Color(0.2f, 0.8f, 0.2f))); // Green
        countries.Add(new Country("Brazil", new Color(0.2f, 0.6f, 0.2f))); // Dark Green
        countries.Add(new Country("Argentina", new Color(0.8f, 0.8f, 0.2f))); // Yellow
        countries.Add(new Country("United Kingdom", new Color(0.8f, 0.2f, 0.6f))); // Pink
        countries.Add(new Country("France", new Color(0.2f, 0.2f, 0.8f))); // Blue
        /*
        countries.Add(new Country("Germany", new Color(0.8f, 0.6f, 0.2f))); // Orange
        countries.Add(new Country("Spain", new Color(0.8f, 0.4f, 0.2f))); // Orange-Red
        countries.Add(new Country("Italy", new Color(0.2f, 0.8f, 0.6f))); // Teal
        countries.Add(new Country("Russia", new Color(0.8f, 0.2f, 0.2f))); // Red
        countries.Add(new Country("China", new Color(0.8f, 0.2f, 0.2f))); // Red
        countries.Add(new Country("Japan", new Color(0.8f, 0.2f, 0.2f))); // Red
        countries.Add(new Country("India", new Color(0.8f, 0.6f, 0.2f))); // Orange
        countries.Add(new Country("Australia", new Color(0.2f, 0.6f, 0.8f))); // Light Blue
        countries.Add(new Country("South Africa", new Color(0.8f, 0.8f, 0.2f))); // Yellow
        countries.Add(new Country("Egypt", new Color(0.8f, 0.6f, 0.2f))); // Orange
        countries.Add(new Country("Nigeria", new Color(0.2f, 0.8f, 0.2f))); // Green
        countries.Add(new Country("Turkey", new Color(0.8f, 0.2f, 0.2f))); // Red
        countries.Add(new Country("Iran", new Color(0.2f, 0.8f, 0.2f))); // Green
        countries.Add(new Country("Saudi Arabia", new Color(0.2f, 0.6f, 0.2f))); // Dark Green
        countries.Add(new Country("Pakistan", new Color(0.2f, 0.8f, 0.2f))); // Green
        countries.Add(new Country("Indonesia", new Color(0.8f, 0.2f, 0.2f))); // Red
        countries.Add(new Country("Thailand", new Color(0.8f, 0.2f, 0.6f))); // Pink
        countries.Add(new Country("Vietnam", new Color(0.8f, 0.2f, 0.2f))); // Red
        countries.Add(new Country("Philippines", new Color(0.2f, 0.2f, 0.8f))); // Blue
        countries.Add(new Country("Malaysia", new Color(0.8f, 0.2f, 0.2f))); // Red
        countries.Add(new Country("Singapore", new Color(0.8f, 0.2f, 0.2f))); // Red
        countries.Add(new Country("South Korea", new Color(0.2f, 0.2f, 0.8f))); // Blue
        countries.Add(new Country("North Korea", new Color(0.8f, 0.2f, 0.2f))); // Red
        countries.Add(new Country("Mongolia", new Color(0.8f, 0.6f, 0.2f))); // Orange
        countries.Add(new Country("Kazakhstan", new Color(0.2f, 0.6f, 0.8f))); // Light Blue
        countries.Add(new Country("Ukraine", new Color(0.8f, 0.8f, 0.2f))); // Yellow
        countries.Add(new Country("Poland", new Color(0.8f, 0.2f, 0.2f))); // Red
        countries.Add(new Country("Sweden", new Color(0.2f, 0.2f, 0.8f))); // Blue
        countries.Add(new Country("Norway", new Color(0.2f, 0.6f, 0.8f))); // Light Blue
        countries.Add(new Country("Finland", new Color(0.2f, 0.2f, 0.8f))); // Blue
        countries.Add(new Country("Denmark", new Color(0.8f, 0.2f, 0.2f))); // Red
        countries.Add(new Country("Netherlands", new Color(0.8f, 0.6f, 0.2f))); // Orange
        countries.Add(new Country("Belgium", new Color(0.8f, 0.2f, 0.2f))); // Red
        countries.Add(new Country("Switzerland", new Color(0.8f, 0.2f, 0.2f))); // Red
        countries.Add(new Country("Austria", new Color(0.8f, 0.2f, 0.2f))); // Red
        countries.Add(new Country("Hungary", new Color(0.8f, 0.2f, 0.2f))); // Red
        countries.Add(new Country("Czech Republic", new Color(0.8f, 0.2f, 0.2f))); // Red
        countries.Add(new Country("Slovakia", new Color(0.2f, 0.6f, 0.8f))); // Light Blue
        countries.Add(new Country("Croatia", new Color(0.8f, 0.2f, 0.2f))); // Red
        countries.Add(new Country("Serbia", new Color(0.8f, 0.2f, 0.2f))); // Red
        countries.Add(new Country("Romania", new Color(0.2f, 0.6f, 0.8f))); // Light Blue
        countries.Add(new Country("Bulgaria", new Color(0.8f, 0.6f, 0.2f))); // Orange
        countries.Add(new Country("Greece", new Color(0.2f, 0.6f, 0.8f))); // Light Blue
        countries.Add(new Country("Portugal", new Color(0.8f, 0.2f, 0.2f))); // Red
        countries.Add(new Country("Ireland", new Color(0.2f, 0.8f, 0.2f))); // Green
        countries.Add(new Country("Iceland", new Color(0.2f, 0.6f, 0.8f))); // Light Blue
        countries.Add(new Country("Greenland", new Color(0.2f, 0.8f, 0.2f))); // Green
        countries.Add(new Country("Chile", new Color(0.8f, 0.2f, 0.2f))); // Red
        countries.Add(new Country("Peru", new Color(0.8f, 0.2f, 0.2f))); // Red
        countries.Add(new Country("Colombia", new Color(0.2f, 0.8f, 0.2f))); // Green
        countries.Add(new Country("Venezuela", new Color(0.8f, 0.2f, 0.2f))); // Red
        countries.Add(new Country("Ecuador", new Color(0.8f, 0.8f, 0.2f))); // Yellow
        countries.Add(new Country("Bolivia", new Color(0.8f, 0.6f, 0.2f))); // Orange
        countries.Add(new Country("Paraguay", new Color(0.2f, 0.6f, 0.8f))); // Light Blue
        countries.Add(new Country("Uruguay", new Color(0.2f, 0.2f, 0.8f))); // Blue
        countries.Add(new Country("Guyana", new Color(0.2f, 0.8f, 0.2f))); // Green
        countries.Add(new Country("Suriname", new Color(0.8f, 0.6f, 0.2f))); // Orange
        countries.Add(new Country("French Guiana", new Color(0.2f, 0.2f, 0.8f))); // Blue
        countries.Add(new Country("Panama", new Color(0.8f, 0.2f, 0.2f))); // Red
        countries.Add(new Country("Costa Rica", new Color(0.2f, 0.8f, 0.2f))); // Green
        countries.Add(new Country("Nicaragua", new Color(0.2f, 0.6f, 0.8f))); // Light Blue
        countries.Add(new Country("Honduras", new Color(0.8f, 0.8f, 0.2f))); // Yellow
        countries.Add(new Country("El Salvador", new Color(0.2f, 0.2f, 0.8f))); // Blue
        countries.Add(new Country("Guatemala", new Color(0.8f, 0.6f, 0.2f))); // Orange
        countries.Add(new Country("Belize", new Color(0.2f, 0.8f, 0.2f))); // Green
        countries.Add(new Country("Cuba", new Color(0.8f, 0.2f, 0.2f))); // Red
        countries.Add(new Country("Jamaica", new Color(0.8f, 0.8f, 0.2f))); // Yellow
        countries.Add(new Country("Haiti", new Color(0.2f, 0.6f, 0.8f))); // Light Blue
        countries.Add(new Country("Dominican Republic", new Color(0.8f, 0.2f, 0.2f))); // Red
        countries.Add(new Country("Puerto Rico", new Color(0.2f, 0.8f, 0.2f))); // Green
        countries.Add(new Country("Bahamas", new Color(0.2f, 0.6f, 0.8f))); // Light Blue
        countries.Add(new Country("Trinidad and Tobago", new Color(0.8f, 0.2f, 0.2f))); // Red
        countries.Add(new Country("Barbados", new Color(0.8f, 0.6f, 0.2f))); // Orange
        countries.Add(new Country("Grenada", new Color(0.2f, 0.8f, 0.2f))); // Green
        countries.Add(new Country("Saint Lucia", new Color(0.2f, 0.6f, 0.8f))); // Light Blue
        countries.Add(new Country("Saint Vincent", new Color(0.8f, 0.2f, 0.2f))); // Red
        countries.Add(new Country("Antigua and Barbuda", new Color(0.8f, 0.8f, 0.2f))); // Yellow
        countries.Add(new Country("Saint Kitts and Nevis", new Color(0.2f, 0.8f, 0.2f))); // Green
        countries.Add(new Country("Dominica", new Color(0.8f, 0.6f, 0.2f))); // Orange
        countries.Add(new Country("Montserrat", new Color(0.2f, 0.2f, 0.8f))); // Blue
        countries.Add(new Country("Anguilla", new Color(0.8f, 0.2f, 0.2f))); // Red
        countries.Add(new Country("British Virgin Islands", new Color(0.2f, 0.6f, 0.8f))); // Light Blue
        countries.Add(new Country("US Virgin Islands", new Color(0.2f, 0.2f, 0.8f))); // Blue
        countries.Add(new Country("Cayman Islands", new Color(0.8f, 0.8f, 0.2f))); // Yellow
        countries.Add(new Country("Turks and Caicos", new Color(0.2f, 0.8f, 0.2f))); // Green
        countries.Add(new Country("Bermuda", new Color(0.8f, 0.2f, 0.2f))); // Red
        countries.Add(new Country("Falkland Islands", new Color(0.2f, 0.6f, 0.8f))); // Light Blue
        countries.Add(new Country("South Georgia", new Color(0.8f, 0.6f, 0.2f))); // Orange
        countries.Add(new Country("Antarctica", new Color(0.8f, 0.8f, 0.8f))); // White*/
    }
    
    /// <summary>
    /// Creates a new country and adds it to the list
    /// </summary>
    public Country CreateCountry()
    {
        Country newCountry = new Country();
        newCountry.InitializeRandomColor(); // Initialize random color on main thread
        newCountry.index = countries.Count;
        countries.Add(newCountry);
        return newCountry;
    }
    
    /// <summary>
    /// Creates a new country with a specific name and color
    /// </summary>
    public Country CreateCountry(string name, Color color)
    {
        Country newCountry = new Country(name, color);
        newCountry.index = countries.Count;
        countries.Add(newCountry);
        return newCountry;
    }
    
    /// <summary>
    /// Removes a country from the list and unclaims all its territory
    /// </summary>
    public void RemoveCountry(Country country)
    {
        if (country == null) return;
        
        // Unclaim all territory
        foreach (var triangle in country.territory.ToList())
        {
            triangle.RemoveFromCountry();
        }
        
        // Remove from list and rebuild all indices to ensure they remain correct.
        if (countries.Remove(country))
        {
            RebuildCountryIndices();
        }
    }
    
    /// <summary>
    /// Recalculates and assigns the correct index to every country in the list.
    /// Crucial to call this after loading countries from a save file.
    /// </summary>
    public void RebuildCountryIndices()
    {
        for (int i = 0; i < countries.Count; i++)
        {
            if (countries[i] != null)
            {
                countries[i].index = i;
            }
        }
    }
    
    /// <summary>
    /// Gets a country by name
    /// </summary>
    public Country GetCountryByName(string name)
    {
        return countries.Find(c => c.name == name);
    }

    /// <summary>
    /// Gets a country by index
    /// </summary>
    public Country GetCountryByIndex(int index)
    {
        if (index >= 0 && index < countries.Count)
        {
            return countries[index];
        }
        return null;
    }
    
    /// <summary>
    /// Gets the index of a country in the list
    /// </summary>
    public int GetCountryIndex(Country country)
    {
        return countries.IndexOf(country);
    }
    
    /// <summary>
    /// Gets the index of a country by its name. This is more reliable than by instance.
    /// </summary>
    public int GetCountryIndexByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return -1;
        
        for (int i = 0; i < countries.Count; i++)
        {
            if (countries[i].name == name)
            {
                return i;
            }
        }
        
        return -1; // Not found
    }
    
    /// <summary>
    /// Gets all unclaimed triangles from a list of triangles
    /// </summary>
    public List<TriangleData> GetUnclaimedTriangles(List<TriangleData> allTriangles)
    {
        return allTriangles.FindAll(t => t.country == null);
    }
    
    /// <summary>
    /// Gets all triangles claimed by a specific country
    /// </summary>
    public List<TriangleData> GetTrianglesByCountry(Country country, List<TriangleData> allTriangles)
    {
        return allTriangles.FindAll(t => t.country == country);
    }
    
    /// <summary>
    /// Gets statistics about country distribution
    /// </summary>
    public Dictionary<Country, int> GetCountryStatistics(List<TriangleData> allTriangles)
    {
        var stats = new Dictionary<Country, int>();
        
        foreach (var country in countries)
        {
            stats[country] = 0;
        }
        
        foreach (var triangle in allTriangles)
        {
            if (triangle.country != null)
            {
                if (stats.ContainsKey(triangle.country))
                {
                    stats[triangle.country]++;
                }
            }
        }
        
        return stats;
    }
    
    /// <summary>
    /// Clears all country assignments from triangles
    /// </summary>
    public void ClearAllCountryAssignments(List<TriangleData> allTriangles)
    {
        foreach (var triangle in allTriangles)
        {
            triangle.RemoveFromCountry();
        }
    }
    
    /// <summary>
    /// Resets to default countries
    /// </summary>
    public void ResetToDefaults(List<TriangleData> allTriangles)
    {
        ClearAllCountryAssignments(allTriangles);
        CreateDefaultCountries();
    }
    
    /// <summary>
    /// Gets a string representation of the country list
    /// </summary>
    public override string ToString()
    {
        return $"CountryList: {countries.Count} countries";
    }
} 