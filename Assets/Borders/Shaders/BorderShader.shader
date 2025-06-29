Shader "Custom/CountryBorder"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Main Color", Color) = (1,1,1,1)
        _ColorA ("Color A", Color) = (1,0,0,1)
        _ColorB ("Color B", Color) = (0,0,1,1)
        _BorderWidth ("Border Width", Float) = 0.05
        _BorderIntensity ("Border Intensity", Float) = 0.6
        _BorderFade ("Border Fade", Float) = 1.0
        _BorderOffset ("Border Offset", Float) = 0.0
        _BorderPulse ("Border Pulse", Float) = 0.0
        _BorderPulseSpeed ("Border Pulse Speed", Float) = 2.0
        _IsWarBorder ("Is War Border", Float) = 0.0
        _IsPeaceTreaty ("Is Peace Treaty", Float) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _ColorA;
            float4 _ColorB;
            float _BorderWidth;
            float _BorderIntensity;
            float _BorderFade;
            float _BorderOffset;
            float _BorderPulse;
            float _BorderPulseSpeed;
            float _IsWarBorder;
            float _IsPeaceTreaty;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // Use vertex color to determine which country color to use
                float countryIndex = i.color.r; // Use red channel as country index
                
                // Blend between ColorA and ColorB based on vertex color
                fixed4 borderColor = lerp(_ColorA, _ColorB, countryIndex);
                
                // Apply main color tint
                borderColor *= _Color;
                
                // Apply border intensity
                borderColor.a *= _BorderIntensity;
                
                // Apply fade effect
                if (_BorderFade > 0)
                {
                    float fadeFactor = 1.0 - (_BorderFade * (1.0 - i.color.a));
                    borderColor.a *= fadeFactor;
                }
                
                // Apply pulse effect
                if (_BorderPulse > 0)
                {
                    float pulse = sin(_Time.y * _BorderPulseSpeed) * 0.5 + 0.5;
                    borderColor.a *= (0.5 + pulse * 0.5);
                }
                
                // Apply war border effect (red tint)
                if (_IsWarBorder > 0)
                {
                    borderColor.rgb = lerp(borderColor.rgb, float3(1, 0, 0), 0.3);
                    borderColor.a *= 1.2; // Make war borders more visible
                }
                
                // Apply peace treaty effect (green tint)
                if (_IsPeaceTreaty > 0)
                {
                    borderColor.rgb = lerp(borderColor.rgb, float3(0, 1, 0), 0.3);
                    borderColor.a *= 0.8; // Make peace borders less aggressive
                }
                
                return borderColor;
            }
            ENDCG
        }
    }
} 