// Shared lighting of the 3D map prototype (Map3D, 22/11/2026; ChatGPT tech-art spec docs/reference/willowbrook-3d-proto/map-3d-ro3-spec-chatgpt.md
// sections 2.4, 4, 5.5). Built-in pipeline, gamma space. One sun (the scene's directional light: direction from _WorldSpaceLightPos0, shadow
// attenuation from AutoLight, strength from the light), our own painterly ambient instead of Unity's (never added twice):
//   light = lerp(_M3DShade, _M3DSun, soft 3-step ramp of wrap(N.L) x shadow), sky tint on up-facing normals.
// Globals (Map3DLighting): _M3DSun / _M3DShade (rgb), _M3DSky (rgb, a = how much up-facing normals take it),
// _M3DHero (viewport x, y of the local hero's chest, radius x, radius y in viewport heights), _M3DHeroDepth (x = eye depth of the hero, y = on/off):
// leaves / trunks between the camera and the hero dissolve with a 4x4 ordered dither inside that ellipse (spec 5.5, no alpha blending).
// World "up" is -z (View25D convention).

#ifndef MAP3D_COMMON_INCLUDED
#define MAP3D_COMMON_INCLUDED

float4 _M3DSun, _M3DShade, _M3DSky, _M3DHero, _M3DHeroDepth, _M3DWind;

// soft 3-step ramp (spec: shadow 0.72, mid 0.92, high 1.08, soft blends ~0.15 wide) on a 0..1 lit value
float M3DRamp(float lit)
{
    float r = lerp(0.0, 0.62, smoothstep(0.18, 0.40, lit));
    r = lerp(r, 1.0, smoothstep(0.55, 0.80, lit));
    return lerp(r, lit, 0.30); // keep some of the continuous gradient: painted, not banded
}

// Lit colour of an albedo with normal N (world), light dir L (toward the light), shadow attenuation atten (0..1), wrap amount
float3 M3DShade(float3 albedo, float3 N, float3 L, float atten, float wrap)
{
    float w = saturate((dot(N, L) + wrap) / (1.0 + wrap));
    float lit = M3DRamp(w * atten);
    float3 light = lerp(_M3DShade.rgb, _M3DSun.rgb, lit);
    float up = saturate(-N.z); // up = -z
    light *= lerp(1.0.xxx, _M3DSky.rgb, up * _M3DSky.a);
    return albedo * light;
}

// 4x4 ordered dither threshold at a screen pixel (0..1)
static const float M3DBayerM[16] = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };
float M3DBayer(float2 px)
{
    int2 p = int2(fmod(floor(px), 4.0));
    return (M3DBayerM[p.y * 4 + p.x] + 0.5) / 16.0;
}

// Occlusion dissolve round the local hero: clip() pixels nearer the camera than the hero inside the screen ellipse. minCover = coverage left in the
// ellipse centre (spec: 30-45 %), screenPos from ComputeScreenPos, eyeDepth = view-space depth of the fragment.
void M3DHeroFade(float4 screenPos, float eyeDepth, float minCover)
{
    if (_M3DHeroDepth.y < 0.5) return;
    if (eyeDepth > _M3DHeroDepth.x - 6.0) return; // behind (or level with) the hero: never hides
    float2 uv = screenPos.xy / screenPos.w;
    float aspect = _ScreenParams.x / _ScreenParams.y;
    float2 d = (uv - _M3DHero.xy) * float2(aspect, 1.0) / max(_M3DHero.zw, 1e-3);
    float e = length(d);
    float cover = lerp(minCover, 1.0, smoothstep(0.55, 1.0, e));
    if (cover >= 1.0) return;
    float2 px = uv * _ScreenParams.xy;
    clip(cover - M3DBayer(px));
}

// Wind sway offset (world units) for a vertex at world position wp with weight w (0 = rooted, 1 = free leaf tip); _M3DWind = (amplitude units, speed, dir x, dir y)
float3 M3DWind(float3 wp, float w)
{
    float t = _Time.y * _M3DWind.y;
    float ph = dot(wp.xy, float2(0.021, 0.017));
    float s = sin(t * 1.1 + ph) * 0.6 + sin(t * 2.3 + ph * 2.7) * 0.3 + sin(t * 4.7 + ph * 5.1 + wp.z * 0.2) * 0.15 * w;
    float2 dir = _M3DWind.zw;
    return float3(dir * s * _M3DWind.x * w, 0.0);
}

// Leaf card vertex: world position with the wind sway, and its clip position. Both foliage passes that share depth (LEAFDEPTH and the lit pass
// with ZTest Equal) must call exactly these so the GPU computes bit-identical depths.
float3 M3DLeafWorld(float4 vertex, float windW, float windMul)
{
    float3 wp = mul(unity_ObjectToWorld, vertex).xyz;
    wp += M3DWind(wp, windW) * windMul;
    return wp;
}
float4 M3DLeafClip(float3 wp)
{
    float4 p = mul(UNITY_MATRIX_VP, float4(wp, 1.0));
    return p;
}

// Mip level a texture of size texSize (pixels) is sampled at: alpha-tested cards widen their alpha in the small mips by it
float M3DMipLevel(float2 uv, float2 texSize)
{
    float2 d = max(abs(ddx(uv)), abs(ddy(uv))) * texSize;
    return max(0.0, log2(max(d.x, d.y)));
}

// Baked sun shadow (Map3DBakedShadow, graphics quality Medium, 22/11 perf pass): the static props' depth from the fixed sun, rendered ONCE when the
// map loads into a 4096 float depth map over the whole terrain. Receivers compare 4 taps against it instead of the realtime cascades
// (no per-frame shadow maps, depth pre-pass or screen-space collect). _M3DBakedVP: world -> (u, v, depth 0..1) of that map;
// _M3DBaked: x = on, y = tap offset (uv), z = receiver offset toward the light / along the normal (world units), w = lit value in full shadow (1 - strength).
sampler2D _M3DBakedShadow;
float4x4 _M3DBakedVP;
float4 _M3DBaked;

float M3DBakedSunP(float3 wp, float3 n, float4 prm)
{
    if (prm.x < 0.5) return 1.0;
    float3 p = wp + (n + normalize(_WorldSpaceLightPos0.xyz) * 1.3) * prm.z;
    float3 c = mul(_M3DBakedVP, float4(p, 1.0)).xyz;
    if (any(c.xy < 0.0) || any(c.xy > 1.0)) return 1.0;
    float o = prm.y;
    float4 d = float4(tex2Dlod(_M3DBakedShadow, float4(c.xy + float2(-o, -o), 0, 0)).r, tex2Dlod(_M3DBakedShadow, float4(c.xy + float2(o, -o), 0, 0)).r,
                      tex2Dlod(_M3DBakedShadow, float4(c.xy + float2(-o, o), 0, 0)).r, tex2Dlod(_M3DBakedShadow, float4(c.xy + float2(o, o), 0, 0)).r);
    float s = dot(step(c.zzzz, d), 0.25.xxxx); // lit where nothing nearer to the sun was baked
    return lerp(prm.w, 1.0, s);
}
float M3DBakedSun(float3 wp, float3 n) { return M3DBakedSunP(wp, n, _M3DBaked); }
// Same bake for the tree impostors (Map3DImpostors): on at Medium AND High (they have no realtime shadow of their own), off at Low.
float4 _M3DBakedImp;

// Tree impostor LOD (22/11 perf pass 2, Map3DImpostors): every vertex of a generated tree / bush carries its tree's root (TEXCOORD1 = world x, y;
// TEXCOORD2 = world z, tree index + 1; written by Map3DBatcher through the combine's lightmap offsets; 0 = not a tree). The CPU decides per tree
// (distance from the camera to the root past hero + StartAhead, with hysteresis) and animates its fade 0 = mesh .. 1 = impostor over ~0.35 s into
// _M3DImpFade (one texel per tree, _M3DImp = (texture width, 1 / width, on, 1 / height)). Fade 1: the mesh tree collapses in the vertex shader
// (no pixels at all) and the impostor billboard draws; during the short swap both draw with complementary 4x4 dither (mesh keeps dither > fade,
// impostor dither <= fade), so it never pops and no tree stays half-dithered while the camera rests.
float4 _M3DImp;
sampler2D _M3DImpFade;
#define M3D_CULLED_POS float4(4.0, 4.0, 4.0, 1.0)
float M3DImpFade(float2 rootXY, float2 rootZFlag)
{
    if (_M3DImp.z < 0.5 || rootZFlag.y < 0.5) return 0.0;
    float i = rootZFlag.y - 1.0;
    float row = floor(i * _M3DImp.y + 1e-4);
    float2 uv = float2((i - row * _M3DImp.x + 0.5) * _M3DImp.y, (row + 0.5) * _M3DImp.w);
    return tex2Dlod(_M3DImpFade, float4(uv, 0, 0)).r;
}
// Shadow-caster passes: the camera depth texture (perspective) follows the LOD, the sun's shadow map (orthographic) keeps every tree.
float M3DImpFadeCaster(float2 rootXY, float2 rootZFlag) { return UNITY_MATRIX_P[3][3] < 0.5 ? M3DImpFade(rootXY, rootZFlag) : 0.0; }
float2 M3DPixel(float4 screenPos) { return screenPos.xy / screenPos.w * _ScreenParams.xy; }
// mesh side of the cross-fade
void M3DImpClipMesh(float4 screenPos, float fade) { if (fade > 0.0) clip(M3DBayer(M3DPixel(screenPos)) - fade); }
// same with the impostor LOD: a tree past the fade band collapses off screen (both depth-sharing passes must call this one)
float4 M3DLeafClipLod(float3 wp, float fade) { return fade >= 1.0 ? M3D_CULLED_POS : M3DLeafClip(wp); }

#endif
