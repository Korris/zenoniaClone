// Screen pass of the 2.5D prototype (View25DAtmosphere, OnRenderImage before the UGUI HUD): distance haze toward the top of the screen (the far
// side of the tilted view), a soft warm-to-cool light gradient from the sun corner, and a gentle vignette. Built-in pipeline, one pass.
Shader "ZenoniaProto/View25DAtmosphere"
{
    Properties
    {
        _MainTex ("Frame", 2D) = "white" {}
        _HazeColor ("Haze colour", Color) = (0.62, 0.80, 0.82, 1)
        _HazeStrength ("Haze at the top edge", Float) = 0.1
        _HazeStart ("Haze starts (screen v)", Float) = 0.5
        _WarmColor ("Sun-side tint", Color) = (1.05, 1.02, 0.95, 1)
        _CoolColor ("Shade-side tint", Color) = (0.95, 0.97, 1.02, 1)
        _SunPos ("Sun corner (uv)", Vector) = (0.05, 1.0, 0, 0)
        _Vignette ("Vignette", Float) = 0.08
        _VignetteColor ("Vignette colour (dark dungeon: deep violet)", Color) = (0, 0, 0, 1)
        _Band ("Far-edge haze band strength", Float) = 0.55
        _Aspect ("Aspect", Float) = 1.78
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _HazeColor, _WarmColor, _CoolColor, _SunPos, _VignetteColor;
            float _HazeStrength, _HazeStart, _Vignette, _Aspect, _Band;

            half4 frag(v2f_img i) : SV_Target
            {
                half4 c = tex2D(_MainTex, i.uv);
                float2 uv = i.uv;
                // light gradient: warm near the sun corner, slightly cool across the frame
                float sun = saturate(1.0 - distance(uv * float2(_Aspect, 1), _SunPos.xy * float2(_Aspect, 1)) / (_Aspect * 0.95));
                c.rgb *= lerp(_CoolColor.rgb, _WarmColor.rgb, sun);
                // distance haze: the top of the screen is the far side of the ground
                float h = smoothstep(_HazeStart, 1.0, uv.y);
                // + a thicker band in the top ~12 %: where the far edge may run past the map border (View25DCamera.TopOvershootV)
                float band = smoothstep(0.84, 1.0, uv.y);
                c.rgb = lerp(c.rgb, _HazeColor.rgb, saturate(h * _HazeStrength + band * band * _Band));
                // vignette
                float2 p = (uv - 0.5) * float2(_Aspect / 1.78, 1.0);
                float v = smoothstep(0.42, 0.85, length(p) * 1.25);
                c.rgb = lerp(c.rgb, c.rgb * _VignetteColor.rgb, v * _Vignette); // black = the old plain darkening
                return c;
            }
            ENDCG
        }
    }
}
