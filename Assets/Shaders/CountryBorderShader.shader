Shader "Custom/CountryBorder"
{
    Properties
    {
        _BorderTex ("Border Texture", 2D) = "white" {}
        _CountryIDTex ("Country ID Texture", 2D) = "black" {} // Black = no countries
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
            
            #define MAX_COUNTRIES 256

            // Use a StructuredBuffer for the color array for better compatibility and performance
            StructuredBuffer<float4> _CountryColors;

            TEXTURE2D(_BorderTex); SAMPLER(sampler_BorderTex);
            TEXTURE2D(_CountryIDTex); SAMPLER(sampler_CountryIDTex);
            
            float4 _BorderTex_ST;

            CBUFFER_START(UnityPerMaterial)
                float4 _CountryIDTex_ST;
                float _BorderWidth;
                float _BorderIntensity;
                float _BorderGlow;
                float _BorderPulse;
                float _BorderPulseSpeed;
                float _BorderBlend;
                int _CountryCount;
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
                
                // Sample country ID texture
                // Use sampler_PointClamp to ensure we get the exact pixel value without any bilinear filtering, which corrupts the ID data.
                float4 countryIDs = SAMPLE_TEXTURE2D(_CountryIDTex, sampler_PointClamp, borderUV);
                
                // The C# code now only provides our country's ID in the R channel.
                // IMPORTANT: We must round to handle floating point inaccuracies during texture sampling.
                int id1_lookup = (int)round(countryIDs.r * 255.0f);

                // Safety check: ensure _CountryCount is valid
                // _CountryCount includes the 'unclaimed' color at index 0.
                int maxCountryIndex = max(1, _CountryCount) - 1;
                
                // Clamp the final index to be within the valid range of the color array.
                int id1 = clamp(id1_lookup, 0, maxCountryIndex);

                // Look up our color from the array
                float4 color1 = _CountryColors[id1];

                // The final color is just our country's color. No blending is needed.
                float3 blendedColor = color1.rgb;
                
                // Add pulsing effect
                float pulse = 1.0;
                if (_BorderPulse > 0)
                {
                    pulse = 1.0 + _BorderPulse * sin(_Time.y * _BorderPulseSpeed) * 0.3;
                }
                
                // Add glow effect
                if (_BorderGlow > 0)
                {
                    blendedColor += _BorderGlow * blendedColor * pulse;
                }
                
                // Apply border intensity
                float borderAlpha = borderMask * _BorderIntensity * pulse;
                
                return float4(blendedColor, borderAlpha);
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
} 