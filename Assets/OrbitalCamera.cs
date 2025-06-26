using UnityEngine;
using UnityEngine.InputSystem;

public class OrbitalCamera : MonoBehaviour
{
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
        mapEditor = FindObjectOfType<MapEditor>();
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
}
