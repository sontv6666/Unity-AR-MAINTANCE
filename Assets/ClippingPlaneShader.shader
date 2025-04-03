Shader "Custom/ClippingPlaneShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {} // ✅ Texture property
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _ClipPlane;  // ✅ Define it here, NOT in Properties!

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz; // ✅ Convert to world space
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // ✅ Correct clipping logic
                if (dot(i.worldPos, _ClipPlane.xyz) - _ClipPlane.w < 0) 
                {
                    discard; // Remove pixels outside the clipping plane
                }

                return tex2D(_MainTex, i.uv);
            }

            ENDCG
        }
    }
    FallBack "Diffuse"
}
