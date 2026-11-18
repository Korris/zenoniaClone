// Shared body of Proto/Leaf and Proto/LeafFade: alpha-cut leaf-cluster quads from the ChatGPT leaf atlas, tinted by the vertex colour
// (per-cluster hue / shade baked by the builder), wrap-Lambert lit so the canopy stays soft. Vertex alpha = sway weight (0 still, 1 tip).
sampler2D _MainTex;
fixed4 _Color;
float _Cutoff, _Sway, _Wrap;

struct Input { float2 uv_MainTex; float4 color : COLOR; };

void vert(inout appdata_full v)
{
    float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
    float s = sin(_Time.y * 1.6 + wp.x * 0.8 + wp.z * 0.6) * _Sway * v.color.a;
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
    fixed4 t = tex2D(_MainTex, IN.uv_MainTex);
    clip(t.a - _Cutoff);
    o.Albedo = t.rgb * IN.color.rgb * _Color.rgb;
    o.Alpha = _Color.a;
}
