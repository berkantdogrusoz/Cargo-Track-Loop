Shader "Color Cargo Loop/Toon Plastic"
{
    Properties
    {
        [MainColor] _Color ("Color", Color) = (1,1,1,1)
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _MainTex ("Main Texture", 2D) = "white" {}
        _BaseMap ("Base Map", 2D) = "white" {}
        _ShadowColor ("Shadow Color", Color) = (0.42,0.36,0.70,1)
        _ShadeStrength ("Shade Strength", Range(0, 1)) = 0.35
        _RampThreshold ("Ramp Threshold", Range(0, 1)) = 0.48
        _HighlightColor ("Highlight Color", Color) = (1,1,1,1)
        _HighlightStrength ("Highlight Strength", Range(0, 1)) = 0.2
        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimStrength ("Rim Strength", Range(0, 1)) = 0
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0, 0.08)) = 0
        _EmissionColor ("Emission Color", Color) = (0,0,0,1)
        _EmissionStrength ("Emission Strength", Range(0, 4)) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        fixed4 _BaseColor;
        fixed4 _EmissionColor;
        half _EmissionStrength;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
            fixed4 col = _Color * tex;
            o.Albedo = col.rgb;
            o.Alpha = col.a;
            o.Metallic = 0;
            o.Smoothness = 0.12;
            o.Emission = _EmissionColor.rgb * _EmissionStrength;
        }
        ENDCG
    }

    Fallback "Standard"
}
