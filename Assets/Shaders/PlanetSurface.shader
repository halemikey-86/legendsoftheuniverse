// Hand-written (not Shader Graph) unlit URP shader for a background planet sphere. Fakes a
// single directional light (the distant "core") without any real-time lighting: a soft
// normal-based falloff for day/night shading, plus a thin Fresnel rim tinted per-planet
// (white or ember-red) that only appears on the lit/leading edge, per the brief. Procedural
// noise banding/mottling means no texture is needed and nothing resembles a real planet.
Shader "LegendsOfTheUniverse/Background/PlanetSurface"
{
    Properties
    {
        _ColorA ("Surface Color A", Color) = (0.55, 0.30, 0.18, 1)
        _ColorB ("Surface Color B", Color) = (0.30, 0.14, 0.08, 1)
        _RimColor ("Rim Light Color", Color) = (1.0, 0.35, 0.25, 1)
        _NoiseScale ("Surface Noise Scale", Float) = 3.0
        _BandingStrength ("Horizontal Banding Strength", Range(0, 1)) = 0.0
        _Seed ("Noise Seed Offset", Float) = 0.0
        _LightDir ("World Light Direction (to light)", Vector) = (0.3, 0.2, -1, 0)
        _MinLight ("Night Side Floor", Range(0, 1)) = 0.12
        _RimPower ("Rim Power", Range(0.5, 8)) = 3.0
        _RimIntensity ("Rim Intensity", Range(0, 3)) = 1.2
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorA;
                float4 _ColorB;
                float4 _RimColor;
                float _NoiseScale;
                float _BandingStrength;
                float _Seed;
                float4 _LightDir;
                float _MinLight;
                float _RimPower;
                float _RimIntensity;
            CBUFFER_END

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS);
                return OUT;
            }

            float Hash31(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float ValueNoise3(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float n000 = Hash31(i + float3(0, 0, 0));
                float n100 = Hash31(i + float3(1, 0, 0));
                float n010 = Hash31(i + float3(0, 1, 0));
                float n110 = Hash31(i + float3(1, 1, 0));
                float n001 = Hash31(i + float3(0, 0, 1));
                float n101 = Hash31(i + float3(1, 0, 1));
                float n011 = Hash31(i + float3(0, 1, 1));
                float n111 = Hash31(i + float3(1, 1, 1));

                float nx00 = lerp(n000, n100, f.x);
                float nx10 = lerp(n010, n110, f.x);
                float nx01 = lerp(n001, n101, f.x);
                float nx11 = lerp(n011, n111, f.x);
                float nxy0 = lerp(nx00, nx10, f.y);
                float nxy1 = lerp(nx01, nx11, f.y);
                return lerp(nxy0, nxy1, f.z);
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float3 normalWS = normalize(IN.normalWS);
                float3 lightDir = normalize(_LightDir.xyz);
                float3 viewDirWS = normalize(_WorldSpaceCameraPos - IN.positionWS);

                float3 noiseCoord = normalWS * _NoiseScale + _Seed;
                float mottle = ValueNoise3(noiseCoord) * 0.6 + ValueNoise3(noiseCoord * 2.3) * 0.4;

                float band = frac(normalWS.y * 3.0 + _Seed * 0.5);
                band = smoothstep(0.0, 0.15, band) - smoothstep(0.55, 0.7, band);
                float pattern = lerp(mottle, band, _BandingStrength);

                float3 baseColor = lerp(_ColorB.rgb, _ColorA.rgb, saturate(pattern));

                float ndotl = dot(normalWS, lightDir);
                float lightAmount = lerp(_MinLight, 1.0, saturate(ndotl * 0.5 + 0.5));
                float3 shaded = baseColor * lightAmount;

                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _RimPower);
                float leadingEdge = saturate(ndotl);
                float3 rim = _RimColor.rgb * fresnel * leadingEdge * _RimIntensity;

                return half4(shaded + rim, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
