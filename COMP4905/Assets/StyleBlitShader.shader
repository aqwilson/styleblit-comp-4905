// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/StyleBlitShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Source1Texture("Source1Texture", 2D) = "white" {}
        _Source2Texture("Source2Texture", 2D) = "white" {}
        _MediumTexture("MediumTexture", 2D) = "white" {}
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            // Upgrade NOTE: excluded shader from DX11; has structs without semantics (struct v2f members vertex)
            //#pragma exclude_renderers d3d11
            // Upgrade NOTE: excluded shader from DX11 because it uses wrong array syntax (type[size] name)
            // #pragma exclude_renderers d3d11
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0
            #include "UnityCG.cginc"

            float4 _Color;
            sampler2D _TargetPositionMap; // target position data, for retrieval via UV
            sampler2D _TargetNormalMapFull;
            sampler2D _Source1Texture; // source texture
            sampler2D _Source2Texture; // secondary texture for mixing
            sampler2D _MediumTexture; // mixing texture
            sampler2D _SphereNormalMap; // the source normal map
            sampler2D _SpherePosMap; // the source position map
            float4 _SourceNormals[386];
            float4 _SourcePos[386];
            float4 _SourceUvs[386];

            float4 _TarL1PT[47];
            float4 _TarL1UV[47];
            float4 _TarL2PT[200];
            float4 _TarL2UV[200];
            float4 _TarL3PT[781];
            float4 _TarL3UV[781];

            sampler2D _Jitter;

            sampler2D _Level1; // jitter table l1
            sampler2D _Level2; // jitter table l2
            sampler2D _Level3; // jitter table l3


            // note: no SV_POSITION in this struct
            struct v2f {
                float2 uv : TEXCOORD0;
                half3 worldNormal : TEXCOORD1;
                float3 n : NORMAL;
                float3 worldPos : TEXCOORD3;
                float3 wp : TEXCOORD4;
                float4 position : SV_POSITION;
                float2 normalizedPosition : TEXCOORD2;
            };

            v2f vert (
                float4 vertex : POSITION, // vertex position input
                float2 uv : TEXCOORD0, // texture coordinate input
                float3 n : NORMAL
                )
            {
                v2f o;
                o.uv = uv;
                
                o.worldPos = vertex.xyz;
                
                o.position = UnityObjectToClipPos(vertex);
                o.normalizedPosition = o.position.xy / length(o.position.xy);
                
                o.n = normalize(n);
                o.worldNormal = UnityObjectToWorldNormal(n); 

                return o;
            }

            int lookupU(float3 p) {
                float smallest = 10000.f;
                int index = 0;
                for (int i=0; i<386; i++) {
                    if (abs(length(p - _SourceNormals[i].xyz)) < smallest) {
                        smallest = abs(length(p - _SourceNormals[i].xyz));
                        index = i;
                    }
                }
                return index;
            }

            int lookupSourceUVByPosition(float3 p) {
                float smallest = 10000.f;
                int index = 0;
                for (int i=0; i<386; i++) {
                    if (abs(length(p - _SourcePos[i].xyz)) < smallest) {
                        smallest = abs(length(p - _SourcePos[i].xyz));
                        index = i;
                    }
                }
                return index;
            }

            int lookupTargetUvL1(float3 p) {
                float smallest = 10000.f;
                int index = 0;
                
                for (int i = 0; i < 47; i++) {
                    if (abs(length(p - _TarL1PT[i].xyz)) < smallest) {
                        smallest = abs(length(p - _TarL1PT[i].xyz));
                        index = i;
                    }
                }
                return index;
            }

            int lookupTargetUvL2(float3 p) {
                float smallest = 10000.f;
                int index = 0;

                for (int i = 0; i < 200; i++) {
                    if (abs(length(p - _TarL2PT[i].xyz)) < smallest) {
                        smallest = abs(length(p - _TarL2PT[i].xyz));
                        index = i;
                    }
                }
                return index;
            }

            int lookupTargetUvL3(float3 p) {
                float smallest = 10000.f;
                int index = 0;

                for (int i = 0; i < 781; i++) {
                    if (abs(length(p - _TarL3PT[i].xyz)) < smallest) {
                        smallest = abs(length(p - _TarL3PT[i].xyz));
                        index = i;
                    }
                }
                return index;
            }

            float2 getJitter(float2 p) {
                float2 jitter = (tex2D(_Jitter, p).xy * 2.0f) - 1.0f;
                jitter *= 0.01f;
                float2 val = float2(p.x + jitter.x, p.y + jitter.y);
                return val.xy;
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

                return c;
            }

            float2 NearestSeed(float2 p, int h) {

                float3 pixelPos = tex2D(_TargetPositionMap, p).xyz;
                float dPrime = 100000.0f;
                float2 sPrime = float2(0.f,0.f);

                for (float x=-0.01f; x<= 0.01f; x+= 0.01f) {
                    for (float y=-0.01f; y<= 0.01f; y+= 0.01f) {
                        float2 ppos = float2(p.x + x, p.y + y);
                            
                        float2 jittered = getJitter(ppos);
                        float3 s = seedPoint(jittered, h).xyz;

                        float d = abs(length(s - pixelPos));
                        if (d < dPrime) {
                            sPrime = jittered;
                            dPrime = d;
                        }
                    }
                }
                return sPrime;
            }

            float4 ParallelStyleBlit(v2f i) {
                //return float4(1, 1, 0, 1);
                float2 p = i.uv;
                float4 pixelPos = tex2D(_TargetPositionMap, p);
                
                float3 norm = i.n;
                norm.x = (norm.x * 0.5f) + 0.5f;
                norm.y = (norm.y * 0.5f) + 0.5f;
                norm.z = (norm.z * 0.5f) + 0.5f;

                for (int l = 1; l <= 3; l++) {

                    float2 ql = NearestSeed(p, l);

                    float3 targetPosAtQL = tex2D(_TargetPositionMap, ql).xyz;
                    float3 targetGuideAtQL = tex2D(_TargetNormalMapFull, ql).xyz;

                    int uIndex = lookupU(targetGuideAtQL); 
                    float2 uStar = _SourceUvs[uIndex].xy;
                    float3 uSrcNorm = _SourceNormals[uIndex].xyz;
                    float3 uSrcPos = _SourcePos[uIndex].xyz;

                    float3 targetGuideAtP = i.n;
                    targetGuideAtP.x = (targetGuideAtP.x * 0.5f) + 0.5f;
                    targetGuideAtP.y = (targetGuideAtP.y * 0.5f) + 0.5f;
                    targetGuideAtP.z = (targetGuideAtP.z * 0.5f) + 0.5f;

                    float2 upql = uStar + (p - ql);
                    float3 upql3D = uSrcPos + (pixelPos -  (0.8 * targetPosAtQL));

                    // go get the uv
                    float2 uvForSrcPos = float2(upql3D.x / upql3D.z, upql3D.y / upql3D.z) / 0.25;
                   
                    float3 sourceGuideAtUStarPQL = tex2D(_SphereNormalMap, uvForSrcPos);

                    float e = length(targetGuideAtP - sourceGuideAtUStarPQL);
                    
                    if (e < 1.0f) {
                        float4 c = float4(1, 1, 1, 1);
                        float4 col1 = tex2D(_Source1Texture, (uStar + (p - 0.01 * ql)));
                        float4 col2 = tex2D(_Source2Texture, (uStar + (p - 0.01 * ql)));
                        float4 col3 = tex2D(_MediumTexture, (uStar + (p - 0.01 * ql)));

                        // Regular Linear Interpolation
                        //if (i.wp.z > 0.48f && i.wp.z < 0.52f) {
                        //    float interpVal = (i.wp.z - 0.48f) / 0.04f;
                        //    return lerp(col1, col2, interpVal);
                        //}

                        //smoothstepping (Hermite Spline)
                        //if (i.wp.x > 0.4f && i.wp.z < 0.6f) {
                        //    float interpVal = smoothstep(0.4f, 0.6f, i.wp.z);
                        //    return lerp(col1, col2, interpVal);
                        //}

                        // Smooth along a third texture
                        //if (i.wp.z <= 0.48 && i.wp.z >= 0.46) {
                        //     float interpVal = (i.wp.z - 0.46f) / 0.02f;
                        //     return lerp(col1, col3, interpVal);
                        //} 

                        //if (i.wp.z >= 0.52 && i.wp.z < 0.54) {
                        //     float interpVal = (i.wp.z - 0.52f) / 0.02f;
                        //     return lerp(col3, col2, interpVal);
                        //}

                        // Add the intermediate colour
                       /* if (i.wp.z > 0.46f && i.wp.z < 0.54f) {
                            return col3;
                        }*/

                        // return 2nd colour
                       /* if (i.wp.z < 0.5f) {
                            return col1;
                        }*/

                        // return regular colour
                        return col1;
                    }
                }
                // return black - no match found
                return float4(0, 0, 0, 1);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return ParallelStyleBlit(i);
            }
            ENDCG
        }
    }
    //FallBack "Diffuse"
}
