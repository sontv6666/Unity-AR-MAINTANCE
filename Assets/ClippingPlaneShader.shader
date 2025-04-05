Shader "Custom/TransparentWithOutline"
{
    Properties
    {
        _Color ("Main Color", Color) = (1, 1, 1, 1)  // Main color
        _OutlineColor ("Outline Color", Color) = (1, 1, 0, 1) // Outline color
        _OutlineWidth ("Outline Width", Range(0.002, 0.02)) = 0.01 // Outline width
    }

    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }

        // Outline Pass
        Pass
        {
            Tags { "LightMode"="Always" }

            // Enable alpha blending for transparency
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off // Don't write to depth buffer for transparency
            ZTest LEqual // Use the 'Less Equal' test for transparency

            // The main outline rendering
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                float3 normal : NORMAL;
            };

            float4 _Color;
            float4 _OutlineColor;
            float _OutlineWidth;
            float4 _ClipPlane; // ClipPlane is a Vector4 passed from C# or through properties

            // Vertex shader to scale the vertices along their normals to create the outline effect
            v2f vert(appdata_t v)
            {
                v2f o;

                // Scale the vertices along their normals to create the outline effect
                float3 outlineOffset = v.normal * _OutlineWidth;

                // Adjust the position of the vertex to create the outline effect
                o.pos = UnityObjectToClipPos(v.vertex + float4(outlineOffset, 0));
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz + outlineOffset; // Update world position
                o.normal = mul((float3x3)unity_ObjectToWorld, v.normal); // Keep normal direction correct

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Apply clipping logic based on the defined ClipPlane
                if (dot(i.worldPos, _ClipPlane.xyz) - _ClipPlane.w < 0)
                {
                    discard; // Clip if the fragment is behind the plane
                }

                // Outline effect based on angle between normal and camera position
                float edge = dot(i.normal, normalize(i.worldPos - _WorldSpaceCameraPos));
                edge = smoothstep(0.2, 0.5, edge);

                // Use the outline color with adjusted alpha
                fixed4 color = _OutlineColor;
                color.a *= smoothstep(0.0, 1.0, edge); // Adjust alpha based on outline edge

                return color;
            }
            ENDCG
        }
    }

    // Fallback if not supported
    Fallback "Diffuse"
}
