Shader "CRClone/SpellShaders/LightningShader"
{
    Properties
    {
        _MainTex ("Lightning Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _Color ("Lightning Color", Color) = (0.5, 0.8, 1, 1)
        _GlowColor ("Glow Color", Color) = (0.2, 0.6, 1, 1)
        _Intensity ("Intensity", Float) = 3.0
        _JitterAmount ("Jitter Amount", Float) = 0.1
        _JitterSpeed ("Jitter Speed", Float) = 30.0
        _BranchAmount ("Branch Amount", Float) = 0.3
        _BranchSpeed ("Branch Speed", Float) = 15.0
        _Thickness ("Bolt Thickness", Float) = 0.05
        _PulseSpeed ("Pulse Speed", Float) = 5.0
        _Additive ("Additive Blending", Float) = 1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200
        Blend One One
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
            float _JitterAmount;
            float _JitterSpeed;
            float _BranchAmount;
            float _BranchSpeed;
            float _Thickness;
            float _PulseSpeed;
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
                
                // Pulse effect
                float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                
                // Jitter
                float jitter = sin(_Time.y * _JitterSpeed + i.worldPos.y * 10.0) * _JitterAmount;
                uv.x += jitter;
                
                // Branching noise
                float2 noiseUV = uv * _NoiseTex_ST.xy + float2(_Time.y * _BranchSpeed, 0);
                float branchNoise = tex2D(_NoiseTex, noiseUV).r;
                float branch = step(1.0 - _BranchAmount, branchNoise);
                
                // Sample lightning texture
                float4 bolt = tex2D(_MainTex, uv);
                
                // Apply branch mask
                bolt.a *= lerp(1.0, 0.3, branch);
                
                // Thickness variation
                float thickness = _Thickness * (1.0 + pulse * 0.5);
                bolt.a *= smoothstep(0.5 - thickness, 0.5 + thickness, uv.x);
                
                // Color with intensity
                float4 color = bolt * i.color * _Color * _Intensity;
                
                // Glow
                color.rgb += _GlowColor.rgb * bolt.a * _Intensity * 0.5 * pulse;
                
                // Additive
                color.a *= _Additive;
                
                return color;
            }
            ENDCG
        }
    }

    FallBack "Particles/Additive"
}