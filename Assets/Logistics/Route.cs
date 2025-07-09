using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Representa una ruta de logística entre un triángulo origen y un acceptor destino
/// </summary>
public class Route : MonoBehaviour
{
    [Header("Route Properties")]
    public TriangleData originTriangle;
    public IResourceAcceptor destinationAcceptor;
    public List<TriangleData> pathTriangles = new List<TriangleData>();
    public RouteType routeType = RouteType.Land;
    public Resource associatedResource; // El resource que originó esta ruta
    
    [Header("Visual")]
    public GameObject visualRepresentation;
    
    /// <summary>
    /// Inicializa la ruta con los datos básicos
    /// </summary>
    /// <param name="origin">Triángulo de origen</param>
    /// <param name="destination">Acceptor de destino</param>
    /// <param name="type">Tipo de ruta (default: Land)</param>
    /// <param name="resource">El resource que originó esta ruta</param>
    public void Initialize(TriangleData origin, IResourceAcceptor destination, RouteType type = RouteType.Land, Resource resource = null)
    {
        originTriangle = origin;
        destinationAcceptor = destination;
        routeType = type;
        associatedResource = resource;
        pathTriangles.Clear();
        
        // Calcular el path real usando DistanceCalculator
        CalculatePath();
    }
    
    /// <summary>
    /// Calcula el path real entre origen y destino usando DistanceCalculator
    /// </summary>
    private void CalculatePath()
    {
        if (originTriangle == null || destinationAcceptor == null)
        {
            Debug.LogWarning("No se puede calcular path: origen o destino son null");
            return;
        }
        
        // Obtener el triángulo de destino del acceptor
        TriangleData destinationTriangle = destinationAcceptor.GetTriangle();
        if (destinationTriangle == null)
        {
            Debug.LogWarning("El acceptor de destino no tiene triángulo asignado");
            return;
        }
        
        // Buscar el DistanceCalculator en la escena
        DistanceCalculator distanceCalculator = FindObjectOfType<DistanceCalculator>();
        if (distanceCalculator == null)
        {
            Debug.LogWarning("No se encontró DistanceCalculator en la escena");
            return;
        }
        
        // Configurar filtros según el tipo de ruta
        ConfigureDistanceCalculatorFilters(distanceCalculator);
        
        // Calcular el path
        List<int> pathTriangleIds;
        int distance = distanceCalculator.CalculateDistance(originTriangle.id, destinationTriangle.id, out pathTriangleIds);
        
        if (distance == -1)
        {
            Debug.LogWarning($"No se encontró path válido entre {originTriangle.id} y {destinationTriangle.id}");
            return;
        }
        
        // Convertir IDs de triángulos a TriangleData y llenar pathTriangles
        FillPathTriangles(pathTriangleIds);
        
        Debug.Log($"Path calculado: {distance} hops desde {originTriangle.id} hasta {destinationTriangle.id}");
    }
    
    /// <summary>
    /// Configura los filtros del DistanceCalculator según el tipo de ruta
    /// </summary>
    /// <param name="distanceCalculator">Instancia del DistanceCalculator</param>
    private void ConfigureDistanceCalculatorFilters(DistanceCalculator distanceCalculator)
    {
        switch (routeType)
        {
            case RouteType.Land:
                distanceCalculator.SetTerrainFilter(DistanceCalculator.TerrainFilter.LandOnly);
                break;
            case RouteType.Water:
                distanceCalculator.SetTerrainFilter(DistanceCalculator.TerrainFilter.WaterOnly);
                break;
            case RouteType.Pipe:
            case RouteType.Electric:
            case RouteType.Virtual:
                // Estos tipos pueden cruzar tanto tierra como agua
                distanceCalculator.SetTerrainFilter(DistanceCalculator.TerrainFilter.Both);
                break;
            case RouteType.Any:
            default:
                distanceCalculator.SetTerrainFilter(DistanceCalculator.TerrainFilter.Both);
                break;
        }
        
        // Por ahora no filtramos por país, pero podríamos agregarlo después
        distanceCalculator.SetCountryFilter(false);
    }
    
    /// <summary>
    /// Llena pathTriangles con los TriangleData correspondientes a los IDs calculados
    /// </summary>
    /// <param name="pathTriangleIds">Lista de IDs de triángulos del path</param>
    private void FillPathTriangles(List<int> pathTriangleIds)
    {
        pathTriangles.Clear();
        
        // Buscar el IcoSphere para acceder a los TriangleData
        IcoSphere icoSphere = FindObjectOfType<IcoSphere>();
        if (icoSphere == null || icoSphere.triangleDataList == null)
        {
            Debug.LogError("No se encontró IcoSphere o triangleDataList");
            return;
        }
        
        // Convertir IDs a TriangleData
        foreach (int triangleId in pathTriangleIds)
        {
            if (triangleId >= 0 && triangleId < icoSphere.triangleDataList.Count)
            {
                TriangleData triangle = icoSphere.triangleDataList[triangleId];
                if (triangle != null)
                {
                    pathTriangles.Add(triangle);
                }
            }
        }
        
        Debug.Log($"Path llenado con {pathTriangles.Count} triángulos");
        
        // Crear visualización del path
        CreatePathVisualization();
    }
    
    /// <summary>
    /// Crea la visualización gráfica del path usando LineRenderer
    /// </summary>
    private void CreatePathVisualization()
    {
        // Limpiar visualización anterior
        ClearPathVisualization();
        
        if (pathTriangles.Count < 2)
        {
            Debug.LogWarning("No hay suficientes triángulos para crear visualización del path");
            return;
        }
        
        // Crear GameObject padre para toda la visualización
        visualRepresentation = new GameObject($"RouteVisual_{originTriangle?.id ?? 0}_to_{destinationAcceptor?.id ?? 0}");
        visualRepresentation.transform.SetParent(transform);
        
        // Configurar color según tipo de ruta
        Color pathColor = GetRouteColor();
        float pathWidth = 10f; // Ancho de línea (en unidades de mundo)
        float pathHeight = 50f; // Altura para que las líneas se vean por encima del terreno
        
        // Crear LineRenderer para cada segmento del path
        for (int i = 0; i < pathTriangles.Count - 1; i++)
        {
            TriangleData currentTriangle = pathTriangles[i];
            TriangleData nextTriangle = pathTriangles[i + 1];
            
            // Obtener centros de los triángulos
            Vector3 currentCenter = currentTriangle.GetCenter();
            Vector3 nextCenter = nextTriangle.GetCenter();
            
            // Crear GameObject para este segmento de línea
            GameObject lineObj = new GameObject($"PathSegment_{i}");
            lineObj.transform.SetParent(visualRepresentation.transform);
            
            // Agregar LineRenderer
            LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
            lineRenderer.material.color = pathColor;
            lineRenderer.startWidth = pathWidth;
            lineRenderer.endWidth = pathWidth;
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;
            lineRenderer.sortingOrder = 1000;
            
            // Aplicar offset de altura para visibilidad
            Vector3 offsetA = currentCenter.normalized * pathHeight;
            Vector3 offsetB = nextCenter.normalized * pathHeight;
            lineRenderer.SetPosition(0, currentCenter + offsetA);
            lineRenderer.SetPosition(1, nextCenter + offsetB);
        }
        
        Debug.Log($"Visualización del path creada con {pathTriangles.Count - 1} segmentos");
    }
    
    /// <summary>
    /// Obtiene el color apropiado según el tipo de ruta
    /// </summary>
    /// <returns>Color para la visualización del path</returns>
    private Color GetRouteColor()
    {
        switch (routeType)
        {
            case RouteType.Land:
                return Color.green;
            case RouteType.Water:
                return Color.blue;
            case RouteType.Pipe:
                return Color.orange;
            case RouteType.Electric:
                return Color.yellow;
            case RouteType.Virtual:
                return Color.cyan;
            case RouteType.Any:
            default:
                return Color.white;
        }
    }
    
    /// <summary>
    /// Limpia la visualización actual del path
    /// </summary>
    private void ClearPathVisualization()
    {
        if (visualRepresentation != null)
        {
            if (Application.isPlaying)
                Destroy(visualRepresentation);
            else
                DestroyImmediate(visualRepresentation);
            visualRepresentation = null;
        }
    }
    
    /// <summary>
    /// Agrega un triángulo al path de la ruta
    /// </summary>
    /// <param name="triangle">Triángulo a agregar al path</param>
    public void AddTriangleToPath(TriangleData triangle)
    {
        if (triangle != null && !pathTriangles.Contains(triangle))
        {
            pathTriangles.Add(triangle);
        }
    }
    
    /// <summary>
    /// Limpia el path actual
    /// </summary>
    public void ClearPath()
    {
        pathTriangles.Clear();
    }
} 