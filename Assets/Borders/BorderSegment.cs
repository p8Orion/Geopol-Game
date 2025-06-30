using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class BorderSegment
{
    [Header("Countries")]
    public Country countryA;
    public Country countryB;
    
    [Header("GameObject References")]
    public GameObject borderObjectA;
    public GameObject borderObjectB;
    public MeshRenderer meshRenderer;
    public Material borderMaterial;
    
    [Header("Geometry")]
    public Vector3[] vertices;
    public Vector3[] verticesA; // Curva del país A
    public Vector3[] verticesB; // Curva del país B
    
    [Header("Border Properties")]
    public float width = 10f;
    public float intensity = 0.6f;
    public float fade = 1f;
    public float offset = 1f;
    public bool isActive = true;
    
    [Header("Special Effects")]
    public bool isWarBorder = false;
    public bool isPeaceTreaty = false;
    public bool enablePulse = false;
    public float pulseSpeed = 2.0f;
    
    [Header("Colors")]
    public Color colorA;
    public Color colorB;
    
    [Header("Debug - Chain Data")]
    public List<(Vector3[], bool, TriangleData, TriangleData)> chainsWithOrientation = new List<(Vector3[], bool, TriangleData, TriangleData)>();
    
    private Transform borderParent;

    public BorderSegment(Country countryA, Country countryB)
    {
        this.countryA = countryA;
        this.countryB = countryB;
        this.colorA = countryA?.color ?? Color.gray;
        this.colorB = countryB?.color ?? Color.gray;
    }
    
    /// <summary>
    /// Creates the GameObject and MeshRenderer for this border segment
    /// </summary>
    public void CreateBorderObject(Transform parent, Material baseMaterial)
    {
        // Store the parent reference for future use
        borderParent = parent;
        
        // Create GameObject
        borderObjectA = new GameObject($"Border_{countryA?.name ?? "Unclaimed"}_{countryB?.name ?? "Unclaimed"}");
        borderObjectA.transform.SetParent(parent);
        borderObjectA.transform.localPosition = Vector3.zero;
        borderObjectA.transform.localRotation = Quaternion.identity;
        borderObjectA.transform.localScale = Vector3.one;
        
        // Add mesh components
        var meshFilter = borderObjectA.AddComponent<MeshFilter>();
        var renderer = borderObjectA.AddComponent<MeshRenderer>();
        
        // Create material instance from base material
        borderMaterial = new Material(baseMaterial);
        renderer.material = borderMaterial;
        
        // Configure renderer
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingOrder = 1; // Render above terrain
        
        // Store references
        meshRenderer = renderer;
        
        // Set layer to match parent
        borderObjectA.layer = parent.gameObject.layer;
        
        // Apply current properties
        UpdateMaterialProperties();
    }
    
    /// <summary>
    /// Updates the material properties with current border settings
    /// </summary>
    public void UpdateMaterialProperties()
    {
        if (borderMaterial == null) return;
        
        borderMaterial.SetFloat("_BorderWidth", width);
        borderMaterial.SetFloat("_BorderIntensity", intensity);
        borderMaterial.SetFloat("_BorderFade", fade);
        borderMaterial.SetFloat("_BorderOffset", offset);
        borderMaterial.SetFloat("_BorderPulse", enablePulse ? 1.0f : 0.0f);
        borderMaterial.SetFloat("_BorderPulseSpeed", pulseSpeed);
        
        // Set colors
        borderMaterial.SetColor("_ColorA", colorA);
        borderMaterial.SetColor("_ColorB", colorB);
        
        // Special effects
        borderMaterial.SetFloat("_IsWarBorder", isWarBorder ? 1.0f : 0.0f);
        borderMaterial.SetFloat("_IsPeaceTreaty", isPeaceTreaty ? 1.0f : 0.0f);
    }
    
    /// <summary>
    /// Updates the mesh with smooth curves from multiple chains, each with its own orientation
    /// </summary>
    public void UpdateMesh(List<(Vector3[], bool, TriangleData, TriangleData)> curvesWithOrientation)
    {
        // Destroy old objects if they exist
        if (borderObjectA != null) Object.DestroyImmediate(borderObjectA);
        if (borderObjectB != null) Object.DestroyImmediate(borderObjectB);

        // --- Lado A ---
        borderObjectA = new GameObject($"Border_{countryA?.name ?? "Unclaimed"}_to_{countryB?.name ?? "Unclaimed"}");
        // Asegurar que se asigne al parent correcto
        if (borderParent != null) 
        {
            borderObjectA.transform.SetParent(borderParent);
        }
        else
        {
            Debug.LogWarning($"BorderSegment: borderParent is null for {countryA?.name ?? "Unclaimed"}_to_{countryB?.name ?? "Unclaimed"}");
        }
        borderObjectA.transform.localPosition = Vector3.zero;
        borderObjectA.transform.localRotation = Quaternion.identity;
        borderObjectA.transform.localScale = Vector3.one;
        var meshFilterA = borderObjectA.AddComponent<MeshFilter>();
        var rendererA = borderObjectA.AddComponent<MeshRenderer>();
        var borderMaterialA = new Material(borderMaterial); // Instancia nueva
        rendererA.material = borderMaterialA;
        meshRenderer = rendererA;

        var meshA = new Mesh();
        meshA.name = $"BorderMesh_{countryA?.name ?? "Unclaimed"}_to_{countryB?.name ?? "Unclaimed"}";
        var allVerticesA = new List<Vector3>();
        var colorsA = new List<Color>();
        var trianglesA = new List<int>();
        float thickness = width;
        
        // Procesar cada cadena con su orientación específica
        foreach (var (chain, countryAIsLeft, triA, triB) in curvesWithOrientation)
        {
            if (chain != null && chain.Length > 1)
            {
                int segmentsPerChain = Mathf.Max(5, Mathf.Min(20, chain.Length * 2));
                var smoothCurve = GenerateSmoothCurve(chain, segmentsPerChain);
                for (int i = 0; i < smoothCurve.Length; i++)
                {
                    Vector3 point = smoothCurve[i];
                    Vector3 tangent;
                    if (i == 0)
                        tangent = (smoothCurve[i + 1] - point).normalized;
                    else if (i == smoothCurve.Length - 1)
                        tangent = (point - smoothCurve[i - 1]).normalized;
                    else
                        tangent = (smoothCurve[i + 1] - smoothCurve[i - 1]).normalized;
                    
                    Vector3 radial = point.normalized;
                    Vector3 perpendicular = Vector3.Cross(tangent, radial).normalized;
                    
                    // Determinar la dirección del offset basándose en la orientación específica de esta cadena
                    Vector3 offsetDirection = DetermineOffsetDirection(point, tangent, perpendicular, countryAIsLeft);
                    
                    allVerticesA.Add(point + offsetDirection * offset); // Offset hacia el país A
                    allVerticesA.Add(point + offsetDirection * (offset - thickness));
                    Color currentColorA = countryA?.color ?? Color.gray;
                    colorsA.Add(currentColorA);
                    colorsA.Add(currentColorA);
                }
                int baseIndex = allVerticesA.Count - smoothCurve.Length * 2;
                for (int i = 0; i < smoothCurve.Length - 1; i++)
                {
                    int idx = baseIndex + i * 2;
                    trianglesA.Add(idx);
                    trianglesA.Add(idx + 2);
                    trianglesA.Add(idx + 1);
                    trianglesA.Add(idx + 2);
                    trianglesA.Add(idx + 3);
                    trianglesA.Add(idx + 1);
                }
            }
        }
        meshA.vertices = allVerticesA.ToArray();
        meshA.colors = colorsA.ToArray();
        meshA.triangles = trianglesA.ToArray();
        meshA.RecalculateNormals();
        meshFilterA.mesh = meshA;
        
        // Actualizar propiedades del material para el lado A
        borderMaterialA.SetFloat("_BorderWidth", width);
        borderMaterialA.SetFloat("_BorderIntensity", intensity);
        borderMaterialA.SetFloat("_BorderFade", fade);
        borderMaterialA.SetFloat("_BorderOffset", offset);
        borderMaterialA.SetFloat("_BorderPulse", enablePulse ? 1.0f : 0.0f);
        borderMaterialA.SetFloat("_BorderPulseSpeed", pulseSpeed);
        borderMaterialA.SetColor("_ColorA", colorA);
        borderMaterialA.SetColor("_ColorB", colorA);
        borderMaterialA.SetFloat("_IsWarBorder", isWarBorder ? 1.0f : 0.0f);
        borderMaterialA.SetFloat("_IsPeaceTreaty", isPeaceTreaty ? 1.0f : 0.0f);
        
        Debug.Log($"BorderSegment: Lado A - {countryA?.name ?? "Unclaimed"} usando color {colorA}");

        // --- Lado B ---
        borderObjectB = new GameObject($"Border_{countryB?.name ?? "Unclaimed"}_to_{countryA?.name ?? "Unclaimed"}");
        // Asegurar que se asigne al parent correcto
        if (borderParent != null) 
        {
            borderObjectB.transform.SetParent(borderParent);
        }
        else
        {
            Debug.LogWarning($"BorderSegment: borderParent is null for {countryB?.name ?? "Unclaimed"}_to_{countryA?.name ?? "Unclaimed"}");
        }
        borderObjectB.transform.localPosition = Vector3.zero;
        borderObjectB.transform.localRotation = Quaternion.identity;
        borderObjectB.transform.localScale = Vector3.one;
        var meshFilterB = borderObjectB.AddComponent<MeshFilter>();
        var rendererB = borderObjectB.AddComponent<MeshRenderer>();
        var borderMaterialB = new Material(borderMaterial); // Instancia nueva
        rendererB.material = borderMaterialB;

        var meshB = new Mesh();
        meshB.name = $"BorderMesh_{countryB?.name ?? "Unclaimed"}_to_{countryA?.name ?? "Unclaimed"}";
        var allVerticesB = new List<Vector3>();
        var colorsB = new List<Color>();
        var trianglesB = new List<int>();
        
        // Procesar cada cadena con su orientación específica (opuesta al país A)
        foreach (var (chain, countryAIsLeft, triA, triB) in curvesWithOrientation)
        {
            if (chain != null && chain.Length > 1)
            {
                int segmentsPerChain = Mathf.Max(5, Mathf.Min(20, chain.Length * 2));
                var smoothCurve = GenerateSmoothCurve(chain, segmentsPerChain);
                for (int i = 0; i < smoothCurve.Length; i++)
                {
                    Vector3 point = smoothCurve[i];
                    Vector3 tangent;
                    if (i == 0)
                        tangent = (smoothCurve[i + 1] - point).normalized;
                    else if (i == smoothCurve.Length - 1)
                        tangent = (point - smoothCurve[i - 1]).normalized;
                    else
                        tangent = (smoothCurve[i + 1] - smoothCurve[i - 1]).normalized;
                    
                    Vector3 radial = point.normalized;
                    Vector3 perpendicular = Vector3.Cross(tangent, radial).normalized;
                    
                    // Determinar la dirección del offset basándose en la orientación específica de esta cadena (opuesta al país A)
                    Vector3 offsetDirection = DetermineOffsetDirection(point, tangent, perpendicular, !countryAIsLeft);
                    
                    allVerticesB.Add(point + offsetDirection * offset); // Offset hacia el país B
                    allVerticesB.Add(point + offsetDirection * (offset - thickness));
                    Color currentColorB = countryB?.color ?? Color.gray;
                    colorsB.Add(currentColorB);
                    colorsB.Add(currentColorB);
                }
                int baseIndex = allVerticesB.Count - smoothCurve.Length * 2;
                for (int i = 0; i < smoothCurve.Length - 1; i++)
                {
                    int idx = baseIndex + i * 2;
                    trianglesB.Add(idx);
                    trianglesB.Add(idx + 2);
                    trianglesB.Add(idx + 1);
                    trianglesB.Add(idx + 2);
                    trianglesB.Add(idx + 3);
                    trianglesB.Add(idx + 1);
                }
            }
        }
        meshB.vertices = allVerticesB.ToArray();
        meshB.colors = colorsB.ToArray();
        meshB.triangles = trianglesB.ToArray();
        meshB.RecalculateNormals();
        meshFilterB.mesh = meshB;
        
        // Actualizar propiedades del material para el lado B
        borderMaterialB.SetFloat("_BorderWidth", width);
        borderMaterialB.SetFloat("_BorderIntensity", intensity);
        borderMaterialB.SetFloat("_BorderFade", fade);
        borderMaterialB.SetFloat("_BorderOffset", offset);
        borderMaterialB.SetFloat("_BorderPulse", enablePulse ? 1.0f : 0.0f);
        borderMaterialB.SetFloat("_BorderPulseSpeed", pulseSpeed);
        borderMaterialB.SetColor("_ColorA", colorB);
        borderMaterialB.SetColor("_ColorB", colorB);
        borderMaterialB.SetFloat("_IsWarBorder", isWarBorder ? 1.0f : 0.0f);
        borderMaterialB.SetFloat("_IsPeaceTreaty", isPeaceTreaty ? 1.0f : 0.0f);
        
        Debug.Log($"BorderSegment: Lado B - {countryB?.name ?? "Unclaimed"} usando color {colorB}");
    }
    
    /// <summary>
    /// Determines the offset direction for border rendering
    /// </summary>
    private Vector3 DetermineOffsetDirection(Vector3 point, Vector3 tangent, Vector3 perpendicular, bool countryAIsLeft)
    {
        // Lógica simple: si countryAIsLeft es true, usar perpendicular positivo
        // Si es false, usar perpendicular negativo
        return countryAIsLeft ? perpendicular : -perpendicular;
    }
    
    /// <summary>
    /// Creates a continuous line mesh for a single curve
    /// </summary>
    private void CreateCurveLineMesh(Vector3[] curve, Color color, 
                                   List<Vector3> allVertices, List<Color> allColors, List<int> allTriangles)
    {
        if (curve.Length < 2) return;
        
        Vector3 center = Vector3.zero;
        
        for (int i = 0; i < curve.Length - 1; i++)
        {
            Vector3 p0 = curve[i];
            Vector3 p1 = curve[i + 1];
            
            // Calculate direction and perpendicular for line thickness
            Vector3 dir = (p1 - p0).normalized;
            Vector3 midPoint = (p0 + p1) * 0.5f;
            Vector3 radial = (midPoint - center).normalized;
            Vector3 perpendicular = Vector3.Cross(dir, radial).normalized;
            
            float lineThickness = width; // Usar el parámetro width en lugar de valor fijo
            
            // Create a thick line segment
            int baseIndex = allVertices.Count;
            allVertices.Add(p0 + perpendicular * lineThickness);
            allVertices.Add(p0 - perpendicular * lineThickness);
            allVertices.Add(p1 + perpendicular * lineThickness);
            allVertices.Add(p1 - perpendicular * lineThickness);
            
            allColors.Add(color);
            allColors.Add(color);
            allColors.Add(color);
            allColors.Add(color);
            
            // Create two triangles for the thick line segment
            allTriangles.Add(baseIndex + 0);
            allTriangles.Add(baseIndex + 2);
            allTriangles.Add(baseIndex + 1);
            
            allTriangles.Add(baseIndex + 2);
            allTriangles.Add(baseIndex + 3);
            allTriangles.Add(baseIndex + 1);
        }
    }
    
    /// <summary>
    /// Generates a smooth curve from a chain of points using Catmull-Rom spline
    /// </summary>
    private Vector3[] GenerateSmoothCurve(Vector3[] chain, int segments)
    {
        if (chain.Length < 2) return chain;
        
        var smoothPoints = new List<Vector3>();
        
        // For each segment in the chain, generate smooth curve points
        for (int i = 0; i < chain.Length - 1; i++)
        {
            Vector3 p0 = (i > 0) ? chain[i - 1] : chain[i];
            Vector3 p1 = chain[i];
            Vector3 p2 = chain[i + 1];
            Vector3 p3 = (i < chain.Length - 2) ? chain[i + 2] : chain[i + 1];
            
            // Generate smooth points between p1 and p2
            for (int j = 0; j < segments; j++)
            {
                float t = j / (float)segments;
                Vector3 smoothPoint = CatmullRomSpline(p0, p1, p2, p3, t);
                smoothPoints.Add(smoothPoint);
            }
        }
        
        // Add the last point
        smoothPoints.Add(chain[chain.Length - 1]);
        
        return smoothPoints.ToArray();
    }
    
    /// <summary>
    /// Catmull-Rom spline interpolation
    /// </summary>
    private Vector3 CatmullRomSpline(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        
        // Catmull-Rom matrix coefficients
        float c0 = -0.5f * t3 + t2 - 0.5f * t;
        float c1 = 1.5f * t3 - 2.5f * t2 + 1.0f;
        float c2 = -1.5f * t3 + 2.0f * t2 + 0.5f * t;
        float c3 = 0.5f * t3 - 0.5f * t2;
        
        return c0 * p0 + c1 * p1 + c2 * p2 + c3 * p3;
    }
    
    /// <summary>
    /// Generates an offset curve parallel to the original curve
    /// </summary>
    private Vector3[] GenerateOffsetCurve(Vector3[] originalCurve, float offset)
    {
        var offsetCurve = new Vector3[originalCurve.Length];
        
        for (int i = 0; i < originalCurve.Length; i++)
        {
            Vector3 point = originalCurve[i];
            
            // Calculate tangent direction at this point
            Vector3 tangent;
            if (i == 0)
            {
                // First point: use direction to next point
                tangent = (originalCurve[i + 1] - point).normalized;
            }
            else if (i == originalCurve.Length - 1)
            {
                // Last point: use direction from previous point
                tangent = (point - originalCurve[i - 1]).normalized;
            }
            else
            {
                // Middle point: average of both directions
                Vector3 dir1 = (originalCurve[i + 1] - point).normalized;
                Vector3 dir2 = (point - originalCurve[i - 1]).normalized;
                tangent = (dir1 + dir2).normalized;
            }
            
            // Calculate perpendicular direction (perpendicular to tangent and radial)
            Vector3 radial = point.normalized;
            Vector3 perpendicular = Vector3.Cross(tangent, radial).normalized;
            
            // Apply offset perpendicular to the curve
            offsetCurve[i] = point + perpendicular * offset;
        }
        
        return offsetCurve;
    }
    
    /// <summary>
    /// Sets the active state of the border
    /// </summary>
    public void SetActive(bool active)
    {
        isActive = active;
        if (borderObjectA != null)
        {
            borderObjectA.SetActive(active);
        }
        if (borderObjectB != null)
        {
            borderObjectB.SetActive(active);
        }
    }
    
    /// <summary>
    /// Destroys the border GameObject and material
    /// </summary>
    public void Destroy()
    {
        if (borderObjectA != null)
        {
            if (Application.isPlaying)
                Object.Destroy(borderObjectA);
            else
                Object.DestroyImmediate(borderObjectA);
            borderObjectA = null;
        }
        if (borderObjectB != null)
        {
            if (Application.isPlaying)
                Object.Destroy(borderObjectB);
            else
                Object.DestroyImmediate(borderObjectB);
            borderObjectB = null;
        }
        if (borderMaterial != null)
        {
            if (Application.isPlaying)
                Object.Destroy(borderMaterial);
            else
                Object.DestroyImmediate(borderMaterial);
            borderMaterial = null;
        }
    }
    
    /// <summary>
    /// Gets a unique key for this border segment
    /// </summary>
    public string GetKey()
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

    public void SetParent(Transform parent)
    {
        borderParent = parent;
        
        // Update existing GameObjects if they exist
        if (borderObjectA != null)
        {
            borderObjectA.transform.SetParent(parent);
        }
        if (borderObjectB != null)
        {
            borderObjectB.transform.SetParent(parent);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (borderObjectA == null && borderObjectB == null) return;
        if (borderObjectA != null)
        {
            var meshFilterA = borderObjectA.GetComponent<MeshFilter>();
            if (meshFilterA == null || meshFilterA.sharedMesh == null) return;
            var meshA = meshFilterA.sharedMesh;
            var vertsA = meshA.vertices;
            var trisA = meshA.triangles;
            Gizmos.color = Color.yellow;
            for (int i = 0; i < trisA.Length; i += 3)
            {
                if (trisA[i] < vertsA.Length && trisA[i+1] < vertsA.Length && trisA[i+2] < vertsA.Length)
                {
                    Gizmos.DrawLine(borderObjectA.transform.TransformPoint(vertsA[trisA[i]]), borderObjectA.transform.TransformPoint(vertsA[trisA[i+1]]));
                    Gizmos.DrawLine(borderObjectA.transform.TransformPoint(vertsA[trisA[i+1]]), borderObjectA.transform.TransformPoint(vertsA[trisA[i+2]]));
                    Gizmos.DrawLine(borderObjectA.transform.TransformPoint(vertsA[trisA[i+2]]), borderObjectA.transform.TransformPoint(vertsA[trisA[i]]));
                }
            }
            // Draw magenta spheres at each vertex
            Gizmos.color = Color.magenta;
            foreach (var v in vertsA)
            {
                Gizmos.DrawSphere(borderObjectA.transform.TransformPoint(v), 0.2f);
            }
        }
        if (borderObjectB != null)
        {
            var meshFilterB = borderObjectB.GetComponent<MeshFilter>();
            if (meshFilterB == null || meshFilterB.sharedMesh == null) return;
            var meshB = meshFilterB.sharedMesh;
            var vertsB = meshB.vertices;
            var trisB = meshB.triangles;
            Gizmos.color = Color.yellow;
            for (int i = 0; i < trisB.Length; i += 3)
            {
                if (trisB[i] < vertsB.Length && trisB[i+1] < vertsB.Length && trisB[i+2] < vertsB.Length)
                {
                    Gizmos.DrawLine(borderObjectB.transform.TransformPoint(vertsB[trisB[i]]), borderObjectB.transform.TransformPoint(vertsB[trisB[i+1]]));
                    Gizmos.DrawLine(borderObjectB.transform.TransformPoint(vertsB[trisB[i+1]]), borderObjectB.transform.TransformPoint(vertsB[trisB[i+2]]));
                    Gizmos.DrawLine(borderObjectB.transform.TransformPoint(vertsB[trisB[i+2]]), borderObjectB.transform.TransformPoint(vertsB[trisB[i]]));
                }
            }
            // Draw magenta spheres at each vertex
            Gizmos.color = Color.magenta;
            foreach (var v in vertsB)
            {
                Gizmos.DrawSphere(borderObjectB.transform.TransformPoint(v), 0.2f);
            }
        }
    }
#endif
} 