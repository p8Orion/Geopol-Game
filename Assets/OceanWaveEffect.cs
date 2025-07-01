using UnityEngine;

public class OceanWaveEffect : MonoBehaviour
{
    [Header("Wave Settings")]
    public float waveSpeed = 1.0f;
    public float waveAmplitude = 0.1f;
    public float waveFrequency = 10.0f;
    public float waveWidth = 0.05f; // Ancho de la zona de oleaje
    [Header("Wave Mask Settings")]
    public int waveMaskThickness = 15; // Grosor de la banda de oleaje en píxeles
    
    [Header("References")]
    public IcoSphere icoSphere;
    
    [Header("Debug Info")]
    [SerializeField] private Texture2D waveMaskTexture;
    [SerializeField] private bool needsUpdate = true;
    [SerializeField] private int lastMapResolution = 0;
    [SerializeField] private int lastOceanTerrainID = -1;

    void Start()
    {
        // Auto-assign IcoSphere if not set
        if (icoSphere == null)
        {
            icoSphere = FindFirstObjectByType<IcoSphere>();
        }
        
        // Subscribe to map changes
        if (icoSphere != null)
        {
            icoSphere.OnDataLoaded += OnMapChanged;
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (icoSphere != null)
        {
            icoSphere.OnDataLoaded -= OnMapChanged;
        }
    }

    void OnMapChanged()
    {
        needsUpdate = true;
    }

    public void UpdateWaveMask(int[] terrainOwner, int resolution, int oceanTerrainID)
    {
        // Check if we need to update
        if (!needsUpdate && resolution == lastMapResolution && oceanTerrainID == lastOceanTerrainID)
        {
            return;
        }

        // Create or resize wave mask texture
        if (waveMaskTexture == null || waveMaskTexture.width != resolution)
        {
            if (waveMaskTexture != null)
            {
                DestroyImmediate(waveMaskTexture);
            }
            waveMaskTexture = new Texture2D(resolution, resolution, TextureFormat.R8, false, true);
            waveMaskTexture.filterMode = FilterMode.Bilinear;
            waveMaskTexture.wrapMode = TextureWrapMode.Clamp;
        }

        // Initialize texture to black
        Color[] pixels = new Color[resolution * resolution];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.black;
        }

        // Detect ocean-land borders and create thick wave band
        bool[] isWaveBorder = new bool[resolution * resolution];
        
        // First pass: detect border pixels
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int pixelIndex = y * resolution + x;
                int currentTerrain = terrainOwner[pixelIndex];
                
                // If this pixel is ocean, check if it has non-ocean neighbors
                if (currentTerrain == oceanTerrainID)
                {
                    bool hasNonOceanNeighbor = false;
                    
                    // Check 8 immediate neighbors
                    for (int dy = -1; dy <= 1 && !hasNonOceanNeighbor; dy++)
                    {
                        for (int dx = -1; dx <= 1 && !hasNonOceanNeighbor; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            
                            // Handle wrapping for seamless textures
                            int neighborX = (x + dx + resolution) % resolution;
                            int neighborY = (y + dy + resolution) % resolution;
                            int neighborIndex = neighborY * resolution + neighborX;
                            
                            if (terrainOwner[neighborIndex] != oceanTerrainID)
                            {
                                hasNonOceanNeighbor = true;
                            }
                        }
                    }
                    
                    // If it has non-ocean neighbors, mark as wave border
                    if (hasNonOceanNeighbor)
                    {
                        isWaveBorder[pixelIndex] = true;
                    }
                }
            }
        }
        
        // Second pass: expand wave border to create thick band
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int pixelIndex = y * resolution + x;
                
                // If this is a wave border pixel, expand it
                if (isWaveBorder[pixelIndex])
                {
                    // Expand in all directions by waveMaskThickness
                    for (int dy = -waveMaskThickness; dy <= waveMaskThickness; dy++)
                    {
                        for (int dx = -waveMaskThickness; dx <= waveMaskThickness; dx++)
                        {
                            int expandX = (x + dx + resolution) % resolution;
                            int expandY = (y + dy + resolution) % resolution;
                            int expandIndex = expandY * resolution + expandX;
                            
                            // Only expand into ocean pixels
                            if (terrainOwner[expandIndex] == oceanTerrainID)
                            {
                                pixels[expandIndex] = Color.white;
                            }
                        }
                    }
                }
            }
        }

        // Apply pixels to texture
        waveMaskTexture.SetPixels(pixels);
        waveMaskTexture.Apply();

        // Set wave mask and parameters in the main terrain material
        if (icoSphere != null)
        {
            var mat = icoSphere.GetMainTerrainMaterial();
            if (mat != null)
            {
                mat.SetTexture("_WaveMask", waveMaskTexture);
                mat.SetFloat("_WaveSpeed", waveSpeed);
                mat.SetFloat("_WaveAmplitude", waveAmplitude);
                mat.SetFloat("_WaveFrequency", waveFrequency);
                mat.SetFloat("_WaveWidth", waveWidth);
            }
        }

        // Update cache
        lastMapResolution = resolution;
        lastOceanTerrainID = oceanTerrainID;
        needsUpdate = false;
        
        Debug.Log($"OceanWaveEffect: Generated wave mask for {resolution}x{resolution} map with ocean ID {oceanTerrainID}");
    }

    // Public method to force update
    public void ForceUpdate()
    {
        needsUpdate = true;
    }

    // Get the wave mask texture (for debugging)
    public Texture2D GetWaveMaskTexture()
    {
        return waveMaskTexture;
    }
    
    // Update shader parameters in real-time
    void Update()
    {
        // No shader parameters to update as the material is no longer used
    }

    public void ApplyWaveMaskToMaterial(Material mat)
    {
        if (mat != null && waveMaskTexture != null)
        {
            mat.SetTexture("_WaveMask", waveMaskTexture);
            mat.SetFloat("_WaveSpeed", waveSpeed);
            mat.SetFloat("_WaveAmplitude", waveAmplitude);
            mat.SetFloat("_WaveFrequency", waveFrequency);
            mat.SetFloat("_WaveWidth", waveWidth);
        }
    }
} 