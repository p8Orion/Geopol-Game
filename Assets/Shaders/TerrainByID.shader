Shader "Custom/TerrainByID"
{
    Properties
    {
        _TerrainControl ("Terrain Control", 2D) = "white" {}
        _TerrainTexArray ("Terrain Textures", 2DArray) = "white" {}
        _TerrainCount ("Terrain Count", Float) = 10
        _ControlTexWidth ("Control Tex Width", Float) = 2048
        _ControlTexHeight ("Control Tex Height", Float) = 2048
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _TerrainControl;
            UNITY_DECLARE_TEX2DARRAY(_TerrainTexArray);
            float _TerrainCount;
            float _ControlTexWidth;
            float _ControlTexHeight;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR; // We'll use this for the triangle ID
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Decode triangle ID from color (assuming 24-bit encoding)
                int id = int(i.color.r * 255.0 + i.color.g * 255.0 * 256.0 + i.color.b * 255.0 * 256.0 * 256.0 + 0.5);

                // Calculate 2D texture coordinates
                int x = id % int(_ControlTexWidth);
                int y = id / int(_ControlTexWidth);
                float2 uv = float2((x + 0.5) / _ControlTexWidth, (y + 0.5) / _ControlTexHeight);
                float terrainType = tex2D(_TerrainControl, uv).r * 255.0;

                // Clamp and use as index
                int idx = clamp(int(terrainType + 0.5), 0, int(_TerrainCount - 1));

                // Use barycentric or spherical mapping for UVs if you want detail, here we just use the interpolated mesh UV
                float2 texUV = i.uv;
                fixed4 col = UNITY_SAMPLE_TEX2DARRAY(_TerrainTexArray, float3(texUV, idx));
                return col;
            }
            ENDCG
        }
    }
}