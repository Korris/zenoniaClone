// Shared body of Proto/Canopy and Proto/CanopyFade (ChatGPT v14 review, item 1): the Tripo trees / bushes keep their painted albedo + normal
// map, but leaves get volume shading the painting lacks - crown top +7 %, inside / lower crown -18 %, faces turned down -20 %, yellow 5 % less
// saturated - and bark gets +10 % contrast. Leaf vs bark is told apart per pixel by colour (green dominance); crown height comes from the
// mesh's object-space Y range (_MinY.._MaxY, set per model by the builder).
sampler2D _MainTex, _BumpMap;
fixed4 _Color;
half _Glossiness, _BumpScale, _MinY, _MaxY;
float4 _UpObj;   // world up in object space (set by the builder)
half _Dither;
half _Moss;      // v17: moss coverage on up-facing rock surfaces (0 for trees)    // v16: 1 = solid; below 1 a screen-space dither keeps only that share of pixels (tree in front of the Warrior)

struct Input { float2 uv_MainTex; float h; float ny; float4 screenPos; float3 worldPos; };

void vert(inout appdata_full v, out Input o)
{
    UNITY_INITIALIZE_OUTPUT(Input, o);
    o.h = saturate((dot(v.vertex.xyz, _UpObj.xyz) - _MinY) / max(_MaxY - _MinY, 1e-3));
    o.ny = UnityObjectToWorldNormal(v.normal).y;
}

void surf(Input IN, inout SurfaceOutputStandard o)
{
    if (_Dither < 0.999)   // v16 (ChatGPT): dithered see-through instead of alpha blending - no ghostly double image
    {
        float2 px = floor(IN.screenPos.xy / max(IN.screenPos.w, 1e-4) * _ScreenParams.xy);
        const float4x4 B = float4x4(0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5);   // 4x4 Bayer ordered dither
        int2 i = int2(fmod(px, 4.0));
        float bayer = (B[i.y][i.x] + 0.5) / 16.0;
        clip(_Dither - bayer);
    }
    fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
    half leaf = saturate((c.g - max(c.r, c.b)) * 6.0);                 // 1 on green leaves, 0 on brown bark / grey stone
    half top = smoothstep(0.45, 1.0, IN.h), inner = 1.0 - smoothstep(0.25, 0.75, IN.h);
    half shade = (1.0 + 0.07 * top) * (1.0 - 0.18 * inner) * (IN.ny < 0 ? 1.0 - 0.2 * saturate(-IN.ny * 2.0) : 1.0);
    // v17: per-cluster hue / value variation +-5 % (world-space noise ~1.5 m) so the crown is not one flat yellow-green
    float3 wp = IN.worldPos;
    half vary = sin(wp.x * 2.1 + sin(wp.z * 1.7) * 1.3) * sin(wp.z * 2.3 + wp.y * 1.9);
    fixed3 leafCol = lerp(c.rgb, Luminance(c.rgb).xxx, 0.05) * shade * (1.0 + 0.05 * vary);  // yellow 5 % less saturated
    leafCol.g *= 1.0 + 0.03 * vary; leafCol.r *= 1.0 - 0.03 * vary;
    fixed3 barkCol = saturate((c.rgb - 0.5) * 1.1 + 0.5);               // bark contrast +10 %
    o.Albedo = lerp(barkCol, leafCol, leaf);
    // v17 rocks: sun-facing (up) faces +8 %, crevices (down) -6 %, moss #7E9A4A on faces pointing up
    half up = saturate(IN.ny);
    if (_Moss > 0)
    {
        o.Albedo *= lerp(0.94, 1.08, up);
        half moss = saturate((up - 0.55) * 4.0) * saturate(_Moss * 4.0) * (0.75 + 0.25 * vary);
        o.Albedo = lerp(o.Albedo, fixed3(0.494, 0.604, 0.29), moss);
    }
    o.Normal = UnpackScaleNormal(tex2D(_BumpMap, IN.uv_MainTex), _BumpScale);
    // v17: leaf detail normal ~6 % - a fine world-space ripple breaks the stiff 'folded paper' look
    o.Normal.xy += leaf * 0.06 * float2(sin(wp.x * 23.0 + wp.y * 17.0), sin(wp.z * 21.0 - wp.y * 19.0));
    o.Normal = normalize(o.Normal);
    o.Metallic = 0;
    o.Smoothness = _Glossiness;
    o.Alpha = _Color.a;
}
