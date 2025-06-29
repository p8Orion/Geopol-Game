using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BorderManager : MonoBehaviour
{
    [Header("Border Settings")]
    public Material baseBorderMaterial;
    public float defaultWidth = 5f;
    public float defaultIntensity = 0.6f;
    public float defaultFade = 1f;
    public float defaultOffset = 5f;
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    
    // Private fields
    private Dictionary<string, BorderSegment> borderSegments = new Dictionary<string, BorderSegment>();
    private Transform borderParent;
    private IcoSphere icoSphere;
    private MapEditor mapEditor;
    
    void Awake()
    {
        Debug.Log("=== BorderManager Awake() Start ===");
        
        icoSphere = GetComponent<IcoSphere>();
        if (icoSphere == null)
        {
            Debug.LogError("BorderManager: IcoSphere component not found!");
            return;
        }
        
        // Find MapEditor for country data
        mapEditor = FindObjectOfType<MapEditor>();
        if (mapEditor == null)
        {
            Debug.LogError("BorderManager: MapEditor not found in scene!");
            return;
        }
        
        // Create border parent object
        borderParent = new GameObject("BorderContainer").transform;
        borderParent.SetParent(transform);
        borderParent.localPosition = Vector3.zero;
        borderParent.localRotation = Quaternion.identity;
        borderParent.localScale = Vector3.one;
        
        // Create base material if not assigned
        if (baseBorderMaterial == null)
        {
            Shader borderShader = Shader.Find("Custom/CountryBorder");
            if (borderShader != null)
            {
                baseBorderMaterial = new Material(borderShader);
                Debug.Log("BorderManager: Created base border material");
            }
            else
            {
                Debug.LogError("BorderManager: Custom/CountryBorder shader not found!");
                return;
            }
        }
        
        Debug.Log("=== BorderManager Awake() End ===");
    }
    
    /// <summary>
    /// Generates all border segments from triangle data
    /// </summary>
    public void GenerateAllBorders()
    {
        Debug.Log("=== BorderManager GenerateAllBorders() Start ===");
        
        if (icoSphere == null || icoSphere.triangleDataList == null)
        {
            Debug.LogError("BorderManager: No triangle data available!");
            return;
        }
        
        Debug.Log($"BorderManager: Found {icoSphere.triangleDataList.Count} triangles");
        
        // Clear existing borders
        ClearAllBorders();
        
        // Find all unique country pairs that share borders
        var borderPairs = FindBorderPairs();
        
        Debug.Log($"BorderManager: Found {borderPairs.Count} unique border pairs");
        
        if (borderPairs.Count == 0)
        {
            Debug.LogWarning("BorderManager: No border pairs found! Check if countries are assigned to triangles.");
            return;
        }
        
        // Create border segments for each pair
        foreach (var pair in borderPairs)
        {
            CreateBorderSegment(pair.Item1, pair.Item2);
        }
        
        Debug.Log($"BorderManager: Created {borderSegments.Count} border segments");
        Debug.Log("=== BorderManager GenerateAllBorders() End ===");
    }
    
    /// <summary>
    /// Finds all unique pairs of countries that share borders
    /// </summary>
    private List<(Country, Country)> FindBorderPairs()
    {
        var pairs = new Dictionary<string, (Country, Country)>();
        var triangleDataList = icoSphere.triangleDataList;
        
        Debug.Log($"BorderManager: Searching for border pairs in {triangleDataList.Count} triangles");
        
        for (int i = 0; i < triangleDataList.Count; i++)
        {
            var ourTriangle = triangleDataList[i];
            if (ourTriangle.country == null) continue;
            
            foreach (int adjacentId in ourTriangle.adjacentTriangles)
            {
                if (adjacentId < triangleDataList.Count)
                {
                    var neighborTriangle = triangleDataList[adjacentId];
                    if (neighborTriangle.country != null && ourTriangle.country != neighborTriangle.country)
                    {
                        // Create consistent key for the pair
                        string key = GetBorderKey(ourTriangle.country, neighborTriangle.country);
                        if (!pairs.ContainsKey(key))
                        {
                            pairs[key] = (ourTriangle.country, neighborTriangle.country);
                            Debug.Log($"BorderManager: Found border pair: {ourTriangle.country.name} - {neighborTriangle.country.name}");
                        }
                    }
                }
            }
        }
        
        Debug.Log($"BorderManager: Total unique border pairs found: {pairs.Count}");
        
        // Convert to list
        return pairs.Values.ToList();
    }
    
    /// <summary>
    /// Creates a border segment between two countries
    /// </summary>
    private void CreateBorderSegment(Country countryA, Country countryB)
    {
        string key = GetBorderKey(countryA, countryB);
        
        if (borderSegments.ContainsKey(key))
        {
            Debug.LogWarning($"BorderManager: Border segment {key} already exists!");
            return;
        }
        
        var segment = new BorderSegment(countryA, countryB);
        segment.width = defaultWidth;
        segment.intensity = defaultIntensity;
        segment.fade = defaultFade;
        segment.offset = defaultOffset;
        
        // Create the GameObject and material
        segment.CreateBorderObject(borderParent, baseBorderMaterial);
        
        // Generate the border geometry
        GenerateBorderGeometry(segment);
        
        // Store the segment
        borderSegments[key] = segment;
        
        if (showDebugInfo)
        {
            Debug.Log($"BorderManager: Created border segment {key}");
        }
    }
    
    /// <summary>
    /// Generates the geometry for a border segment
    /// </summary>
    private void GenerateBorderGeometry(BorderSegment segment)
    {
        Debug.Log($"BorderManager: Generating geometry for {segment.GetKey()}");
        var triangleDataList = icoSphere.triangleDataList;
        var sharedEdges = new List<Vector3[]>();
        var edgeSet = new HashSet<(Vector3, Vector3)>();
        // Find all shared edges between the two countries, avoiding duplicates
        for (int i = 0; i < triangleDataList.Count; i++)
        {
            var ourTriangle = triangleDataList[i];
            if (ourTriangle.country != segment.countryA && ourTriangle.country != segment.countryB) continue;
            foreach (int adjacentId in ourTriangle.adjacentTriangles)
            {
                if (adjacentId < triangleDataList.Count)
                {
                    var neighborTriangle = triangleDataList[adjacentId];
                    if ((ourTriangle.country == segment.countryA && neighborTriangle.country == segment.countryB) ||
                        (ourTriangle.country == segment.countryB && neighborTriangle.country == segment.countryA))
                    {
                        Vector3[] sharedVertices = FindSharedEdgeVertices(ourTriangle, neighborTriangle);
                        if (sharedVertices.Length == 2)
                        {
                            // Ordenar los vértices para evitar duplicados
                            Vector3 a = sharedVertices[0];
                            Vector3 b = sharedVertices[1];
                            if (a.sqrMagnitude > b.sqrMagnitude)
                            {
                                var tmp = a; a = b; b = tmp;
                            }
                            var edgeKey = (a, b);
                            if (!edgeSet.Contains(edgeKey))
                            {
                                edgeSet.Add(edgeKey);
                                sharedEdges.Add(new Vector3[] { a, b });
                            }
                        }
                    }
                }
            }
        }
        Debug.Log($"BorderManager: Found {sharedEdges.Count} shared edges for {segment.GetKey()}");
        if (sharedEdges.Count == 0)
        {
            Debug.LogWarning($"BorderManager: No shared edges found for border {segment.GetKey()}");
            return;
        }
        // Create curves for both countries (sin offset, solo las cadenas originales)
        var curveA = CreateBorderCurves(sharedEdges, segment.countryA, 0f);
        var curveB = CreateBorderCurves(sharedEdges, segment.countryB, 0f);
        Debug.Log($"BorderManager: Created curves - A: {curveA.Count}, B: {curveB.Count}");
        // Update the segment mesh
        segment.UpdateMesh(curveA, curveB);
        if (showDebugInfo)
        {
            Debug.Log($"BorderManager: Generated geometry for {segment.GetKey()} with {sharedEdges.Count} edges");
        }
    }
    
    /// <summary>
    /// Creates border curves from shared edges
    /// </summary>
    private List<Vector3[]> CreateBorderCurves(List<Vector3[]> sharedEdges, Country country, float offset)
    {
        // Ordenar los edges en cadenas continuas
        var orderedChains = OrderEdgeChains(sharedEdges);
        
        // Crear una curva para cada cadena (sin offset, solo los puntos originales)
        var curves = new List<Vector3[]>();
        
        foreach (var chain in orderedChains)
        {
            // Usar directamente los puntos de la cadena sin offset
            curves.Add(chain.ToArray());
        }
        
        return curves;
    }
    
    /// <summary>
    /// Cuantiza un Vector3 a la grilla especificada
    /// </summary>
    private static Vector3 Quantize(Vector3 v, float grid)
    {
        return new Vector3(
            Mathf.Round(v.x / grid) * grid,
            Mathf.Round(v.y / grid) * grid,
            Mathf.Round(v.z / grid) * grid
        );
    }
    
    /// <summary>
    /// Ordena los edges frontera en cadenas continuas de puntos, haciendo cada cadena lo más larga posible
    /// </summary>
    private List<List<Vector3>> OrderEdgeChains(List<Vector3[]> edges)
    {
        if (edges.Count == 0) return new List<List<Vector3>>();
        float tolerance = 0.05f;
        float grid = 1e-3f;
        // Normalizar edges (menor primero)
        var normalizedEdges = new List<(Vector3, Vector3)>();
        foreach (var e in edges)
        {
            var a = Quantize(e[0], grid);
            var b = Quantize(e[1], grid);
            if (a.sqrMagnitude < b.sqrMagnitude)
                normalizedEdges.Add((a, b));
            else
                normalizedEdges.Add((b, a));
        }
        // Diccionario de conexiones
        var pointToEdges = new Dictionary<Vector3, List<int>>();
        for (int i = 0; i < normalizedEdges.Count; i++)
        {
            var (a, b) = normalizedEdges[i];
            if (!pointToEdges.ContainsKey(a)) pointToEdges[a] = new List<int>();
            if (!pointToEdges.ContainsKey(b)) pointToEdges[b] = new List<int>();
            pointToEdges[a].Add(i);
            pointToEdges[b].Add(i);
        }
        var used = new HashSet<int>();
        var chains = new List<List<Vector3>>();
        for (int i = 0; i < normalizedEdges.Count; i++)
        {
            if (used.Contains(i)) continue;
            var chain = new List<Vector3>();
            var (start, end) = normalizedEdges[i];
            chain.Add(start);
            chain.Add(end);
            used.Add(i);
            // Extender por adelante
            bool extended = true;
            while (extended)
            {
                extended = false;
                var front = chain[chain.Count - 1];
                foreach (var idx in pointToEdges[front])
                {
                    if (used.Contains(idx)) continue;
                    var (a, b) = normalizedEdges[idx];
                    if ((a - front).sqrMagnitude < tolerance * tolerance)
                    {
                        chain.Add(b);
                        used.Add(idx);
                        extended = true;
                        break;
                    }
                    else if ((b - front).sqrMagnitude < tolerance * tolerance)
                    {
                        chain.Add(a);
                        used.Add(idx);
                        extended = true;
                        break;
                    }
                }
            }
            // Extender por atrás
            extended = true;
            while (extended)
            {
                extended = false;
                var back = chain[0];
                foreach (var idx in pointToEdges[back])
                {
                    if (used.Contains(idx)) continue;
                    var (a, b) = normalizedEdges[idx];
                    if ((a - back).sqrMagnitude < tolerance * tolerance)
                    {
                        chain.Insert(0, b);
                        used.Add(idx);
                        extended = true;
                        break;
                    }
                    else if ((b - back).sqrMagnitude < tolerance * tolerance)
                    {
                        chain.Insert(0, a);
                        used.Add(idx);
                        extended = true;
                        break;
                    }
                }
            }
            chains.Add(chain);
        }
        // Intentar unir cadenas abiertas (extremos iguales)
        bool merged = true;
        while (merged)
        {
            merged = false;
            for (int i = 0; i < chains.Count; i++)
            {
                for (int j = i + 1; j < chains.Count; j++)
                {
                    var ci = chains[i];
                    var cj = chains[j];
                    if ((ci[ci.Count - 1] - cj[0]).sqrMagnitude < tolerance * tolerance)
                    {
                        ci.AddRange(cj.Skip(1));
                        chains.RemoveAt(j);
                        merged = true;
                        break;
                    }
                    else if ((ci[0] - cj[cj.Count - 1]).sqrMagnitude < tolerance * tolerance)
                    {
                        cj.AddRange(ci.Skip(1));
                        chains[i] = cj;
                        chains.RemoveAt(j);
                        merged = true;
                        break;
                    }
                    else if ((ci[0] - cj[0]).sqrMagnitude < tolerance * tolerance)
                    {
                        cj.Reverse();
                        cj.AddRange(ci.Skip(1));
                        chains[i] = cj;
                        chains.RemoveAt(j);
                        merged = true;
                        break;
                    }
                    else if ((ci[ci.Count - 1] - cj[cj.Count - 1]).sqrMagnitude < tolerance * tolerance)
                    {
                        cj.Reverse();
                        ci.AddRange(cj.Skip(1));
                        chains.RemoveAt(j);
                        merged = true;
                        break;
                    }
                }
                if (merged) break;
            }
        }
        Debug.Log($"[BORDER] Cadenas generadas para esta frontera: {chains.Count} (longitudes: {string.Join(", ", chains.ConvertAll(c => c.Count))})");
        return chains;
    }
    
    /// <summary>
    /// Gets the center point of a country's territory
    /// </summary>
    private Vector3 GetCountryCenter(Country country)
    {
        if (country == null || country.territory == null || country.territory.Count == 0)
        {
            return Vector3.zero;
        }
        
        Vector3 center = Vector3.zero;
        foreach (var triangle in country.territory)
        {
            center += triangle.GetCenter();
        }
        return center / country.territory.Count;
    }
    
    /// <summary>
    /// Finds shared edge vertices between two triangles
    /// </summary>
    private Vector3[] FindSharedEdgeVertices(TriangleData tri1, TriangleData tri2)
    {
        Vector3[] vertices1 = { tri1.a, tri1.b, tri1.c };
        Vector3[] vertices2 = { tri2.a, tri2.b, tri2.c };
        
        List<Vector3> shared = new List<Vector3>();
        
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (Vector3.Distance(vertices1[i], vertices2[j]) < 0.01f)
                {
                    shared.Add(vertices1[i]);
                }
            }
        }
        
        return shared.ToArray();
    }
    
    /// <summary>
    /// Gets a consistent key for a border between two countries
    /// </summary>
    private string GetBorderKey(Country countryA, Country countryB)
    {
        string nameA = countryA?.name ?? "Unclaimed";
        string nameB = countryB?.name ?? "Unclaimed";
        
        // Ensure consistent ordering
        if (string.Compare(nameA, nameB) > 0)
        {
            return $"{nameB}_{nameA}";
        }
        return $"{nameA}_{nameB}";
    }
    
    /// <summary>
    /// Gets a border segment between two countries
    /// </summary>
    public BorderSegment GetBorder(Country countryA, Country countryB)
    {
        string key = GetBorderKey(countryA, countryB);
        borderSegments.TryGetValue(key, out BorderSegment segment);
        return segment;
    }
    
    /// <summary>
    /// Sets border properties for a specific border
    /// </summary>
    public void SetBorderProperties(Country countryA, Country countryB, float width, float intensity, float fade)
    {
        var segment = GetBorder(countryA, countryB);
        if (segment == null)
        {
            Debug.LogWarning($"BorderManager: Border not found for {countryA?.name} - {countryB?.name}");
            return;
        }
        
        segment.width = width;
        segment.intensity = intensity;
        segment.fade = fade;
        segment.UpdateMaterialProperties();
    }
    
    /// <summary>
    /// Sets border colors for a specific border
    /// </summary>
    public void SetBorderColors(Country countryA, Country countryB, Color colorA, Color colorB)
    {
        var segment = GetBorder(countryA, countryB);
        if (segment == null)
        {
            Debug.LogWarning($"BorderManager: Border not found for {countryA?.name} - {countryB?.name}");
            return;
        }
        
        segment.colorA = colorA;
        segment.colorB = colorB;
        segment.UpdateMaterialProperties();
    }
    
    /// <summary>
    /// Sets war border effect
    /// </summary>
    public void SetWarBorder(Country countryA, Country countryB, bool isWar)
    {
        var segment = GetBorder(countryA, countryB);
        if (segment == null)
        {
            Debug.LogWarning($"BorderManager: Border not found for {countryA?.name} - {countryB?.name}");
            return;
        }
        
        segment.isWarBorder = isWar;
        segment.UpdateMaterialProperties();
    }
    
    /// <summary>
    /// Sets peace treaty border effect
    /// </summary>
    public void SetPeaceTreatyBorder(Country countryA, Country countryB, bool isPeace)
    {
        var segment = GetBorder(countryA, countryB);
        if (segment == null)
        {
            Debug.LogWarning($"BorderManager: Border not found for {countryA?.name} - {countryB?.name}");
            return;
        }
        
        segment.isPeaceTreaty = isPeace;
        segment.UpdateMaterialProperties();
    }
    
    /// <summary>
    /// Regenerates borders for a specific country
    /// </summary>
    public void RegenerateBordersForCountry(Country country)
    {
        Debug.Log($"BorderManager: Regenerating borders for country {country?.name}");
        
        // Find all borders involving this country
        var bordersToRegenerate = new List<string>();
        
        foreach (var kvp in borderSegments)
        {
            if (kvp.Value.countryA == country || kvp.Value.countryB == country)
            {
                bordersToRegenerate.Add(kvp.Key);
            }
        }
        
        // Regenerate each border
        foreach (var key in bordersToRegenerate)
        {
            var segment = borderSegments[key];
            GenerateBorderGeometry(segment);
        }
        
        Debug.Log($"BorderManager: Regenerated {bordersToRegenerate.Count} borders for {country?.name}");
    }
    
    /// <summary>
    /// Clears all border segments
    /// </summary>
    public void ClearAllBorders()
    {
        foreach (var segment in borderSegments.Values)
        {
            segment.Destroy();
        }
        borderSegments.Clear();
        
        Debug.Log("BorderManager: Cleared all border segments");
    }
    
    /// <summary>
    /// Gets all border segments
    /// </summary>
    public List<BorderSegment> GetAllBorders()
    {
        return borderSegments.Values.ToList();
    }
    
    /// <summary>
    /// Gets the number of border segments
    /// </summary>
    public int GetBorderCount()
    {
        return borderSegments.Count;
    }
    
    /// <summary>
    /// Updates the mesh for all border segments when the main mesh changes
    /// </summary>
    public void UpdateMesh(Mesh newMesh)
    {
        Debug.Log("BorderManager: Updating mesh for all border segments");
        
        // Regenerate all borders with the new mesh data
        GenerateAllBorders();
    }
    
    void OnDestroy()
    {
        ClearAllBorders();
    }
} 