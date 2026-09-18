// Hand-written (not Shader Graph) unlit URP shader for a single drifting nebula sheet.
// Procedural value-noise cloud shape + a two-color gradient plus a sharper "ember filament"
// tint, alpha-blended over whatever is behind it so gaps between clouds stay true black.
Shader "LegendsOfTheUniverse/Background/NebulaLayer"
{
    Properties
    {
        _ColorA ("Cloud Color A (e.g. indigo)", Color) = (0.20, 0.10, 0.45, 1)
        _ColorB ("Cloud Color B (e.g. magenta)", Color) = (0.55, 0.10, 0.40, 1)
        _EmberColor ("Ember Filament Color", Color) = (0.85, 0.25, 0.10, 1)
        _NoiseScale ("Noise Scale", Float) = 2.5
        _EmberScale ("Ember Noise Scale", Float) = 7.0
        _ScrollSpeed ("Scroll Speed (uv/sec)", Vector) = (0.01, 0.006, 0, 0)
        _Density ("Cloud Density Bias", Range(-1, 1)) = 0.0
        _EmberThreshold ("Ember Threshold", Range(0, 1)) = 0.72
        _Alpha ("Overall Alpha", Range(0, 1)) = 0.8
        _EdgeSoftness ("Radial Edge Softness", Range(0.05, 1)) = 0.45
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
                float4 _EmberColor;
                float _NoiseScale;
                float _EmberScale;
                float4 _ScrollSpeed;
                float _Density;
                float _EmberThreshold;
                float _Alpha;
                float _EdgeSoftness;
            CBUFFER_END

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv = IN.uv;
                return OUT;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float Fbm(float2 p)
            {
                float sum = 0.0;
                float amp = 0.5;
                [unroll]
                for (int i = 0; i < 3; i++)
                {
                    sum += ValueNoise(p) * amp;
                    p *= 2.02;
                    amp *= 0.5;
                }
                return sum;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float2 scroll = _ScrollSpeed.xy * _Time.y;
                float2 uv = IN.uv * _NoiseScale + scroll;

                float shape = Fbm(uv) + _Density;
                float density = saturate(shape * 1.3 - 0.28);

                float grad = ValueNoise(uv * 0.6 + 11.0);
                float3 cloudColor = lerp(_ColorA.rgb, _ColorB.rgb, saturate(grad));

                float emberNoise = Fbm(IN.uv * _EmberScale + scroll * 1.7 + 31.7);
                float emberMask = smoothstep(_EmberThreshold, 1.0, emberNoise) * density;
                float3 color = lerp(cloudColor, _EmberColor.rgb, emberMask);

                float2 centered = IN.uv - 0.5;
                float radial = 1.0 - smoothstep(_EdgeSoftness, 0.71, length(centered));

                float alpha = saturate(density * radial * _Alpha);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
