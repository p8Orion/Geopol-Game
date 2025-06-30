using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class BorderDebugGizmos : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool showBorderOrientations = true;
    public float arrowLength = 50f;
    public float arrowHeadSize = 20f;
    public float labelOffset = 30f;
    
    [Header("Border Configuration")]
    public Country countryA;
    public Country countryB;
    
    private IcoSphere icoSphere;
    private BorderManager borderManager;
    
    void Start()
    {
        icoSphere = FindObjectOfType<IcoSphere>();
        borderManager = FindObjectOfType<BorderManager>();
    }
    
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!showBorderOrientations) return;
        
        // Obtener referencias si no las tenemos
        if (icoSphere == null) icoSphere = FindObjectOfType<IcoSphere>();
        if (borderManager == null) borderManager = FindObjectOfType<BorderManager>();
        
        if (icoSphere == null || borderManager == null) return;
        
        // Si no están configurados los países, intentar obtenerlos del BorderManager
        if (countryA == null || countryB == null)
        {
            var allBorders = borderManager.GetAllBorders();
            if (allBorders.Count > 0)
            {
                var firstBorder = allBorders[0];
                countryA = firstBorder.countryA;
                countryB = firstBorder.countryB;
            }
        }
        
        if (countryA == null || countryB == null) return;
        
        var triangleDataList = icoSphere.triangleDataList;
        var edgeToTriangles = icoSphere.EdgeToTriangles;
        
        if (edgeToTriangles == null || triangleDataList == null) return;
        
        int edgeCount = 0;
        
        // Dibujar orientación solo de los edges de este segmento específico
        foreach (var kvp in edgeToTriangles)
        {
            var edge = kvp.Key;
            var tris = kvp.Value;
            
            if (tris.Count == 2)
            {
                var triL = triangleDataList[tris[0]]; // izquierda
                var triR = triangleDataList[tris[1]]; // derecha
                var countryL = triL.country;
                var countryR = triR.country;
                
                // Solo mostrar edges que pertenecen a este segmento específico
                if (countryL != null && countryR != null && 
                    ((countryL == countryA && countryR == countryB) ||
                     (countryL == countryB && countryR == countryA)))
                {
                    DrawBorderEdge(edge, countryL, countryR, edgeCount);
                    edgeCount++;
                }
            }
        }
        
        // Mostrar información del segmento
        if (edgeCount > 0)
        {
            Vector3 segmentCenter = GetSegmentCenter();
            DrawLabel(segmentCenter, $"Segmento: {countryA?.name} - {countryB?.name}\nEdges: {edgeCount}", Color.cyan);
        }
    }
    
    private Vector3 GetSegmentCenter()
    {
        // Calcular el centro del segmento basado en los países
        Vector3 centerA = countryA != null ? GetCountryCenter(countryA) : Vector3.zero;
        Vector3 centerB = countryB != null ? GetCountryCenter(countryB) : Vector3.zero;
        return (centerA + centerB) * 0.5f;
    }
    
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
    
    private void DrawBorderEdge(IcoSphere.Edge edge, Country countryL, Country countryR, int edgeIndex)
    {
        Vector3 v0 = edge.a;
        Vector3 v1 = edge.b;
        Vector3 edgeDir = (v1 - v0).normalized;
        Vector3 edgeMid = (v0 + v1) * 0.5f;
        
        // Color basado en el índice para distinguir edges
        Color edgeColor = Color.HSVToRGB((edgeIndex * 0.1f) % 1f, 0.8f, 1f);
        Gizmos.color = edgeColor;
        
        // Dibujar flecha de orientación del edge
        DrawArrow(v0, v1, arrowLength, arrowHeadSize);
        
        // Dibujar línea del edge
        Gizmos.DrawLine(v0, v1);
        
        // Dibujar etiquetas de países
        Vector3 perp = Vector3.Cross(edgeDir, edgeMid.normalized).normalized;
        Vector3 labelPosL = edgeMid + perp * labelOffset;
        Vector3 labelPosR = edgeMid - perp * labelOffset;
        
        // Etiqueta izquierda
        Gizmos.color = countryL.color;
        Gizmos.DrawSphere(labelPosL, 15f);
        DrawLabel(labelPosL, $"IZQ: {countryL.name}", Color.white);
        
        // Etiqueta derecha
        Gizmos.color = countryR.color;
        Gizmos.DrawSphere(labelPosR, 15f);
        DrawLabel(labelPosR, $"DER: {countryR.name}", Color.white);
        
        // Etiqueta del edge
        DrawLabel(edgeMid, $"Edge {edgeIndex}", Color.yellow);
    }
    
    private void DrawArrow(Vector3 start, Vector3 end, float length, float headSize)
    {
        Vector3 direction = (end - start).normalized;
        Vector3 arrowEnd = start + direction * length;
        
        // Línea principal de la flecha
        Gizmos.DrawLine(start, arrowEnd);
        
        // Cabeza de la flecha
        Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
        if (right.magnitude < 0.1f)
            right = Vector3.Cross(direction, Vector3.forward).normalized;
        
        Vector3 up = Vector3.Cross(right, direction).normalized;
        
        Vector3 head1 = arrowEnd - direction * headSize + right * headSize * 0.5f;
        Vector3 head2 = arrowEnd - direction * headSize - right * headSize * 0.5f;
        Vector3 head3 = arrowEnd - direction * headSize + up * headSize * 0.5f;
        Vector3 head4 = arrowEnd - direction * headSize - up * headSize * 0.5f;
        
        Gizmos.DrawLine(arrowEnd, head1);
        Gizmos.DrawLine(arrowEnd, head2);
        Gizmos.DrawLine(arrowEnd, head3);
        Gizmos.DrawLine(arrowEnd, head4);
    }
    
    private void DrawLabel(Vector3 position, string text, Color color)
    {
#if UNITY_EDITOR
        // Solo en el editor
        var style = new GUIStyle();
        style.normal.textColor = color;
        style.fontSize = 12;
        style.fontStyle = FontStyle.Bold;
        
        Handles.Label(position, text, style);
#endif
    }
#endif
} 