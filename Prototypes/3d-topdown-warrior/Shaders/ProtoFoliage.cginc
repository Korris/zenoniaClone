// Shared body of Proto/Foliage and Proto/FoliageFade: stylised wrap-Lambert lit by the vertex colour (the builder bakes light-top / dark-bottom
// shading into the mesh colours), plus an optional wind sway weighted by uv.y (0 at a grass blade's root, 1 at its tip; trees and bushes use 0).
fixed4 _Color;
float _Sway, _Wrap;

struct Input { float4 color : COLOR; };

void vert(inout appdata_full v)
{
    float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
    float s = sin(_Time.y * 1.7 + wp.x * 0.9 + wp.z * 0.7) * _Sway * v.texcoord.y;
    v.vertex.xz += float2(s, s * 0.6);
}

half4 LightingWrap(SurfaceOutput s, half3 lightDir, half atten)
{
    half diff = saturate((dot(s.Normal, lightDir) + _Wrap) / (1 + _Wrap));
    half4 c; c.rgb = s.Albedo * _LightColor0.rgb * (diff * atten); c.a = s.Alpha;
    return c;
}

void surf(Input IN, inout SurfaceOutput o)
{
    o.Albedo = IN.color.rgb * _Color.rgb;
    o.Alpha = _Color.a;
}
