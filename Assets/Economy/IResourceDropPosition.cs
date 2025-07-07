using UnityEngine;

/// <summary>
/// Interfaz que define un punto donde se puede droppear un recurso
/// </summary>
public interface IResourceDropPosition
{
    /// <summary>
    /// Obtiene la posición en el mundo donde colocar el recurso
    /// </summary>
    /// <returns>La posición en el mundo</returns>
    Vector3 GetWorldPosition();
    
    /// <summary>
    /// Obtiene el nombre o identificador de este punto de drop
    /// </summary>
    /// <returns>El nombre del punto de drop</returns>
    string GetDropPositionName();
    
    /// <summary>
    /// Verifica si este punto de drop está disponible para recibir recursos
    /// </summary>
    /// <returns>True si está disponible, false en caso contrario</returns>
    bool IsAvailable();
} 