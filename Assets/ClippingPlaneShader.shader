Shader "Custom/TransparentOutlined"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,0.3)
        _OutlineColor ("Outline Color", Color) = (1,1,0,1)
        _OutlineWidth ("Outline Width", Float) = 0.02
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200

        Cull Back
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        // Base transparent pass
        Pass
        {
            Name "BASE"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _Color;
            }
            ENDCG
        }

        // Outline pass
        Pass
        {
            Name "OUTLINE"
            Cull Front
            ZWrite Off
            ZTest Less
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _OutlineColor;
            float _OutlineWidth;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                float3 norm = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                float3 offset = norm * _OutlineWidth;
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex + float4(offset, 0));
                v2f o;
                o.pos = UnityObjectToClipPos(worldPos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
    }
    FallBack "Transparent/Diffuse"
}
