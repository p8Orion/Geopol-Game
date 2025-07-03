using UnityEngine;
using System.Collections.Generic;

public class OceanWaveEffect : MonoBehaviour
{
    [Header("Wave Settings")]
    public float waveSpeed = 0.3f;
    public float waveAmplitude = 0.1f;
    public float waveFrequency = 10.0f;
    public float waveWidth = 0.2f; // Ancho de la zona de oleaje
    [Header("Wave Mask Settings")]
    public int waveMaskThickness = 5; // Grosor de la banda de oleaje en píxeles
    
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

    public void UpdateWaveMask(int[] terrainOwner, int resolution, List<int> oceanTerrainIDs)
    {
        // Check if we need to update
        if (!needsUpdate && resolution == lastMapResolution && oceanTerrainIDs.Count > 0 && oceanTerrainIDs[0] == lastOceanTerrainID)
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

        // Detect ocean-land borders and create distance-based wave mask
        bool[] isWaveBorder = new bool[resolution * resolution];
        
        // First pass: detect border pixels
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int pixelIndex = y * resolution + x;
                int currentTerrain = terrainOwner[pixelIndex];
                
                // If this pixel is ocean (any of the ocean terrain IDs), check if it has non-ocean neighbors
                if (oceanTerrainIDs.Contains(currentTerrain))
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
                            
                            if (!oceanTerrainIDs.Contains(terrainOwner[neighborIndex]))
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
        
        // Second pass: calculate distance-based wave mask for all ocean pixels
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int pixelIndex = y * resolution + x;
                
                // Only process ocean pixels (any of the ocean terrain IDs)
                if (oceanTerrainIDs.Contains(terrainOwner[pixelIndex]))
                {
                    // Find minimum distance to any wave border
                    float minDistance = float.MaxValue;
                    
                    // Search in a reasonable radius (waveMaskThickness * 2 for performance)
                    int searchRadius = waveMaskThickness * 2;
                    for (int dy = -searchRadius; dy <= searchRadius; dy++)
                    {
                        for (int dx = -searchRadius; dx <= searchRadius; dx++)
                        {
                            int searchX = (x + dx + resolution) % resolution;
                            int searchY = (y + dy + resolution) % resolution;
                            int searchIndex = searchY * resolution + searchX;
                            
                            // If we found a wave border, calculate distance
                            if (isWaveBorder[searchIndex])
                            {
                                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                                if (distance < minDistance)
                                {
                                    minDistance = distance;
                                }
                            }
                        }
                    }
                    
                    // If we found a border within range, set grayscale value
                    if (minDistance <= waveMaskThickness)
                    {
                        // Normalize distance: 0 = at border, 1 = at max distance
                        float normalizedDistance = minDistance / waveMaskThickness;
                        // Invert so closer to coast = brighter
                        float intensity = 1.0f - normalizedDistance;
                        pixels[pixelIndex] = new Color(intensity, intensity, intensity, 1.0f);
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
        lastOceanTerrainID = oceanTerrainIDs.Count > 0 ? oceanTerrainIDs[0] : -1;
        needsUpdate = false;
        
        Debug.Log($"OceanWaveEffect: Generated wave mask for {resolution}x{resolution} map with {oceanTerrainIDs.Count} ocean IDs: [{string.Join(", ", oceanTerrainIDs)}]");
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