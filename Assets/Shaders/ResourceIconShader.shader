Shader "Custom/ResourceIcon"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Brightness ("Brightness", Range(0.5, 2.0)) = 1.0
        _PulseSpeed ("Pulse Speed", Range(0.1, 5.0)) = 1.0
        _PulseAmount ("Pulse Amount", Range(0.0, 0.3)) = 0.1
        _GlowIntensity ("Glow Intensity", Range(0.0, 1.0)) = 0.3
        _GlowColor ("Glow Color", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0.0, 0.1)) = 0.02
        _Opacity ("Opacity", Range(0.0, 1.0)) = 1.0
        _Saturation ("Saturation", Range(0.0, 1.0)) = 1.0
    }

    SubShader
    {
        Tags
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
            };
            
            fixed4 _Color;
            float _Brightness;
            float _PulseSpeed;
            float _PulseAmount;
            float _GlowIntensity;
            fixed4 _GlowColor;
            fixed4 _OutlineColor;
            float _OutlineWidth;
            float _Opacity;
            float _Saturation;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            sampler2D _MainTex;

            fixed4 frag(v2f IN) : SV_Target
            {
                // Sample texture with multiple offsets for smooth outline
                float2 offsets[8];
                offsets[0] = float2(_OutlineWidth, 0);
                offsets[1] = float2(-_OutlineWidth, 0);
                offsets[2] = float2(0, _OutlineWidth);
                offsets[3] = float2(0, -_OutlineWidth);
                offsets[4] = float2(_OutlineWidth * 0.707, _OutlineWidth * 0.707);
                offsets[5] = float2(-_OutlineWidth * 0.707, _OutlineWidth * 0.707);
                offsets[6] = float2(_OutlineWidth * 0.707, -_OutlineWidth * 0.707);
                offsets[7] = float2(-_OutlineWidth * 0.707, -_OutlineWidth * 0.707);
                
                float outlineAlpha = 0;
                for (int i = 0; i < 8; i++)
                {
                    outlineAlpha += tex2D(_MainTex, IN.texcoord + offsets[i]).a;
                }
                outlineAlpha /= 8.0;
                
                // Main texture
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;
                
                // Apply smooth outline - only where there's no main texture
                float outlineMask = outlineAlpha * (1.0 - c.a);
                c.rgb = lerp(_OutlineColor.rgb, c.rgb, c.a);
                c.a = max(c.a, outlineMask);
                
                // Apply brightness
                c.rgb *= _Brightness;
                
                // Apply saturation
                float luminance = dot(c.rgb, float3(0.299, 0.587, 0.114));
                c.rgb = lerp(float3(luminance, luminance, luminance), c.rgb, _Saturation);
                
                // Apply pulse effect
                float pulse = 1.0 + _PulseAmount * sin(_Time.y * _PulseSpeed);
                c.rgb *= pulse;
                
                // Apply glow effect
                float glowMask = c.a;
                float3 glow = _GlowColor.rgb * _GlowIntensity * glowMask;
                c.rgb += glow;
                
                // Apply opacity
                c.a *= _Opacity;
                
                return c;
            }
        ENDCG
        }
    }
} 