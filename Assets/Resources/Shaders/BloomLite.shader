// Lightweight bloom for the Built-in pipeline (used by BloomEffect.OnRenderImage).
// Passes: 0 prefilter (soft threshold + downsample), 1 downsample, 2 blur horizontal, 3 blur vertical, 4 composite.
Shader "ZenoniaProto/BloomLite"
{
    Properties { _MainTex ("Texture", 2D) = "white" {} }

    CGINCLUDE
    #include "UnityCG.cginc"

    sampler2D _MainTex;
    sampler2D _Bloom;
    sampler2D _Bloom2;
    float4 _MainTex_TexelSize;
    float _Threshold;
    float _Knee;
    float _Intensity;

    half3 Box4(float2 uv)
    {
        float2 o = _MainTex_TexelSize.xy * 0.5;
        return (tex2D(_MainTex, uv + float2(-o.x, -o.y)).rgb + tex2D(_MainTex, uv + float2(o.x, -o.y)).rgb
              + tex2D(_MainTex, uv + float2(-o.x, o.y)).rgb + tex2D(_MainTex, uv + float2(o.x, o.y)).rgb) * 0.25;
    }

    half4 fragPrefilter(v2f_img i) : SV_Target
    {
        half3 c = Box4(i.uv);
        half br = max(c.r, max(c.g, c.b));
        half soft = clamp(br - _Threshold + _Knee, 0.0, 2.0 * _Knee);
        soft = soft * soft / (4.0 * _Knee + 1e-4);
        half contrib = max(soft, br - _Threshold) / max(br, 1e-4);
        return half4(c * contrib, 1.0);
    }

    half4 fragDown(v2f_img i) : SV_Target { return half4(Box4(i.uv), 1.0); }

    half4 Blur(float2 uv, float2 dir)
    {
        // 5-tap linear-sampled gaussian (equivalent to 9 taps)
        float2 d1 = dir * 1.3846153846;
        float2 d2 = dir * 3.2307692308;
        half3 c = tex2D(_MainTex, uv).rgb * 0.2270270270;
        c += (tex2D(_MainTex, uv + d1).rgb + tex2D(_MainTex, uv - d1).rgb) * 0.3162162162;
        c += (tex2D(_MainTex, uv + d2).rgb + tex2D(_MainTex, uv - d2).rgb) * 0.0702702703;
        return half4(c, 1.0);
    }

    half4 fragBlurH(v2f_img i) : SV_Target { return Blur(i.uv, float2(_MainTex_TexelSize.x, 0.0)); }
    half4 fragBlurV(v2f_img i) : SV_Target { return Blur(i.uv, float2(0.0, _MainTex_TexelSize.y)); }

    half4 fragComposite(v2f_img i) : SV_Target
    {
        half4 src = tex2D(_MainTex, i.uv);
        half3 glow = tex2D(_Bloom, i.uv).rgb * 0.7 + tex2D(_Bloom2, i.uv).rgb * 1.0;
        return half4(src.rgb + glow * _Intensity, src.a);
    }
    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment fragPrefilter
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment fragDown
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment fragBlurH
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment fragBlurV
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment fragComposite
            ENDCG
        }
    }
    Fallback Off
}
