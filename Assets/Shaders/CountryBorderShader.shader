Shader "Custom/CountryBorder"
{
    Properties
    {
        _BorderTex ("Border Texture", 2D) = "white" {}
        _CountryColorTex ("Country Color Texture", 2D) = "black" {} // Now stores actual colors
        _BorderWidth ("Border Width", Range(0.001, 0.1)) = 0.01
        _BorderIntensity ("Border Intensity", Range(0, 1)) = 1.0
        _BorderGlow ("Border Glow", Range(0, 1)) = 0.1
        _BorderPulse ("Border Pulse", Range(0, 1)) = 0.0
        _BorderPulseSpeed ("Border Pulse Speed", Range(0, 10)) = 2.0
        _BorderBlend ("Border Blend", Range(0, 1)) = 0.5
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+100" "RenderPipeline"="UniversalPipeline" }
        Offset -1, -1 // Pull closer to the camera to prevent Z-fighting
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back
        
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BorderTex); SAMPLER(sampler_BorderTex);
            TEXTURE2D(_CountryColorTex); SAMPLER(sampler_CountryColorTex);
            
            float4 _BorderTex_ST;
            float4 _CountryColorTex_ST;

            CBUFFER_START(UnityPerMaterial)
                float _BorderWidth;
                float _BorderIntensity;
                float _BorderGlow;
                float _BorderPulse;
                float _BorderPulseSpeed;
                float _BorderBlend;
            CBUFFER_END
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float4 color : COLOR;
            };
            
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }
            
            half4 frag(Varyings IN) : SV_Target
            {
                // Sample the border texture (mask)
                float2 borderUV = IN.uv * _BorderTex_ST.xy + _BorderTex_ST.zw;
                float borderMask = SAMPLE_TEXTURE2D(_BorderTex, sampler_BorderTex, borderUV).r;

                // Early exit if not a border
                if (borderMask < 0.01)
                {
                    discard;
                }
                
                // Sample country color texture - much simpler!
                float2 colorUV = IN.uv * _CountryColorTex_ST.xy + _CountryColorTex_ST.zw;
                float4 countryColor = SAMPLE_TEXTURE2D(_CountryColorTex, sampler_CountryColorTex, colorUV);
                
                // Use the color directly - no lookup needed!
                float3 borderColor = countryColor.rgb;
                
                // Add pulsing effect
                float pulse = 1.0;
                if (_BorderPulse > 0)
                {
                    pulse = 1.0 + _BorderPulse * sin(_Time.y * _BorderPulseSpeed) * 0.3;
                }
                
                // Add glow effect
                if (_BorderGlow > 0)
                {
                    borderColor += _BorderGlow * borderColor * pulse;
                }
                
                // Apply border intensity and use the alpha from the color texture for fade effect
                float borderAlpha = borderMask * countryColor.a * _BorderIntensity * pulse;
                
                return float4(borderColor, borderAlpha);
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
} 