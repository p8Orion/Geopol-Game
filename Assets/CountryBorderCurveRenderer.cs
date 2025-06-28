using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(IcoSphere))]
public class CountryBorderCurveRenderer : MonoBehaviour
{
    public Material borderCurveMaterial;
    public float borderWidth = 0.2f;
    public Color borderColor = Color.white;
    [Range(-0.1f, 0.1f)] public float borderOffset = 0.01f; // Tangential offset (inward/outward)
    [Range(0, 0.05f)] public float borderRadialOffset = 0.05f; // Radial offset (outward from sphere)
    [Range(0, 1)] public float borderAlphaFade = 0.5f; // How much to fade towards the sides

    private IcoSphere icoSphere;
    private List<List<Vector3>> borderRegions = new List<List<Vector3>>();
    private List<List<Vector3>> offsetSmoothBorders = new List<List<Vector3>>();
    private List<GameObject> borderObjects = new List<GameObject>();

    void Awake()
    {
        icoSphere = GetComponent<IcoSphere>();
        
        // Create border curve material if not assigned
        if (borderCurveMaterial == null)
        {
            Debug.Log("CountryBorderCurveRenderer: No border curve material assigned, creating one...");
            Shader borderShader = Shader.Find("Custom/CountryBorder");
            if (borderShader != null)
            {
                borderCurveMaterial = new Material(borderShader);
                Debug.Log("CountryBorderCurveRenderer: Successfully created border curve material");
            }
            else
            {
                Debug.LogError("CountryBorderCurveRenderer: Custom/CountryBorder shader not found! Please ensure the shader is compiled.");
                
                // Try to find any shader as fallback
                Shader fallbackShader = Shader.Find("Universal Render Pipeline/Lit");
                if (fallbackShader != null)
                {
                    borderCurveMaterial = new Material(fallbackShader);
                    Debug.LogWarning("CountryBorderCurveRenderer: Using fallback shader for debugging");
                }
                else
                {
                    Debug.LogError("CountryBorderCurveRenderer: No shader found at all!");
                    return;
                }
            }
        }
        else
        {
            Debug.Log("CountryBorderCurveRenderer: Border curve material already assigned");
        }
    }

    void OnEnable()
    {
        Debug.Log("CountryBorderCurveRenderer: Component enabled");
    }
    
    void OnDisable()
    {
        Debug.Log("CountryBorderCurveRenderer: Component disabled, cleaning up border objects");
        // Clean up border objects when disabled
        foreach (var obj in borderObjects)
        {
            if (obj != null) DestroyImmediate(obj);
        }
        borderObjects.Clear();
    }

    public void RefreshBorders()
    {
        Debug.Log("=== CountryBorderCurveRenderer RefreshBorders() Start ===");
        
        // Clean up old border objects
        foreach (var obj in borderObjects)
        {
            if (obj != null) DestroyImmediate(obj);
        }
        borderObjects.Clear();
        borderRegions.Clear();
        offsetSmoothBorders.Clear();
        
        if (icoSphere == null)
        {
            Debug.LogError("CountryBorderCurveRenderer: IcoSphere is null!");
            return;
        }
        
        if (icoSphere.triangleDataList == null)
        {
            Debug.LogError("CountryBorderCurveRenderer: triangleDataList is null!");
            return;
        }
        
        Debug.Log($"CountryBorderCurveRenderer: Processing {icoSphere.triangleDataList.Count} triangles");

        var visited = new HashSet<int>();
        int regionsFound = 0;
        
        for (int i = 0; i < icoSphere.triangleDataList.Count; i++)
        {
            var tri = icoSphere.triangleDataList[i];
            if (tri.country == null || visited.Contains(i)) continue;

            // Find all triangles in this contiguous region
            var region = new List<int>();
            var stack = new Stack<int>();
            stack.Push(i);
            visited.Add(i);
            while (stack.Count > 0)
            {
                int idx = stack.Pop();
                region.Add(idx);
                var t = icoSphere.triangleDataList[idx];
                foreach (var adj in t.adjacentTriangles)
                {
                    if (!visited.Contains(adj) && icoSphere.triangleDataList[adj].country == tri.country)
                    {
                        stack.Push(adj);
                        visited.Add(adj);
                    }
                }
            }

            Debug.Log($"CountryBorderCurveRenderer: Found region with {region.Count} triangles for country: {tri.country?.name ?? "Unknown"}");

            // Trace the border of this region
            var border = TraceRegionBorder(region, icoSphere.triangleDataList);
            if (border.Count > 0)
            {
                borderRegions.Add(border);
                var smooth = GenerateCatmullRomSpline(border, 10);
                var offset = OffsetBorderInward(smooth, region, icoSphere.triangleDataList, borderOffset);
                offsetSmoothBorders.Add(offset);
                // Create mesh and GameObject
                var borderObj = CreateBorderMeshObject(offset, tri.country.color);
                if (borderObj != null)
                {
                    borderObjects.Add(borderObj);
                    regionsFound++;
                    Debug.Log($"CountryBorderCurveRenderer: Created border object for country: {tri.country?.name ?? "Unknown"}");
                }
            }
        }
        
        Debug.Log($"CountryBorderCurveRenderer: Created {regionsFound} border objects");
        Debug.Log("=== CountryBorderCurveRenderer RefreshBorders() End ===");
    }

    private GameObject CreateBorderMeshObject(List<Vector3> border, Color countryColor)
    {
        if (border.Count < 2) return null;
        int segs = border.Count;
        int vertsPerSeg = 2;
        Vector3[] vertices = new Vector3[segs * vertsPerSeg];
        Color[] colors = new Color[segs * vertsPerSeg];
        int[] tris = new int[(segs - 1) * 6];
        float halfWidth = borderWidth * 0.5f;
        for (int i = 0; i < segs; i++)
        {
            Vector3 p = border[i];
            Vector3 prev = border[(i - 1 + segs) % segs];
            Vector3 next = border[(i + 1) % segs];
            Vector3 tangent = (next - prev).normalized;
            Vector3 normal = Vector3.Cross(tangent, p.normalized).normalized;
            // Inward/outward: both sides for fade
            vertices[i * 2 + 0] = p - normal * halfWidth;
            vertices[i * 2 + 1] = p + normal * halfWidth;
            // Alpha fade: center = full alpha, edge = fade
            colors[i * 2 + 0] = new Color(countryColor.r, countryColor.g, countryColor.b, borderAlphaFade * borderColor.a);
            colors[i * 2 + 1] = new Color(countryColor.r, countryColor.g, countryColor.b, 0f);
        }
        int triIdx = 0;
        for (int i = 0; i < segs - 1; i++)
        {
            int i0 = i * 2;
            int i1 = i * 2 + 1;
            int i2 = (i + 1) * 2;
            int i3 = (i + 1) * 2 + 1;
            tris[triIdx++] = i0;
            tris[triIdx++] = i2;
            tris[triIdx++] = i1;
            tris[triIdx++] = i1;
            tris[triIdx++] = i2;
            tris[triIdx++] = i3;
        }
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        GameObject obj = new GameObject("BorderCurve");
        obj.transform.SetParent(transform);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;
        var mf = obj.AddComponent<MeshFilter>();
        var mr = obj.AddComponent<MeshRenderer>();
        mf.mesh = mesh;
        mr.material = borderCurveMaterial;
        return obj;
    }

    // Traces the outer border of a region (returns list of world positions)
    private List<Vector3> TraceRegionBorder(List<int> region, List<TriangleData> triangles)
    {
        var regionSet = new HashSet<int>(region);
        var edgeCounts = new Dictionary<(Vector3, Vector3), int>();

        // Count all edges in the region
        foreach (var triIdx in region)
        {
            var tri = triangles[triIdx];
            var edges = new[] { (tri.a, tri.b), (tri.b, tri.c), (tri.c, tri.a) };
            foreach (var (v1, v2) in edges)
            {
                var edge = (v1, v2);
                var edgeRev = (v2, v1);
                if (edgeCounts.ContainsKey(edgeRev))
                    edgeCounts[edgeRev]++;
                else if (edgeCounts.ContainsKey(edge))
                    edgeCounts[edge]++;
                else
                    edgeCounts[edge] = 1;
            }
        }

        // Border edges are those that appear only once
        var borderEdges = new List<(Vector3, Vector3)>();
        foreach (var kvp in edgeCounts)
        {
            if (kvp.Value == 1)
                borderEdges.Add(kvp.Key);
        }

        // Trace the border as a continuous path
        var borderPath = new List<Vector3>();
        if (borderEdges.Count == 0) return borderPath;
        var edgeDict = new Dictionary<Vector3, Vector3>();
        foreach (var (start, end) in borderEdges)
            edgeDict[start] = end;

        // Start from any border edge
        var first = borderEdges[0];
        borderPath.Add(first.Item1);
        var current = first.Item2;
        while (current != first.Item1 && borderPath.Count <= borderEdges.Count)
        {
            borderPath.Add(current);
            if (edgeDict.TryGetValue(current, out var next))
                current = next;
            else
                break;
        }
        return borderPath;
    }

    // Generate a Catmull-Rom spline from the border points
    private List<Vector3> GenerateCatmullRomSpline(List<Vector3> points, int subdivisions)
    {
        var spline = new List<Vector3>();
        int n = points.Count;
        if (n < 3) return new List<Vector3>(points);
        for (int i = 0; i < n; i++)
        {
            Vector3 p0 = points[(i - 1 + n) % n];
            Vector3 p1 = points[i];
            Vector3 p2 = points[(i + 1) % n];
            Vector3 p3 = points[(i + 2) % n];
            for (int j = 0; j < subdivisions; j++)
            {
                float t = j / (float)subdivisions;
                Vector3 pt = 0.5f * (
                    (2 * p1) +
                    (-p0 + p2) * t +
                    (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t +
                    (-p0 + 3 * p1 - 3 * p2 + p3) * t * t * t
                );
                spline.Add(pt.normalized * p1.magnitude); // Keep on sphere
            }
        }
        return spline;
    }

    // Offset the border inward by a given amount
    private List<Vector3> OffsetBorderInward(List<Vector3> border, List<int> region, List<TriangleData> triangles, float offsetAmount)
    {
        var regionSet = new HashSet<int>(region);
        var offsetBorder = new List<Vector3>();
        // Compute region center
        Vector3 center = Vector3.zero;
        foreach (var triIdx in region)
        {
            var tri = triangles[triIdx];
            center += (tri.a + tri.b + tri.c) / 3f;
        }
        center /= region.Count;
        center.Normalize();
        // For each border point, move slightly towards the center (inward/outward), and also outward from the sphere
        foreach (var pt in border)
        {
            Vector3 inward = (center - pt).normalized;
            Vector3 tangentialOffset = inward * offsetAmount;
            Vector3 radialOffset = pt.normalized * borderRadialOffset;
            Vector3 offsetPt = (pt + tangentialOffset + radialOffset).normalized * pt.magnitude;
            offsetBorder.Add(offsetPt);
        }
        return offsetBorder;
    }

    // Rendering will be added in the next step
} 