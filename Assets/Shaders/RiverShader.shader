Shader "CRClone/RiverShader"
{
    Properties
    {
        _MainTex ("Water Texture", 2D) = "white" {}
        _FlowTex ("Flow Noise", 2D) = "white" {}
        _FoamTex ("Foam Texture", 2D) = "white" {}
        _Color ("Water Color", Color) = (0.1, 0.3, 0.6, 0.8)
        _ShallowColor ("Shallow Color", Color) = (0.2, 0.5, 0.8, 0.6)
        _DeepColor ("Deep Color", Color) = (0.05, 0.15, 0.4, 0.9)
        _FlowSpeed ("Flow Speed", Float) = 1.0
        _FlowDirection ("Flow Direction", Vector) = (1, 0, 0, 0)
        _WaveScale ("Wave Scale", Float) = 10.0
        _WaveSpeed ("Wave Speed", Float) = 2.0
        _WaveHeight ("Wave Height", Float) = 0.05
        _FoamStrength ("Foam Strength", Float) = 1.0
        _FoamThreshold ("Foam Threshold", Range(0,1)) = 0.5
        _RefractionStrength ("Refraction Strength", Float) = 0.02
        _RefractionScale ("Refraction Scale", Float) = 1.0
        _RefractionSpeed ("Refraction Speed", Float) = 1.0
        _FresnelPower ("Fresnel Power", Float) = 3.0
        _FresnelIntensity ("Fresnel Intensity", Float) = 0.5
        _DepthTexture ("Depth Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 300
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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float3 viewDir : TEXCOORD3;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _FlowTex;
            float4 _FlowTex_ST;
            sampler2D _FoamTex;
            float4 _FoamTex_ST;
            float4 _Color;
            float4 _ShallowColor;
            float4 _DeepColor;
            float _FlowSpeed;
            float2 _FlowDirection;
            float _WaveScale;
            float _WaveSpeed;
            float _WaveHeight;
            float _FoamStrength;
            float _FoamThreshold;
            float _RefractionStrength;
            float _RefractionScale;
            float _RefractionSpeed;
            float _FresnelPower;
            float _FresnelIntensity;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.screenPos = ComputeScreenPos(o.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewDir = normalize(UnityWorldSpaceViewDir(o.worldPos));
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                // Animated UV for flow
                float2 flowUV = i.uv * _FlowTex_ST.xy + _FlowDirection * _Time.y * _FlowSpeed;
                float2 waveUV = i.uv * _WaveScale + float2(_Time.y * _WaveSpeed, 0);
                
                // Sample flow noise
                float4 flowNoise = tex2D(_FlowTex, flowUV);
                float flow = flowNoise.r;
                
                // Wave distortion
                float wave = sin(waveUV.x + flow * 3.14159) * _WaveHeight;
                
                // Depth-based color (shallow vs deep)
                float depth = 0.5 + flow * 0.5; // Simplified depth from flow
                float4 waterColor = lerp(_ShallowColor, _DeepColor, depth);
                waterColor = lerp(waterColor, _Color, 0.5);
                
                // Foam at edges and high flow areas
                float foam = smoothstep(_FoamThreshold, 1.0, flow) * _FoamStrength;
                float4 foamColor = tex2D(_FoamTex, i.uv * _FoamTex_ST.xy + float2(_Time.y * 0.5, 0));
                foamColor.a *= foam;
                
                // Fresnel effect
                float fresnel = pow(1.0 - abs(dot(normalize(float3(0, 1, 0)), i.viewDir)), _FresnelPower) * _FresnelIntensity;
                
                // Combine colors
                float4 finalColor = waterColor;
                finalColor.rgb += foamColor.rgb * foamColor.a;
                finalColor.rgb += fresnel;
                finalColor.a = waterColor.a * (1.0 - foam * 0.5) + foamColor.a;
                
                // Refraction (grab pass would be needed for true refraction)
                // This is a screen-space approximation
                #if defined(UNITY_PASS_FORWARDBASE) || defined(UNITY_PASS_FORWARDADD)
                // Refraction handled via GrabPass in separate pass
                #endif
                
                return finalColor;
            }
            ENDCG
        }

        // GrabPass for refraction
        GrabPass { "_RefractionTex" }

        Pass
        {
            Name "Refraction"
            Tags { "LightMode" = "ForwardBase" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 grabPos : TEXCOORD1;
            };

            sampler2D _RefractionTex;
            float _RefractionStrength;
            float _RefractionScale;
            float _RefractionSpeed;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.grabPos = ComputeGrabScreenPos(o.vertex);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                // Distort grab texture UVs
                float2 distort = float2(
                    sin(i.uv.x * _RefractionScale + _Time.y * _RefractionSpeed) * _RefractionStrength,
                    cos(i.uv.y * _RefractionScale + _Time.y * _RefractionSpeed) * _RefractionStrength
                );
                
                float2 grabUV = i.grabPos.xy / i.grabPos.w + distort;
                float4 refracted = tex2Dproj(_RefractionTex, float4(grabUV, 0, i.grabPos.w));
                
                return refracted * 0.5; // Subtle refraction
            }
            ENDCG
        }
    }

    FallBack "Transparent/Diffuse"
}