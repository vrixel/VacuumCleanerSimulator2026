// Exploration (explore/hot-chrome): screen-space ink outlines from the depth-normals texture, Roberts cross on
// depth (relative) and on view normals. Driven by HotChromeEdges (Post Processing v2 custom effect).
Shader "Hidden/VCS/HotChromeEdges"
{
    HLSLINCLUDE
    #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"

    TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
    TEXTURE2D_SAMPLER2D(_CameraDepthNormalsTexture, sampler_CameraDepthNormalsTexture);
    float4 _MainTex_TexelSize;
    float4 _EdgeColor;
    float _Thickness;
    float _DepthThreshold;
    float _NormalThreshold;
    float _FarFade;

    float DecodeDepth(float2 enc) { return dot(enc, float2(1.0, 1.0 / 255.0)); }

    float3 DecodeNormal(float4 enc)
    {
        const float k = 1.7777;
        float3 nn = enc.xyz * float3(2.0 * k, 2.0 * k, 0.0) + float3(-k, -k, 1.0);
        float g = 2.0 / dot(nn, nn);
        return float3(g * nn.xy, g - 1.0);
    }

    void Probe(float2 uv, out float d, out float3 n)
    {
        float4 e = SAMPLE_TEXTURE2D(_CameraDepthNormalsTexture, sampler_CameraDepthNormalsTexture, uv);
        d = DecodeDepth(e.zw);
        n = DecodeNormal(e);
    }

    float4 Frag(VaryingsDefault i) : SV_Target
    {
        float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
        float2 o = _MainTex_TexelSize.xy * _Thickness;
        float d0, d1, d2, d3, d4;
        float3 n0, n1, n2, n3, n4;
        Probe(i.texcoord, d0, n0);
        Probe(i.texcoord + float2(o.x, o.y), d1, n1);
        Probe(i.texcoord - float2(o.x, o.y), d2, n2);
        Probe(i.texcoord + float2(o.x, -o.y), d3, n3);
        Probe(i.texcoord + float2(-o.x, o.y), d4, n4);

        float dd = (abs(d1 - d2) + abs(d3 - d4)) / max(d0, 1e-4);
        float edgeD = saturate((dd - _DepthThreshold) * 12.0);
        float3 a = n1 - n2;
        float3 b = n3 - n4;
        float edgeN = saturate((sqrt(dot(a, a) + dot(b, b)) - _NormalThreshold) * 3.0);
        float e = max(edgeD, edgeN) * (1.0 - saturate(d0 * _FarFade)) * step(d0, 0.995);
        return float4(lerp(col.rgb, _EdgeColor.rgb, e * _EdgeColor.a), col.a);
    }
    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertDefault
            #pragma fragment Frag
            ENDHLSL
        }
    }
}
