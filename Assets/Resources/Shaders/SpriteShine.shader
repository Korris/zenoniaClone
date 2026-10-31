// Additive light band scrolling over a held item (Built-in pipeline), drawn from the mask HeldGlowBaker bakes: R = how much light the pixel takes,
// G = position along the weapon (0..1), A = silhouette, so the light can never leave the art. The renderer colour tints it (rgb) and sets its strength (a).
// _Shine (per renderer, see FeelMaterials.SetShine): x = band centre, y = band half width, z = constant lift of the whole surface.
Shader "ZenoniaProto/SpriteShine"
{
    Properties
    {
        [PerRendererData] _MainTex ("Shine Mask", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Shine ("Band centre, half width, lift", Vector) = (-2,0.1,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _Shine;

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 m = tex2D(_MainTex, i.uv);
                half band = saturate(1.0 - abs(m.g - _Shine.x) / _Shine.y);
                band = band * band * (3.0 - 2.0 * band);        // smooth falloff, no visible band edge
                return half4(i.color.rgb * m.r * (band + _Shine.z), i.color.a * m.a);
            }
            ENDCG
        }
    }
}
