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
            sampler3D _JitterTable; // seed point position map
            sampler3D _TargetNormalMap; // target normal map
            sampler2D _TargetPositionMap; // target position data, for retrieval via UV
            sampler3D _UVLut;
            sampler2D _SourceTexture; // source texture for transfer
            sampler2D _SphereNormalMap; // the source normal map
            sampler2D _SpherePosMap; // the source position map
            float4 _SourceNormals[386];
            float4 _SourcePos[386];
            float4 _SourceUvs[386];

            sampler2D _Level1; // jitter table l1
            sampler2D _Level2; // jitter table l2
            sampler2D _Level3; // jitter table l3

            sampler2D _Norm1; // target normal map l1
            sampler2D _Norm2; // target normal map l2
            sampler2D _Norm3; // target normal map l3

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


            int lookupU(float3 p) {
                float smallest = 10000.f;
                int index = 0;
                for (int i=0; i<386; i++) {
                    if (length(p - _SourceNormals[i].xyz) < smallest) {
                        smallest = length(p - _SourceNormals[i].xyz);
                        index = i;
                    }
                }
                return index;
            }

            int lookupSourceUVByPosition(float3 p) {
                float smallest = 10000.f;
                int index = 0;
                for (int i=0; i<386; i++) {
                    if (length(p - _SourcePos[i].xyz) < smallest) {
                        smallest = length(p - _SourcePos[i].xyz);
                        index = i;
                    }
                }
                return index;
            }


            float4 seedPoint(float2 p, int h) {
                float4 c = float4(0, 0, 0, 0);
                if (h == 1) {
                    c = tex2D(_Level1, p);
                } else if (h == 2) {
                    c = tex2D(_Level2, p);
                } else  { 
                    c = tex2D(_Level3, p);
                }

                // float4 c = tex3D(_JitterTable, float3(p.xy, h));
                return c;
            }

            float2 NearestSeed(float2 p, int h) {
                float3 pixelPos = tex2D(_TargetPositionMap, p).xyz;
                float dPrime = 100000.0f;
                float2 sPrime = float2(0.f,0.f);
                for (float x=-0.01f; x<=0.01f; x+=0.01f) {
                    for (float y=-0.01f; y<=0.01f; y+=0.01f) {
                        float2 pos = float2(p.x + x , p.y + y);
                        float3 s = seedPoint(p, h).xyz;
                        float d = length(s - pixelPos);
                        if (d < dPrime) {
                            sPrime = pos; 
                            dPrime = d;
                        }
                    }
                }
                return sPrime;
            }

            float4 ParallelStyleBlit(v2f i) {
                
                float2 p = i.uv;
                float4 pixelPos = tex2D(_TargetPositionMap, p);
                // return tex3D(_JitterTable, float3(p,0.2f));
                // return tex2D(_Level3, p);
                // return (tex2D(_TargetPositionMap, p));
                // return (tex3D(_TargetNormalMap, float3(p, 0.f)));
                int L = 3;
                for (int l = 1; l <= L; l++) {
                    float h = 0.f;

                    if (l == 2) {
                        h = 0.4f;
                    } else if (l == 3) {
                        h = 0.8f;
                    }

                    float2 ql = NearestSeed(p, l);
                    
                    // return float4(ql.xy, 0, 1);
                    // int u = 0; // tree search! or lookup of some kind

                    // float2 normalizedQL = ql;



                    // float3 targetGuideAtQL = tex3D(_TargetNormalMap, float3(ql, h)).xyz;
                    float3 targetGuideAtQL;
                    if (l == 1) {
                        targetGuideAtQL = tex2D(_Norm1, ql).xyz;
                    } else if (l == 2) {
                        targetGuideAtQL = tex2D(_Norm2, ql).xyz;
                    } else {
                        targetGuideAtQL = tex2D(_Norm3, ql).xyz;
                    }

                    float3 targetPosAtQL = tex2D(_TargetPositionMap, ql).xyz;

                    int uIndex = lookupU(targetGuideAtQL); 

                    float2 uStar = _SourceUvs[uIndex].xy;
                    float3 uSrcNorm = _SourceNormals[uIndex].xyz;
                    float3 uSrcPos = _SourcePos[uIndex].xyz;

                    // float3 targetGuideAtP = tex3D(_TargetNormalMap, float3(p, h));
                    float3 targetGuideAtP;
                    if (l == 1) {
                        targetGuideAtP = tex2D(_Norm1, p).xyz;
                    } else if (l == 2) {
                        targetGuideAtP = tex2D(_Norm2, p).xyz;
                    } else {
                        targetGuideAtP = tex2D(_Norm3, p).xyz;
                    }

                    float2 upql = uStar + (p - ql);

                    float3 upql3D = uSrcPos + (pixelPos - targetPosAtQL);


                    float testL1 = length(tex2D(_SphereNormalMap, uStar.xy).xyz);
                    float testL2 = length(uSrcNorm);
                    float testVal = testL1 - testL2;

                    if (testL1 == 0) {
                        return float4(1, 1, 1, 1);
                    } else {
                        return float4(0, 0, 0, 1);
                    }

                    // return float4(testL1 / 10, 0, 0, 1);

                    // if (length(tex2D(_SphereNormalMap, uStar.xy).xyz) - uSrcNorm) < 0.50) {
                    //     return float4(testL1, 0, 0, 1);
                    // } else {
                    //     return float4(0, 0, 0, 1);
                    // }

                    // go get the uv
                    int srcPosIndex = lookupSourceUVByPosition(upql3D);

                    float2 uvForSrcPos = _SourceUvs[srcPosIndex];

                    float3 sourceGuideAtUStarPQL = tex2D(_SphereNormalMap, uvForSrcPos);
                    return tex2D(_SpherePosMap, uvForSrcPos);


                    // if (uStar.x > 1) {

                    // }
                    
                    // return float4(uSrcNorm, 1);
                    // return tex2D(_SphereNormalMap, p);
                    // return float4(sourceGuideAtUStarPQL, 1.0f);

                    float e = length(targetGuideAtP - sourceGuideAtUStarPQL);

                    // return tex2D(_SourceTexture, uStar);

                    // return float4(normalizedQL.xy, 0, 1);
                    // return float4(1, 1, 1 ,1);
                    // float2 normalizedQL = ql / length(ql);

                    // return float4(normalizedQL.xy, 0, 1);

                    // return tex2D(_TargetNormalMap, normalizedQL.xy);

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
                    // float2 uPrime = tex3D(_UVLut, (tex2D(_TargetNormalMap, ql.xy).xyz)); 

                    // return tex2D(_TargetNormalMap, p);

                    // return tex2D(_SphereNormalMap, p * 10);

                    // return tex3D(_UVLut, float3(p.x, p.y, 0.2));

                    // return tex3D(_UVLut, tex3D(_TargetNormalMap, float3(p.xy, h)).xyz);

                    // float3 targetGuideAtP = tex3D(_UVLut, tex3D(_TargetNormalMap, float3(p.xy, h)).xyz).xyz;
                    
                    // float3 targetGuideAtQ = tex3D(_UVLut, tex3D(_TargetNormalMap, float3(normalizedQL.xy, h)).xyz).xyz;

                    // return float4(targetGuideAtQ, 1);

                    // float2 uPrime = tex3D(_UVLut, targetGuideAtQ).xy; // CONFIRM THAT THIS IS RIGHT

                    // return tex3D(_UVLut, targetGuideAtQ);

                    // float3 sourceGuideAtUPrimePQ = tex2D(_SphereNormalMap, uPrime + (p - normalizedQL)).xyz;

                    // return tex2D(_SphereNormalMap, uPrime + (p - normalizedQL));

                    // float e = length(targetGuideAtP - sourceGuideAtUPrimePQ);

                    if (e < 0.8f) {
                        
                        // if ( l == 1) {
                        //     return fixed4(1, 0, 0, 1);
                        // } else if (l == 2) {
                        //     return fixed4(0, 1, 0, 1);
                        // } else if (l == 3) {
                        //     return fixed4(0, 0, 1, 1);
                        // }
                        return tex2D(_SourceTexture, uvForSrcPos);
                    }
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
