Shader "Hidden/DM2350/RouteMap"
{
    Properties { _Color ("Map colour", Color) = (0.6,0.6,0.6,1) _GroundY ("Walking surface height", Float) = 0 }
    SubShader
    {
        Pass
        {
            Cull Off ZWrite Off ZTest Always
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Color;
            float _GroundY;
            struct v2f { float4 position:SV_POSITION; float height:TEXCOORD0; };
            v2f vert(float4 vertex:POSITION) { v2f o; o.position=UnityObjectToClipPos(vertex); o.height=mul(unity_ObjectToWorld,vertex).y; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                float3 colour=_Color.rgb;
                if(i.height<_GroundY+0.35)colour=lerp(float3(0.10,0.13,0.16),colour,0.25);
                return fixed4(colour,1);
            }
            ENDCG
        }
    }
}
