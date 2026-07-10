// Buz kabugu (BUILT-IN): yari saydam, kenarlari parlayan (fresnel) + don deseni.
// Donmus pandayi saran kabukta ve kirilma parcalarinda kullanilir. Tek pass, ucuz.
Shader "Color Cargo Loop/Ice"
{
    Properties
    {
        _Color ("Buz Rengi", Color) = (0.62, 0.86, 0.98, 0.5)
        _RimColor ("Kenar Parlama", Color) = (1, 1, 1, 1)
        _RimPower ("Kenar Sertligi", Range(0.5, 6)) = 2.2
        _NoiseTex ("Don Deseni", 2D) = "gray" {}
        _FrostAmount ("Don Yogunlugu", Range(0, 1)) = 0.35
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color, _RimColor;
            float _RimPower, _FrostAmount;
            sampler2D _NoiseTex;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 wN : TEXCOORD1;
                float3 wV : TEXCOORD2;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord.xy;
                o.wN = UnityObjectToWorldNormal(v.normal);
                o.wV = normalize(_WorldSpaceCameraPos - mul(unity_ObjectToWorld, v.vertex).xyz);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float fres = pow(1.0 - saturate(dot(normalize(i.wN), normalize(i.wV))), _RimPower);
                float frost = tex2D(_NoiseTex, i.uv * 2.3).r;
                fixed4 col = _Color;
                col.rgb += _RimColor.rgb * fres * 0.9;                      // kenarlar buz gibi parlar
                col.rgb = lerp(col.rgb, fixed3(1,1,1), frost * _FrostAmount * 0.5); // don lekeleri
                col.a = saturate(_Color.a + fres * 0.4 + frost * _FrostAmount * 0.15);
                return col;
            }
            ENDCG
        }
    }
    FallBack Off
}
