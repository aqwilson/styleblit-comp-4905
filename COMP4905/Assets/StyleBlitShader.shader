// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/StyleBlitShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _SampleTex("THING", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.5
        // _JitterTable ("JitterTable", Vector4[])
    }
    SubShader
    {


        Pass
        {
            CGPROGRAM
            // Upgrade NOTE: excluded shader from DX11; has structs without semantics (struct v2f members vertex)
            #pragma exclude_renderers d3d11
            // Upgrade NOTE: excluded shader from DX11 because it uses wrong array syntax (type[size] name)
            // #pragma exclude_renderers d3d11
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0
            #include "UnityCG.cginc"

            float4 _Color;
            sampler3D _JitterTable;
            sampler2D _normals;
            sampler3D _UVLut;
            sampler2D _BlitTex;
            sampler2D _SphereNormalMap;
            sampler3D _RabbitMap;
            float4 _Vertices[500];
            // shared static half3 _myNormals[100][100];

            // note: no SV_POSITION in this struct
            struct v2f {
                float2 uv : TEXCOORD0;
                half3 worldNormal : TEXCOORD1;
                float3 n : NORMAL;
                float3 worldPos : TEXCOORD3;
                float4 position : SV_POSITION;
                float2 normalizedPosition : TEXCOORD2;
            };

            v2f vert (
                float4 vertex : POSITION, // vertex position input
                float2 uv : TEXCOORD0, // texture coordinate input
                float3 n : NORMAL
                // out float4 outpos : SV_POSITION // clip space position output
                // out float3 ndcCoord : 
                )
            {
                v2f o;
                o.uv = uv;

                float3 wp = ((vertex.xyz / length(vertex.xyz)) * 0.5f) + 0.5f;
                o.worldPos = wp;
                
                o.position = UnityObjectToClipPos(vertex);
                o.normalizedPosition = o.position.xy / length(o.position.xy);
                
                o.n = n;
                o.worldNormal = UnityObjectToWorldNormal(n); 

                return o;
            }

            sampler2D _MainTex;

            float2 seedPoint(float2 p, float h) {
                // float2 b = clamp(floor(p * 100/h), 0.f, 99.f) / 100;

                // float2 b = 
                
                // float2 b = floor((p / h) ) / 100.f;
                // float2 j = float2(0.01345, 0.962); // get this out of here, we need a jitter table
                // float4 c = tex2D(_JitterTable, b.xy);
                // float2 j = c.rg;
                // b *= length(p/h);


                float4 c = tex3D(_JitterTable, float3(p.xy, h));
                
                return c.xy;
                // return floor(h * (b + j));
            }

            float2 NearestSeed(float2 p, float h) {
                float dPrime = 100000.0f;
                float2 sPrime = float2(0.f,0.f);
                for (float x=-0.05f; x<=0.05f; x+=0.05f) {
                    for (float y=-0.05f; y<=0.05f; y+=0.05f) {
                        float2 pos = float2(p.x + x , p.y + y);
                        float2 s = seedPoint(p, h);
                        float d = length(s - p);
                        if (d < dPrime) {
                            sPrime = s; 
                            dPrime = d;
                        }
                    }
                }
                return sPrime;
            }

            float4 ParallelStyleBlit(v2f i) {
                // return (tex2D(_SphereNormalMap, i.worldPos.xy));
                // return tex3D(_UVLut, i.worldPos.xyz);
                float2 p = i.worldPos.xy;
                int L = 3;
                float t = 10.0f;
                // return tex3D(_JitterTable, float3(p.xy, 0));
                // return float4(0, 0, 0, 1);
                for (int l = 1; l <= L; l++) {
                    float h = 0.f;

                    if (l == 2) {
                        h = 0.4f;
                    } else if (l == 3) {
                        h = 0.8f;
                    }

                    float2 ql = NearestSeed(p, h);
                    // return float4(ql.xy, 0, 1);
                    int u = 0; // tree search! or lookup of some kind

                    float2 normalizedQL = ql;

                    // return float4(normalizedQL.xy, 0, 1);
                    // return float4(1, 1, 1 ,1);
                    // float2 normalizedQL = ql / length(ql);

                    // return float4(normalizedQL.xy, 0, 1);

                    // return tex2D(_RabbitMap, normalizedQL.xy);

                    // return float4(_myNormals[70][70].xyz, 1);

                    // float2 uPrime = tex3D(_UVLut, (_myNormals[normalizedQL.x * 100][normalizedQL.y * 100].xyz - 0.5f) * 2.f).xy;
                    
                    // return 

                    // return float4(1, 1, 1, 1);
                    // return float4(_myNormals[normalizedQL.x * 100][normalizedQL.y * 100].xyz + 0.5, 1);

                    // return uPrime

                    // return float4(uPrime.xy, 0, 1);
                    // return uPrime;
                    // return float4((uPrime * -0.5f) * 2.f, 0, 1);
                    // return float4(uPrime.xy, 0, 1);
                    // float2 uPrime = tex3D(_UVLut, (tex2D(_RabbitMap, ql.xy).xyz)); 

                    // return tex2D(_RabbitMap, p);

                    // return tex2D(_SphereNormalMap, p * 10);

                    // return tex3D(_UVLut, float3(p.x, p.y, 0.2));

                    // return tex3D(_UVLut, tex3D(_RabbitMap, float3(p.xy, h)).xyz);

                    float3 targetGuideAtP = tex3D(_UVLut, tex3D(_RabbitMap, float3(p.xy, h)).xyz).xyz;
                    
                    float3 targetGuideAtQ = tex3D(_UVLut, tex3D(_RabbitMap, float3(normalizedQL.xy, h)).xyz).xyz;

                    // return float4(targetGuideAtQ, 1);

                    float2 uPrime = tex3D(_UVLut, targetGuideAtQ).xy; // CONFIRM THAT THIS IS RIGHT

                    // return tex3D(_UVLut, targetGuideAtQ);

                    float3 sourceGuideAtUPrimePQ = tex2D(_SphereNormalMap, uPrime + (p - normalizedQL)).xyz;

                    // return tex2D(_SphereNormalMap, uPrime + (p - normalizedQL));

                    float e = length(targetGuideAtP - sourceGuideAtUPrimePQ);

                    if (e < 1.2f) {
                        return tex2D(_BlitTex, uPrime + (p - normalizedQL));
                    }

                    // targetGuid = _UVLut 

                    // ql = NearestSeed -> normalizedQL
                    // uPrime = || target[ql] - src[u] || -> basically, where the minimum length of targetGuide - sourceGuide is
                    // e = || target[p] - src[uPrime + (p - ql)]

                    // if (e < t) {
                    //     targetColor[p] = sourceColor[uPrime + (p - ql)]
                    // }


                    // float3 test = (tex2D(_SphereNormalMap, clamp((uPrime + (p - normalizedQL)).xy, 0, 1))).xyz;  
                    // return float4(test, 1);
                    // float3 test = 2.f * (tex2D(_SphereNormalMap, (uPrime + ((p / length(p)) - normalizedQL))).xyz - 0.5);

                    // return float4(test.xyz, 1);

                    // float3 test = tex3D(_UVLut, (uPrime + (p - ql)));

                    // float e = length(((i.worldNormal + 0.5f) + 0.5f) - test.xyz);

                    // float e = length(pxNorm - tex3D(_UVLut, (uPrime + (p - ql))).rgb);
                    // if (e <= 1.2f) {
                    //     return tex2D(_BlitTex, clamp(uPrime + (p - normalizedQL), 0, 1));
                    // }
                    // int uPrime = argmin u length(GT[ql]-GS[u]); // tree search! or lookup of some kind
                    // float e = length(GT[p]-GS[uPrime + (p - ql)]);
                    // if e < t then {
                        // return CS[uPrime + (p - ql)]
                        // break;
                    // }
                }
                return float4(0, 0, 0, 1);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // return fixed4(1, 1, 1, 1);
                return ParallelStyleBlit(i);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
