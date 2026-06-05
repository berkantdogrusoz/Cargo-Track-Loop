Shader "Color Cargo Loop/Toon Plastic"
{
    Properties
    {
        [MainColor] _Color ("Color", Color) = (1,1,1,1)
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _MainTex ("Main Texture", 2D) = "white" {}
        _BaseMap ("Base Map", 2D) = "white" {}
        _ShadowColor ("Shadow Color", Color) = (0.42,0.36,0.70,1)
        _ShadeStrength ("Shade Strength", Range(0, 1)) = 0.42
        _RampThreshold ("Ramp Threshold", Range(0, 1)) = 0.48
        _HighlightColor ("Highlight Color", Color) = (1,1,1,1)
        _HighlightStrength ("Highlight Strength", Range(0, 1)) = 0.34
        _RimColor ("Rim Color", Color) = (0.72,0.86,1,1)
        _RimStrength ("Rim Strength", Range(0, 1)) = 0.18
        _OutlineColor ("Outline Color", Color) = (0.045,0.035,0.10,1)
        _OutlineWidth ("Outline Width", Range(0, 0.08)) = 0.012
        _EmissionColor ("Emission Color", Color) = (0,0,0,1)
        _EmissionStrength ("Emission Strength", Range(0, 4)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ToonOutline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex OutlineVertex
            #pragma fragment OutlineFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _BaseColor;
                float4 _MainTex_ST;
                float4 _ShadowColor;
                float _ShadeStrength;
                float _RampThreshold;
                float4 _HighlightColor;
                float _HighlightStrength;
                float4 _RimColor;
                float _RimStrength;
                float4 _OutlineColor;
                float _OutlineWidth;
                float4 _EmissionColor;
                float _EmissionStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings OutlineVertex(Attributes input)
            {
                Varyings output;
                float3 inflated = input.positionOS.xyz + normalize(input.normalOS) * _OutlineWidth;
                output.positionHCS = TransformObjectToHClip(inflated);
                return output;
            }

            half4 OutlineFragment(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ToonForward"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex ToonVertex
            #pragma fragment ToonFragment
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _BaseColor;
                float4 _MainTex_ST;
                float4 _ShadowColor;
                float _ShadeStrength;
                float _RampThreshold;
                float4 _HighlightColor;
                float _HighlightStrength;
                float4 _RimColor;
                float _RimStrength;
                float4 _OutlineColor;
                float _OutlineWidth;
                float4 _EmissionColor;
                float _EmissionStrength;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
            };

            Varyings ToonVertex(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceViewDir(output.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 ToonFragment(Varyings input) : SV_Target
            {
                half3 normalWS = normalize(input.normalWS);
                half3 viewDirWS = normalize(input.viewDirWS);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));

                half ndl = saturate(dot(normalWS, mainLight.direction));
                half band = step(_RampThreshold, ndl * mainLight.shadowAttenuation);
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half3 baseColor = _Color.rgb * _BaseColor.rgb * tex.rgb;
                half3 shadowed = lerp(baseColor, baseColor * _ShadowColor.rgb, _ShadeStrength);
                half3 color = lerp(shadowed, baseColor, band);

                half spec = pow(saturate(dot(reflect(-mainLight.direction, normalWS), viewDirWS)), 18.0);
                color += _HighlightColor.rgb * step(0.55, spec) * _HighlightStrength;

                half rim = pow(1.0 - saturate(dot(normalWS, viewDirWS)), 2.2);
                color += _RimColor.rgb * rim * _RimStrength;
                color += _EmissionColor.rgb * _EmissionStrength;

                return half4(saturate(color), _Color.a * _BaseColor.a * tex.a);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
