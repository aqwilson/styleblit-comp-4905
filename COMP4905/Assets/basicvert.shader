Shader "Custom/basicvert"
{
    SubShader{
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct v2f {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                
                o.color.x = v.vertex.x;
                o.color.y = 0;
                o.color.z = 0;

                //o.color.xyz = v.vertex;

                if (v.vertex.x < 0) {
                    o.color.y = 1;
                }
                
                
                 


                o.color.w = 1.0;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target { return i.color; }
            ENDCG
        }
    }
    //FallBack "Diffuse"
}
