using System.Collections.Generic;
using UnityEngine;

public class KoppenTerrainMapper : MonoBehaviour
{
    public Texture2D koppenTexture;
    private Dictionary<string, TerrainType> codeToTerrain;
    private Dictionary<Color32, string> colorToCode;
    private const float colorTolerance = 25f;

    public enum TerrainType
    {
        Unknown,
        BosqueTropical,
        Sabana,
        Desierto,
        Estepa,
        BosqueTemplado,
        Llanura,
        BosqueBoreal,
        Tundra,
        Hielo,

        Ocean
    }

    public static KoppenTerrainMapper Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<KoppenTerrainMapper>();
                if (_instance == null)
                {
                    var go = new GameObject("KoppenTerrainMapper (AutoCreated)");
                    _instance = go.AddComponent<KoppenTerrainMapper>();
                }
            }
            return _instance;
        }
    }
    private static KoppenTerrainMapper _instance;

    void Awake()
    {
        if (_instance == null)
            _instance = this;
        else if (_instance != this)
            Destroy(gameObject); // Prevent duplicates

        if (koppenTexture == null)
            koppenTexture = Resources.Load<Texture2D>("Maps/Koppen");

        InitCodeMappings();
        InitColorMappings();
    }

    void InitCodeMappings()
    {
        codeToTerrain = new Dictionary<string, TerrainType>
        {
            // Tropical
            ["Af"] = TerrainType.BosqueTropical,
            ["Am"] = TerrainType.BosqueTropical,
            ["Aw"] = TerrainType.Sabana,
            ["As"] = TerrainType.Sabana,

            // Árido
            ["BWh"] = TerrainType.Desierto,
            ["BWk"] = TerrainType.Desierto,
            ["BSh"] = TerrainType.Estepa,
            ["BSk"] = TerrainType.Estepa,

            // Templado (Mediterráneo + Húmedo + Subhúmedo)
            ["Csa"] = TerrainType.BosqueTemplado,
            ["Csb"] = TerrainType.BosqueTemplado,
            ["Csc"] = TerrainType.BosqueTemplado,
            ["Cwa"] = TerrainType.BosqueTemplado,
            ["Cwb"] = TerrainType.Llanura,
            ["Cwc"] = TerrainType.BosqueTemplado,
            ["Cfa"] = TerrainType.Llanura,
            ["Cfb"] = TerrainType.Llanura,
            ["Cfc"] = TerrainType.Llanura,

            // Continental
            ["Dsa"] = TerrainType.BosqueBoreal,
            ["Dsb"] = TerrainType.BosqueBoreal,
            ["Dsc"] = TerrainType.BosqueBoreal,
            ["Dsd"] = TerrainType.BosqueBoreal,
            ["Dwa"] = TerrainType.BosqueBoreal,
            ["Dwb"] = TerrainType.BosqueBoreal,
            ["Dwc"] = TerrainType.BosqueBoreal,
            ["Dwd"] = TerrainType.BosqueBoreal,
            ["Dfa"] = TerrainType.BosqueBoreal,
            ["Dfb"] = TerrainType.BosqueBoreal,
            ["Dfc"] = TerrainType.BosqueBoreal,
            ["Dfd"] = TerrainType.BosqueBoreal,

            // Polar
            ["ET"] = TerrainType.Tundra,
            ["EF"] = TerrainType.Hielo,

            ["Ocean"] = TerrainType.Ocean
        };
    }

    void InitColorMappings()
    {
        // Colores reales del mapa Köppen-Geiger sin bordes (Wikipedia)
        colorToCode = new Dictionary<Color32, string>
        {
            // Tropical
            [new Color32(0, 0, 255, 255)] = "Af",
            [new Color32(0, 119, 255, 255)] = "Am",
            [new Color32(70, 169, 250, 255)] = "Aw",

            // Árido
            [new Color32(255, 0, 0, 255)] = "BWh",
            [new Color32(255, 150, 149, 255)] = "BWk",
            [new Color32(245, 163, 1, 255)] = "BSh",
            [new Color32(255, 219, 99, 255)] = "BSk",

            // Templado (Mediterráneo)
            [new Color32(255, 255, 0, 255)] = "Csa",
            [new Color32(198, 199, 0, 255)] = "Csb",
            [new Color32(150, 150, 0, 255)] = "Csc",

            // Templado (Subhúmedo)
            [new Color32(150, 255, 150, 255)] = "Cwa",
            [new Color32(99, 199, 100, 255)] = "Cwb",
            [new Color32(50, 150, 51, 255)] = "Cwc",

            // Templado (húmedo/oceánico)
            [new Color32(198, 255, 78, 255)] = "Cfa",
            [new Color32(100, 255, 80, 255)] = "Cfb",
            [new Color32(51, 199, 1, 255)] = "Cfc",

            // Continental (verano seco)
            [new Color32(255, 0, 255, 255)] = "Dsa",
            [new Color32(198, 0, 199, 255)] = "Dsb",
            [new Color32(150, 50, 150, 255)] = "Dsc",
            [new Color32(150, 100, 149, 255)] = "Dsd",

            // Continental (monzónico)
            [new Color32(171, 177, 255, 255)] = "Dwa",
            [new Color32(90, 119, 219, 255)] = "Dwb",
            [new Color32(76, 81, 181, 255)] = "Dwc",
            [new Color32(50, 0, 135, 255)] = "Dwd",

            // Continental (húmedo)
            [new Color32(0, 255, 255, 255)] = "Dfa",
            [new Color32(56, 199, 255, 255)] = "Dfb",
            [new Color32(0, 126, 125, 255)] = "Dfc",
            [new Color32(0, 69, 94, 255)] = "Dfd",

            // Polar
            [new Color32(178, 178, 178, 255)] = "ET",
            [new Color32(104, 104, 104, 255)] = "EF",

            [new Color32(0,0,0,0)] = "Ocean"
        };
    }

    public TerrainType GetTerrainFromLatLon(float lat, float lon, bool log = false)
    {
        if (koppenTexture == null)
        {
            if (log) Debug.LogWarning("Falta la textura Köppen");
            return TerrainType.Unknown;
        }

        float u = (lon + 180f) / 360f;
        float v = (lat + 90f) / 180f;

        int texX = Mathf.Clamp(Mathf.RoundToInt(u * (koppenTexture.width - 1)), 0, koppenTexture.width - 1);
        int texY = Mathf.Clamp(Mathf.RoundToInt(v * (koppenTexture.height - 1)), 0, koppenTexture.height - 1);

        // Sample a 10x10 square around (texX, texY)
        int halfWindow = 2;
        var colorCounts = new Dictionary<Color32, int>();
        for (int dx = -halfWindow; dx <= halfWindow; dx++)
        {
            for (int dy = -halfWindow; dy <= halfWindow; dy++)
            {
                int x = Mathf.Clamp(texX + dx, 0, koppenTexture.width - 1);
                int y = Mathf.Clamp(texY + dy, 0, koppenTexture.height - 1);
                Color32 c = koppenTexture.GetPixel(x, y);
                if (c.a < 128) continue; // Ignore transparent
                if (!colorCounts.ContainsKey(c)) colorCounts[c] = 0;
                colorCounts[c]++;
            }
        }
        Color32 modeColor = new Color32(0,0,0,0);
        int maxCount = 0;
        foreach (var kvp in colorCounts)
        {
            if (kvp.Value > maxCount)
            {
                maxCount = kvp.Value;
                modeColor = kvp.Key;
            }
        }
        if (log) Debug.Log($"[Koppen] lat: {lat}, lon: {lon}, u: {u}, v: {v}, modeColor: {modeColor}, count: {maxCount}");


        // Snap to nearest color in colorToCode
        float minDist = float.MaxValue;
        string nearestCode = null;
        foreach (var kvp in colorToCode)
        {
            float dist = Mathf.Pow(modeColor.r - kvp.Key.r, 2) +
                         Mathf.Pow(modeColor.g - kvp.Key.g, 2) +
                         Mathf.Pow(modeColor.b - kvp.Key.b, 2);
            if (dist < minDist)
            {
                minDist = dist;
                nearestCode = kvp.Value;
            }
        }
        if (nearestCode != null && codeToTerrain.TryGetValue(nearestCode, out var nearestTerrain))
        {
            if (log) Debug.Log($"[Koppen] Snapped to nearest code {nearestCode} (dist {minDist}) => {nearestTerrain}");
            return nearestTerrain;
        }

        if (log) Debug.Log("[Koppen] No match found, returning Unknown");
        return TerrainType.Unknown;
    }

    private bool IsSimilarColor(Color a, Color32 b, float tolerance)
    {
        return Mathf.Abs(a.r * 255 - b.r) < tolerance &&
               Mathf.Abs(a.g * 255 - b.g) < tolerance &&
               Mathf.Abs(a.b * 255 - b.b) < tolerance;
    }
}
