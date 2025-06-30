using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEditor;

public class IDPicker : MonoBehaviour
{
    public Camera idCamera;
    public IcoSphere icoSphere;  // Reference to the IcoSphere component
    public Text infoText;  // UI Text component to display info

    #if UNITY_EDITOR
    private bool showDebugInfo = false;
    private Vector2 lastMousePos;
    private Vector2 lastViewportPoint;
    private Vector2 lastTexturePoint;
    #endif

    void Start()
    {
        // Find IcoSphere if not assigned
        if (icoSphere == null)
        {
            icoSphere = UnityEngine.Object.FindFirstObjectByType<IcoSphere>();
        }

        // Hide text initially
        if (infoText != null)
        {
            infoText.gameObject.SetActive(false);
        }

        var koppenMapper = KoppenTerrainMapper.Instance;
    }

    void Update()
    {
        if (Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        UpdateIDDisplay(mousePos, Camera.main);
    }

    void UpdateIDDisplay(Vector2 mousePos, Camera camera)
    {
        if (camera == null || idCamera == null || IDCameraSetup.sharedRenderTexture == null) return;

        #if UNITY_EDITOR
        lastMousePos = mousePos;
        #endif

        // Convert screen position to viewport point (0-1 range)
        Vector3 viewportPoint = camera.ScreenToViewportPoint(mousePos);
        
        // Convert viewport point to ID camera's screen space
        Vector3 idCameraScreenPoint = idCamera.ViewportToScreenPoint(viewportPoint);
        
        #if UNITY_EDITOR
        lastViewportPoint = idCameraScreenPoint;
        #endif

        // Convert to texture coordinates
        int x = Mathf.FloorToInt(idCameraScreenPoint.x);
        int y = Mathf.FloorToInt(idCameraScreenPoint.y);

        #if UNITY_EDITOR
        lastTexturePoint = new Vector2(x, y);
        #endif

        // Ensure coordinates are within bounds
        x = Mathf.Clamp(x, 0, IDCameraSetup.sharedRenderTexture.width - 1);
        y = Mathf.Clamp(y, 0, IDCameraSetup.sharedRenderTexture.height - 1);

        // Get the color from the render texture
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = IDCameraSetup.sharedRenderTexture;

        Texture2D tex = new Texture2D(1, 1, TextureFormat.ARGB32, false);
        tex.filterMode = FilterMode.Point;
        tex.ReadPixels(new Rect(x, y, 1, 1), 0, 0);
        tex.Apply();

        Color color = tex.GetPixel(0, 0);
        int id = DecodeIDFromColor(color);

        // Debug RGB values with raw values
        int rawR = Mathf.RoundToInt(color.r * 255f);
        int rawG = Mathf.RoundToInt(color.g * 255f);
        int rawB = Mathf.RoundToInt(color.b * 255f);

        // Update UI text with triangle data
        if (infoText != null)
        {
            if (icoSphere != null && id >= 0 && id < icoSphere.triangleDataList.Count)
            {
                var triangleData = icoSphere.triangleDataList[id];
                // Recalculate terrain using KoppenTerrainMapper
                string terrainInfo = "";
                var koppenMapper = KoppenTerrainMapper.Instance;
                if (koppenMapper != null)
                {
                    var center = triangleData.GetCenter().normalized;
                    var (lat, lon) = TriangleData.Vector3ToLatLon(center);
                    var terrain = koppenMapper.GetTerrainFromLatLon(lat, lon, false);
                    terrainInfo = $"\nKoppen: {terrain} (lat: {lat}, lon: {lon})";
                    //Debug.Log($"[IDPicker] Triangle {id} center lat/lon: {lat}, {lon} => Terrain: {terrain}");
                }
                infoText.text = $"{terrainInfo}\n{triangleData}";
                infoText.color = Color.white;
                infoText.gameObject.SetActive(true);
                // Position text near mouse
                infoText.transform.position = mousePos + new Vector2(20, 20);
            }
            else
            {
                infoText.gameObject.SetActive(false);
            }
        }

        RenderTexture.active = currentRT;
        Destroy(tex);
    }

    int DecodeIDFromColor(Color color)
    {
        int r = Mathf.RoundToInt(color.r * 255f);
        int g = Mathf.RoundToInt(color.g * 255f);
        int b = Mathf.RoundToInt(color.b * 255f);

        int decoded = r + (g << 8) + (b << 16);
        return decoded;
    }

    // Get the currently selected triangle ID based on mouse position
    public int GetSelectedTriangleID()
    {
        if (Mouse.current == null || Camera.main == null || idCamera == null || IDCameraSetup.sharedRenderTexture == null)
            return -1;
            
        Vector2 mousePos = Mouse.current.position.ReadValue();
        
        // Convert screen position to viewport point (0-1 range)
        Vector3 viewportPoint = Camera.main.ScreenToViewportPoint(mousePos);
        
        // Convert viewport point to ID camera's screen space
        Vector3 idCameraScreenPoint = idCamera.ViewportToScreenPoint(viewportPoint);
        
        // Convert to texture coordinates
        int x = Mathf.FloorToInt(idCameraScreenPoint.x);
        int y = Mathf.FloorToInt(idCameraScreenPoint.y);
        
        // Ensure coordinates are within bounds
        x = Mathf.Clamp(x, 0, IDCameraSetup.sharedRenderTexture.width - 1);
        y = Mathf.Clamp(y, 0, IDCameraSetup.sharedRenderTexture.height - 1);
        
        // Get the color from the render texture
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = IDCameraSetup.sharedRenderTexture;
        
        Texture2D tex = new Texture2D(1, 1, TextureFormat.ARGB32, false);
        tex.filterMode = FilterMode.Point;
        tex.ReadPixels(new Rect(x, y, 1, 1), 0, 0);
        tex.Apply();
        
        Color color = tex.GetPixel(0, 0);
        int id = DecodeIDFromColor(color);
        
        RenderTexture.active = currentRT;
        Destroy(tex);
        
        return id;
    }

    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!icoSphere?.showGizmos ?? true) return;

        var sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null) return;

        Event e = Event.current;
        if (e.type == EventType.MouseMove)
        {
            UpdateIDDisplay(e.mousePosition, sceneView.camera);
        }

        // Toggle debug info with F3
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.F3)
        {
            showDebugInfo = !showDebugInfo;
            e.Use();
        }

        if (showDebugInfo)
        {
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(10, 10, 500, 300), EditorStyles.helpBox);
            GUILayout.Label("ID Picker Debug Info", EditorStyles.boldLabel);
            GUILayout.Label($"Mouse Pos: {lastMousePos}");
            GUILayout.Label($"Viewport Point: {lastViewportPoint}");
            GUILayout.Label($"Texture Point: {lastTexturePoint}");
            GUILayout.Label($"Texture Size: {IDCameraSetup.sharedRenderTexture?.width}x{IDCameraSetup.sharedRenderTexture?.height}");
            GUILayout.Label($"Camera Viewport: {idCamera?.rect}");
            GUILayout.EndArea();
            Handles.EndGUI();
        }
    }
    #endif
}
