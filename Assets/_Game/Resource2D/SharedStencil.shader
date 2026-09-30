Shader "CoinCollector/Shared Stencil Shadow"
{
    Properties
    {
        [PerRendererData] _MainTex("Shadow Texture", 2D) = "white" {}
        _Color("Tint", Color) = (0, 0, 0, 1)
        _Opacity("Overall Opacity", Range(0, 1)) = 0.5
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0

        [IntRange] _StencilRef("Shared Stencil ID", Range(1, 255)) = 1
        [IntRange] _StencilReadMask("Stencil Read Mask", Range(0, 255)) = 255
        [IntRange] _StencilWriteMask("Stencil Write Mask", Range(0, 255)) = 255
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
        }

        CGINCLUDE
        #include "UnityCG.cginc"

        sampler2D _MainTex;
        fixed4 _Color;
        half _Opacity;
        half _Cutoff;

        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 uv : TEXCOORD0;
            fixed4 color : COLOR;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
            fixed4 color : COLOR;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            output.positionCS =
                UnityObjectToClipPos(input.positionOS);

            output.uv = input.uv;
            output.color = input.color * _Color;
            return output;
        }

        fixed4 SampleShadow(Varyings input)
        {
            return tex2D(_MainTex, input.uv) * input.color;
        }
        ENDCG

        Pass
        {
            Name "SharedStencilShadow"

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            Lighting Off
            ZWrite Off
            ZTest LEqual

            Stencil
            {
                Ref [_StencilRef]
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
                Comp NotEqual
                Pass Replace
                Fail Keep
                ZFail Keep
            }

            CGPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            fixed4 Frag(Varyings input) : SV_Target
            {
                fixed4 color =
                    SampleShadow(input);

                color.a *= _Opacity;

                // Luôn loại pixel alpha bằng 0 ngay cả khi Cutoff = 0,
                // để vùng trong suốt không chiếm stencil.
                clip(color.a - max(_Cutoff, 0.0001));
                return color;
            }
            ENDCG
        }
    }

    FallBack Off
}
