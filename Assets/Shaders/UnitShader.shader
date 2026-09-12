Shader "CRClone/UnitShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color Tint", Color) = (1,1,1,1)
        _TeamColor ("Team Color", Color) = (0,0.5,1,1) // Blue team default
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineWidth ("Outline Width", Float) = 0.05
        _DissolveAmount ("Dissolve Amount", Range(0,1)) = 0
        _DissolveEdgeColor ("Dissolve Edge Color", Color) = (1,0.5,0,1)
        _DissolveEdgeWidth ("Dissolve Edge Width", Float) = 0.02
        _HoverHighlight ("Hover Highlight", Float) = 0
        _HoverColor ("Hover Color", Color) = (1,1,0,0.3)
        _HitFlash ("Hit Flash Amount", Float) = 0
        _HitColor ("Hit Flash Color", Color) = (1,1,1,1)
        _SelectionRing ("Show Selection Ring", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ SPINE_SKELETON
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
                float4 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _TeamColor;
            float4 _OutlineColor;
            float _OutlineWidth;
            float _DissolveAmount;
            float4 _DissolveEdgeColor;
            float _DissolveEdgeWidth;
            float _HoverHighlight;
            float4 _HoverColor;
            float _HitFlash;
            float4 _HitColor;
            float _SelectionRing;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                fixed4 color = tex * i.color * _Color;

                // Team color tint (multiply with vertex color for Spine)
                color.rgb = lerp(color.rgb, color.rgb * _TeamColor.rgb, _TeamColor.a);

                // Hit flash
                if (_HitFlash > 0)
                {
                    color.rgb = lerp(color.rgb, _HitColor.rgb, _HitFlash);
                }

                // Hover highlight
                if (_HoverHighlight > 0)
                {
                    color.rgb = lerp(color.rgb, _HoverColor.rgb, _HoverHighlight * _HoverColor.a);
                }

                // Dissolve effect
                if (_DissolveAmount > 0)
                {
                    float noise = frac(sin(dot(i.worldPos.xy, float2(12.9898, 78.233))) * 43758.5453);
                    float dissolve = step(noise, _DissolveAmount);
                    
                    // Edge glow
                    float edge = step(noise, _DissolveAmount + _DissolveEdgeWidth) - dissolve;
                    color.rgb = lerp(color.rgb, _DissolveEdgeColor.rgb, edge * _DissolveEdgeColor.a);
                    
                    // Discard dissolved pixels
                    clip(dissolve - 0.5);
                }

                // Outline (using screen-space derivative approximation)
                if (_OutlineWidth > 0)
                {
                    float2 texelSize = fwidth(i.uv);
                    float alpha = tex.a;
                    float outline = smoothstep(0.5 - _OutlineWidth, 0.5 + _OutlineWidth, alpha);
                    color.rgb = lerp(color.rgb, _OutlineColor.rgb, outline * _OutlineColor.a);
                    color.a = max(color.a, outline * _OutlineColor.a);
                }

                // Selection ring (rendered separately via LineRenderer, but can tint here)
                if (_SelectionRing > 0)
                {
                    color.rgb += _OutlineColor.rgb * _SelectionRing * 0.3;
                }

                return color;
            }
            ENDCG
        }

        // Outline Pass (render behind main pass)
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "ForwardBase" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Front

            CGPROGRAM
            #pragma vertex vert_outline
            #pragma fragment frag_outline
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
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _OutlineWidth;
            float4 _OutlineColor;

            v2f vert_outline(appdata v)
            {
                v2f o;
                float3 normal = normalize(mul((float3x3)unity_ObjectToWorld, float3(0,0,1)));
                o.vertex = UnityObjectToClipPos(v.vertex + float4(normal * _OutlineWidth, 0));
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag_outline(v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
    }

    FallBack "Sprites/Default"
}