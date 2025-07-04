using UnityEngine;
using UnityEngine.UI;

public class CanvasMundo : MonoBehaviour
{
    [Header("Canvas Settings")]
    public RenderMode renderMode = RenderMode.ScreenSpaceCamera;
    public float planeDistance = 1f;
    
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    
    void Awake()
    {
        CreateCanvasMundo();
    }
    
    void CreateCanvasMundo()
    {
        // Check if CanvasMundo already exists
        Canvas existingCanvas = GameObject.Find("CanvasMundo")?.GetComponent<Canvas>();
        if (existingCanvas != null)
        {
            Debug.Log("CanvasMundo: CanvasMundo already exists, using existing one.");
            canvas = existingCanvas;
            canvasGroup = canvas.GetComponent<CanvasGroup>();
            return;
        }
        
        // Create new CanvasMundo
        GameObject canvasGO = new GameObject("CanvasMundo");
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = renderMode;
        canvas.worldCamera = Camera.main;
        canvas.planeDistance = planeDistance;
        
        // Configure blending for transparency
        canvasGroup = canvasGO.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        
        RectTransform rect = canvasGO.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        
        Debug.Log("CanvasMundo: CanvasMundo created and configured as Screen Space - Camera with transparency.");
    }
    
    public Canvas GetCanvas()
    {
        return canvas;
    }
    
    public CanvasGroup GetCanvasGroup()
    {
        return canvasGroup;
    }
} 