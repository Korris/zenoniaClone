// Ground shadow composite (FakeSun, GroundShadowRenderer): one quad over the map, drawn just above the ground painting and flat decor and below
// every y-sorted unit, ring and telegraph. Reads the max-opacity mask by world position and multiplies the ground toward the map's shadow colour
// (ground x lerp(1, tint, a), a capped): a cool tint that darkens grass and dirt alike, never black, never stacked. Plain alpha blending of the
// mid-grey tint barely darkened bright grass (first in-game look, 21/11).
Shader "Hidden/ZenoniaProto/GroundShadowComposite"
{
    Properties { }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Cull Off Lighting Off ZWrite Off
        Blend DstColor Zero

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _GroundShadowMask;
            float4 _GroundShadowRect;   // xy = world min of the mask, zw = 1 / world size
            float4 _GroundShadowTint;   // rgb = shadow colour, a = alpha cap
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                float3 w = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = (w.xy - _GroundShadowRect.xy) * _GroundShadowRect.zw;
                return o;
            }
            half4 frag(v2f i) : SV_Target
            {
                float inside = step(0.0, i.uv.x) * step(i.uv.x, 1.0) * step(0.0, i.uv.y) * step(i.uv.y, 1.0);
                float a = min(tex2D(_GroundShadowMask, i.uv).r, _GroundShadowTint.a) * inside;
                return half4(lerp(float3(1, 1, 1), _GroundShadowTint.rgb, a), 1); // multiply: ground x (1 - a + a * tint)
            }
            ENDCG
        }
    }
}
