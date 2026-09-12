Shader "CRClone/SpellShaders/PoisonShader"
{
    Properties
    {
        _MainTex ("Gas Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _Color ("Gas Color", Color) = (0.3, 0.7, 0.2, 0.5)
        _GlowColor ("Glow Color", Color) = (0.5, 1, 0.3, 0.3)
        _Intensity ("Intensity", Float) = 1.0
        _SwirlSpeed ("Swirl Speed", Float) = 0.5
        _SwirlAmount ("Swirl Amount", Float) = 0.3
        _RiseSpeed ("Rise Speed", Float) = 0.2
        _ExpandSpeed ("Expand Speed", Float) = 0.1
        _DissolveSpeed ("Dissolve Speed", Float) = 0.3
        _PulseSpeed ("Pulse Speed", Float) = 2.0
        _DamagePulse ("Damage Pulse Sync", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
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
            float4 _GlowColor;
            float _Intensity;
            float _SwirlSpeed;
            float _SwirlAmount;
            float _RiseSpeed;
            float _ExpandSpeed;
            float _DissolveSpeed;
            float _PulseSpeed;
            float _DamagePulse;

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
                
                // Rising gas
                uv.y += _Time.y * _RiseSpeed;
                
                // Expanding radius
                float radius = 1.0 + _Time.y * _ExpandSpeed;
                uv = (uv - 0.5) * radius + 0.5;
                
                // Swirl distortion
                float2 center = float2(0.5, 0.5);
                float2 toCenter = uv - center;
                float dist = length(toCenter);
                float angle = atan2(toCenter.y, toCenter.x);
                angle += sin(dist * 10.0 - _Time.y * _SwirlSpeed) * _SwirlAmount;
                uv = center + float2(cos(angle), sin(angle)) * dist;
                
                // Noise for organic shape
                float2 noiseUV = uv * _NoiseTex_ST.xy + float2(_Time.y * 0.3, _Time.y * 0.2);
                float noise = tex2D(_NoiseTex, noiseUV).r;
                
                // Pulse on damage ticks
                float pulse = sin(_Time.y * _PulseSpeed + _DamagePulse * 6.283) * 0.5 + 0.5;
                
                // Sample gas texture
                float4 gas = tex2D(_MainTex, uv);
                
                // Combine with noise
                gas.a *= noise;
                
                // Dissolve over time
                float dissolve = 1.0 - smoothstep(0.0, 10.0, _Time.y * _DissolveSpeed);
                gas.a *= dissolve;
                
                // Color
                float4 color = gas * i.color * _Color * _Intensity;
                
                // Pulse glow on damage tick
                color.rgb += _GlowColor.rgb * gas.a * pulse * _Intensity * 0.5;
                
                // Edge fade
                float edgeFade = smoothstep(0.9, 1.0, dist * 2.0);
                color.a *= 1.0 - edgeFade;
                
                return color;
            }
            ENDCG
        }
    }

    FallBack "Transparent/Diffuse"
}