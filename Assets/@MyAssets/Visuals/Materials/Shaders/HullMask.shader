Shader "Custom/HullMask"
{
    SubShader
    {
        // Render AFTER the opaque ship hull
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1" }
        
        ColorMask 0
        ZWrite Off
        ZTest LEqual // Ensures it only works if not hidden behind a wall
        Cull Off 

        Stencil
        {
            Ref 1
            Comp Always
            Pass Replace
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                return half4(0,0,0,0);
            }
            ENDCG
        }
    }
}