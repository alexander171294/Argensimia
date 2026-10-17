// Tilt-shift de pantalla completa: plaza nítida, desenfoque suave arriba/abajo y más fuerte en los bordes y esquinas.
// En el menú (global _MenuBlur 0..1, lo maneja GameFlow) pasa a blur circular: nítido alrededor de _MenuCenter.
Shader "Argensimia/TiltShift"
{
    Properties
    {
        _Focus ("Centro de foco (uv.y)", Range(0, 1)) = 0.46
        _Band ("Media franja nítida", Range(0, 0.5)) = 0.31
        _Soft ("Transición", Range(0.01, 0.6)) = 0.24
        _Strength ("Intensidad arriba/abajo", Range(0, 1)) = 0.55
        _Radius ("Radio máx (px a 1080p)", Float) = 9
        _Edge ("Desenfoque de bordes", Range(0, 2)) = 1.0
        _EdgeStart ("Inicio bordes", Range(0, 1.5)) = 0.78
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always
        Pass
        {
            Name "TiltShift"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Focus, _Band, _Soft, _Strength, _Radius, _Edge, _EdgeStart;
            // globales del menú (no están en Properties para no quedar fijadas en el material)
            float _MenuBlur, _MenuR, _MenuSoft, _MenuRadius;
            float4 _MenuCenter;

            static const float2 K[16] = {
                float2(-0.326, -0.406), float2(-0.840, -0.074), float2(-0.696, 0.457), float2(-0.203, 0.621),
                float2(0.962, -0.195), float2(0.473, -0.480), float2(0.519, 0.767), float2(0.185, -0.893),
                float2(0.507, 0.064), float2(0.896, 0.412), float2(-0.322, -0.933), float2(-0.792, -0.598),
                float2(0.000, 1.000), float2(-0.420, 0.880), float2(0.140, 0.300), float2(-0.250, 0.050)
            };

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 uv = i.texcoord;
                float a = _Strength * smoothstep(_Band, _Band + _Soft, abs(uv.y - _Focus));
                float2 c = (uv - 0.5) * float2(1.9, 1.0);
                a = saturate(a + _Edge * smoothstep(_EdgeStart, _EdgeStart + 0.35, length(c)));
                // menú: círculo nítido en el centro de interés, todo lo demás desenfocado
                float2 mc = (uv - _MenuCenter.xy) * float2(_ScreenParams.x / _ScreenParams.y, 1.0);
                float m = smoothstep(_MenuR, _MenuR + _MenuSoft, length(mc));
                a = lerp(a, m, saturate(_MenuBlur));
                float radius = lerp(_Radius, _MenuRadius, saturate(_MenuBlur));
                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                if (a < 0.01) return col;
                float2 px = (radius * a * _ScreenParams.y / 1080.0) / _ScreenParams.xy;
                // rotación por píxel para que un radio grande no deje "fantasmas" con el patrón de muestras
                float ang = 6.2831853 * frac(52.9829189 * frac(dot(uv * _ScreenParams.xy, float2(0.06711056, 0.00583715)))) * saturate(_MenuBlur);
                float2x2 rot = float2x2(cos(ang), -sin(ang), sin(ang), cos(ang));
                half4 acc = col;
                [unroll] for (int k = 0; k < 16; k++)
                    acc += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + mul(rot, K[k]) * px);
                return acc / 17.0;
            }
            ENDHLSL
        }
    }
}
