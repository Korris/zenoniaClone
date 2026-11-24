// Monster foot-label HP bar (Monster.RoFootLabel.cs): a plain sprite (texture x vertex colour) with no depth test, so the 3D terrain of the
// Map3D maps cannot hide the bar under the monster's feet (24/11: the name TextMesh drew, the Sprites-Default bar vanished into the ground).
// Draw order between labels and sprites still comes from the SpriteRenderer sorting order, as on the 2D map.
Shader "Zenonia/RoLabelOverlaySprite"
{
    Properties { [PerRendererData] _MainTex ("Sprite", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "CanUseSpriteAtlas" = "True" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off ZTest Always Cull Off Lighting Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };
            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.color = v.color; o.uv = v.uv; return o; }
            fixed4 frag (v2f i) : SV_Target { return tex2D(_MainTex, i.uv) * i.color; }
            ENDCG
        }
    }
}
