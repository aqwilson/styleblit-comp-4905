using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;

public class StanfordShadingScript : MonoBehaviour
{
    Texture2D tex;

    public GameObject bunny;
    public Mesh bunnyMesh;
    Mesh sphereMesh;
    GameObject sphere;
    Texture2D sphereNormalMap;
    Texture2D spherePositionMap;
    Texture3D posByNormal;

    Texture2D thatTex;
    Texture2D normalTexture;

    // 3D FOR JITTER
    Texture3D jitterPos3D;
    Texture3D jitterNorm3D;

    Vector4[] sourceNormals;
    Vector4[] sourceUvs;

    Vector4[] sourcePositions;


    Texture2D targetPositionData;
    Texture2D targetNormData;

    List<Vector4> l1ArrPoints;
    List<Vector4> l1ArrUv;
    List<Vector4> l2ArrPoints, l2ArrUv, l3ArrPoints, l3ArrUv;
    Texture2D rando;


    Texture2D _level1;
    Texture2D _level2;
    Texture2D _level3;

    Texture2D _norm1;
    Texture2D _norm2;
    Texture2D _norm3;
    // Start is called before the first frame update
    void Start()
    {
        // capsule = GameObject.Find("Capsule");
        sphere = GameObject.Find("Sphere");
        sphereMesh = GameObject.Find("Sphere").GetComponent<MeshFilter>().mesh;
       
        System.Random random = new System.Random();

        // sphereNormalMap = (Texture2D)sphere.GetComponent<MeshRenderer>().material.GetTexture("_BumpMap");


        // PART ONE: THE SOURCE
        // fetch the sphere's data
        Vector3[] sphereVerts = sphereMesh.vertices;
        Vector2[] sphereUvs = sphereMesh.uv;
        Vector3[] sphereNormals = sphereMesh.normals;
        int[] sphereFaces = sphereMesh.triangles;

        //string str  ="";
        //float lowesty = 0.0f;
        //float highesty = 0.0f;
        for (int i = 0; i < sphereVerts.Length; i++)
        {
            //sphereVerts[i] *= 0.05f;
            //sphereVerts[i].y += 0.025f;
            //if (sphereVerts[i].y < lowesty)
            //{
            //    lowesty = sphereVerts[i].y;
            //}
            //if (sphereVerts[i].y > highesty)
            //{
            //    highesty = sphereVerts[i].y;
            //}
        }

        //Debug.Log("Lowest Y: " + lowesty);
        //Debug.Log("HighestY: " + highesty);

        //Debug.Log(str);

        List<Vector4> refinedSphereNorms = new List<Vector4>();
        List<Vector4> refinedSpherePositions = new List<Vector4>();
        List<Vector4> refinedSphereUvs = new List<Vector4>();
        // step one: build out the arrays
        for (int i=0; i<sphereNormals.Length; i++) {

            float red = (sphereNormals[i].x * 0.5f) + 0.5f;
            float green = (sphereNormals[i].y + 0.5f) + 0.5f;
            float blue = (sphereNormals[i].z + 0.5f) + 0.5f;

            float a = (sphereVerts[i].x * 0.1f) + 0.1f;
            float b = ((sphereVerts[i].y + 0.1f) * 0.1f) + 0.1f;
            float c = (sphereVerts[i].z * 0.1f) + 0.1f;

            // do som

            Vector4 sn = new Vector4(red, green, blue, 1.0f);

            if (refinedSphereNorms.Contains(sn)) {
                continue;
            }

            refinedSphereNorms.Add(sn);
            refinedSpherePositions.Add(new Vector4(a, b, c, 1.0f));
            //refinedSpherePositions.Add(new Vector4(sphereVerts[i].x, sphereVerts[i].y, sphereVerts[i].z, 0.0f));
            refinedSphereUvs.Add(new Vector4(sphereUvs[i].x, sphereUvs[i].y, 0.0f, 0.0f));
        }

        sourceNormals = refinedSphereNorms.ToArray();
        sourceUvs = refinedSphereUvs.ToArray();
        sourcePositions = refinedSpherePositions.ToArray();

        //Debug.Log("Sphere Array Lengths: " + sourceNormals.Length); 
        //Debug.Log("Sphere Position Lengths: " + sourcePositions.Length);       

        // step two: create the Normal Map.
        Texture2D sphereNormalTexture = new Texture2D(512, 512);
        Texture2D spherePositionTexture = new Texture2D(512, 512);
        sphereNormalTexture = TextureBlackout(sphereNormalTexture, 512, 512);
        spherePositionTexture = TextureBlackout(spherePositionTexture, 512, 512);
        for (int i=0; i<sphereUvs.Length; i++) {
            int x = (int) Mathf.Floor(sphereUvs[i].x * 512.0f);
            int y = (int) Mathf.Floor(sphereUvs[i].y * 512.0f);



            float red = (sphereNormals[i].x * 0.5f) + 0.5f;
            float green = (sphereNormals[i].y * 0.5f) + 0.5f;
            float blue = (sphereNormals[i].z * 0.5f) + 0.5f;

            float a = (sphereVerts[i].x * 0.5f) + 0.5f;
            float b = (sphereVerts[i].y * 0.5f) + 0.5f;
            float c = (sphereVerts[i].z * 0.5f) + 0.5f;

            sphereNormalTexture.SetPixel(x, y, new Color(red, green, blue, 1.0f));
            spherePositionTexture.SetPixel(x, y, new Color(a, b, c, 1.0f));
        }
        sphereNormalTexture.Apply();
        spherePositionTexture.Apply();

        // BLOOM NORMALS
        bool[,] floodedSphereNormals = new bool[512,512];
        bool blackLeftSphereNormals = true;
        int iterationsOfBloomSphereNormals = 0;
        Color texColorSphereNormals = Color.white;
        Color tcsPos = Color.white;

        while (iterationsOfBloomSphereNormals < 75 && blackLeftSphereNormals) {
            blackLeftSphereNormals = false;

            for (int i=0; i<512; i++) {
                for (int j=0; j<512; j++) {
                    texColorSphereNormals = sphereNormalTexture.GetPixel(i, j);
                    tcsPos = spherePositionTexture.GetPixel(i, j);

                    if (texColorSphereNormals == Color.black) {
                        blackLeftSphereNormals = true;
                        continue;
                    }

                    if (floodedSphereNormals[i,j] == true) {
                        continue;
                    }

                    if (i>0) {
                        if (j < 511 && sphereNormalTexture.GetPixel(i-1, j+1) == Color.black) {
                            sphereNormalTexture.SetPixel(i-1, j+1, texColorSphereNormals);
                            spherePositionTexture.SetPixel(i-1, j+1, tcsPos);
                            floodedSphereNormals[i-1, j+1] = true;
                        }
                        if (j > 0 && sphereNormalTexture.GetPixel(i-1, j-1) == Color.black) {
                            sphereNormalTexture.SetPixel(i-1, j-1, texColorSphereNormals);
                            spherePositionTexture.SetPixel(i-1, j-1, tcsPos);
                            floodedSphereNormals[i-1, j-1] = true;
                        }
                        if (sphereNormalTexture.GetPixel(i-1, j) == Color.black) {
                            sphereNormalTexture.SetPixel(i-1, j, texColorSphereNormals);
                            spherePositionTexture.SetPixel(i-1, j, tcsPos);
                            floodedSphereNormals[i-1, j] = true;
                        }
                    }

                    if (i < 511) {
                        if (j < 511 && sphereNormalTexture.GetPixel(i+1, j+1) == Color.black) {
                            sphereNormalTexture.SetPixel(i+1, j+1, texColorSphereNormals);
                            spherePositionTexture.SetPixel(i+1, j+1, tcsPos);
                            floodedSphereNormals[i+1, j+1] = true;
                        }
                        if (j > 0 && sphereNormalTexture.GetPixel(i+1, j-1) == Color.black) {
                            sphereNormalTexture.SetPixel(i+1, j-1, texColorSphereNormals); 
                            spherePositionTexture.SetPixel(i+1, j-1, tcsPos);
                            floodedSphereNormals[i+1, j-1] = true;
                        }
                        if (sphereNormalTexture.GetPixel(i+1, j) == Color.black) {
                            sphereNormalTexture.SetPixel(i+1, j, texColorSphereNormals);
                            spherePositionTexture.SetPixel(i+1, j, tcsPos);
                            floodedSphereNormals[i+1, j] = true;
                        }
                    }

                    if (j<511 && sphereNormalTexture.GetPixel(i, j+1) == Color.black) {
                        sphereNormalTexture.SetPixel(i, j+1, texColorSphereNormals);
                        spherePositionTexture.SetPixel(i, j+1, tcsPos);
                        floodedSphereNormals[i, j+1] = true;
                    }
                    if (j>0 && sphereNormalTexture.GetPixel(i, j-1) == Color.black) {
                        sphereNormalTexture.SetPixel(i, j-1, texColorSphereNormals);
                        spherePositionTexture.SetPixel(i, j-1, tcsPos);
                        floodedSphereNormals[i, j-1] = true;
                    }

                    floodedSphereNormals[i, j] = true;
                }
            }

            for (int i=0; i<512; i++) {
                for (int j=0; j<512; j++) {
                    floodedSphereNormals[i, j] = false;
                }
            }

            iterationsOfBloomSphereNormals++;

            sphereNormalTexture.Apply();
            spherePositionTexture.Apply();
        }        

        sphereNormalMap = sphereNormalTexture;
        spherePositionMap = spherePositionTexture;


        // sphereNormalMap = Resources.Load("normalsSMALL") as Texture2D;
        // sphereNormalMap.Resize(100, 100);
        // sphereNormalMap.Apply();

        // posByNormal = new Texture3D(100, 100, 100, TextureFormat.RGBAHalf, 0);
        // posByNormal = TextureBlackout(posByNormal, 100, 100, 100);

        // Color[,,] sncol = new Color[100,100,100];

        // for (int i=0; i<100; i++) {
        //     for (int j=0; j<100; j++) {
        //         for (int k=0; k<100; k++) {
        //             sncol[i, j, k] = Color.black;
        //         }
        //     }
        // }


        // for (int i=0; i<sphereNormalMap.width; i++) {
        //     for (int j=0; j<sphereNormalMap.height; j++) {
        //         Color x = sphereNormalMap.GetPixel(i, j);

        //         if (x.r >= 1.0f || x.g >= 1.0f || x.b >= 1.0f) {
        //             // Debug.Log("Too big!" + x);
        //             continue;
        //         } else if (x.r < 0.0f || x.g < 0.0f || x.b < 0.0f) {
        //             // Debug.Log("too small!" + x);
        //             continue;
        //         }
                
        //         // Debug.Log("The position: " + x.r + ", " + x.g + ", " + x.b);
        //         sncol[(int)Mathf.Floor(x.r * 100), (int)Mathf.Floor(x.g * 100), (int)Mathf.Floor(x.b * 100)] = new Color(i/100f, j/100f, 0f, 1f);
        //         // posByNormal.SetPixel((int)Mathf.Floor((x.r * 100)), (int)Mathf.Floor((x.g * 100)), (int)Mathf.Floor((x.b * 100)), new Color((float)i/100f, (float)j/100f, 0f, 1f));
        //     }
        // }


        // bool[,,] filled = new bool[100, 100, 100];
        // bool blackPixelsRemaining = true;
        // int its = 0;
        // Color tc = Color.black;
        // while (blackPixelsRemaining && its < 500) {
        //     for (int i=0; i<100; i++) {
        //         for (int j=0; j<100; j++) {
        //             for (int k=0; k<100; k++) {

        //                 blackPixelsRemaining = false;

        //                 tc = sncol[i, j, k];
                        
        //                 if (tc == Color.black) {
        //                     blackPixelsRemaining = true;
        //                     continue;
        //                 }

        //                 if (filled[i,j,k] == true) {
        //                     continue;
        //                 }

        //                 if (k > 0) {
        //                     if (j>0) {
        //                         if (i>0 && sncol[i-1,j-1,k-1] == Color.black) {
        //                             sncol[i-1,j-1,k-1] = tc;
        //                             filled[i-1, j-1, k-1] = true;
        //                         }
        //                         if (i<99 && sncol[i+1, j-1, k-1] == Color.black) {
        //                             sncol[i+1,j-1,k-1] = tc;
        //                             filled[i+1, j-1, k-1] = true;
        //                         }
        //                         if (sncol[i, j-1, k-1] == Color.black) {
        //                             sncol[i, j-1, k-1] = tc;
        //                             filled[i, j-1, k-1] = true;
        //                         }
        //                     }
        //                     if (j < 99) {
        //                         if (i>0 && sncol[i-1, j+1, k-1] == Color.black) {
        //                             sncol[i-1, j+1, k-1] = tc;
        //                             filled[i-1, j+1, k-1] = true;
        //                         }
        //                         if (i<99 && sncol[i+1, j+1, k-1] == Color.black) {
        //                             sncol[i+1, j+1, k-1] = tc;
        //                             filled[i+1, j+1, k-1] = true;
        //                         }
        //                         if (sncol[i, j+1, k-1] == Color.black) {
        //                             sncol[i, j+1, k-1] = tc;
        //                             filled[i, j+1, k-1] = true;
        //                         }
        //                     }

        //                     if (i>0 && sncol[i-1, j, k-1] == Color.black) {
        //                         sncol[i-1, j, k-1] = tc;
        //                         filled[i-1, j, k-1] = true;
        //                     }

        //                     if (i<99 && sncol[i+1, j, k-1] == Color.black) {
        //                         sncol[i+1, j, k-1] = tc;
        //                         filled[i+1, j, k-1] = true;
        //                     }

        //                     if (sncol[i, j, k-1] == Color.black) {
        //                         sncol[i, j, k-1] = tc;
        //                         filled[i, j, k-1] = true;
        //                     }
        //                 }


        //                 // K + 1
        //                 if (k < 99) {
        //                     if (j>0) {
        //                         if (i>0 && sncol[i-1,j-1,k+1] == Color.black) {
        //                             sncol[i-1,j-1,k+1] = tc;
        //                             filled[i-1, j-1, k+1] = true;
        //                         }
        //                         if (i<99 && sncol[i+1, j-1, k+1] == Color.black) {
        //                             sncol[i+1,j-1,k+1] = tc;
        //                             filled[i+1, j-1, k+1] = true;
        //                         }
        //                         if (sncol[i, j-1, k+1] == Color.black) {
        //                             sncol[i, j-1, k+1] = tc;
        //                             filled[i, j-1, k+1] = true;
        //                         }
        //                     }
        //                     if (j < 99) {
        //                         if (i>0 && sncol[i-1, j+1, k+1] == Color.black) {
        //                             sncol[i-1, j+1, k+1] = tc;
        //                             filled[i-1, j+1, k+1] = true;
        //                         }
        //                         if (i<99 && sncol[i+1, j+1, k+1] == Color.black) {
        //                             sncol[i+1, j+1, k+1] = tc;
        //                             filled[i+1, j+1, k+1] = true;
        //                         }
        //                         if (sncol[i, j+1, k+1] == Color.black) {
        //                             sncol[i, j+1, k+1] = tc;
        //                             filled[i, j+1, k+1] = true;
        //                         }
        //                     }

        //                     if (i>0 && sncol[i-1, j, k+1] == Color.black) {
        //                         sncol[i-1, j, k+1] = tc;
        //                         filled[i-1, j, k+1] = true;
        //                     }

        //                     if (i<99 && sncol[i+1, j, k+1] == Color.black) {
        //                         sncol[i+1, j, k+1] = tc;
        //                         filled[i+1, j, k+1] = true;
        //                     }

        //                     if (sncol[i, j, k+1] == Color.black) {
        //                         sncol[i, j, k+1] = tc;
        //                         filled[i, j, k+1] = true;
        //                     }
        //                 }




        //                 // K = NORMAL
        //                 if (j>0) {
        //                     if (i>0 && sncol[i-1,j-1,k] == Color.black) {
        //                         sncol[i-1,j-1,k] = tc;
        //                         filled[i-1, j-1, k] = true;
        //                     }
        //                     if (i<99 && sncol[i+1, j-1, k] == Color.black) {
        //                         sncol[i+1,j-1,k] = tc;
        //                         filled[i+1, j-1, k] = true;
        //                     }
        //                     if (sncol[i, j-1, k] == Color.black) {
        //                         sncol[i, j-1, k] = tc;
        //                         filled[i, j-1, k] = true;
        //                     }
        //                 }
        //                 if (j < 99) {
        //                     if (i>0 && sncol[i-1, j+1, k] == Color.black) {
        //                         sncol[i-1, j+1, k] = tc;
        //                         filled[i-1, j+1, k] = true;
        //                     }
        //                     if (i<99 && sncol[i+1, j+1, k] == Color.black) {
        //                         sncol[i+1, j+1, k] = tc;
        //                         filled[i+1, j+1, k] = true;
        //                     }
        //                     if (sncol[i, j+1, k] == Color.black) {
        //                         sncol[i, j+1, k] = tc;
        //                         filled[i, j+1, k] = true;
        //                     }
        //                 }

        //                 if (i>0 && sncol[i-1, j, k] == Color.black) {
        //                     sncol[i-1, j, k] = tc;
        //                     filled[i-1, j, k] = true;
        //                 }

        //                 if (i<99 && sncol[i+1, j, k] == Color.black) {
        //                     sncol[i+1, j, k] = tc;
        //                     filled[i+1, j, k] = true;
        //                 }

        //                 filled[i, j, k] = true;
                    
        //             }
        //         }
        //     }

        //     for (int i=0; i<100; i++) {
        //         for (int j=0; j<100; j++) {
        //             for (int k=0; k<100; k++) {
        //                 filled[i, j, k] = false;
        //             }
        //         }
        //     }

        //     its++;

        //     // posByNormal.Apply();

        // }

        // if (blackPixelsRemaining) {
        //     Debug.Log("Exited with black pixels remaining, after " + its + " iterations.");
        // } else {
        //     Debug.Log("Exited withOUT black pixels remaining, after " + its + " iterations.");
        // }


        // for (int i=0; i<100; i++) {
        //     for (int j=0; j< 100; j++) {
        //         for (int k=0; k<100; k++) {
        //             posByNormal.SetPixel(i, j, k, sncol[i, j, k]);
        //         }
        //     }
        // }

        // posByNormal.Apply();

        

        bunny = GameObject.Find("gdef");
        bunnyMesh = bunny.GetComponent<MeshFilter>().mesh;

        Vector3[] verts = bunnyMesh.vertices;
        //verts.

        //List<Vector3> vs = new List<Vector3>();
        //string str = "";
        //foreach (Vector3 v in verts)
        //{
        //    //vs.Add(v);
        //    str += v;
        //    str += ", ";
        //}

        //Debug.Log(str);
        //Debug.Log(vs.Join(",", vs.ToArray());
        


        List<Vector3> reducedVerts = new List<Vector3>();
        List<Vector3> xyOnlyComparison = new List<Vector3>();
        Vector3[] normals = bunnyMesh.normals;
        List<Vector3> reducedNormals = new List<Vector3>();
        List<Vector3> xyComparedNormals = new List<Vector3>();
        Vector2[] uvs = bunnyMesh.uv;

        //Debug.Log("LENGTHS: " + verts.Length + ", " + uvs.Length);

        int[] triangles = bunnyMesh.triangles;

        // ADDING BUNNY's UV DATA TO A BIGASS TEXTURE THEN FLOODING IT
        targetPositionData = new Texture2D(256, 256);
        Color[,] tpd = new Color[256, 256];

        targetPositionData = TextureBlackout(targetPositionData, 256, 256);
        tpd = ArrayBlackout(tpd, 256, 256);

        targetNormData = new Texture2D(256, 256);
        Color[,] tnd = new Color[256, 256];

        targetNormData = TextureBlackout(targetNormData, 256, 256);
        tnd = ArrayBlackout(tnd, 256, 256);

        List<Vector2> bunnyUVRed = new List<Vector2>();
        List<Vector3> bunnyPosRed = new List<Vector3>();
        List<Vector3> bunnyNormRed = new List<Vector3>();

        for (int i=0; i<uvs.Length; i++) {
            if (bunnyUVRed.Contains(uvs[i])) {
                continue;
            }

            bunnyUVRed.Add(uvs[i]);
            bunnyPosRed.Add(verts[i]);
            bunnyNormRed.Add(normals[i]);
        }

        
        for (int i=0; i<bunnyUVRed.Count; i++) {

            Vector3 pos = bunnyPosRed[i];
            //pos *= 5f;

            float red = (pos.x * 0.5f) + 0.5f;
            float green = (pos.y * 0.5f) + 0.5f;
            float blue = (pos.z * 0.5f) + 0.5f;

            Color c = new Color(red, green, blue, 1.0f);

            Vector3 n = bunnyNormRed[i];
            float nx = (n.x * 0.5f) + 0.5f;
            float ny = (n.y * 0.5f) + 0.5f;
            float nz = (n.z * 0.5f) + 0.5f;
            Color cn = new Color(nx, ny, nz, 1.0f);

            int x = (int) Mathf.Floor(bunnyUVRed[i].x * 255);
            int y = (int) Mathf.Floor(bunnyUVRed[i].y * 255);

            // Debug.Log(c);
            // Debug.Log(bunnyU[i]);

            targetPositionData.SetPixel(x, y, c);
            tpd[x, y] = c;

            targetNormData.SetPixel(x, y, cn);
            tnd[x, y] = cn;
        }
        targetPositionData.Apply();
        byte[] by = targetPositionData.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/../TARGET_POS_DATA_INIT.png", by);

        targetNormData.Apply();
        by = targetNormData.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/../TARGET_NORM_DATA_INT.png", by);

        // BLOOM THE BUN
        bool[,] floodedBunnyPos = new bool[256,256];
        bool blackLeftBunnyPos = true;
        int iterationsOfBloomBunnyPos = 0;
        Color texColorBunnyPos = Color.white;


        while (iterationsOfBloomBunnyPos < 75 && blackLeftBunnyPos) {
            blackLeftBunnyPos = false;

            for (int i=0; i<256; i++) {
                for (int j=0; j<256; j++) {
                    texColorBunnyPos = tpd[i, j];

                    if (texColorBunnyPos == Color.black) {
                        blackLeftBunnyPos = true;
                        continue;
                    }

                    if (floodedBunnyPos[i,j] == true) {
                        continue;
                    }

                    if (i>0) {
                        if (j < 255 && tpd[i-1, j+1] == Color.black) {
                            tpd[i-1, j+1] = texColorBunnyPos;
                            floodedBunnyPos[i-1, j+1] = true;
                        }
                        if (j > 0 && tpd[i-1, j-1] == Color.black) {
                            tpd[i-1, j-1] = texColorBunnyPos;
                            floodedBunnyPos[i-1, j-1] = true;
                        }
                        if (tpd[i-1, j] == Color.black) {
                            tpd[i-1, j] = texColorBunnyPos;
                            floodedBunnyPos[i-1, j] = true;
                        }
                    }

                    if (i < 255) {
                        if (j < 255 && tpd[i+1, j+1] == Color.black) {
                            tpd[i+1, j+1] = texColorBunnyPos;
                            floodedBunnyPos[i+1, j+1] = true;
                        }
                        if (j > 0 && tpd[i+1, j-1] == Color.black) {
                            tpd[i+1, j-1] = texColorBunnyPos; 
                            floodedBunnyPos[i+1, j-1] = true;
                        }
                        if (tpd[i+1, j] == Color.black) {
                            tpd[i+1, j] = texColorBunnyPos;
                            floodedBunnyPos[i+1, j] = true;
                        }
                    }

                    if (j<255 && tpd[i, j+1] == Color.black) {
                        tpd[i, j+1] = texColorBunnyPos;
                        floodedBunnyPos[i, j+1] = true;
                    }
                    if (j>0 && tpd[i, j-1] == Color.black) {
                        tpd[i, j-1] = texColorBunnyPos;
                        floodedBunnyPos[i, j-1] = true;
                    }

                    floodedBunnyPos[i, j] = true;
                }
            }

            for (int i=0; i<256; i++) {
                for (int j=0; j<256; j++) {
                    floodedBunnyPos[i, j] = false;
                }
            }

            iterationsOfBloomBunnyPos++;

            // targetPositionData.Apply();
        }

        bool[,] floodedBunnNorm = new bool[256, 256];
        bool blackLeftBunnNorm = true;
        int iterationsOfBloomBunnNorm = 0;
        Color texColorBunnNorm = Color.white;


        while (iterationsOfBloomBunnNorm < 75 && blackLeftBunnNorm)
        {
            blackLeftBunnNorm = false;

            for (int i = 0; i < 256; i++)
            {
                for (int j = 0; j < 256; j++)
                {
                    texColorBunnNorm = tnd[i, j];

                    if (texColorBunnNorm == Color.black)
                    {
                        blackLeftBunnNorm = true;
                        continue;
                    }

                    if (floodedBunnNorm[i, j] == true)
                    {
                        continue;
                    }

                    if (i > 0)
                    {
                        if (j < 255 && tnd[i - 1, j + 1] == Color.black)
                        {
                            tnd[i - 1, j + 1] = texColorBunnNorm;
                            floodedBunnNorm[i - 1, j + 1] = true;
                        }
                        if (j > 0 && tnd[i - 1, j - 1] == Color.black)
                        {
                            tnd[i - 1, j - 1] = texColorBunnNorm;
                            floodedBunnNorm[i - 1, j - 1] = true;
                        }
                        if (tnd[i - 1, j] == Color.black)
                        {
                            tnd[i - 1, j] = texColorBunnNorm;
                            floodedBunnyPos[i - 1, j] = true;
                        }
                    }

                    if (i < 255)
                    {
                        if (j < 255 && tnd[i + 1, j + 1] == Color.black)
                        {
                            tnd[i + 1, j + 1] = texColorBunnNorm;
                            floodedBunnNorm[i + 1, j + 1] = true;
                        }
                        if (j > 0 && tnd[i + 1, j - 1] == Color.black)
                        {
                            tnd[i + 1, j - 1] = texColorBunnNorm;
                            floodedBunnNorm[i + 1, j - 1] = true;
                        }
                        if (tnd[i + 1, j] == Color.black)
                        {
                            tnd[i + 1, j] = texColorBunnNorm;
                            floodedBunnNorm[i + 1, j] = true;
                        }
                    }

                    if (j < 255 && tnd[i, j + 1] == Color.black)
                    {
                        tnd[i, j + 1] = texColorBunnNorm;
                        floodedBunnNorm[i, j + 1] = true;
                    }
                    if (j > 0 && tnd[i, j - 1] == Color.black)
                    {
                        tnd[i, j - 1] = texColorBunnNorm;
                        floodedBunnNorm[i, j - 1] = true;
                    }

                    floodedBunnNorm[i, j] = true;
                }
            }

            for (int i = 0; i < 256; i++)
            {
                for (int j = 0; j < 256; j++)
                {
                    floodedBunnNorm[i, j] = false;
                }
            }

            iterationsOfBloomBunnNorm++;

            // targetPositionData.Apply();
        }

        for (int i=0; i<256; i++) {
            for (int j=0; j<256; j++) {
                targetPositionData.SetPixel(i, j, tpd[i, j]);
                targetNormData.SetPixel(i, j, tnd[i, j]);
            }
        }

        targetPositionData.Apply();
        targetNormData.Apply();

        by = targetPositionData.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/../TARGET_POS_DATA_END.png", by);

        by = targetNormData.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/../TARGET_NORM_DATA_END.png", by);

        // Debug.Log("triangles: "+ rabbitMesh.triangles.Length);

        // RELEVANT DATASTRUCTURES: L1Points, l1Normals
        List<Vector3> l1Points = new List<Vector3>();
        List<Vector3> l1Normals = new List<Vector3>();
        List<Vector2> l1Uvs = new List<Vector2>();
        List<int> l1FacesUsed = new List<int>();

        float maxX = 0f;
        float minX = 0f;

        float maxy = 0f;
        float miny = 0f;

        float maxZ = 0f;
        float minZ = 0f; 


        for (int i=0; i<verts.Length; i++) {
            if (verts[i].x > maxX) {
                maxX = verts[i].x;
            } else if (verts[i].x < minX) {
                minX = verts[i].x;
            }

            if (verts[i].z > maxZ) {
                maxZ = verts[i].z;
            } else if (verts[i].z < minZ) {
                minZ = verts[i].z;
            }

            if (verts[i].y > maxy) {
                maxy = verts[i].y;
            } else if (verts[i].y < miny) {
                miny = verts[i].y;
            }
        }

        //Debug.Log("Max X: " + maxX);
        //Debug.Log("Min X: " + minX);
        //Debug.Log("Min Y: " + miny);
        //Debug.Log("Max Y: " + maxy);

        //Debug.Log("Max Z: " + maxZ);
        //Debug.Log("Min Z: " + minZ);

        float greatestDistance = 0;
        float smallestDistance = 0;
        for (int i=0; i<triangles.Length; i+=3) {
            Vector3 p1 = verts[triangles[i]];
            Vector3 p2 = verts[triangles[i+1]];
            Vector3 p3 = verts[triangles[i+2]];

            Vector2 uv1 = uvs[triangles[i]];
            Vector2 uv2 = uvs[triangles[i+1]];
            Vector2 uv3 = uvs[triangles[i+2]];

            Vector3 A = p1 - p2;
            Vector3 B = p1 - p3;

            Vector2 UVA = uv1 - uv2;
            Vector2 UVB = uv1 - uv3;

            Vector3 mid = p1 + (0.5f * A) + (0.5f * B);
            Vector2 uvMid = uv1 + (0.5f * UVA) + (0.5f * UVB);

            for (int j=0; j<triangles.Length; j+=3) {
                Vector3 q1 = verts[triangles[j]];
                Vector3 q2 = verts[triangles[j + 1]];
                Vector3 q3 = verts[triangles[j+2]];

                Vector3 S = q1 - q2;
                Vector3 T = q1 - q3;

                Vector3 mid2 = q1 +  (0.5f * S) + (0.5f * T);

                float dist = Vector3.Distance(mid, mid2);

                if (dist > greatestDistance) {
                    greatestDistance = dist;
                } else if (dist < smallestDistance) {
                    smallestDistance = dist;
                }

            }
        }

        //Debug.Log("Greatest Distance: " + greatestDistance);
        //Debug.Log("Smallest Distance: " + smallestDistance);

        float diskSize = greatestDistance / 5.0f;

        selectPoints(triangles, verts, uvs, ref l1Uvs, ref l1Points, ref l1Normals, diskSize);

        // l1Packing
        l1ArrPoints = new List<Vector4>();
        l1ArrUv = new List<Vector4>();
        for (int i=0; i<l1Points.Count; i++)
        {
            Vector4 p = new Vector4(l1Points[i].x, l1Points[i].y, l1Points[i].z, 1.0f);
            Vector4 u = new Vector4(l1Uvs[i].x, l1Uvs[i].y, 1.0f, 1.0f);

            l1ArrPoints.Add(p);
            l1ArrUv.Add(u);
        }

        Debug.Log("L1ArrPoints Count: " + l1ArrPoints.Count);
        Debug.Log("l1ArrUV Count: " + l1ArrUv.Count);

        // Debug.Log("points selected length: " + l1Points.Count());

        List<Vector3> l2Points = new List<Vector3>();
        List<Vector3> l2Normals = new List<Vector3>();
        List<Vector2> l2Uvs = new List<Vector2>();
        for (int i=0; i<l1Points.Count(); i++) {
            l2Points.Add(l1Points[i]);
            l2Normals.Add(l1Normals[i]);
            l2Uvs.Add(l1Uvs[i]);
        }

        diskSize /= 2.0f;
        
        selectPoints(triangles, verts, uvs, ref l2Uvs, ref l2Points, ref l2Normals, diskSize);

        l2ArrPoints = new List<Vector4>();
        l2ArrUv = new List<Vector4>();
        for (int i = 0; i < l2Points.Count; i++)
        {
            Vector4 p = new Vector4(l2Points[i].x, l2Points[i].y, l2Points[i].z, 1.0f);
            Vector4 u = new Vector4(l2Uvs[i].x, l2Uvs[i].y, 1.0f, 1.0f);

            l2ArrPoints.Add(p);
            l2ArrUv.Add(u);
        }
        Debug.Log("L2ArrPoints Count: " + l2ArrPoints.Count);
        Debug.Log("l2ArrUV Count: " + l2ArrUv.Count);
        //Debug.Log("pointed for l2: " + l2Points.Count());

        List<Vector3> l3Points = new List<Vector3>();
        List<Vector3> l3Normals = new List<Vector3>();
        List<Vector2> l3Uvs = new List<Vector2>();
        for (int i=0; i<l2Points.Count(); i++) {
            l3Points.Add(l2Points[i]);
            l3Normals.Add(l2Normals[i]);
            l3Uvs.Add(l2Uvs[i]);
        }

        diskSize /= 2.0f;
        
        selectPoints(triangles, verts, uvs, ref l3Uvs, ref l3Points, ref l3Normals, diskSize);

        l3ArrPoints = new List<Vector4>();
        l3ArrUv = new List<Vector4>();
        for (int i = 0; i < l3Points.Count; i++)
        {
            Vector4 p = new Vector4(l3Points[i].x, l3Points[i].y, l3Points[i].z, 1.0f);
            

            l3ArrPoints.Add(p);
            
        }

        for (int i=0; i<l3Uvs.Count; i++)
        {
            Vector4 u = new Vector4(l3Uvs[i].x, l3Uvs[i].y, 1.0f, 1.0f);
            l3ArrUv.Add(u);
        }
        Debug.Log("l3ArrPoints Count: " + l3ArrPoints.Count);
        Debug.Log("l3ArrUv Count: " + l3ArrUv.Count);

        //Debug.Log("L3 Points: " + l3Points.Count());

        List<Vector3> floored = new List<Vector3>();
        List<Vector2> coordSet1 = new List<Vector2>();
        List<Vector3> coloursToUse = new List<Vector3>();
        List<Vector3> colourNormals = new List<Vector3>();

        bool contained=false;

        for (int i =0; i<l1Points.Count; i++) {
            contained = false;

            Vector2 pt = l1Uvs[i];
            coordSet1.Add(pt);

            Vector3 floor = (l1Points[i]);

            floor *= 100;
            // floor *= 5;

            floor.x = Mathf.Floor(floor.x);
            floor.y = Mathf.Floor(floor.y);
            floor.z = Mathf.Floor(floor.z);
            // for (int j=0; j<floored.Count; j++) {
            //     if (floor.x == floored[j].x && floor.y == floored[j].y) {
            //         contained = true;
            //         break;
            //     }
            // }

            // for (int j=0; j<coordSet1.Count; j++) {
            //     if (posByNormal.App)
            // }

            // if (!contained) {
                // Debug.Log("floor: " + floor.x + ", " + floor.y + ", " + floor.z);
                floored.Add(floor);


                Vector3 colour = l1Points[i];

            // colour *= 100f;
            //colour.x *= 0.5f;
            // colour.y

            //colour.x *= 5f;
            //colour.y *= 5f;
            //colour.z *= 5f;

            //colour.x += 0.5f;
            //    colour.y += 0.2f;
            //    colour.z += 0.5f;
            colour.x = (colour.x * 0.5f) + 0.5f;
            colour.y = (colour.y * 0.5f) + 0.5f;
            colour.z = (colour.z * 0.5f) + 0.5f;
            coloursToUse.Add(colour);

                Vector3 normalColor = Vector3.Normalize(l1Normals[i]);
            // normalColor *= 0.5f;
            //normalColor.x += 0.5f;
            //normalColor.y += 0.2f;
            //normalColor.z += 0.5f;

            normalColor.x = (normalColor.x * 0.5f) + 0.5f;
            normalColor.y = (normalColor.y * 0.5f) + 0.5f;
            normalColor.z = (normalColor.z * 0.5f) + 0.5f;

            colourNormals.Add(normalColor);
            // }
        }

        List<Vector3> floored2 = new List<Vector3>();
        List<Vector2> coordSet2 = new List<Vector2>();
        List<Vector3> coloursToUse2 = new List<Vector3>();
        List<Vector3> colourNormals2 = new List<Vector3>();

        bool contained2=false;

        for (int i =0; i<l2Points.Count; i++) {
            contained2 = false;
            Vector3 floor = (l2Points[i]);

            floor *= 100;

            floor.x = Mathf.Floor(floor.x);
            floor.y = Mathf.Floor(floor.y);
            floor.z = Mathf.Floor(floor.z);
            for (int j=0; j<floored2.Count; j++) {
                if (floor.x == floored2[j].x && floor.y == floored2[j].y) {
                    contained2 = true;
                    break;
                }
            }

            if (!contained2) {
                floored2.Add(floor);

                Vector3 colour = (l2Points[i]);
                // colour *= 100f;
                // colour *= 0.5f;
                //colour *= 5f;
                //colour.x += 0.5f;
                //colour.y += 0.2f;
                //colour.z += 0.5f;
                colour.x = (colour.x * 0.5f) + 0.5f;
                colour.y = (colour.y * 0.5f) + 0.5f;
                colour.z = (colour.z * 0.5f) + 0.5f;
                coloursToUse2.Add(colour);

                Vector3 normalColor = Vector3.Normalize(l2Normals[i]);
                // normalColor *= 0.5f;
                //normalColor.x += 0.5f;
                //normalColor.y += 0.2f;
                //normalColor.z += 0.5f;
                normalColor.x = (normalColor.x * 0.5f) + 0.5f;
                normalColor.y = (normalColor.y * 0.5f) + 0.5f;
                normalColor.z = (normalColor.z * 0.5f) + 0.5f;

                colourNormals2.Add(normalColor);
            }
        }

        List<Vector3> floored3 = new List<Vector3>();
        List<Vector3> coloursToUse3 = new List<Vector3>();
        List<Vector3> colourNormals3 = new List<Vector3>();

        bool contained3=false;

        for (int i =0; i<l3Points.Count; i++) {
            contained3 = false;
            Vector3 floor = (l3Points[i]);

            floor *= 100;

            floor.x = Mathf.Floor(floor.x);
            floor.y = Mathf.Floor(floor.y);
            floor.z = Mathf.Floor(floor.z);
            for (int j=0; j<floored3.Count; j++) {
                if (floor.x == floored3[j].x && floor.y == floored3[j].y) {
                    contained3 = true;
                    break;
                }
            }

            if (!contained3) {
                // Debug.Log("floor: " + floor.x + ", " + floor.y + ", " + floor.z);
                floored3.Add(floor);


                Vector3 colour = (l3Points[i]);
                //colour *= 5f;
                //// colour *= 100f;
                //// colour *= 0.5f;
                //colour.x += 0.5f;
                //colour.y += 0.2f;
                colour.x = (colour.x * 0.5f) + 0.5f;
                colour.y = (colour.y * 0.5f) + 0.5f;
                colour.z = (colour.z * 0.5f) + 0.5f;
                //colour.z += 0.5f;
                coloursToUse3.Add(colour);

                Vector3 normalColor = Vector3.Normalize(l3Normals[i]);
                //normalColor *= 0.5f;
                //normalColor.x += 0.5f;
                //normalColor.y += 0.2f;
                //normalColor.z += 0.5f;
                normalColor.x = (normalColor.x * 0.5f) + 0.5f;
                normalColor.y = (normalColor.y * 0.5f) + 0.5f;
                normalColor.z = (normalColor.z * 0.5f) + 0.5f;

                colourNormals3.Add(normalColor);
            }
        }




        // CREATE TEXTURES
        tex = new Texture2D(100, 100);
        Texture2D normalTexture = new Texture2D(100, 100);

        Color[,] layer1 = new Color[100, 100];
        Color[,] layer2 = new Color[100, 100];
        Color[,] layer3 = new Color[100, 100];

        layer1 = ArrayBlackout(layer1, 100, 100);
        layer2 = ArrayBlackout(layer2, 100, 100);
        layer3 = ArrayBlackout(layer3, 100, 100);

        Color[,] norm1 = new Color[100, 100];
        Color[,] norm2 = new Color[100, 100];
        Color[,] norm3 = new Color[100, 100];

        norm1 = ArrayBlackout(norm1, 100, 100);
        norm2 = ArrayBlackout(norm2, 100, 100);
        norm3 = ArrayBlackout(norm3, 100, 100);

        // SET TEXTURES TO BLACK
        tex = TextureBlackout(tex, 100, 100);
        normalTexture = TextureBlackout(normalTexture, 100, 100);

        

        // SET KNOWN PIXELS
        for (var i=0; i<floored.Count; i++) {
            
            Vector3 f = floored[i];
            // f.y /= 2f;
            f.x += 55f;
            f.y += 20f;



            if (f.x >= 100 || f.y >= 100) {
                // Debug.Log("Too big!" + f);
                continue;
            } else if (f.x  <0 || f.y < 0) {
                // Debug.Log("too small!" + f);
                continue;
            }

            int x = (int) Mathf.Floor(l1Uvs[i].x * 100);
            int y = (int) Mathf.Floor(l1Uvs[i].y * 100);

            layer1[x, y] = new Color(coloursToUse[i].x, coloursToUse[i].y, coloursToUse[i].z, 1f);
            norm1[x, y] = new Color(colourNormals[i].x, colourNormals[i].y, colourNormals[i].z, 1f);

            tex.SetPixel(x, y, new Color(coloursToUse[i].x, coloursToUse[i].y, coloursToUse[i].z, 1f));
            normalTexture.SetPixel(x, y, new Color(colourNormals[i].x, colourNormals[i].y, colourNormals[i].z, 1f));
        }

        tex.Apply();
        normalTexture.Apply();
       
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/../BUNNY-InitialSavedTex--LAYER1.png", bytes);
       
        bytes = normalTexture.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/../BUNNY-InitialNormalTex--LAYER1.png", bytes);


        tex = TextureBlackout(tex, 100, 100);
        normalTexture = TextureBlackout(normalTexture, 100, 100);

        // LAYER 2
        for (var i=0; i<floored2.Count; i++) {
            Vector3 f = floored2[i];
            // f.y /= 2f;
            f.x += 55f;
            f.y += 20f;

            if (f.x >= 100 || f.y >= 100) {
                // Debug.Log("Too big!" + f);
                continue;
            } else if (f.x  <0 || f.y < 0) {
                // Debug.Log("too small!" + f);
                continue;
            }

            int x = (int) Mathf.Floor(l2Uvs[i].x * 100);
            int y = (int) Mathf.Floor(l2Uvs[i].y * 100);
            

            layer2[x, y] = new Color(coloursToUse2[i].x, coloursToUse2[i].y, coloursToUse2[i].z, 1f);
            norm2[x, y] = new Color(colourNormals2[i].x, colourNormals2[i].y, colourNormals2[i].z, 1f);

            tex.SetPixel(x, y, new Color(coloursToUse2[i].x, coloursToUse2[i].y, coloursToUse2[i].z, 1f));
            normalTexture.SetPixel(x, y, new Color(colourNormals2[i].x, colourNormals2[i].y, colourNormals2[i].z, 1f));
        }

        tex.Apply();
        normalTexture.Apply();
       
        bytes = tex.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/../BUNNY-InitialSavedTex--LAYER2.png", bytes);
       
        bytes = normalTexture.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/../BUNNY-InitialNormalTex--LAYER2.png", bytes);


        tex = TextureBlackout(tex, 100, 100);
        normalTexture = TextureBlackout(normalTexture, 100, 100);
        // LAYER 3
        for (var i=0; i<floored3.Count; i++) {
            Vector3 f = floored3[i];
            // f.y /= 2f;
            f.x += 55f;
            f.y += 20f;

            if (f.x >= 100 || f.y >= 100) {
                // Debug.Log("Too big!" + f);
                continue;
            } else if (f.x  <0 || f.y < 0) {
                // Debug.Log("too small!" + f);
                continue;
            }

            int x = (int) Mathf.Floor(l3Uvs[i].x * 100);
            int y = (int) Mathf.Floor(l3Uvs[i].y * 100);

            layer3[x, y] = new Color(coloursToUse3[i].x, coloursToUse3[i].y, coloursToUse3[i].z, 1f);
            norm3[x, y] = new Color(colourNormals3[i].x, colourNormals3[i].y, colourNormals3[i].z, 1f);

            tex.SetPixel(x, y, new Color(coloursToUse3[i].x, coloursToUse3[i].y, coloursToUse3[i].z, 1f));
            normalTexture.SetPixel(x, y, new Color(colourNormals3[i].x, colourNormals3[i].y, colourNormals3[i].z, 1f));
        }

        tex.Apply();
        normalTexture.Apply();
       
        bytes = tex.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/../BUNNY-InitialSavedTex--LAYER3.png", bytes);
       
        bytes = normalTexture.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/../BUNNY-InitialNormalTex--LAYER3.png", bytes);


        // BLOOM COLOURS
        bool[,] flooded = new bool[100,100];
        bool blackLeft = true;
        int iterationsOfBloom = 0;
        Color normColor = Color.white;
        Color texColor = Color.white;

        while (iterationsOfBloom < 75 && blackLeft) {
            blackLeft = false;

            for (int i=0; i<100; i++) {
                for (int j=0; j<100; j++) {
                    texColor = layer1[i, j];
                    normColor = norm1[i, j];

                    if (texColor == Color.black) {
                        blackLeft = true;
                        continue;
                    }

                    if (flooded[i,j] == true) {
                        continue;
                    }

                    if (i>0) {
                        if (j < 99 && layer1[i-1, j+1] == Color.black) {
                            layer1[i-1, j+1] = texColor;
                            norm1[i-1, j+1] = normColor;
                            flooded[i-1, j+1] = true;
                        }
                        if (j > 0 && layer1[i-1, j-1] == Color.black) {
                            layer1[i-1, j-1] = texColor;
                            norm1[i-1, j-1] = normColor;
                            flooded[i-1, j-1] = true;
                        }
                        if (layer1[i-1, j] == Color.black) {
                            layer1[i-1, j] = texColor;
                            norm1[i-1, j] = normColor;
                            flooded[i-1, j] = true;
                        }
                    }

                    if (i < 99) {
                        if (j < 99 && layer1[i+1, j+1] == Color.black) {
                            layer1[i+1, j+1] = texColor;
                            norm1[i+1, j+1] = normColor;
                            flooded[i+1, j+1] = true;
                        }
                        if (j > 0 && layer1[i+1, j-1] == Color.black) {
                            layer1[i+1, j-1] = texColor; 
                            norm1[i+1, j-1] = normColor;
                            flooded[i+1, j-1] = true;
                        }
                        if (layer1[i+1, j] == Color.black) {
                            layer1[i+1, j] = texColor;
                            norm1[i+1, j] = normColor;
                            flooded[i+1, j] = true;
                        }
                    }

                    if (j<99 && layer1[i, j+1] == Color.black) {
                        layer1[i, j+1] = texColor;
                        norm1[i, j+1] = normColor;
                        flooded[i, j+1] = true;
                    }
                    if (j>0 && layer1[i, j-1] == Color.black) {
                        layer1[i, j-1] = texColor;
                        norm1[i, j-1] = normColor;
                        flooded[i, j-1] = true;
                    }

                    flooded[i, j] = true;
                }
            }

            for (int i=0; i<100; i++) {
                for (int j=0; j<100; j++) {
                    flooded[i, j] = false;
                }
            }

            iterationsOfBloom++;

            tex.Apply();
            normalTexture.Apply();
        }

        

        // BLOOM COLOURS
        bool[,] flooded2 = new bool[100,100];
        bool blackLeft2 = true;
        int iterationsOfBloom2 = 0;
        Color normColor2 = Color.white;
        Color texColor2 = Color.white;

        while (iterationsOfBloom2 < 75 && blackLeft2) {
            blackLeft2 = false;

            for (int i=0; i<100; i++) {
                for (int j=0; j<100; j++) {
                    texColor2 = layer2[i, j];
                    normColor2 = norm2[i, j];

                    if (texColor2 == Color.black) {
                        blackLeft2 = true;
                        continue;
                    }

                    if (flooded2[i,j] == true) {
                        continue;
                    }

                    if (i>0) {
                        if (j < 99 && layer2[i-1, j+1] == Color.black) {
                            layer2[i-1, j+1] = texColor2;
                            norm2[i-1, j+1] = normColor2;
                            flooded2[i-1, j+1] = true;
                        }
                        if (j > 0 && layer2[i-1, j-1] == Color.black) {
                            layer2[i-1, j-1] = texColor2;
                            norm2[i-1, j-1] = normColor2;
                            flooded2[i-1, j-1] = true;
                        }
                        if (layer2[i-1, j] == Color.black) {
                            layer2[i-1, j] = texColor2;
                            norm2[i-1, j] = normColor2;
                            flooded2[i-1, j] = true;
                        }
                    }

                    if (i < 99) {
                        if (j < 99 && layer2[i+1, j+1] == Color.black) {
                            layer2[i+1, j+1] = texColor2;
                            norm2[i+1, j+1] = normColor2;
                            flooded2[i+1, j+1] = true;
                        }
                        if (j > 0 && layer2[i+1, j-1] == Color.black) {
                            layer2[i+1, j-1] = texColor2; 
                            norm2[i+1, j-1] = normColor2;
                            flooded2[i+1, j-1] = true;
                        }
                        if (layer2[i+1, j] == Color.black) {
                            layer2[i+1, j] = texColor2;
                            norm2[i+1, j] = normColor2;
                            flooded2[i+1, j] = true;
                        }
                    }

                    if (j<99 && layer2[i, j+1] == Color.black) {
                        layer2[i, j+1] = texColor2;
                        norm2[i, j+1] = normColor2;
                        flooded2[i, j+1] = true;
                    }
                    if (j>0 && layer2[i, j-1] == Color.black) {
                        layer2[i, j-1] = texColor2;
                        norm2[i, j-1] = normColor2;
                        flooded2[i, j-1] = true;
                    }

                    flooded2[i, j] = true;
                }
            }

            for (int i=0; i<100; i++) {
                for (int j=0; j<100; j++) {
                    flooded2[i, j] = false;
                }
            }

            iterationsOfBloom2++;
        }


        // BLOOM COLOURS
        bool[,] flooded3 = new bool[100,100];
        bool blackLeft3 = true;
        int iterationsOfBloom3 = 0;
        Color normColor3 = Color.white;
        Color texColor3 = Color.white;

        while (iterationsOfBloom3 < 75 && blackLeft3) {
            blackLeft3 = false;

            for (int i=0; i<100; i++) {
                for (int j=0; j<100; j++) {
                    texColor3 = layer3[i, j];
                    normColor3 = norm3[i, j];

                    if (texColor3 == Color.black) {
                        blackLeft3 = true;
                        continue;
                    }

                    if (flooded3[i,j] == true) {
                        continue;
                    }

                    if (i>0) {
                        if (j < 99 && layer3[i-1, j+1] == Color.black) {
                            layer3[i-1, j+1] = texColor3;
                            norm3[i-1, j+1] = normColor3;
                            flooded3[i-1, j+1] = true;
                        }
                        if (j > 0 && layer3[i-1, j-1] == Color.black) {
                            layer3[i-1, j-1] = texColor3;
                            norm3[i-1, j-1] = normColor3;
                            flooded3[i-1, j-1] = true;
                        }
                        if (layer3[i-1, j] == Color.black) {
                            layer3[i-1, j] = texColor3;
                            norm3[i-1, j] = normColor3;
                            flooded3[i-1, j] = true;
                        }
                    }

                    if (i < 99) {
                        if (j < 99 && layer3[i+1, j+1] == Color.black) {
                            layer3[i+1, j+1] = texColor3;
                            norm3[i+1, j+1] = normColor3;
                            flooded3[i+1, j+1] = true;
                        }
                        if (j > 0 && layer3[i+1, j-1] == Color.black) {
                            layer3[i+1, j-1] = texColor3; 
                            norm3[i+1, j-1] = normColor3;
                            flooded3[i+1, j-1] = true;
                        }
                        if (layer3[i+1, j] == Color.black) {
                            layer3[i+1, j] = texColor3;
                            norm3[i+1, j] = normColor3;
                            flooded3[i+1, j] = true;
                        }
                    }

                    if (j<99 && layer3[i, j+1] == Color.black) {
                        layer3[i, j+1] = texColor3;
                        norm3[i, j+1] = normColor3;
                        flooded3[i, j+1] = true;
                    }
                    if (j>0 && layer3[i, j-1] == Color.black) {
                        layer3[i, j-1] = texColor3;
                        norm3[i, j-1] = normColor3;
                        flooded3[i, j-1] = true;
                    }

                    flooded3[i, j] = true;
                }
            }

            for (int i=0; i<100; i++) {
                for (int j=0; j<100; j++) {
                    flooded3[i, j] = false;
                }
            }

            iterationsOfBloom3++;
        }
        

        // PRINT
       
        thatTex = tex;


        jitterPos3D = new Texture3D(100, 100, 3, TextureFormat.RGBAHalf, 0);
        jitterNorm3D = new Texture3D(100, 100, 3, TextureFormat.RGBAHalf, 0);

        _level1 = new Texture2D(100, 100);
        _level2 = new Texture2D(100, 100);
        _level3 = new Texture2D(100, 100);

        _norm1 = new Texture2D(100, 100);
        _norm2 = new Texture2D(100, 100);
        _norm3 = new Texture2D(100, 100);

        for (int i=0; i<100; i++) {
            for (int j=0; j<100; j++) {
                jitterPos3D.SetPixel(i, j, 0, layer1[i,j]);
                jitterNorm3D.SetPixel(i, j, 0, norm1[i, j]);
                _level1.SetPixel(i, j, layer1[i, j]);
                _norm1.SetPixel(i, j, norm1[i, j]);

                jitterPos3D.SetPixel(i, j, 1, layer2[i,j]);
                jitterNorm3D.SetPixel(i, j, 1, norm2[i, j]);
                _level2.SetPixel(i, j, layer2[i,j]);
                _norm2.SetPixel(i, j, norm2[i, j]);

                jitterPos3D.SetPixel(i, j, 2, layer3[i,j]);
                jitterNorm3D.SetPixel(i, j, 2, norm3[i,j]);
                _level3.SetPixel(i, j, layer3[i,j]);
                _norm3.SetPixel(i, j, norm3[i, j]);
            }
        }

        jitterPos3D.Apply();
        jitterNorm3D.Apply();

        _level1.Apply();
        _level2.Apply();
        _level3.Apply();

        _norm1.Apply();
        _norm2.Apply();
        _norm3.Apply();

        bytes = _level1.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/../LEVEL1.png", bytes);

        bytes = _level2.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/../LEVEL2.png", bytes);

        bytes = _level3.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/../LEVEL3.png", bytes);

        bytes = _norm1.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/../NORM1.png", bytes);

        bytes = _norm2.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/../NORM2.png", bytes);

        bytes = _norm3.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/../NORM3.png", bytes);

        bytes = sphereNormalMap.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/../SPHERENORMS.png", bytes);

        bytes = spherePositionMap.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/../SPHEREPOS.png", bytes);

        rando = new Texture2D(100, 100);
        for (int x=0; x<100; x++)
        {
            for (int y = 0; y<100; y++)
            {
                Color c = Color.white;

                float a = Random.Range(0.0f, 1.0f);
                float b = Random.Range(0.0f, 1.0f);

                c.r = a;
                c.g = b;

                rando.SetPixel(x, y, c);
            }
        }
        rando.Apply();


        bytes = rando.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/../RANDO.png", bytes);

        // EXTRA SETTING
        GameObject r = GameObject.Find("gdef");
        bunny = r;
        Mesh m = r.GetComponent<MeshFilter>().mesh;
        m.SetIndices(m.GetIndices(0), MeshTopology.Triangles, 0);
        r.GetComponent<Renderer>().material.SetTexture("_JitterTable", jitterPos3D);
        r.GetComponent<Renderer>().material.SetTexture("_TargetPositionMap", targetPositionData);
        r.GetComponent<Renderer>().material.SetTexture("_TargetNormalMapFull", targetNormData);
        r.GetComponent<Renderer>().material.SetTexture("_SourceTexture", (Texture2D)sphere.GetComponent<MeshRenderer>().material.GetTexture("_MainTex"));
        r.GetComponent<Renderer>().material.SetTexture("_SphereNormalMap", sphereNormalMap);
        r.GetComponent<Renderer>().material.SetTexture("_SpherePosMap", spherePositionMap);
        r.GetComponent<Renderer>().material.SetTexture("_TargetNormalMap", jitterNorm3D);
        r.GetComponent<Renderer>().material.SetVectorArray("_SourceNormals", sourceNormals);
        r.GetComponent<Renderer>().material.SetVectorArray("_SourcePos", sourcePositions);
        r.GetComponent<Renderer>().material.SetVectorArray("_SourceUvs", sourceUvs);

        r.GetComponent<Renderer>().material.SetVectorArray("_TarL1PT", l1ArrPoints.ToArray());
        r.GetComponent<Renderer>().material.SetVectorArray("_TarL1UV", l1ArrUv.ToArray());

        r.GetComponent<Renderer>().material.SetVectorArray("_TarL2PT", l2ArrPoints.ToArray());
        r.GetComponent<Renderer>().material.SetVectorArray("_TarL2UV", l2ArrUv.ToArray());

        r.GetComponent<Renderer>().material.SetVectorArray("_TarL3PT", l3ArrPoints.ToArray());
        r.GetComponent<Renderer>().material.SetVectorArray("_TarL3UV", l3ArrUv.ToArray());

        r.GetComponent<Renderer>().material.SetTexture("_Rando", rando);

        r.GetComponent<Renderer>().material.SetTexture("_Level1", _level1);
        r.GetComponent<Renderer>().material.SetTexture("_Level2", _level2);
        r.GetComponent<Renderer>().material.SetTexture("_Level3", _level3);

        r.GetComponent<Renderer>().material.SetTexture("_Norm1", _norm1);
        r.GetComponent<Renderer>().material.SetTexture("_Norm2", _norm2);
        r.GetComponent<Renderer>().material.SetTexture("_Norm3", _norm3);

    }

    // Update is called once per frame
    void Update()
    {
        // EXTRA SETTING
        GameObject r = GameObject.Find("gdef");
        bunny = r;
        Mesh m = r.GetComponent<MeshFilter>().mesh;
        m.SetIndices(m.GetIndices(0), MeshTopology.Triangles, 0);
        r.GetComponent<Renderer>().material.SetTexture("_JitterTable", jitterPos3D);
        r.GetComponent<Renderer>().material.SetTexture("_TargetPositionMap", targetPositionData);
        r.GetComponent<Renderer>().material.SetTexture("_TargetNormalMapFull", targetNormData);
        r.GetComponent<Renderer>().material.SetTexture("_SourceTexture", (Texture2D)sphere.GetComponent<MeshRenderer>().material.GetTexture("_MainTex"));
        r.GetComponent<Renderer>().material.SetTexture("_SphereNormalMap", sphereNormalMap);
        r.GetComponent<Renderer>().material.SetTexture("_SpherePosMap", spherePositionMap);
        r.GetComponent<Renderer>().material.SetTexture("_TargetNormalMap", jitterNorm3D);
        r.GetComponent<Renderer>().material.SetVectorArray("_SourceNormals", sourceNormals);
        r.GetComponent<Renderer>().material.SetVectorArray("_SourcePos", sourcePositions);
        r.GetComponent<Renderer>().material.SetVectorArray("_SourceUvs", sourceUvs);

        r.GetComponent<Renderer>().material.SetVectorArray("_TarL1PT", l1ArrPoints.ToArray());
        r.GetComponent<Renderer>().material.SetVectorArray("_TarL1UV", l1ArrUv.ToArray());

        r.GetComponent<Renderer>().material.SetVectorArray("_TarL2PT", l2ArrPoints.ToArray());
        r.GetComponent<Renderer>().material.SetVectorArray("_TarL2UV", l2ArrUv.ToArray());

        r.GetComponent<Renderer>().material.SetVectorArray("_TarL3PT", l3ArrPoints.ToArray());
        r.GetComponent<Renderer>().material.SetVectorArray("_TarL3UV", l3ArrUv.ToArray());

        r.GetComponent<Renderer>().material.SetTexture("_Rando", rando);

        r.GetComponent<Renderer>().material.SetTexture("_Level1", _level1);
        r.GetComponent<Renderer>().material.SetTexture("_Level2", _level2);
        r.GetComponent<Renderer>().material.SetTexture("_Level3", _level3);

        r.GetComponent<Renderer>().material.SetTexture("_Norm1", _norm1);
        r.GetComponent<Renderer>().material.SetTexture("_Norm2", _norm2);
        r.GetComponent<Renderer>().material.SetTexture("_Norm3", _norm3);
    }


    public Texture2D TextureBlackout(Texture2D tex, int width, int height) {
        for (int i=0; i<width; i++) {
            for (int j=0; j<height; j++) {
                tex.SetPixel(i, j, Color.black);
            }        
        }
        tex.Apply();

        return tex;
    }

    public Color[,] ArrayBlackout(Color[,] tex, int width, int height) {
        for (int i=0; i<width; i++) {
            for (int j=0; j<height; j++) {
                tex[i,j] = Color.black;
            }
        }
        return tex;
    }

    public Texture3D TextureBlackout(Texture3D texture, int width, int height, int depth) {
         
        for (int z=0; z<depth; z++) {
            for (int y=0; y<height; y++) {
                for (int x=0; x<width; x++) {
                    texture.SetPixel(x, y, z, Color.black);
                }
            }
        }

        texture.Apply();
        return texture;
    }

    void selectPoints(int[] faces, Vector3[] verts, Vector2[] uvs, ref List<Vector2> uvPointsToUse, ref List<Vector3>  selected, ref List<Vector3> normals, float radius) {
        // for each of the faces, check if it's too close to one of the selected
        System.Random random = new System.Random();
        for (int i=0; i < faces.Length; i+=3) {
            Vector3 p1 = verts[faces[i]];
            Vector3 p2 = verts[faces[i+1]];
            Vector3 p3 = verts[faces[i+2]];

            Vector2 uv1 = uvs[faces[i]];
            Vector2 uv2 = uvs[faces[i+1]];
            Vector2 uv3 = uvs[faces[i+2]];

            Vector3 A = p1 - p2;
            Vector3 B = p1 - p3;

            Vector2 UVA = uv1 - uv2;
            Vector2 UVB = uv1 - uv3;

            Vector3 mid = p1 + (0.5f * A) + (0.5f * B);
            Vector2 uvMid = uv1 + (0.5f * UVA) + (0.5f * UVB);

            bool choosing = true;
            for (int j=0; j<selected.Count(); j++) {
                float dist = Vector3.Distance(mid, selected[j]);

                if (dist < radius) {
                    choosing = false;
                    break;
                }

            }

            if (choosing) {

                float r1 = (float) (random.NextDouble());
                float r2 = (float) (random.NextDouble());
    
                while (r1 + r2 > 1.00) {
                    r1 = (float) random.NextDouble();
                    r2 = (float) random.NextDouble();
                }

                mid = p1 + (r1 * A) + (r2 * B);
                uvMid = uv1 + (r1 * UVA) + (r2 * UVB);

                Vector3 X = p2 - p1;
                Vector3 Y = p3 - p1;

                Vector3 normal = Vector3.Cross(X, Y);

                normal = Vector3.Normalize(normal);

                uvPointsToUse.Add(uvMid);
                selected.Add(mid);
                normals.Add(normal);
                
            }
        }
    }

}