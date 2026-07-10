// Toon selale (BUILT-IN pipeline - Shader Graph URP ister, bizde calismaz; bu el yazmasi CG).
// Referans stil: turkuaz su + kayan koyu benekler + ust/alt beyaz kopuk. A21s dostu: tek pass, 2 texture okuma.
// Animasyon _Time ile shader icinde akar -> script Update gerekmez.
Shader "Color Cargo Loop/Toon Waterfall"
{
    Properties
    {
        _ColorA ("Su Acik", Color) = (0.42, 0.90, 0.96, 1)
        _ColorB ("Su Koyu (benek)", Color) = (0.10, 0.58, 0.78, 1)
        _FoamColor ("Kopuk", Color) = (1, 1, 1, 1)
        _NoiseTex ("Noise", 2D) = "gray" {}
        _FlowSpeed ("Akis Hizi", Float) = 0.35
        _PatchScale ("Benek Olcek", Float) = 3.0
        _PatchCut ("Benek Esik", Range(0,1)) = 0.52
        _FoamBottom ("Alt Kopuk", Range(0,0.6)) = 0.20
        _TopFoam ("Ust Kopuk", Range(0,0.3)) = 0.06
        _WobbleAmp ("Salinim", Float) = 0.03
    }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _NoiseTex;
            fixed4 _ColorA, _ColorB, _FoamColor;
            float _FlowSpeed, _PatchScale, _PatchCut, _FoamBottom, _TopFoam, _WobbleAmp;

            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata_base v)
            {
                v2f o;
                // organik salinim: alt taraf daha genis sallanir (quad uv.y=0 alt kenar)
                float sway = sin(v.texcoord.y * 12.5664 + _Time.y * 1.6) * _WobbleAmp * (1.0 - v.texcoord.y * 0.6);
                v.vertex.x += sway;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord.xy;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = _Time.y * _FlowSpeed;
                // iki katman kayan noise (farkli hiz/olcek) -> derinlikli akis hissi
                float2 uv1 = float2(i.uv.x * _PatchScale, (i.uv.y + t) * _PatchScale * 0.85);
                float2 uv2 = float2(i.uv.x * _PatchScale * 1.7 + 0.37, (i.uv.y + t * 1.55) * _PatchScale * 1.35);
                float n1 = tex2D(_NoiseTex, uv1).r;
                float n2 = tex2D(_NoiseTex, uv2).r;
                float n = n1 * 0.65 + n2 * 0.35;

                // toon benekler: esikli koyu su lekeleri (referanstaki desen)
                float patch = smoothstep(_PatchCut, _PatchCut + 0.13, n);
                fixed3 col = lerp(_ColorB.rgb, _ColorA.rgb, patch);

                // parlak isik oynamalari (ince acik cizgiler)
                col = lerp(col, _FoamColor.rgb, smoothstep(0.82, 0.93, n2) * 0.35);

                // ALT KOPUK: noise ile kenari yenmis kabarik beyaz bolge (dusme noktasi)
                float edgeB = _FoamBottom * (0.7 + n * 0.6);
                float foamB = 1.0 - smoothstep(edgeB * 0.5, edgeB, i.uv.y);
                // UST KOPUK: dokulme dudagi (ince beyaz serit)
                float foamT = smoothstep(1.0 - _TopFoam * (0.6 + n * 0.8), 1.0, i.uv.y);
                col = lerp(col, _FoamColor.rgb, saturate(foamB + foamT));

                return fixed4(col, 1);
            }
            ENDCG
        }
    }
    FallBack Off
}
