// Hand-written (not Shader Graph, not URP's default particle shader) unlit shader for the
// star-streak particle system. Reads the ParticleSystem's per-particle vertex color/alpha
// directly and fades the stretched billboard quad toward its tail and edges so each streak
// reads as a soft comet-like trail rather than a hard rectangle.
Shader "LegendsOfTheUniverse/Background/StarStreak"
{
    Properties
    {
        _Tint ("Tint", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Tint;
            CBUFFER_END

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float tailFade = smoothstep(0.0, 0.55, IN.uv.x);
                float edgeFade = 1.0 - saturate(abs(IN.uv.y - 0.5) * 2.0);
                float alpha = tailFade * edgeFade * IN.color.a * _Tint.a;
                return half4(IN.color.rgb * _Tint.rgb, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
