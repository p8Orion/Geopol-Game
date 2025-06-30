using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class BorderDebugGizmos : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool showBorderChains = true;
    public bool showEdgeDirections = true;
    public bool showTriangleInfo = true;
    public bool showCountryInfo = true;
    
    [Header("Visual Settings")]
    public float arrowLength = 30f;
    public float arrowHeadSize = 15f;
    public float labelOffset = 25f;
    public float chainPointSize = 8f;
    public float edgeLineWidth = 3f;
    
    [Header("Chain Selection")]
    public int selectedChainIndex = 0;
    public bool showAllChains = true;
    
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
        if (!showBorderChains) return;
        
        // Obtener referencias si no las tenemos
        if (icoSphere == null) icoSphere = FindObjectOfType<IcoSphere>();
        if (borderManager == null) borderManager = FindObjectOfType<BorderManager>();
        
        if (icoSphere == null || borderManager == null) 
        {
            Debug.LogWarning("BorderDebugGizmos: IcoSphere o BorderManager no encontrados!");
            return;
        }
        
        // Obtener todos los borders
        var allBorders = borderManager.GetAllBorders();
        
        if (allBorders == null || allBorders.Count == 0)
        {
            Debug.LogWarning("BorderDebugGizmos: No hay borders generados!");
            return;
        }
        
        Debug.Log($"BorderDebugGizmos: Encontrados {allBorders.Count} borders");
        
        // Dibujar todos los borders
        foreach (var border in allBorders)
        {
            if (border == null || border.chainsWithOrientation == null)
            {
                Debug.LogWarning($"BorderDebugGizmos: Border null o sin chains!");
                continue;
            }
            
            DrawBorderSegment(border);
        }
    }
    
    private void DrawBorderSegment(BorderSegment border)
    {
        var chains = border.chainsWithOrientation;
        
        if (chains == null || chains.Count == 0)
        {
            Debug.LogWarning($"BorderDebugGizmos: Border {border.GetKey()} no tiene chains!");
            return;
        }
        
        Debug.Log($"BorderDebugGizmos: Dibujando border {border.GetKey()} con {chains.Count} chains");
        
        // Color base para este border
        Color borderColor = Color.HSVToRGB((border.GetKey().GetHashCode() * 0.1f) % 1f, 0.8f, 1f);
        
        for (int chainIndex = 0; chainIndex < chains.Count; chainIndex++)
        {
            if (!showAllChains && chainIndex != selectedChainIndex) continue;
            
            var (chain, countryAIsLeft, triA, triB) = chains[chainIndex];
            if (chain == null || chain.Length < 2) 
            {
                Debug.LogWarning($"BorderDebugGizmos: Chain {chainIndex} es null o muy corto!");
                continue;
            }
            
            // Color del chain
            Color chainColor = Color.HSVToRGB((chainIndex * 0.2f) % 1f, 0.8f, 1f);
            
            // Dibujar información del chain
            DrawChainInfo(chain, border, chainIndex, countryAIsLeft, triA, triB);
            
            // Dibujar puntos del chain
            DrawChainPoints(chain, chainColor, chainIndex);
            
            // Dibujar edges del chain
            DrawChainEdges(chain, chainColor, chainIndex, border, countryAIsLeft);
        }
    }
    
    private void DrawChainInfo(Vector3[] chain, BorderSegment border, int chainIndex, bool countryAIsLeft, TriangleData triA, TriangleData triB)
    {
        // Dibujar información del chain en el primer punto
        if (chain.Length > 0)
        {
            Vector3 infoPos = chain[0] + Vector3.up * 50f + Vector3.up * (chainIndex * 30f);
            string info = $"Border: {border.GetKey()}\n";
            info += $"Chain: {chainIndex}\n";
            info += $"Points: {chain.Length}\n";
            info += $"A izq: {countryAIsLeft}";
            
            // Mostrar información de los triángulos reales usados para renderizar
            if (triA != null && triB != null)
            {
                Country leftCountry = countryAIsLeft ? border.countryA : border.countryB;
                Country rightCountry = countryAIsLeft ? border.countryB : border.countryA;
                TriangleData leftTriangle = countryAIsLeft ? triA : triB;
                TriangleData rightTriangle = countryAIsLeft ? triB : triA;
                
                info += $"\nIZQ: {leftCountry?.name} (Tri {leftTriangle.id})";
                info += $"\nDER: {rightCountry?.name} (Tri {rightTriangle.id})";
            }
            
            DrawLabel(infoPos, info, Color.cyan);
        }
    }
    
    private void DrawChainPoints(Vector3[] chain, Color color, int chainIndex)
    {
        Gizmos.color = color;
        
        for (int i = 0; i < chain.Length; i++)
        {
            Vector3 point = chain[i];
            
            // Dibujar punto
            Gizmos.DrawSphere(point, chainPointSize);
            
            // Etiqueta del punto
            if (showTriangleInfo)
            {
                Vector3 labelOffset = Vector3.up * 10f + Vector3.up * (chainIndex * 15f);
                DrawLabel(point + labelOffset, $"P{i}", Color.white);
            }
        }
    }
    
    private void DrawChainEdges(Vector3[] chain, Color color, int chainIndex, BorderSegment border, bool countryAIsLeft)
    {
        for (int i = 0; i < chain.Length - 1; i++)
        {
            Vector3 v0 = chain[i];
            Vector3 v1 = chain[i + 1];
            
            // Dibujar línea del edge
            Gizmos.color = color;
            Gizmos.DrawLine(v0, v1);
            
            // Dibujar dirección del edge
            if (showEdgeDirections)
            {
                DrawEdgeDirection(v0, v1, color);
            }
            
            // Dibujar información del edge
            if (showTriangleInfo || showCountryInfo)
            {
                DrawEdgeInfo(v0, v1, i, chainIndex, border, countryAIsLeft);
            }
        }
    }
    
    private void DrawChainEdges(Vector3[] chain, Color color, int chainIndex, bool countryAIsLeft)
    {
        // Método legacy - mantener para compatibilidad
        DrawChainEdges(chain, color, chainIndex, null, countryAIsLeft);
    }
    
    private void DrawEdgeDirection(Vector3 v0, Vector3 v1, Color color)
    {
        Vector3 edgeMid = (v0 + v1) * 0.5f;
        Vector3 direction = (v1 - v0).normalized;
        
        // Dibujar flecha de dirección
        Gizmos.color = color;
        DrawArrow(edgeMid, edgeMid + direction * arrowLength, arrowLength, arrowHeadSize);
    }
    
    private void DrawEdgeInfo(Vector3 v0, Vector3 v1, int edgeIndex, int chainIndex, BorderSegment border, bool countryAIsLeft)
    {
        Vector3 edgeMid = (v0 + v1) * 0.5f;
        Vector3 edgeDir = (v1 - v0).normalized;
        Vector3 perp = Vector3.Cross(edgeDir, edgeMid.normalized).normalized;
        
        // Encontrar los triángulos que comparten este edge
        var (triA, triB) = FindTrianglesForEdge(v0, v1, border.countryA, border.countryB);
        
        if (triA != null && triB != null)
        {
            // Determinar cuál está a la izquierda y cuál a la derecha
            Country leftCountry, rightCountry;
            TriangleData leftTriangle, rightTriangle;
            
            if (countryAIsLeft)
            {
                leftCountry = border.countryA;
                rightCountry = border.countryB;
                leftTriangle = triA;
                rightTriangle = triB;
            }
            else
            {
                leftCountry = border.countryB;
                rightCountry = border.countryA;
                leftTriangle = triB;
                rightTriangle = triA;
            }
            
            // Etiqueta izquierda
            Vector3 leftPos = edgeMid + perp * labelOffset;
            Gizmos.color = leftCountry?.color ?? Color.gray;
            Gizmos.DrawSphere(leftPos, 12f);
            
            string leftInfo = "";
            if (showCountryInfo) leftInfo += $"IZQ: {leftCountry?.name ?? "Unclaimed"}\n";
            if (showTriangleInfo) leftInfo += $"Tri: {leftTriangle.id}";
            
            Vector3 leftLabelOffset = Vector3.up * (chainIndex * 15f);
            DrawLabel(leftPos + leftLabelOffset, leftInfo, Color.white);
            
            // Etiqueta derecha
            Vector3 rightPos = edgeMid - perp * labelOffset;
            Gizmos.color = rightCountry?.color ?? Color.gray;
            Gizmos.DrawSphere(rightPos, 12f);
            
            string rightInfo = "";
            if (showCountryInfo) rightInfo += $"DER: {rightCountry?.name ?? "Unclaimed"}\n";
            if (showTriangleInfo) rightInfo += $"Tri: {rightTriangle.id}";
            
            Vector3 rightLabelOffset = Vector3.up * (chainIndex * 15f);
            DrawLabel(rightPos + rightLabelOffset, rightInfo, Color.white);
            
            // Etiqueta del edge
            string edgeInfo = $"Chain {chainIndex}\nEdge {edgeIndex}";
            Vector3 edgeLabelOffset = Vector3.up * (chainIndex * 20f);
            DrawLabel(edgeMid + edgeLabelOffset, edgeInfo, Color.yellow);
        }
    }
    
    private (TriangleData, TriangleData) FindTrianglesForEdge(Vector3 v0, Vector3 v1, Country countryA, Country countryB)
    {
        var triangleDataList = icoSphere.triangleDataList;
        float tolerance = 0.01f;
        
        for (int i = 0; i < triangleDataList.Count; i++)
        {
            var ourTriangle = triangleDataList[i];
            if (ourTriangle.country != countryA && ourTriangle.country != countryB) continue;
            
            foreach (int adjacentId in ourTriangle.adjacentTriangles)
            {
                if (adjacentId < triangleDataList.Count)
                {
                    var neighborTriangle = triangleDataList[adjacentId];
                    if (neighborTriangle.country != null && ourTriangle.country != neighborTriangle.country)
                    {
                        // Verificar si este edge conecta estos dos triángulos
                        if (IsSharedEdge(ourTriangle, neighborTriangle, v0, v1, tolerance))
                        {
                            return (ourTriangle, neighborTriangle);
                        }
                    }
                }
            }
        }
        
        return (null, null);
    }
    
    private bool IsSharedEdge(TriangleData tri1, TriangleData tri2, Vector3 v0, Vector3 v1, float tolerance)
    {
        // Verificar si el edge v0-v1 es compartido entre tri1 y tri2
        Vector3[] tri1Vertices = { tri1.a, tri1.b, tri1.c };
        Vector3[] tri2Vertices = { tri2.a, tri2.b, tri2.c };
        
        bool v0InTri1 = false, v1InTri1 = false;
        bool v0InTri2 = false, v1InTri2 = false;
        
        foreach (var vertex in tri1Vertices)
        {
            if (Vector3.Distance(vertex, v0) < tolerance) v0InTri1 = true;
            if (Vector3.Distance(vertex, v1) < tolerance) v1InTri1 = true;
        }
        
        foreach (var vertex in tri2Vertices)
        {
            if (Vector3.Distance(vertex, v0) < tolerance) v0InTri2 = true;
            if (Vector3.Distance(vertex, v1) < tolerance) v1InTri2 = true;
        }
        
        return (v0InTri1 && v1InTri1) && (v0InTri2 && v1InTri2);
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