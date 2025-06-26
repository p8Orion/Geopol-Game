Shader "Custom/TerrainSplatMap12"
{
    Properties
    {
        // Declare all properties, even if unused, to ensure they don't cause a compile error.
        _SplatMap1 ("Splat 1", 2D) = "white" {}
        _SplatMap2 ("Splat 2", 2D) = "white" {}
        _SplatMap3 ("Splat 3", 2D) = "white" {}
        _TerrainTex1 ("Tex 1", 2D) = "white" {}
        _TerrainTex2 ("Tex 2", 2D) = "white" {}
        _TerrainTex3 ("Tex 3", 2D) = "white" {}
        _TerrainTex4 ("Tex 4", 2D) = "white" {}
        _TerrainTex5 ("Tex 5", 2D) = "white" {}
        _TerrainTex6 ("Tex 6", 2D) = "white" {}
        _TerrainTex7 ("Tex 7", 2D) = "white" {}
        _TerrainTex8 ("Tex 8", 2D) = "white" {}
        _TerrainTex9 ("Tex 9", 2D) = "white" {}
        _TerrainTex10 ("Tex 10", 2D) = "white" {}
        _TerrainTex11 ("Tex 11", 2D) = "white" {}
        _TerrainTex12 ("Tex 12", 2D) = "white" {}
        _TilingScale ("Tiling Scale", Range(1.0, 50.0)) = 10.0
        _SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
        _Shininess ("Shininess", Range(0.01, 1)) = 0.078125
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }

        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_SplatMap1); SAMPLER(sampler_SplatMap1);
            TEXTURE2D(_SplatMap2); SAMPLER(sampler_SplatMap2);
            TEXTURE2D(_SplatMap3); SAMPLER(sampler_SplatMap3);

            TEXTURE2D(_TerrainTex1); SAMPLER(sampler_TerrainTex1);
            TEXTURE2D(_TerrainTex2); SAMPLER(sampler_TerrainTex2);
            TEXTURE2D(_TerrainTex3); SAMPLER(sampler_TerrainTex3);
            TEXTURE2D(_TerrainTex4); SAMPLER(sampler_TerrainTex4);
            TEXTURE2D(_TerrainTex5); SAMPLER(sampler_TerrainTex5);
            TEXTURE2D(_TerrainTex6); SAMPLER(sampler_TerrainTex6);
            TEXTURE2D(_TerrainTex7); SAMPLER(sampler_TerrainTex7);
            TEXTURE2D(_TerrainTex8); SAMPLER(sampler_TerrainTex8);
            TEXTURE2D(_TerrainTex9); SAMPLER(sampler_TerrainTex9);
            TEXTURE2D(_TerrainTex10); SAMPLER(sampler_TerrainTex10);
            TEXTURE2D(_TerrainTex11); SAMPLER(sampler_TerrainTex11);
            TEXTURE2D(_TerrainTex12); SAMPLER(sampler_TerrainTex12);


            CBUFFER_START(UnityPerMaterial)
                float4 _SplatMap1_ST;
                float4 _SplatMap2_ST;
                float4 _SplatMap3_ST;
                float4 _TerrainTex1_ST; // All terrain textures share same tiling
                float _TilingScale;
                half4 _SpecColor;
                half _Shininess;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float3 positionWS   : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 splat1 = SAMPLE_TEXTURE2D(_SplatMap1, sampler_SplatMap1, IN.uv);
                float4 splat2 = SAMPLE_TEXTURE2D(_SplatMap2, sampler_SplatMap2, IN.uv);
                float4 splat3 = SAMPLE_TEXTURE2D(_SplatMap3, sampler_SplatMap3, IN.uv);

                float splatWeights[12];
                splatWeights[0] = splat1.r; splatWeights[1] = splat1.g; splatWeights[2] = splat1.b; splatWeights[3] = splat1.a;
                splatWeights[4] = splat2.r; splatWeights[5] = splat2.g; splatWeights[6] = splat2.b; splatWeights[7] = splat2.a;
                splatWeights[8] = splat3.r; splatWeights[9] = splat3.g; splatWeights[10] = splat3.b; splatWeights[11] = splat3.a;

                float2 tiledUV = IN.uv * _TilingScale;
                float3 blendedColor = float3(0,0,0);
                float totalWeight = 0.0001; // Avoid division by zero

                blendedColor += SAMPLE_TEXTURE2D(_TerrainTex1, sampler_TerrainTex1, tiledUV).rgb * splatWeights[0];
                blendedColor += SAMPLE_TEXTURE2D(_TerrainTex2, sampler_TerrainTex2, tiledUV).rgb * splatWeights[1];
                blendedColor += SAMPLE_TEXTURE2D(_TerrainTex3, sampler_TerrainTex3, tiledUV).rgb * splatWeights[2];
                blendedColor += SAMPLE_TEXTURE2D(_TerrainTex4, sampler_TerrainTex4, tiledUV).rgb * splatWeights[3];
                blendedColor += SAMPLE_TEXTURE2D(_TerrainTex5, sampler_TerrainTex5, tiledUV).rgb * splatWeights[4];
                blendedColor += SAMPLE_TEXTURE2D(_TerrainTex6, sampler_TerrainTex6, tiledUV).rgb * splatWeights[5];
                blendedColor += SAMPLE_TEXTURE2D(_TerrainTex7, sampler_TerrainTex7, tiledUV).rgb * splatWeights[6];
                blendedColor += SAMPLE_TEXTURE2D(_TerrainTex8, sampler_TerrainTex8, tiledUV).rgb * splatWeights[7];
                blendedColor += SAMPLE_TEXTURE2D(_TerrainTex9, sampler_TerrainTex9, tiledUV).rgb * splatWeights[8];
                blendedColor += SAMPLE_TEXTURE2D(_TerrainTex10, sampler_TerrainTex10, tiledUV).rgb * splatWeights[9];
                blendedColor += SAMPLE_TEXTURE2D(_TerrainTex11, sampler_TerrainTex11, tiledUV).rgb * splatWeights[10];
                blendedColor += SAMPLE_TEXTURE2D(_TerrainTex12, sampler_TerrainTex12, tiledUV).rgb * splatWeights[11];
                
                for (int j = 0; j < 12; j++) { totalWeight += splatWeights[j]; }
                
                blendedColor /= totalWeight;
                
                Light mainLight = GetMainLight();
                half3 mainLightDirection = mainLight.direction;
                half mainLightAttenuation = mainLight.shadowAttenuation * mainLight.distanceAttenuation;
                half3 mainLightColor = mainLight.color;

                // Ambient lighting from skybox/environment
                half3 ambient = SampleSH(IN.normalWS);

                // Diffuse lighting
                half ndotl = saturate(dot(IN.normalWS, mainLightDirection));
                half3 diffuse = ndotl * mainLightColor * mainLightAttenuation;

                // Specular (Blinn-Phong)
                half3 viewDir = normalize(_WorldSpaceCameraPos - IN.positionWS);
                half3 halfVec = normalize(mainLightDirection + viewDir);
                half nDotH = saturate(dot(IN.normalWS, halfVec));
                half specPower = exp2(_Shininess * 10.0 + 1.0); // Map shininess to a more usable range
                half3 specular = pow(nDotH, specPower) * _SpecColor.rgb * mainLightAttenuation;
                
                // Combine lighting: (Ambient + Diffuse) * Albedo + Specular
                half3 finalColor = (ambient + diffuse) * blendedColor.rgb + specular;
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
} 