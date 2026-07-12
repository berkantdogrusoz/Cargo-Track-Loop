// Built-in pipeline icin hafif su akisi. Arka planda mor su damarlari, dekor modunda kopuklu selale verir.
// Tek pass + iki texture okuma; animasyon shader _Time ile akar, script Update gerekmez.
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
        _CausticStrength ("Isik Damar Siddeti", Range(0,1)) = 0.28
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
            float _FlowSpeed, _PatchScale, _PatchCut, _FoamBottom, _TopFoam, _WobbleAmp, _CausticStrength;

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
                // Iki yone kayan noise. Kesisim cizgileri su yuzeyindeki parlak damar hissini verir.
                float2 uv1 = i.uv * _PatchScale + float2(t * 0.32, t);
                float2 uv2 = i.uv * (_PatchScale * 1.73) + float2(0.37 - t * 0.68, 0.19 + t * 0.46);
                float n1 = tex2D(_NoiseTex, uv1).r;
                float n2 = tex2D(_NoiseTex, uv2).r;
                float n = n1 * 0.62 + n2 * 0.38;

                float body = smoothstep(0.26, 0.76, n);
                fixed3 col = lerp(_ColorB.rgb, _ColorA.rgb, body);

                // Iki noise degeri birbirine yaklastiginda ince, organik isik damari olusur.
                float ridge = 1.0 - saturate(abs(n1 - n2) * 4.0);
                float caustic = smoothstep(_PatchCut, min(0.99, _PatchCut + 0.16), ridge);
                col = lerp(col, _FoamColor.rgb, caustic * _CausticStrength);

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
