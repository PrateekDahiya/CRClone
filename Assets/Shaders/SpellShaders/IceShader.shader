Shader "CRClone/SpellShaders/IceShader"
{
    Properties
    {
        _MainTex ("Ice Texture", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _Color ("Ice Color", Color) = (0.5, 0.8, 1, 0.6)
        _RimColor ("Rim Color", Color) = (0.8, 1, 1, 1)
        _RefractionStrength ("Refraction", Float) = 0.05
        _RefractionScale ("Refraction Scale", Float) = 1.0
        _RefractionSpeed ("Refraction Speed", Float) = 1.0
        _CrackAmount ("Crack Amount", Range(0,1)) = 0
        _CrackColor ("Crack Color", Color) = (0.2, 0.5, 0.8, 1)
        _ShatterProgress ("Shatter Progress", Range(0,1)) = 0
        _FresnelPower ("Fresnel Power", Float) = 3.0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        GrabPass { "_BackgroundTex" }

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
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldNormal : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
                float4 grabPos : TEXCOORD3;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _NormalMap;
            float4 _NormalMap_ST;
            sampler2D _BackgroundTex;
            float4 _Color;
            float4 _RimColor;
            float _RefractionStrength;
            float _RefractionScale;
            float _RefractionSpeed;
            float _CrackAmount;
            float4 _CrackColor;
            float _ShatterProgress;
            float _FresnelPower;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(UnityWorldSpaceViewDir(mul(unity_ObjectToWorld, v.vertex).xyz));
                o.grabPos = ComputeGrabScreenPos(o.vertex);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                
                // Refraction distortion
                float2 normalMap = tex2D(_NormalMap, uv * _NormalMap_ST.xy + float2(_Time.y * _RefractionSpeed, 0)).rg * 2.0 - 1.0;
                float2 distort = normalMap * _RefractionStrength * _RefractionScale;
                
                float2 grabUV = i.grabPos.xy / i.grabPos.w + distort;
                float4 background = tex2Dproj(_BackgroundTex, float4(grabUV, 0, i.grabPos.w));
                
                // Ice base color
                float4 iceTex = tex2D(_MainTex, uv);
                float4 color = iceTex * _Color;
                
                // Fresnel rim lighting
                float fresnel = pow(1.0 - abs(dot(i.worldNormal, i.viewDir)), _FresnelPower);
                color.rgb += _RimColor.rgb * fresnel * _RimColor.a;
                
                // Cracks
                if (_CrackAmount > 0)
                {
                    float cracks = step(1.0 - _CrackAmount, iceTex.r);
                    color.rgb = lerp(color.rgb, _CrackColor.rgb, cracks * _CrackColor.a);
                    color.a = lerp(color.a, 1.0, cracks * 0.5);
                }
                
                // Shatter effect
                if (_ShatterProgress > 0)
                {
                    float shatter = smoothstep(0.5, 1.0, _ShatterProgress);
                    color.a *= 1.0 - shatter;
                    color.rgb *= 1.0 - shatter * 0.5;
                }
                
                // Combine with refracted background
                color.rgb = lerp(background.rgb, color.rgb, color.a);
                
                return color;
            }
            ENDCG
        }
    }

    FallBack "Transparent/Diffuse"
}