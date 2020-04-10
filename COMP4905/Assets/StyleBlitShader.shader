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
            sampler3D _JitterTable; // seed point position map
            sampler3D _TargetNormalMap; // target normal map
            sampler2D _TargetPositionMap; // target position data, for retrieval via UV
            sampler2D _TargetNormalMapFull;
            sampler3D _UVLut;
            sampler2D _SourceTexture; // source texture for transfer
            sampler2D _Source2Texture; // secondary texture for mixing
            sampler2D _MediumTexture; // mixing texture
            sampler2D _SphereNormalMap; // the source normal map
            sampler2D _SpherePosMap; // the source position map
            float4 _SourceNormals[386];
            float4 _SourcePos[386];
            float4 _SourceUvs[386];

            float4 _TarL1PT[21];
            float4 _TarL1UV[21];
            float4 _TarL2PT[83];
            float4 _TarL2UV[83];
            float4 _TarL3PT[337];
            float4 _TarL3UV[337];

            sampler2D _Rando;

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
                float3 wp : TEXCOORD4;
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
                o.wp = wp;
                o.worldPos = wp;
                o.worldPos = vertex.xyz;
                
                o.position = UnityObjectToClipPos(vertex);
                o.normalizedPosition = o.position.xy / length(o.position.xy);
                
                o.n = normalize(n);
                o.worldNormal = UnityObjectToWorldNormal(n); 

                return o;
            }

            sampler2D _MainTex;


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
                
                for (int i = 0; i < 21; i++) {
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

                for (int i = 0; i < 83; i++) {
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

                for (int i = 0; i < 337; i++) {
                    if (abs(length(p - _TarL3PT[i].xyz)) < smallest) {
                        smallest = abs(length(p - _TarL3PT[i].xyz));
                        index = i;
                    }
                }
                return index;
            }

            float2 getJitter(float2 p) {
                float2 jitter = (tex2D(_Rando, p).xy * 2.0f) - 1.0f;
                //float2 jitter = (tex2D(_Rando, p).xy) - 0.5f;
                jitter *= 0.01f;
                float2 val = float2(p.x + jitter.x, p.y + jitter.y);
                return val.xy;
            }

            float4 seedPoint(float2 p, int h) {

                
                //jitter.x -= 0.5f;

                

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

                for (float x=-0.01f; x<= 0.01f; x+= 0.01f) {
                    for (float y=-0.01f; y<= 0.01f; y+= 0.01f) {
                        //for (float z = -0.05f; z <= 0.05f; z += 0.05f) {
                            //float2 pos = float2(p.x + x , p.y + y);

                            //float3 pos = float3(pixelPos.x + x, pixelPos.y + y, pixelPos.z + z);
                        float2 ppos = float2(p.x + x, p.y + y);
                            float2 uv;
                            int index = 0;
                            //if (h == 1) {
                            //    index = lookupTargetUvL1(pos);
                            //    uv = _TarL1UV[index];
                            //}
                            //else if (h == 2) {
                            //    index = lookupTargetUvL2(pos);
                            //    uv = _TarL2UV[index];
                            //}
                            //else {
                            //    index = lookupTargetUvL3(pos);
                            //    uv = _TarL3UV[index];
                            //}

                            //float3 s = seedPoint(uv, h).xyz;
                            float2 jittered = getJitter(ppos);
                            //float2 jittered = ppos;
                            float3 s = seedPoint(jittered, h).xyz;

                            float d = abs(length(s - pixelPos));
                            if (d < dPrime) {
                                sPrime = jittered;
                                dPrime = d;
                            }
                        //}
                        
                    }
                }
                return sPrime;
            }

            float4 ParallelStyleBlit(v2f i) {
                
                float2 p = i.uv;
                float4 pixelPos = tex2D(_TargetPositionMap, p);
                /*return float4(pixelPos.xyz, 1);*/
                //return float4(pixelPos.xyz, 1);
                //return 
                //float3 pixelPos = i.worldPos;
                //return float4(pixelPos.xyz, 1);
               /* pixelPos.x *= 0.5f;
                pixelPos.x += 0.5f;*/

                //pixelPos.x = (pixelPos.x * 0.5f) + 0.5f;
                //pixelPos.y = (pixelPos.y * 0.5f) + 0.5f;
                //pixelPos.z = (pixelPos.z * 0.5f) + 0.5f;

   /*             if (i.worldPos.z < 0) {
                    return float4(1, 0, 0, 1);
                } 

                return float4(0, 1, 0, 1);*/
                //return float4(pixelPos.xyz, 1);

                //float4 norm = tex2D(_TargetNormalMapFull, p);
                float3 norm = i.n;
                norm.x = (norm.x * 0.5f) + 0.5f;
                norm.y = (norm.y * 0.5f) + 0.5f;
                norm.z = (norm.z * 0.5f) + 0.5f;
                //return norm;

                //float4 normTest = tex2D(_Norm1, p);

              /*  if (i.n.z < 0) {
                    return float4(1, 0, 0, 1);
                }
                return float4(0, 1, 0, 1);*/
                //float a = normTest.x;
                //normTest.x

                //normTest = (normTest);
                //normTest.z *= -1;
                //normTest.z = 0;
                //normTest.z += 0.5;

                //normTest.z *= 0.25f;

                //normTest.x *= 0.5;
                //normTest.y *= 0.5;

                //normTest.xy += 0.25;

                //if (pixelPos.z < 0.5) {
                //    normTest.z = 0;
                //}
                //else {
                //    normTest.z = 0.75f;
                //}

                //return float4(normTest.xyz, 1);
                //return float4(norm.xyz, 1);



                int L = 3;
                //int l = 1;
                for (int l = 1; l <= L; l++) {
                    float h = 0.f;

                    if (l == 2) {
                        h = 0.4f;
                    } else if (l == 3) {
                        h = 0.8f;
                    }

                    //float4 thingy = tex2D(_Level3, p);
                    //thingy.x = thingy.x * 0.5 + 0.5;
                    //thingy.y = thingy.y * 0.5f + 0.5f;
                    //thingy.z = thingy.z * 0.5f + 0.5f;
                    //thingy *= 0.5f;
                    //thingy += 0.5f;
                    //return float4(thingy.xyz, 1);

                    float2 ql = NearestSeed(p, l);

                    //return float4(ql.xy, 0, 1);

                    //return float4((p.x - ql.x) * 10, 0, 0, 1);
                    //return float4(ql.xy, 0, 1);

                    float3 targetPosAtQL = tex2D(_TargetPositionMap, ql).xyz;

                    //float3 targetGuideAtQL;
                    //if (l == 1) {
                    //    targetGuideAtQL = tex2D(_Norm1, ql).xyz;
                    //} else if (l == 2) {
                    //    targetGuideAtQL = tex2D(_Norm2, ql).xyz;
                    //} else {
                    //    targetGuideAtQL = tex2D(_Norm3, ql).xyz;
                    //}

                    float3 targetGuideAtQL = tex2D(_TargetNormalMapFull, ql).xyz;

                    //targetGuideAtQL.xy *= 0.5;
                    //targetGuideAtQL.xy += 0.25;

                    //if (targetPosAtQL.z < 0.5) {
                    //    targetGuideAtQL.z = 0;
                    //}
                    //else {
                    //    targetGuideAtQL.z = 0.75f;
                    //}

                    //return float4(targetGuideAtQL.xyz, 1);

                    
                    //return float4(targetPosAtQL.xyz, 1);

                    int uIndex = lookupU(targetGuideAtQL); 

                    float2 uStar = _SourceUvs[uIndex].xy;
                    float3 uSrcNorm = _SourceNormals[uIndex].xyz;
                    float3 uSrcPos = _SourcePos[uIndex].xyz;

                    //return float
                    //return float4(uStar.xy, 0, 1);
                    //return float4(uSrcPos.xyz, 1);

                    //uSrcNorm.xyz *= 2;
                    //return float4(uSrcNorm.xyz, 1);

                    // float3 targetGuideAtP = tex3D(_TargetNormalMap, float3(p, h));
                    //float3 targetGuideAtP;
                    //if (l == 1) {
                    //    targetGuideAtP = tex2D(_Norm1, p).xyz;
                    //} else if (l == 2) {
                    //    targetGuideAtP = tex2D(_Norm2, p).xyz;
                    //} else {
                    //    targetGuideAtP = tex2D(_Norm3, p).xyz;
                    //}

                    float3 targetGuideAtP = i.n;
                    targetGuideAtP.x = (targetGuideAtP.x * 0.5f) + 0.5f;
                    targetGuideAtP.y = (targetGuideAtP.y * 0.5f) + 0.5f;
                    targetGuideAtP.z = (targetGuideAtP.z * 0.5f) + 0.5f;

                    float2 upql = uStar + (p - ql);
                    //return float4(upql.xy, 0, 1);

                    float3 upql3D = uSrcPos + (pixelPos - targetPosAtQL);
                    //return float4(uSrcPos.xyz * 0.5 + 0.5, 1);
                    //return float4(uSrcPos.xyz, 1);
                    //return float4(targetGuideAtP.xyz, 1);
                    //return float4(targetPosAtQL.xyz, 1);
                    //return float4(upql3D.xyz, 1);

                    float3 testerino = uSrcPos - pixelPos - targetPosAtQL;


                    //float testL1 = length(tex2D(_SphereNormalMap, uStar.xy).xyz);
                    //float testL2 = length(uSrcNorm);
                    //float testVal = testL1 - testL2;

                    // go get the uv
                    int srcPosIndex = lookupSourceUVByPosition(upql3D);
                    int testerinoIdex = lookupSourceUVByPosition(testerino);

                    float2 uvForSrcPos = _SourceUvs[srcPosIndex];
                    float2 uvForTesterino = _SourceUvs[srcPosIndex];
                    //return float4()

                    float3 sourceGuideAtUStarPQL = tex2D(_SphereNormalMap, uStar + (p - ql));
                    //return float4(sourceGuideAtUStarPQL.xyz, 1);

                    float e = length(targetGuideAtP - sourceGuideAtUStarPQL);
                    //return float4(distance(targetGuideAtP, sourceGuideAtUStarPQL), 0, 0, 1);

                    /*if (length(p - ql) == 0.00f) {
                        return fixed4(1, 1, 1, 1);
                    }*/

                    //return tex2D(_SourceTexture, uvForSrcPos);
                    //return fixed4(0, uvForSrcPos.xy, 1) ;

                    //if (e < 0.f) {
                    //    return fixed4(0, 0, 0, 1);
                    //}
                    //else if (e == 0.0f) {
                    //    return fixed4(1, 0, 0, 1);
                    //}
                    //else if (e == 1.0f) {
                    //    return fixed4(0, 1, 0, 1);
                    //}
                    //else if (e > 1.0f) {
                    //    return fixed4(1, 1, 1, 1);
                    //} 

                    //return  fixed4(0, 0, e, 1);

                    //return tex2D(_SourceTexture, uvForSrcPos + p - ql);
                    //return te
                    //return float4(p - ql, 0, 1);
                    if (e < 0.9f) {
                        float4 c = float4(1, 1, 1, 1);
                        float4 col1 = tex2D(_SourceTexture, (uStar + (p - 0.01 * ql)));
                        float4 col2 = tex2D(_Source2Texture, (uStar + (p - 0.01 * ql)));
                        float4 col3 = tex2D(_MediumTexture, (uStar + (p - 0.01 * ql)));
                        // regular averagine
                        //if (i.wp.z > 0.48f && i.wp.z < 0.52f) {
                        //    //return float4(0, abs(wp.x) * 10, 0, 1);
                        //    float interpVal = (i.wp.z - 0.482f) / 0.04f;
                        //    return lerp(col2, col1, interpVal);
                        //}

                        // smoothstepping
                        //if (i.wp.x > 0.48f && i.wp.z < 0.52f) {
                        //    float interpVal = smoothstep(i.wp.z, 0.48f, 0.52f);
                        //    return lerp(col2, col1, interpVal);
                        //}

                        //if (i.wp.z > 0.48f && i.wp.z < 0.52f) {
                        //    return col3;
                        //}

                        /*if (i.wp.z < 0.5f) {
                            return col2;
                        }*/

                        return col1;
                    }
                }
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
