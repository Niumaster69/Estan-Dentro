// La mente de Mateo encima de la imagen (pase de pantalla completa de HDRP, antes del posproceso).
// - Garabatos de lápiz neón que "hierven" y entran desde los bordes con forma orgánica (los de Jinx en Arcane).
// - Contornos: el mundo se dibuja a lápiz (bordes de profundidad y normales del G-buffer de HDRP), temblando.
// - Onda que deforma la imagen desde el centro, cortes de glitch, aberración de color, tinte, negativo y destello.
// Lo maneja Scripts/Mente/EstadoMente.cs.
Shader "FullScreen/EstanDentro/Mente"
{
    Properties
    {
        _Garabatos ("Garabatos (3 cuadros en RGB)", 2D) = "black" {}
    }
    HLSLINCLUDE
    #pragma vertex Vert
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"

    TEXTURE2D(_Garabatos);
    SAMPLER(sampler_Garabatos);
    float _Aberracion;      // 0..1
    float _Glitch;          // 0..1 (cortes horizontales desplazados)
    float _GarabatosFuerza; // 0..1
    float _GarabatosBorde;  // 0..1: qué tanto entran desde los bordes (1 = toda la pantalla)
    float _Cuadro;          // 0,1,2: cuál de los 3 dibujos (el "hervor" a 12 cuadros por segundo)
    float4 _ColorGarabato;  // rgb + brillo en a
    float4 _Tinte;          // rgb + fuerza en a
    float _Destello;        // 0..1 blanco
    float _Negativo;        // 0..1 invertir
    float _Semilla;
    float _Contorno;        // 0..1 el mundo dibujado a lápiz
    float4 _ColorContorno;
    float _Onda;            // 0..1 onda que sale del centro
    float _OndaFase;        // 0..1 qué tan lejos va la onda

    float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7)) + _Semilla) * 43758.5453); }
    float HashFijo(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
    float Ruido(float2 p)
    {
        float2 i = floor(p), f = frac(p); f = f * f * (3.0 - 2.0 * f);
        return lerp(lerp(HashFijo(i), HashFijo(i + float2(1, 0)), f.x), lerp(HashFijo(i + float2(0, 1)), HashFijo(i + float2(1, 1)), f.x), f.y);
    }

    float Profundidad(float2 uv)
    {
        return LinearEyeDepth(SampleCameraDepth(uv), _ZBufferParams);
    }
    float3 Normal(float2 uv)
    {
        NormalData nd;
        DecodeFromNormalBuffer(uint2(uv * _ScreenSize.xy), nd);
        return nd.normalWS;
    }

    float4 FullScreenPass(Varyings varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);
        float2 uv0 = varyings.positionCS.xy * _ScreenSize.zw;
        float2 uv = uv0;
        float aspecto = _ScreenSize.x * _ScreenSize.w;
        float2 desde = uv - 0.5;
        float2 dA = float2(desde.x * aspecto, desde.y);
        float radio = length(dA);

        // onda: un anillo que se aleja del centro y empuja la imagen
        float anillo = exp(-pow((radio - _OndaFase * 1.1) * 9.0, 2.0)) * _Onda;
        uv -= normalize(dA + 1e-5) * anillo * 0.035 * float2(1.0 / aspecto, 1.0);

        // glitch: franjas horizontales que se corren de lado (unas pocas, al azar)
        float franja = floor(uv.y * 34.0);
        float h = Hash(float2(franja, floor(_Time.y * 24.0)));
        float corre = (h > 1.0 - _Glitch * 0.45) ? (Hash(float2(franja, 7.0)) - 0.5) * 0.12 * _Glitch : 0.0;
        uv.x += corre;

        // aberración de color: más fuerte hacia los bordes
        float2 off = desde * _Aberracion * 0.035 + float2(corre * 0.4, 0.0) + desde * anillo * 0.03;
        float r = CustomPassSampleCameraColor(uv + off, 0).r;
        float g = CustomPassSampleCameraColor(uv, 0).g;
        float b = CustomPassSampleCameraColor(uv - off, 0).b;
        float3 c = float3(r, g, b);

        // tinte del estado (conserva la luz)
        float lum = dot(c, float3(0.299, 0.587, 0.114));
        c = lerp(c, lum * _Tinte.rgb * 1.6, _Tinte.a);

        // contornos del mundo: bordes de profundidad y de normales, con el trazo que tiembla cada cuadro
        if (_Contorno > 0.001)
        {
            float2 tiembla = (float2(Hash(float2(_Cuadro, 1.0)), Hash(float2(_Cuadro, 2.0))) - 0.5) * 2.0 * _ScreenSize.zw;
            float2 px = _ScreenSize.zw * 1.5;
            float2 u = uv + tiembla;
            float d0 = Profundidad(u);
            float dd = abs(Profundidad(u + float2(px.x, 0)) - d0) + abs(Profundidad(u - float2(px.x, 0)) - d0)
                     + abs(Profundidad(u + float2(0, px.y)) - d0) + abs(Profundidad(u - float2(0, px.y)) - d0);
            float bordeP = saturate((dd / max(d0, 0.1)) * 6.0 - 0.15);
            float3 n0 = Normal(u);
            float dn = (1.0 - dot(n0, Normal(u + float2(px.x, 0)))) + (1.0 - dot(n0, Normal(u + float2(0, px.y))));
            float bordeN = saturate(dn * 2.5 - 0.2);
            float lapiz = 0.65 + 0.35 * Ruido(uv0 * float2(260.0, 90.0) + _Cuadro * 13.0);        // el grano del lápiz
            float linea = saturate(max(bordeP, bordeN)) * lapiz * saturate(1.0 - d0 / 45.0);
            c = lerp(c, c * (1.0 - _Contorno * 0.65), _Contorno);                                   // el mundo se apaga…
            c += _ColorContorno.rgb * linea * _Contorno * (1.0 + 3.0 * _ColorContorno.a);            // …y quedan las líneas
        }

        // garabatos: el cuadro actual (canal R/G/B = cuadro 0/1/2), entran desde los bordes con un contorno orgánico
        float3 gtex = SAMPLE_TEXTURE2D(_Garabatos, sampler_Garabatos, uv0 + float2(Hash(float2(_Cuadro, 3.0)) - 0.5, Hash(float2(_Cuadro, 9.0)) - 0.5) * 0.004).rgb;
        float trazo = _Cuadro < 0.5 ? gtex.r : (_Cuadro < 1.5 ? gtex.g : gtex.b);
        float ang = atan2(dA.y, dA.x);
        float bordeOrg = Ruido(float2(ang * 2.2 + 11.0, _Time.y * 0.6)) * 0.55 + Ruido(float2(ang * 6.0, _Time.y * 1.7 + 4.0)) * 0.25;
        float elipse = length(desde * float2(1.9, 2.15));                                        // 0 centro, ~1 en los bordes, ~1.4 esquinas
        float limite = 1.25 - _GarabatosBorde * 1.35 + (bordeOrg - 0.4) * 0.35;
        float mascara = saturate((elipse - limite) / 0.22);
        float a = trazo * mascara * _GarabatosFuerza;
        c = lerp(c, _ColorGarabato.rgb * (1.0 + 3.0 * _ColorGarabato.a), a);
        c += _ColorGarabato.rgb * mascara * _GarabatosFuerza * 0.08 * _ColorGarabato.a;           // halo leve del neón

        // destellos
        c += _ColorContorno.rgb * anillo * 0.6;
        c = lerp(c, 1.0 - saturate(c), _Negativo);
        c = lerp(c, float3(1.0, 1.0, 1.0) * 6.0, _Destello);
        return float4(c, 1.0);
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Name "Mente"
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off
            HLSLPROGRAM
                #pragma fragment FullScreenPass
            ENDHLSL
        }
    }
    Fallback Off
}
