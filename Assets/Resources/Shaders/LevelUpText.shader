// World TextMesh "LEVEL UP" (LevelUpWordFx, 22/11): glyph coverage from the font atlas alpha, no painted image.
//  _Grad 1: vertical gold gradient in object space (_Bottom at y = _Y0 -> _Top at y = _Y1) plus a light sweep (_Shine = sweep x in object units, _ShineW wide).
//  _Grad 0: flat vertex colour (outline / glow copies).
// Blend from _SrcBlend / _DstBlend: alpha blend for the fill and the dark rim, additive (SrcAlpha One) for the glow copies.
// ZTest Always: trees / terrain of the 3D map never cut the word.
Shader "ZenoniaProto/LevelUpText"
{
    Properties
    {
        _MainTex ("Font Texture", 2D) = "white" {}
        _Top ("Top", Color) = (1, 0.98, 0.88, 1)
        _Bottom ("Bottom", Color) = (0.95, 0.72, 0.28, 1)
        _Y0 ("Gradient bottom y", Float) = -1
        _Y1 ("Gradient top y", Float) = 1
        _Grad ("Gradient on", Float) = 1
        _Shine ("Shine x", Float) = -100
        _ShineW ("Shine width", Float) = 3
        _Intensity ("Intensity", Float) = 1
        _Alpha ("Alpha", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst", Float) = 10
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend [_SrcBlend] [_DstBlend]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Top, _Bottom;
            float _Y0, _Y1, _Grad, _Shine, _ShineW, _Intensity, _Alpha;

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float2 obj : TEXCOORD1; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                o.obj = v.vertex.xy;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half a = tex2D(_MainTex, i.uv).a * i.color.a * _Alpha;
                half3 rgb = i.color.rgb;
                if (_Grad > 0.5)
                {
                    half t = saturate((i.obj.y - _Y0) / max(0.001, _Y1 - _Y0));
                    rgb = lerp(_Bottom.rgb, _Top.rgb, t * t * (3 - 2 * t));
                    // slanted light sweep across the word
                    half d = abs(i.obj.x - _Shine - (i.obj.y - _Y0) * 0.6) / max(0.001, _ShineW);
                    rgb += (1 - saturate(d)) * half3(0.55, 0.5, 0.35);
                }
                return half4(rgb * _Intensity, a);
            }
            ENDCG
        }
    }
}
