Shader "Horus/VFX/Water Fan Particle"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _Softness ("Edge softness", Range(0.05,1)) = 0.12
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };
            fixed4 _Color;
            float _Softness;
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                o.uv = v.uv;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = i.uv * 2 - 1;
                float mask = 1 - smoothstep(1 - _Softness, 1, dot(p,p));
                // Draw a crisp drop and its highlight in the same particle.
                float shading = lerp(0.8, 1.08, saturate(0.55 - p.y * 0.3 - p.x * 0.2));
                float2 shinePosition = (p - float2(-0.3, 0.23)) / float2(0.19, 0.36);
                float shine = 1 - smoothstep(0.65, 1, dot(shinePosition, shinePosition));
                fixed3 water = lerp(i.color.rgb * shading, fixed3(0.9, 0.98, 1), shine * 0.85);
                return fixed4(water, i.color.a * mask);
            }
            ENDCG
        }
    }
}
