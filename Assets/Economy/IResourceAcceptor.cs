using UnityEngine;

/// <summary>
/// Interfaz que define objetos que pueden recibir recursos mediante drag & drop
/// </summary>
public interface IResourceAcceptor
{
    /// <summary>
    /// Verifica si este objeto puede aceptar el recurso especificado
    /// </summary>
    /// <param name="resource">El recurso que se intenta droppear</param>
    /// <returns>True si puede aceptar el recurso, false en caso contrario</returns>
    bool CanAcceptResource(Resource resource);
    
    /// <summary>
    /// Procesa el drop de un recurso en este objeto
    /// </summary>
    /// <param name="resource">El recurso que se está droppeando</param>
    /// <returns>True si el drop fue exitoso, false en caso contrario</returns>
    bool AcceptResource(Resource resource);
    
    /// <summary>
    /// Obtiene el punto donde se debe colocar el recurso cuando se hace drop
    /// </summary>
    /// <returns>El punto donde colocar el recurso</returns>
    IResourceDropPosition GetDropPosition();
} 