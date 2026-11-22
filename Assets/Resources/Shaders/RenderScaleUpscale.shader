// Upscale of the world camera's reduced-size frame to the screen (RenderScaleCamera, 22/11 perf pass 2): bilinear tap plus a light
// 4-neighbour unsharp mask (_Sharpen 0 at 100 %, ~0.1 at 85 %, ~0.25 at 60 %) so edges and painted detail do not go soft. Gamma space, opaque copy.
Shader "Hidden/ZenoniaProto/RenderScaleUpscale"
{
    Properties { _MainTex ("Frame", 2D) = "white" {} _Sharpen ("Sharpen", Float) = 0.1 }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; float4 _MainTex_TexelSize; float _Sharpen;
            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 t = _MainTex_TexelSize.xy;
                float3 c = tex2D(_MainTex, i.uv).rgb;
                float3 n = tex2D(_MainTex, i.uv + float2(t.x, 0)).rgb + tex2D(_MainTex, i.uv - float2(t.x, 0)).rgb
                         + tex2D(_MainTex, i.uv + float2(0, t.y)).rgb + tex2D(_MainTex, i.uv - float2(0, t.y)).rgb;
                c = saturate(c + (c * 4.0 - n) * _Sharpen);
                return fixed4(c, 1.0);
            }
            ENDCG
        }
    }
}
