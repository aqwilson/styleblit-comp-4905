Shader "Custom/StyleBlitShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        int2 seedPoint(int2 p, float h) {
            float2 b = floor(p/h);
            float2 j = float2(0.01345, 0.962); // get this out of here, we need a jitter table
            // float2 j = RandomJitterTable(b);
            
            return floor(h * (b + j));
        }

        int2 NearestSeed(int2 p, float h) {
            float dPrime = 100000.0f;
            int2 sPrime = {0, 0};
            for (int x=-1; x<=1; x++) {
                for (int y=-1; y<=1; y++) {
                    int2 pos = int2(x, y);
                    int2 s = seedPoint(p + (h * pos), h);
                    float d = length(s - p);
                    if (d < dPrime) {
                        sPrime = s; 
                        dPrime = d;
                    }
                }
            }
            return sPrime;
        }

        float4 ParallelStyleBlit(int2 p) {
            int L = 3;
            float t = 10.0f;
            for (int l = L; l >= 1; l--) {
                int2 ql = NearestSeed(p, pow(2, l));
                int u = 0; // tree search! or lookup of some kind
                
                // int uPrime = argmin u length(GT[ql]-GS[u]); // tree search! or lookup of some kind
                // float e = length(GT[p]-GS[uPrime + (p - ql)]);
                // if e < t then {
                    // return CS[uPrime + (p - ql)]
                    // break;
                // }
            }
            return float4(1, 1, 1, 1);
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;

            
        }
        ENDCG
    }
    FallBack "Diffuse"
}
