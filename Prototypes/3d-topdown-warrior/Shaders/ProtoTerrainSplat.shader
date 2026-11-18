// Glade ground: four tiling layers (main grass, light grass, path dirt, dark soil) blended by the terrain mesh's vertex colour (R, G, B, A),
// world-space UVs in metres (_TileN = 1 / tile size), Lambert-lit with real shadows so tree shade lands on the ground. Each layer's height
// (its texture's luminance) sharpens the blend a little so the path rim breaks up instead of fading smoothly.
Shader "Proto/TerrainSplat"
{
    Properties
    {
        _Tex0 ("Grass", 2D) = "white" {}      _Tile0 ("Grass 1/m", Float) = 0.5
        _Tex1 ("Light grass", 2D) = "white" {} _Tile1 ("Light grass 1/m", Float) = 0.33
        _Tex2 ("Path dirt", 2D) = "white" {}   _Tile2 ("Dirt 1/m", Float) = 0.4
        _Tex3 ("Dark soil", 2D) = "white" {}   _Tile3 ("Soil 1/m", Float) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        CGPROGRAM
        #pragma surface surf Lambert fullforwardshadows
        #pragma target 3.0
        sampler2D _Tex0, _Tex1, _Tex2, _Tex3;
        float _Tile0, _Tile1, _Tile2, _Tile3;
        struct Input { float3 worldPos; float4 color : COLOR; };

        void surf(Input IN, inout SurfaceOutput o)
        {
            float2 w = IN.worldPos.xz;
            fixed3 c0 = tex2D(_Tex0, w * _Tile0).rgb, c1 = tex2D(_Tex1, w * _Tile1).rgb, c2 = tex2D(_Tex2, w * _Tile2).rgb, c3 = tex2D(_Tex3, w * _Tile3).rgb;
            // v13: two-tone path (ChatGPT light #D6AE73 / dark #B88E59) - slow 2-4 m variation so the dirt is not one flat band
            float tone = 0.5 + 0.25 * (sin(w.x * 0.9 + sin(w.y * 0.6) * 2.0) + sin(w.y * 1.3 - w.x * 0.4));
            c2 *= lerp(fixed3(0.9, 0.87, 0.84), fixed3(1.06, 1.05, 1.02), saturate(tone));
            float4 h = float4(Luminance(c0), Luminance(c1), Luminance(c2), Luminance(c3));
            float4 wt = IN.color * (0.6 + h);                       // height-aware blend
            wt = pow(max(wt, 1e-4), 2.0); wt /= dot(wt, 1);
            o.Albedo = c0 * wt.x + c1 * wt.y + c2 * wt.z + c3 * wt.w;
            // v15: low-frequency colour breakup over 2-4 m, brightness +-6 % (ChatGPT), no round decals
            float big = sin(w.x * 1.7 + sin(w.y * 1.1) * 1.5) * sin(w.y * 1.9 - w.x * 0.7 + 1.3);
            o.Albedo *= 1.0 + 0.06 * big;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
