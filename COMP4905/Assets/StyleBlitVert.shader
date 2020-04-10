Shader "Custom/StyleBlitVert"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _SampleTex("THING", 2D) = "white" {}
        _Source2Texture("Source2Texture", 2D) = "white" {}
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.5
    }
    SubShader
        {
            Pass
            {

                Tags { "RenderType" = "Opaque" }
                LOD 200

                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma target 5.0
                #include "UnityCG.cginc"
                // Physically based Standard lighting model, and enable shadows on all light types
                float4 _Color;
                sampler3D _JitterTable; // seed point position map
                sampler3D _TargetNormalMap; // target normal map
                sampler2D _TargetPositionMap; // target position data, for retrieval via UV
                sampler3D _UVLut;
                sampler2D _SourceTexture; // source texture for transfer
                sampler2D _Source2Texture;
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

                //float4 _TarL1PT[55];
                //float4 _TarL1UV[55];
                //float4 _TarL2PT[214];
                //float4 _TarL2UV[214];
                //float4 _TarL3PT[799];
                //float4 _TarL3UV[799];

                sampler2D _Rando;

                sampler2D _Level1; // jitter table l1
                sampler2D _Level2; // jitter table l2
                sampler2D _Level3; // jitter table l3

                sampler2D _Norm1; // target normal map l1
                sampler2D _Norm2; // target normal map l2
                sampler2D _Norm3; // target normal map l3

                struct v2f {
                    float2 uv : TEXCOORD0;
                    half3 worldNormal : TEXCOORD1;
                    float3 n : NORMAL;
                    float3 worldPos : TEXCOORD3;
                    float4 position : SV_POSITION;
                    float2 normalizedPosition : TEXCOORD2;
                    fixed4 color : COLOR;
                };

                sampler2D _MainTex;

                int lookupU(float3 p) {
                    float smallest = 10000.f;
                    int index = 0;
                    for (int i = 0; i < 386; i++) {
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
                    for (int i = 0; i < 386; i++) {
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
                    float2 jitter = (tex2Dlod(_Rando, float4(p.xy * (_Time.x * 0.01f), 0, 0)).xy * 0.5f) - 0.75f;
                    float2 val = float2(p.x + jitter.x, p.y + jitter.y);
                    return val.xy;
                }

                float4 seedPoint(float2 p, int h) {
                    float4 c = float4(0, 0, 0, 0);
                    if (h == 1) {
                        c = tex2Dlod(_Level1, float4(p.xy, 0, 0));
                    }
                    else if (h == 2) {
                        c = tex2Dlod(_Level2, float4(p.xy, 0, 0));
                    }
                    else {
                        c = tex2Dlod(_Level3, float4(p.xy, 0, 0));
                    }

                    return c;
                }

                float2 NearestSeed(float2 p, int h) {

                    float3 pixelPos = tex2Dlod(_TargetPositionMap, float4(p.xy, 0, 0)).xyz;
                    float dPrime = 100000.0f;
                    float2 sPrime = float2(0.f, 0.f);

                    for (float x = -0.05f; x <= 0.05f; x += 0.05f) {
                        for (float y = -0.05f; y <= 0.05f; y += 0.05f) {

                            float2 ppos = float2(p.x + x, p.y + y);

                            float2 jittered = getJitter(ppos);
                            //float2 jittered = ppos;
                            float3 s = seedPoint(jittered, h).xyz;

                            float d = length(s - pixelPos);
                            if (d < dPrime) {
                                sPrime = jittered;
                                dPrime = d;
                            }

                        }
                    }
                    return sPrime;
                }

                float4 ParallelStyleBlit(float2 i, float3 wp) {

                    float2 p = i;
                    //float3 pixelPos = pos;
                    float4 pixelPos = tex2Dlod(_TargetPositionMap, float4(p.xy, 0, 0));

                    int L = 3;
                    //int l = 1;
                    for (int l = 1; l <= L; l++) {

                        float2 ql = NearestSeed(p, l);

                        float3 targetGuideAtQL;
                        if (l == 1) {
                            targetGuideAtQL = tex2Dlod(_Norm1, float4(ql.xy, 0, 0)).xyz;
                        }
                        else if (l == 2) {
                            targetGuideAtQL = tex2Dlod(_Norm2, float4(ql.xy, 0, 0)).xyz;
                        }
                        else {
                            targetGuideAtQL = tex2Dlod(_Norm3, float4(ql.xy, 0, 0)).xyz;
                        }

                        float3 targetPosAtQL = tex2Dlod(_TargetPositionMap, float4(ql.xy, 0, 0)).xyz;

                        int uIndex = lookupU(targetGuideAtQL);

                        float2 uStar = _SourceUvs[uIndex].xy;
                        float3 uSrcNorm = _SourceNormals[uIndex].xyz;
                        float3 uSrcPos = _SourcePos[uIndex].xyz;

                        // float3 targetGuideAtP = tex3D(_TargetNormalMap, float3(p, h));
                        float3 targetGuideAtP;
                        if (l == 1) {
                            targetGuideAtP = tex2Dlod(_Norm1, float4(p.xy, 0, 0)).xyz;
                        }
                        else if (l == 2) {
                            targetGuideAtP = tex2Dlod(_Norm2, float4(p.xy, 0, 0)).xyz;
                        }
                        else {
                            targetGuideAtP = tex2Dlod(_Norm3, float4(p.xy, 0, 0)).xyz;
                        }

                        float2 upql = uStar + (p - ql);

                        float3 upql3D = uSrcPos + (pixelPos - targetPosAtQL);

                        //float3 testerino = uSrcPos - pixelPos - targetPosAtQL;

                        // go get the uv
                        int srcPosIndex = lookupSourceUVByPosition(upql3D);

                        float2 uvForSrcPos = _SourceUvs[srcPosIndex];

                        float3 sourceGuideAtUStarPQL = tex2Dlod(_SphereNormalMap, float4(uvForSrcPos.xy, 0, 0));

                        float e = length(targetGuideAtP - sourceGuideAtUStarPQL);

                        if (e <1.5f) {
                            float4 c = float4(1, 1, 1, 1);
                            float4 col1 = tex2Dlod(_SphereNormalMap , float4(uvForSrcPos.xy, 0, 0));;
                            float4 col2 = tex2Dlod(_Source2Texture, float4(uvForSrcPos.xy, 0, 0));

                            
                            // regular averagine
                            //if (wp.x > -0.02f && wp.x < 0.02f) {
                            //    //return float4(0, abs(wp.x) * 10, 0, 1);
                            //    float interpVal = (wp.x + 0.02f) / 0.04f;
                            //    return lerp(col2, col1, interpVal);
                            //}

                            // smoothstepping
                            /*if (wp.x > -0.02f && wp.x < 0.02f) {
                                float interpVal = smoothstep(wp.x, -0.02f, 0.02f);
                                return lerp(col2, col1, interpVal);
                            }

                            if (wp.x < 0) {
                                return col2;
                            }*/

                            return col1;
                            //return uvForSrcPos;
                        }
                    }
                    //return float2(0, 0);
                    return float4(0, 0, 0, 0);
                }

                v2f vert(
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

                    //o.uv = ParallelStyleBlit(o.uv);

                    //o.color = float4(0, 0, 0, 0);

                    o.color = ParallelStyleBlit(o.uv, vertex);

                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    return i.color;
                    //return tex2D(_MainTex, i.uv);
                }

            ENDCG
        }
    }
}
