using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Camera))]
public class IDCameraSetup : MonoBehaviour
{
    public static RenderTexture sharedRenderTexture;
    public Material idMaterial;
    public GameObject sphereObject;
    public LayerMask idLayer;
    public Camera mainCameraToFollow;

    #if UNITY_EDITOR
    private static bool showDebugWindow = false;
    private Vector2 scrollPosition;
    private float debugWindowScale = 0.5f;

    [MenuItem("Debug/ID Camera/Toggle Debug Window")]
    private static void ToggleDebugWindow()
    {
        showDebugWindow = !showDebugWindow;
        SceneView.RepaintAll();
    }

    void OnGUI()
    {
        if (!showDebugWindow) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 400));
        GUILayout.BeginVertical(EditorStyles.helpBox);
        
        GUILayout.Label("ID Camera Debug", EditorStyles.boldLabel);
        
        if (sharedRenderTexture != null)
        {
            GUILayout.Label($"Texture Size: {sharedRenderTexture.width}x{sharedRenderTexture.height}");
            GUILayout.Label($"Format: {sharedRenderTexture.format}");
            
            // Display the render texture
            Rect textureRect = GUILayoutUtility.GetRect(256, 256);
            GUI.DrawTexture(textureRect, sharedRenderTexture, ScaleMode.ScaleToFit);
            
            // Add scale slider
            debugWindowScale = GUILayout.HorizontalSlider(debugWindowScale, 0.1f, 1f);
            GUILayout.Label($"Scale: {debugWindowScale:F2}");
        }
        else
        {
            GUILayout.Label("No render texture available");
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
    #endif

    void Start()
    {
        var cam = GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(1,0,1,1);
        cam.allowHDR = false;
        cam.allowMSAA = false;
        
        // Ensure render texture is properly set up
        if (sharedRenderTexture == null)
        {
            sharedRenderTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            sharedRenderTexture.antiAliasing = 1;
            sharedRenderTexture.useMipMap = false;
            sharedRenderTexture.Create();
            sharedRenderTexture.filterMode = FilterMode.Point;
        }
        cam.targetTexture = sharedRenderTexture;
        // Set up camera to only render the ID layer
        cam.cullingMask = idLayer;

        if (mainCameraToFollow != null)
        {
            CopyCameraSettings(cam, mainCameraToFollow);
        }

        if (idMaterial == null)
        {
            Shader idShader = Shader.Find("Custom/TriangleID");
            if (idShader != null)
            {
                idMaterial = new Material(idShader);
            }
            else
            {
                Debug.LogError("Shader 'Unlit/TriangleID' not found. Please create it manually.");
                return;
            }
        }
    }

    void LateUpdate()
    {
        if (mainCameraToFollow != null)
        {
            // Ensure the ID camera exactly matches the main camera's transform
            transform.position = mainCameraToFollow.transform.position;
            transform.rotation = mainCameraToFollow.transform.rotation;
            
            // Force the camera to render
            GetComponent<Camera>().Render();
        }
    }

    void CopyCameraSettings(Camera target, Camera source)
    {
        target.fieldOfView = source.fieldOfView;
        target.nearClipPlane = source.nearClipPlane;
        target.farClipPlane = source.farClipPlane;
        target.orthographic = source.orthographic;
        target.orthographicSize = source.orthographicSize;
        target.aspect = source.aspect;
        target.rect = source.rect;
    }

    void OnDestroy()
    {
        if (sharedRenderTexture != null)
        {
            sharedRenderTexture.Release();
            sharedRenderTexture = null;
        }
    }
}
