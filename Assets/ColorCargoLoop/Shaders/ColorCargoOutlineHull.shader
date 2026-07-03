// Ters-kabuk (inverted hull) dis cizgi: mesh normal boyunca sisirilir, on yuzler kirpilir.
// Ana materyale DOKUNMAZ - ayri renderer olarak modelin ustune eklenir (BuildPandaOutlineHull).
Shader "Color Cargo Loop/Outline Hull"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0.055, 0.045, 0.09, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.2)) = 0.01
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry-1" }
        Cull Front
        ZWrite On

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _OutlineWidth;
            fixed4 _OutlineColor;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; };

            v2f vert(appdata v)
            {
                v2f o;
                float3 n = normalize(v.normal);
                float4 pos = v.vertex;
                pos.xyz += n * _OutlineWidth;   // obje-uzayinda sisir (model olcegiyle buyur)
                o.pos = UnityObjectToClipPos(pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target { return _OutlineColor; }
            ENDCG
        }
    }
    Fallback Off
}
