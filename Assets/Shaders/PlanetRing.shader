// Hand-written (not Shader Graph) unlit URP shader for a flat ring quad around a planet.
// Masks a soft-edged ring band by UV-space radius (quad UVs run 0..1, center 0.5,0.5), adds a
// little radial banding noise so it isn't a flat gradient, and biases brightness toward one
// side to imply the same single distant light source the planet shader fakes.
Shader "LegendsOfTheUniverse/Background/PlanetRing"
{
    Properties
    {
        _ColorA ("Ring Color A", Color) = (0.65, 0.55, 0.45, 1)
        _ColorB ("Ring Color B", Color) = (0.35, 0.28, 0.22, 1)
        _InnerRadius ("Inner Radius", Range(0, 0.5)) = 0.18
        _OuterRadius ("Outer Radius", Range(0, 0.5)) = 0.46
        _Softness ("Edge Softness", Range(0.001, 0.1)) = 0.015
        _BandScale ("Band Noise Scale", Float) = 40.0
        _LightSideDir ("UV-space Light Side Direction", Vector) = (0.4, 0.3, 0, 0)
        _MinLight ("Dark Side Floor", Range(0, 1)) = 0.25
        _Alpha ("Overall Alpha", Range(0, 1)) = 0.85
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
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
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorA;
                float4 _ColorB;
                float _InnerRadius;
                float _OuterRadius;
                float _Softness;
                float _BandScale;
                float4 _LightSideDir;
                float _MinLight;
                float _Alpha;
            CBUFFER_END

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv = IN.uv;
                return OUT;
            }

            float Hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float2 centered = IN.uv - 0.5;
                float radius = length(centered);

                float outerMask = 1.0 - smoothstep(_OuterRadius - _Softness, _OuterRadius, radius);
                float innerMask = smoothstep(_InnerRadius - _Softness, _InnerRadius, radius);
                float ringMask = saturate(outerMask * innerMask);

                float bandCoord = radius * _BandScale;
                float band = Hash11(floor(bandCoord));
                float3 baseColor = lerp(_ColorB.rgb, _ColorA.rgb, band);

                float2 lightSide = normalize(_LightSideDir.xy + 1e-5);
                float facing = dot(normalize(centered + 1e-5), lightSide);
                float lightAmount = lerp(_MinLight, 1.0, saturate(facing * 0.5 + 0.5));

                float alpha = ringMask * _Alpha;
                return half4(baseColor * lightAmount, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
