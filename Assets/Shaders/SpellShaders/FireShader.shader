Shader "CRClone/SpellShaders/FireShader"
{
    Properties
    {
        _MainTex ("Flame Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _Color ("Flame Color", Color) = (1, 0.4, 0.1, 1)
        _EmissiveColor ("Emissive Color", Color) = (1, 0.6, 0, 1)
        _Intensity ("Intensity", Float) = 2.0
        _FlickerSpeed ("Flicker Speed", Float) = 10.0
        _FlickerAmount ("Flicker Amount", Float) = 0.3
        _RiseSpeed ("Rise Speed", Float) = 1.0
        _DissolveAmount ("Dissolve", Range(0,1)) = 0
        _Additive ("Additive Blending", Float) = 1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200
        Blend One OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float3 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _NoiseTex;
            float4 _NoiseTex_ST;
            float4 _Color;
            float4 _EmissiveColor;
            float _Intensity;
            float _FlickerSpeed;
            float _FlickerAmount;
            float _RiseSpeed;
            float _DissolveAmount;
            float _Additive;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                
                // Rising flame effect
                uv.y += _Time.y * _RiseSpeed * 0.5;
                
                // Flicker noise
                float flicker = sin(_Time.y * _FlickerSpeed + i.worldPos.x * 5.0) * 0.5 + 0.5;
                flicker = lerp(1.0, flicker, _FlickerAmount);
                
                // Sample flame texture
                float4 flame = tex2D(_MainTex, uv);
                
                // Add noise for turbulence
                float2 noiseUV = uv * _NoiseTex_ST.xy + float2(_Time.y * 0.5, _Time.y * 0.3);
                float noise = tex2D(_NoiseTex, noiseUV).r;
                flame.rgb *= lerp(1.0, noise, 0.5);
                
                // Color with intensity
                float4 color = flame * i.color * _Color * _Intensity * flicker;
                
                // Emissive glow
                color.rgb += _EmissiveColor.rgb * flame.a * _Intensity * 0.5;
                
                // Dissolve
                if (_DissolveAmount > 0)
                {
                    float dissolveNoise = frac(sin(dot(i.worldPos.xz, float2(12.9898, 78.233))) * 43758.5453);
                    clip(dissolveNoise - _DissolveAmount);
                }
                
                // Additive blending control
                color.a *= _Additive;
                
                return color;
            }
            ENDCG
        }
    }

    FallBack "Particles/Additive"
}