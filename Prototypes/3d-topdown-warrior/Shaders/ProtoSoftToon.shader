// Character "Soft Toon" V2.1 (ChatGPT Warrior V2 review): 3 soft light bands, shadow strength 0.78, rim 0.10 over a 0.35 width, no black outline,
// saturation +3 %. Material groups come from the Tripo metallic/roughness map: metal (armour / gold trims) gets a tighter, brighter
// highlight (smoothness 0.40); skin / hair / cloth stays matte (0.15). Built-in RP surface shader with a custom lighting function.
Shader "Proto/SoftToon"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        [Normal] _BumpMap ("Normal", 2D) = "bump" {}
        _MetalMap ("Metallic (R) / Roughness (G)", 2D) = "black" {}
        _ShadowStrength ("Shadow strength", Range(0,1)) = 0.78
        _ShadowTint ("Shadow tint", Color) = (0.79, 0.75, 0.84, 1)   // V2.1: 30 % less purple
        _RimColor ("Rim", Color) = (1, 0.95, 0.85, 1)
        _RimAmount ("Rim amount", Range(0,0.5)) = 0.10
        _RimWidth ("Rim width", Range(0.05,1)) = 0.35
        _Saturation ("Saturation", Range(0.8,1.3)) = 1.03
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf SoftToon fullforwardshadows
        #pragma target 3.0
        sampler2D _MainTex, _BumpMap, _MetalMap;
        fixed4 _Color, _ShadowTint, _RimColor;
        half _ShadowStrength, _RimAmount, _RimWidth, _Saturation;

        struct SurfaceOutputToon { fixed3 Albedo; fixed3 Normal; fixed3 Emission; half Specular; fixed Gloss; fixed Alpha; half Metal; };

        // 3 bands with soft steps: lit 1.0, mid 0.82, shadow (1 - strength); the shadow side is tinted cool so it never goes grey
        half4 LightingSoftToon(SurfaceOutputToon s, half3 lightDir, half3 viewDir, half atten)
        {
            half nl = dot(s.Normal, lightDir) * atten;
            half band = lerp(1.0 - _ShadowStrength, 0.82, smoothstep(-0.05, 0.08, nl));
            band = lerp(band, 1.0, smoothstep(0.45, 0.6, nl));
            fixed3 shade = lerp(_ShadowTint.rgb, 1.0, saturate((band - (1.0 - _ShadowStrength)) / _ShadowStrength));
            half3 h = normalize(lightDir + viewDir);
            half smooth = lerp(0.15, 0.40, s.Metal);                         // V2.1: skin / hair / cloth 0.15, armour 0.40
            half spec = smoothstep(0.5, 0.56, pow(saturate(dot(s.Normal, h)), exp2(smooth * 10 + 1))) * smooth * 0.6 * atten;
            half4 c;
            c.rgb = s.Albedo * _LightColor0.rgb * band * shade + _LightColor0.rgb * spec;
            c.a = s.Alpha;
            return c;
        }

        struct Input { float2 uv_MainTex; float3 viewDir; };

        void surf(Input IN, inout SurfaceOutputToon o)
        {
            fixed3 c = tex2D(_MainTex, IN.uv_MainTex).rgb * _Color.rgb;
            c = lerp(Luminance(c).xxx, c, _Saturation);
            o.Albedo = c;
            o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
            o.Metal = saturate(tex2D(_MetalMap, IN.uv_MainTex).r * 1.5);
            o.Albedo = lerp(o.Albedo, o.Albedo * 0.92, o.Metal * 0.6);   // V2.1: gold trims a touch less loud (metallic ~0.2)
            half rim = smoothstep(1.0 - _RimWidth, 1.0, 1.0 - saturate(dot(normalize(IN.viewDir), o.Normal)));
            o.Emission = _RimColor.rgb * rim * _RimAmount;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
