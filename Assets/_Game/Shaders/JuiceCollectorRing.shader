Shader "Horus/JuiceCollectorRing"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ProgressStart ("Progress Start", Range(0,1)) = 0
        _ProgressEnd ("Progress End", Range(0,1)) = 1
        _ProgressLength ("Progress Length", Range(0,1)) = 1
        _StartAngle ("Start Angle", Range(0,1)) = 0.25
        _Clockwise ("Clockwise", Float) = 0
        _RingWidth ("Circle Thickness", Range(0,1)) = 0.1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _ProgressStart;
            float _ProgressEnd;
            float _ProgressLength;
            float _StartAngle;
            float _Clockwise;
            float _RingWidth;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 textureColor = tex2D(_MainTex, input.uv);
                fixed4 color = fixed4(
                    textureColor.rgb * _Color.rgb * input.color.rgb,
                    _Color.a * input.color.a);

                float2 localPosition = input.uv - 0.5;
                float distanceFromCenter = sqrt(
                    localPosition.x * localPosition.x
                    + localPosition.y * localPosition.y) * 2.0;
                // Keep only the outer band. A smaller value produces a thinner outline.
                clip(distanceFromCenter - (1.0 - _RingWidth));
                clip(1.0 - distanceFromCenter);
                clip(_ProgressLength - 0.0001);
                float angle = atan2(localPosition.y, localPosition.x) / (2.0 * UNITY_PI);
                float normalizedAngle = frac(angle - _StartAngle + 1.0);
                float progress = lerp(normalizedAngle, 1.0 - normalizedAngle, _Clockwise);
                float visible = _ProgressStart <= _ProgressEnd
                    ? step(_ProgressStart, progress) * step(progress, _ProgressEnd)
                    : max(step(_ProgressStart, progress), step(progress, _ProgressEnd));
                clip(visible - 0.5);
                return color;
            }
            ENDCG
        }
    }
}
