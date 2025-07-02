using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BorderManager : MonoBehaviour
{
    [Header("Border Settings")]
    public Material baseBorderMaterial;
    public float defaultWidth = 20f;
    public float defaultIntensity = 0.6f;
    public float defaultFade = 1f;
    public float defaultOffset = 20f;
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    
    // Private fields
    private Dictionary<string, BorderSegment> borderSegments = new Dictionary<string, BorderSegment>();
    private Transform borderParent;
    private IcoSphere icoSphere;
    private MapEditor mapEditor;
    
    void Awake()
    {
        //Debug.Log("=== BorderManager Awake() Start ===");
        
        icoSphere = GetComponent<IcoSphere>();
        if (icoSphere == null)
        {
            Debug.LogError("BorderManager: IcoSphere component not found!");
            return;
        }
        
        // Find MapEditor for country data
        mapEditor = UnityEngine.Object.FindFirstObjectByType<MapEditor>();
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
                //Debug.Log("BorderManager: Created base border material");
            }
            else
            {
                Debug.LogError("BorderManager: Custom/CountryBorder shader not found!");
                return;
            }
        }
        
        //Debug.Log("=== BorderManager Awake() End ===");
    }
    
    /// <summary>
    /// Generates all border segments from triangle data
    /// </summary>
    public void GenerateAllBorders()
    {
        //Debug.Log("=== BorderManager GenerateAllBorders() Start ===");
        
        if (icoSphere == null || icoSphere.triangleDataList == null)
        {
            Debug.LogError("BorderManager: No triangle data available!");
            return;
        }
        
        //Debug.Log($"BorderManager: Found {icoSphere.triangleDataList.Count} triangles");
        
        // Clear existing borders
        ClearAllBorders();
        
        // Find all unique country pairs that share borders
        var borderPairs = FindBorderPairs();
        
        //Debug.Log($"BorderManager: Found {borderPairs.Count} unique border pairs");
        
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
        
        ///Debug.Log($"BorderManager: Created {borderSegments.Count} border segments");
        //Debug.Log("=== BorderManager GenerateAllBorders() End ===");
    }
    
    /// <summary>
    /// Finds all unique pairs of countries that share borders
    /// </summary>
    private List<(Country, Country)> FindBorderPairs()
    {
        var pairs = new Dictionary<string, (Country, Country)>();
        var triangleDataList = icoSphere.triangleDataList;
        
        //Debug.Log($"BorderManager: Searching for border pairs in {triangleDataList.Count} triangles");
        
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
                            //Debug.Log($"BorderManager: Found border pair: {ourTriangle.country.name} - {neighborTriangle.country.name}");
                        }
                    }
                }
            }
        }
        
        //Debug.Log($"BorderManager: Total unique border pairs found: {pairs.Count}");
        
        // Convert to list
        return pairs.Values.ToList();
    }
    
    /// <summary>
    /// Creates a border segment between two countries
    /// </summary>
    private void CreateBorderSegment(Country countryA, Country countryB)
    {
        // Ordenar los países según el criterio de GetBorderKey
        string nameA = countryA?.name ?? "Unclaimed";
        string nameB = countryB?.name ?? "Unclaimed";
        Country first = countryA;
        Country second = countryB;
        if (string.Compare(nameA, nameB) > 0)
        {
            first = countryB;
            second = countryA;
        }
        string key = GetBorderKey(first, second);
        if (borderSegments.ContainsKey(key))
        {
            Debug.LogWarning($"BorderManager: Border segment {key} already exists!");
            return;
        }
        var segment = new BorderSegment(first, second);
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
            //Debug.Log($"BorderManager: Created border segment {key}");
        }
    }
    
    /// <summary>
    /// Generates the geometry for a border segment
    /// </summary>
    private void GenerateBorderGeometry(BorderSegment segment)
    {
        var triangleDataList = icoSphere.triangleDataList;
        var sharedEdges = new List<(Vector3[], TriangleData, TriangleData)>();
        var edgeSet = new HashSet<string>(); // Cambiar a string para mejor detección de duplicados
        

        
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
                            // MEJORA: Normalizar edge de manera más robusta
                            Vector3 a = Quantize(sharedVertices[0], 0.1f);  // Aumentado de 1e-3f a 0.1f
                            Vector3 b = Quantize(sharedVertices[1], 0.1f);  // Aumentado de 1e-3f a 0.1f
                            
                            // Crear key consistente independientemente del orden
                            string edgeKey;
                            if (CompareVector3(a, b) < 0)
                            {
                                edgeKey = $"{a.x:F3},{a.y:F3},{a.z:F3}|{b.x:F3},{b.y:F3},{b.z:F3}";
                            }
                            else
                            {
                                edgeKey = $"{b.x:F3},{b.y:F3},{b.z:F3}|{a.x:F3},{a.y:F3},{a.z:F3}";
                            }
                            
                            if (!edgeSet.Contains(edgeKey))
                            {
                                edgeSet.Add(edgeKey);
                                
                                // Guardar edge con sus triángulos correspondientes
                                TriangleData triA, triB;
                                if (ourTriangle.country == segment.countryA)
                                {
                                    triA = ourTriangle;
                                    triB = neighborTriangle;
                                }
                                else
                                {
                                    triA = neighborTriangle;
                                    triB = ourTriangle;
                                }
                                
                                sharedEdges.Add((new Vector3[] { a, b }, triA, triB));
                            }
                            else
                            {
                              
                            }
                        }
                    }
                }
            }
        }
        
        //Debug.Log($"[BORDER] Total de edges únicos encontrados: {sharedEdges.Count}");
        
        if (sharedEdges.Count == 0)
        {
            Debug.LogWarning($"[Border] No shared edges found for {segment.countryA?.name ?? "Unclaimed"} - {segment.countryB?.name ?? "Unclaimed"}");
            return;
        }
        
        // Crear las curvas con orientación por cadena
        var curvesWithOrientation = CreateBorderCurvesWithOrientationPerChain(sharedEdges, segment.countryA, segment.countryB);
        
        // Store chains for debugging
        segment.chainsWithOrientation = curvesWithOrientation;
        
        // Update the segment mesh with orientation per chain
        segment.UpdateMesh(curvesWithOrientation);
        if (showDebugInfo)
        {
            //Debug.Log($"BorderManager: Generated geometry for {segment.GetKey()} with {sharedEdges.Count} edges");
        }
    }
    
    /// <summary>
    /// Determina la orientación del primer edge usando los triángulos de referencia
    /// </summary>
    private bool DetermineFirstEdgeOrientation(Vector3[] firstEdge, TriangleData countryTriangle, Country country)
    {
        if (countryTriangle == null)
            return false;
            
        Vector3 edgeMidpoint = (firstEdge[0] + firstEdge[1]) * 0.5f;
        Vector3 edgeDirection = (firstEdge[1] - firstEdge[0]).normalized;
        
        // Calcular el perpendicular (tangente a la esfera)
        Vector3 radial = edgeMidpoint.normalized;
        Vector3 perpendicular = Vector3.Cross(edgeDirection, radial).normalized;
        
        // Calcular el vector desde el edge hacia el centro del triángulo
        Vector3 triangleCenter = countryTriangle.GetCenter();
        Vector3 directionToTriangle = (triangleCenter - edgeMidpoint).normalized;
        
        // Verificar si el perpendicular apunta hacia el triángulo
        float dotProduct = Vector3.Dot(perpendicular, directionToTriangle);
        
        // Si el dot product es positivo, el perpendicular apunta hacia el triángulo
        // Si es negativo, necesitamos invertir la orientación
        return dotProduct < 0;
    }
    
    /// <summary>
    /// Creates border curves with orientation calculated per chain
    /// </summary>
    private List<(Vector3[], bool, TriangleData, TriangleData)> CreateBorderCurvesWithOrientationPerChain(List<(Vector3[], TriangleData, TriangleData)> sharedEdges, Country countryA, Country countryB)
    {
        // Ordenar los edges en cadenas continuas
        var orderedChains = OrderEdgeChainsWithOrientation(sharedEdges, countryA, countryB);
        
        // Crear una curva para cada cadena con orientación real calculada
        var curvesWithOrientation = new List<(Vector3[], bool, TriangleData, TriangleData)>();
        
        foreach (var (chain, triA, triB) in orderedChains)
        {
            if (chain.Count < 2) continue;
            
            // Determinar la orientación real del primer edge de esta cadena
            Vector3[] firstEdge = { chain[0], chain[1] };
            bool countryAIsLeft = true; // Por defecto
            
            if (triA != null && triB != null)
            {
                // Lógica geométrica correcta: determinar qué país está a la izquierda del edge
                Vector3 centerA = triA.GetCenter();
                Vector3 centerB = triB.GetCenter();
                Vector3 edgeMidpoint = (firstEdge[0] + firstEdge[1]) * 0.5f;
                Vector3 edgeDirection = (firstEdge[1] - firstEdge[0]).normalized;
                
                // Vector desde el punto medio del edge hacia cada país
                Vector3 toCountryA = (centerA - edgeMidpoint).normalized;
                Vector3 toCountryB = (centerB - edgeMidpoint).normalized;
                
                // Vector perpendicular al edge (tangente a la esfera)
                Vector3 radial = edgeMidpoint.normalized;
                Vector3 perpendicular = Vector3.Cross(edgeDirection, radial).normalized;
                
                // Dot products para determinar qué país está a cada lado
                float dotA = Vector3.Dot(perpendicular, toCountryA);
                float dotB = Vector3.Dot(perpendicular, toCountryB);
                
                // Verificar que los dot products tengan signos opuestos
                if (Mathf.Sign(dotA) != Mathf.Sign(dotB))
                {
                    // El país con dot product positivo está a la izquierda del edge
                    countryAIsLeft = dotA > 0;
                }
                else
                {
                    // Si ambos tienen el mismo signo, hay un problema - usar fallback
                    countryAIsLeft = true;
                }
            }
            

            
            // Usar la orientación real calculada
            curvesWithOrientation.Add((chain.ToArray(), countryAIsLeft, triA, triB));
        }
        
        return curvesWithOrientation;
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
    /// Ordena los edges frontera en cadenas continuas de puntos sin forzar orientación
    /// </summary>
    private List<(List<Vector3>, TriangleData, TriangleData)> OrderEdgeChainsWithOrientation(List<(Vector3[], TriangleData, TriangleData)> edges, Country countryA, Country countryB)
    {
        if (edges.Count == 0) return new List<(List<Vector3>, TriangleData, TriangleData)>();
        float tolerance = 0.05f;
        float grid = 0.05f;  // Aumentado de 1e-3f a 0.1f para mayor tolerancia
        
        // Normalizar edges de manera consistente
        var normalizedEdges = new List<(Vector3, Vector3, TriangleData, TriangleData)>();
        
        foreach (var (e, triA, triB) in edges)
        {
            var a = Quantize(e[0], grid);
            var b = Quantize(e[1], grid);
            Vector3 start, end;
            
            // Usar la misma lógica de comparación que en GenerateBorderGeometry
            if (CompareVector3(a, b) < 0)
            {
                start = a; end = b;
            }
            else
            {
                start = b; end = a;
            }
            normalizedEdges.Add((start, end, triA, triB));
        }
        
        // Diccionario de conexiones
        var pointToEdges = new Dictionary<Vector3, List<int>>();
        for (int i = 0; i < normalizedEdges.Count; i++)
        {
            var (a, b, triA, triB) = normalizedEdges[i];
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
            var (start, end, triA, triB) = normalizedEdges[i];
            
            // Comenzar con orientación normal (start -> end)
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
                    var (a, b, _, _) = normalizedEdges[idx];
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
                    var (a, b, _, _) = normalizedEdges[idx];
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
        
        // MEJORA: Intentar unir cadenas con lógica más inteligente
        bool merged = true;
        int maxMergeIterations = 10; // Evitar loops infinitos
        int iteration = 0;
        
        while (merged && iteration < maxMergeIterations)
        {
            iteration++;
            merged = false;
            
            for (int i = 0; i < chains.Count; i++)
            {
                for (int j = i + 1; j < chains.Count; j++)
                {
                    var ci = chains[i];
                    var cj = chains[j];
                    
                    // OPTIMIZADO: Solo probar con reversas de las otras cadenas
                    bool foundConnection = false;
                    
                    // Caso 1: ci[end] == cj[start] (ambos normales)
                    if ((ci[ci.Count - 1] - cj[0]).sqrMagnitude < tolerance * tolerance)
                    {
                        ci.AddRange(cj.Skip(1));
                        chains.RemoveAt(j);
                        merged = true;
                        foundConnection = true;
               
                    }
                    // Caso 2: ci[start] == cj[end] (ambos normales)
                    else if ((ci[0] - cj[cj.Count - 1]).sqrMagnitude < tolerance * tolerance)
                    {
                        cj.AddRange(ci.Skip(1));
                        chains[i] = cj;
                        chains.RemoveAt(j);
                        merged = true;
                        foundConnection = true;
                   
                    }
                    // Caso 3: ci[end] == cj[end] (girar cj)
                    else if ((ci[ci.Count - 1] - cj[cj.Count - 1]).sqrMagnitude < tolerance * tolerance)
                    {
                        cj.Reverse();
                        ci.AddRange(cj.Skip(1));
                        chains.RemoveAt(j);
                        merged = true;
                        foundConnection = true;
              
                    }
                    // Caso 4: ci[start] == cj[start] (girar cj)
                    else if ((ci[0] - cj[0]).sqrMagnitude < tolerance * tolerance)
                    {
                        cj.Reverse();
                        cj.AddRange(ci.Skip(1));
                        chains[i] = cj;
                        chains.RemoveAt(j);
                        merged = true;
                        foundConnection = true;
                  
                    }
                    
                    if (foundConnection) break;
                }
                if (merged) break;
            }
        }
        
        if (iteration >= maxMergeIterations)
        {
            Debug.LogWarning($"[BORDER] Alcanzado máximo de iteraciones de merge ({maxMergeIterations})");
        }
        
        // NUEVO: Eliminar cadenas que empiecen en el mismo punto
        var uniqueChains = new List<List<Vector3>>();
        var startingPoints = new HashSet<string>();
        
        foreach (var chain in chains)
        {
            if (chain.Count >= 2)
            {
                string startPoint = $"{chain[0].x:F3},{chain[0].y:F3},{chain[0].z:F3}";
                if (!startingPoints.Contains(startPoint))
                {
                    startingPoints.Add(startPoint);
                    uniqueChains.Add(chain);
                    //Debug.Log($"[BORDER] Cadena única agregada, empieza en: {startPoint}");
                }
                else
                {
                    Debug.LogWarning($"[BORDER] Cadena duplicada eliminada, empieza en: {startPoint}");
                }
            }
        }
        
        //Debug.Log($"[BORDER] Cadenas después de eliminar duplicados: {uniqueChains.Count}");
        
        // Crear el resultado final con las cadenas y sus triángulos de referencia
        var result = new List<(List<Vector3>, TriangleData, TriangleData)>();
        foreach (var chain in uniqueChains)
        {
            if (chain.Count >= 2)
            {
                // Buscar los triángulos de referencia para el primer edge de la cadena completa
                Vector3[] firstEdge = { chain[0], chain[1] };
                var (triA, triB) = FindTrianglesForEdge(firstEdge, countryA, countryB);
                
                // Log error si no se encuentran triángulos
                if (triA == null || triB == null)
                {
                    Debug.LogError($"[BORDER] ERROR: No se encontraron triángulos para el edge [{firstEdge[0]}, {firstEdge[1]}] entre países {countryA?.name ?? "null"} y {countryB?.name ?? "null"}. TriA: {(triA?.country?.name ?? "null")}, TriB: {(triB?.country?.name ?? "null")}");
                }
                
                // Agregar la cadena sin forzar orientación - la orientación se calculará en CreateBorderCurvesWithOrientationPerChain
                result.Add((chain, triA, triB));
            }
        }
        
        //Debug.Log($"[BORDER] Cadenas generadas para esta frontera: {result.Count} (longitudes: {string.Join(", ", result.ConvertAll(c => c.Item1.Count))})");
        return result;
    }
    
    /// <summary>
    /// Encuentra los triángulos específicos que comparten un edge entre dos países
    /// </summary>
    private (TriangleData, TriangleData) FindTrianglesForEdge(Vector3[] edge, Country countryA, Country countryB)
    {
        var triangleDataList = icoSphere.triangleDataList;
        float tolerance = 0.15f; // Aumentado para coincidir con la cuantización de OrderEdgeChainsWithOrientation
        
        for (int i = 0; i < triangleDataList.Count; i++)
        {
            var ourTriangle = triangleDataList[i];
            if (ourTriangle.country != countryA && ourTriangle.country != countryB) continue;
            
            foreach (int adjacentId in ourTriangle.adjacentTriangles)
            {
                if (adjacentId < triangleDataList.Count)
                {
                    var neighborTriangle = triangleDataList[adjacentId];
                    if ((ourTriangle.country == countryA && neighborTriangle.country == countryB) ||
                        (ourTriangle.country == countryB && neighborTriangle.country == countryA))
                    {
                        Vector3[] sharedVertices = FindSharedEdgeVertices(ourTriangle, neighborTriangle);
                        if (sharedVertices.Length == 2)
                        {
                            // Verificar si estos vértices coinciden con el edge que buscamos
                            if ((Vector3.Distance(sharedVertices[0], edge[0]) < tolerance && Vector3.Distance(sharedVertices[1], edge[1]) < tolerance) ||
                                (Vector3.Distance(sharedVertices[0], edge[1]) < tolerance && Vector3.Distance(sharedVertices[1], edge[0]) < tolerance))
                            {
                                // Retornar en el orden correcto: countryA, countryB
                                if (ourTriangle.country == countryA)
                                {
                                    return (ourTriangle, neighborTriangle);
                                }
                                else
                                {
                                    return (neighborTriangle, ourTriangle);
                                }
                            }
                        }
                    }
                }
            }
        }
        
        Debug.LogWarning($"[BORDER] No se encontraron triángulos para el edge entre {countryA?.name ?? "null"} y {countryB?.name ?? "null"}");
        
        return (null, null);
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
                if (Vector3.Distance(vertices1[i], vertices2[j]) < 0.15f) // Aumentado para consistencia
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
        
        //Debug.Log($"BorderManager: Regenerated {bordersToRegenerate.Count} borders for {country?.name}");
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
        
        //Debug.Log("BorderManager: Cleared all border segments");
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
        //Debug.Log("BorderManager: Updating mesh for all border segments");
        
        // Regenerate all borders with the new mesh data
        GenerateAllBorders();
    }
    
    /// <summary>
    /// Compara dos Vector3 de manera consistente
    /// </summary>
    private static int CompareVector3(Vector3 a, Vector3 b)
    {
        if (a.x != b.x) return a.x.CompareTo(b.x);
        if (a.y != b.y) return a.y.CompareTo(b.y);
        return a.z.CompareTo(b.z);
    }
    
    void OnDestroy()
    {
        ClearAllBorders();
    }
} 