using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manager que controla la creación y gestión de rutas de logística
/// </summary>
public class RouteManager : MonoBehaviour
{
    [Header("Route Management")]
    public List<Route> activeRoutes = new List<Route>();
    
    /// <summary>
    /// Crea una nueva ruta cuando se droppea un resource en un acceptor
    /// </summary>
    /// <param name="originTriangle">Triángulo de origen del resource</param>
    /// <param name="destinationAcceptor">Acceptor de destino</param>
    /// <param name="routeType">Tipo de ruta (default: Land)</param>
    /// <param name="resource">El resource que originó la ruta</param>
    /// <returns>La ruta creada o null si no se pudo crear</returns>
    public Route CreateRoute(TriangleData originTriangle, IResourceAcceptor destinationAcceptor, RouteType routeType = RouteType.Land, Resource resource = null)
    {
        if (originTriangle == null || destinationAcceptor == null)
        {
            Debug.LogWarning("No se puede crear ruta: origen o destino son null");
            return null;
        }
        
        // Crear el GameObject de la ruta dinámicamente
        string resourceName = resource != null ? resource.type.ToString() : "Unknown";
        GameObject routeObject = new GameObject($"Route_{resourceName}_{originTriangle.id}_to_{destinationAcceptor.id}");
        routeObject.transform.SetParent(transform);
        
        // Agregar el componente Route
        Route newRoute = routeObject.AddComponent<Route>();
        
        // Inicializar la ruta
        newRoute.Initialize(originTriangle, destinationAcceptor, routeType, resource);
        
        // Agregar a la lista de rutas activas
        activeRoutes.Add(newRoute);
        
        Debug.Log($"Ruta creada: {originTriangle.id} -> {destinationAcceptor.id}");
        
        return newRoute;
    }
    
    /// <summary>
    /// Elimina una ruta específica
    /// </summary>
    /// <param name="route">Ruta a eliminar</param>
    public void RemoveRoute(Route route)
    {
        if (route != null && activeRoutes.Contains(route))
        {
            activeRoutes.Remove(route);
            Destroy(route.gameObject);
        }
    }
    
    /// <summary>
    /// Elimina todas las rutas activas
    /// </summary>
    public void ClearAllRoutes()
    {
        foreach (Route route in activeRoutes)
        {
            if (route != null)
            {
                Destroy(route.gameObject);
            }
        }
        activeRoutes.Clear();
    }
} 