using UnityEngine;
using UnityEngine.InputSystem;

public class OrbitalCamera : MonoBehaviour
{
    // Zoom Slots - Distancias fijas para diferentes rangos de zoom
    [Header("Zoom Levels")]
    public static float planetLevel = 7000f; // Máximo acercamiento (tope de zoom)
    public static float closeZoomMax = 8200f;
    public static float mediumZoomMax = 9500f; // Centro - tamaño base
    public static float farZoomMax = 10000f;
    public static float veryFarZoomMax = 15000f;

    public enum ZoomLevel
    {
        None,
        Ground, // Máximo acercamiento
        Close,
        Medium, // Centro - tamaño base
        Far,
        VeryFar
    }

    [Header("Camera Settings")]
    public Transform target;
    public float distance = 50000f;
    public float zoomSpeed = 1000f;
    public float rotationSpeed = 3f;
    public Vector2 pitchLimits = new(-85f, 85f);

    private float yaw = 0f;
    private float pitch = 20f;

    private Mouse mouse => Mouse.current;
    private MapEditor mapEditor; // Reference to MapEditor for GUI detection

    void Start()
    {
        // Find the MapEditor component
        mapEditor = Object.FindFirstObjectByType<MapEditor>();
    }

    void LateUpdate()
    {
        if (target == null || mouse == null) return;

        // Check if mouse is over MapEditor GUI before allowing zoom
        if (mapEditor != null && mapEditor.IsMouseOverGUI())
        {
            return; // Don't process camera controls if mouse is over GUI
        }

        // Zoom
        float scroll = mouse.scroll.ReadValue().y;
        distance -= scroll * zoomSpeed * Time.deltaTime;
        distance = Mathf.Clamp(distance, 100f, 1_000_000f);

        // Rotar con click derecho
        if (mouse.rightButton.isPressed)
        {
            yaw += mouse.delta.ReadValue().x * rotationSpeed * Time.deltaTime;
            pitch -= mouse.delta.ReadValue().y * rotationSpeed * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, pitchLimits.x, pitchLimits.y);
        }

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 direction = rotation * Vector3.forward;

        transform.position = target.position - direction * distance;
        transform.LookAt(target.position);
    }

    /// <summary>
    /// Gets the distance value for a specific zoom level
    /// </summary>
    public static float GetDistanceForZoomLevel(ZoomLevel level)
    {
        switch (level)
        {
            case ZoomLevel.Ground: return planetLevel;
            case ZoomLevel.Close: return closeZoomMax;
            case ZoomLevel.Medium: return mediumZoomMax;
            case ZoomLevel.Far: return farZoomMax;
            case ZoomLevel.VeryFar: return veryFarZoomMax;
            default: return 0f;
        }
    }

    /// <summary>
    /// Gets the current zoom level based on distance to camera
    /// </summary>
    public static ZoomLevel GetCurrentZoomLevel(float distance)
    {
        if (distance <= planetLevel)
            return ZoomLevel.Ground;
        else if (distance <= closeZoomMax)
            return ZoomLevel.Close;
        else if (distance <= mediumZoomMax)
            return ZoomLevel.Medium;
        else if (distance <= farZoomMax)
            return ZoomLevel.Far;
        else if (distance <= veryFarZoomMax)
            return ZoomLevel.VeryFar;
        else
            return ZoomLevel.None;
    }
}
