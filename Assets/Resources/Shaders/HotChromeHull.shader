// Exploration (explore/hot-chrome): inverted-hull outline. Back faces pushed out along the world normal and drawn
// in one flat colour, so the hero objects (vacuum, cat) get a thick rim behind their own surface.
Shader "VCS/HotChromeHull"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.18, 0.54, 1)
        _Width ("Width (m)", Float) = 0.02
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        Pass
        {
            Cull Front
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Color;
            float _Width;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };

            float4 vert(appdata v) : SV_POSITION
            {
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                wp += UnityObjectToWorldNormal(v.normal) * _Width;
                return mul(UNITY_MATRIX_VP, float4(wp, 1.0));
            }

            fixed4 frag() : SV_Target { return _Color; }
            ENDCG
        }
    }
}
