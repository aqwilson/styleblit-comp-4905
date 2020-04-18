using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;

public class StanfordShadingScript : MonoBehaviour
{
    Texture2D tex;

    public GameObject target;
    public Mesh targetMesh;
    Mesh sphereMesh;
    GameObject sphere;
    Texture2D sphereNormalMap;
    Texture2D spherePositionMap;

    Vector4[] sourceNormals;
    Vector4[] sourceUvs;
    Vector4[] sourcePositions;

    Texture2D targetPositionData;
    Texture2D targetNormData;

    List<Vector4> l1ArrPoints;
    List<Vector4> l1ArrUv;
    List<Vector4> l2ArrPoints, l2ArrUv, l3ArrPoints, l3ArrUv;
    Texture2D jitterTex;

    Texture2D _level1;
    Texture2D _level2;
    Texture2D _level3;

    // Start is called before the first frame update
    void Start()
    {
        // Start with sphere-based data
        sphere = GameObject.Find("Sphere");
        sphereMesh = GameObject.Find("Sphere").GetComponent<MeshFilter>().mesh;
       
        System.Random random = new System.Random();

        // PART ONE: THE SOURCE
        // fetch the sphere's data
        Vector3[] sphereVerts = sphereMesh.vertices;
        Vector2[] sphereUvs = sphereMesh.uv;
        Vector3[] sphereNormals = sphereMesh.normals;

        // make the sphere a little smaller
        for (int i = 0; i < sphereVerts.Length; i++)
        {
            sphereVerts[i] *= 0.25f;
        }

        List<Vector4> refinedSphereNorms = new List<Vector4>();
        List<Vector4> refinedSpherePositions = new List<Vector4>();
        List<Vector4> refinedSphereUvs = new List<Vector4>();

        // step one: build out the arrays
        for (int i=0; i<sphereNormals.Length; i++) {
            // pack data into colours
            float red = (sphereNormals[i].x * 0.5f) + 0.5f;
            float green = (sphereNormals[i].y + 0.5f) + 0.5f;
            float blue = (sphereNormals[i].z + 0.5f) + 0.5f;

            // we move y up because most models' positions are NOT below 0 on Y
            float a = (sphereVerts[i].x * 0.5f) + 0.5f;
            float b = ((sphereVerts[i].y + 0.125f) * 0.5f) + 0.5f;
            float c = (sphereVerts[i].z * 0.5f) + 0.5f;

            Vector4 sn = new Vector4(red, green, blue, 1.0f);

            if (refinedSphereNorms.Contains(sn)) {
                continue;
            }

            refinedSphereNorms.Add(sn);
            refinedSpherePositions.Add(new Vector4(a, b, c, 1.0f));
            refinedSphereUvs.Add(new Vector4(sphereUvs[i].x, sphereUvs[i].y, 0.0f, 0.0f));
        }

        // convert to array
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
            // same process of packing colours based on UV coordinates
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


        // FLOOD FILL NORMALS
        bool[,] floodedSphereNormals = new bool[512,512];
        bool blackLeftSphereNormals = true;
        int iterationsOfBloomSphereNormals = 0;
        Color texColorSphereNormals = Color.white;
        Color tcsPos = Color.white;

        // flood fill by finding empty cells and filling them
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

        // "save to texture"
        sphereNormalMap = sphereNormalTexture;
        spherePositionMap = spherePositionTexture;

        target = GameObject.Find("gdef");
        targetMesh = target.GetComponent<MeshFilter>().mesh;

        Vector3[] verts = targetMesh.vertices;
        Vector3[] normals = targetMesh.normals;
        Vector2[] uvs = targetMesh.uv;
        int[] triangles = targetMesh.triangles;

        // ADDING TARGET'S UV DATA TO A LARGE TEXTURE THEN FLOODING IT
        // we leave everything as black to start to make our lives easier
        targetPositionData = new Texture2D(256, 256);
        Color[,] tpd = new Color[256, 256];

        targetPositionData = TextureBlackout(targetPositionData, 256, 256);
        tpd = ArrayBlackout(tpd, 256, 256);

        targetNormData = new Texture2D(256, 256);
        Color[,] tnd = new Color[256, 256];

        targetNormData = TextureBlackout(targetNormData, 256, 256);
        tnd = ArrayBlackout(tnd, 256, 256);

        List<Vector2> targetUVRed = new List<Vector2>();
        List<Vector3> targetPosRed = new List<Vector3>();
        List<Vector3> targetNormRed = new List<Vector3>();

        // make sure we don't have any duplicates
        for (int i=0; i<uvs.Length; i++) {
            if (targetUVRed.Contains(uvs[i])) {
                continue;
            }

            targetUVRed.Add(uvs[i]);
            targetPosRed.Add(verts[i]);
            targetNormRed.Add(normals[i]);
        }

        // Convert into colours
        for (int i=0; i<targetUVRed.Count; i++) {

            Vector3 pos = targetPosRed[i];

            float red = (pos.x * 0.5f) + 0.5f;
            float green = (pos.y * 0.5f) + 0.5f;
            float blue = (pos.z * 0.5f) + 0.5f;

            Color c = new Color(red, green, blue, 1.0f);

            Vector3 n = targetNormRed[i];
            float nx = (n.x * 0.5f) + 0.5f;
            float ny = (n.y * 0.5f) + 0.5f;
            float nz = (n.z * 0.5f) + 0.5f;
            Color cn = new Color(nx, ny, nz, 1.0f);

            int x = (int) Mathf.Floor(targetUVRed[i].x * 255);
            int y = (int) Mathf.Floor(targetUVRed[i].y * 255);

            targetPositionData.SetPixel(x, y, c);
            tpd[x, y] = c;

            targetNormData.SetPixel(x, y, cn);
            tnd[x, y] = cn;
        }

        // save initial data to file so that we can make sure it's correct
        targetPositionData.Apply();
        byte[] by = targetPositionData.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/../TARGET_POS_DATA_INIT.png", by);

        targetNormData.Apply();
        by = targetNormData.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/../TARGET_NORM_DATA_INT.png", by);

        // flood fill using a similar method to the sphere, just with arrays first
        floodFillColours(ref tpd, 256);
        floodFillColours(ref tnd, 256);

        // fill textures
        for (int i=0; i<256; i++) {
            for (int j=0; j<256; j++) {
                targetPositionData.SetPixel(i, j, tpd[i, j]);
                targetNormData.SetPixel(i, j, tnd[i, j]);
            }
        }

        // save
        targetPositionData.Apply();
        targetNormData.Apply();

        by = targetPositionData.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/../TARGET_POS_DATA_END.png", by);

        by = targetNormData.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/../TARGET_NORM_DATA_END.png", by);

        // Find the greatest distance so that we can find a reasonably small distribution of points
        float greatestDistance = 0;
        List<Vector3> mps = new List<Vector3>();
        for (int i=0; i<triangles.Length; i+=3) {
            Vector3 p1 = verts[triangles[i]];
            Vector3 p2 = verts[triangles[i+1]];
            Vector3 p3 = verts[triangles[i+2]];

            Vector3 A = p1 - p2;
            Vector3 B = p1 - p3;

            Vector3 mid = p1 + (0.5f * A) + (0.5f * B);
            mps.Add(mid);
        }

        for (int i = 0; i < mps.Count; i++)
        {
            Vector3 m1 = mps[i];

            for (int j = i + 1; j < mps.Count; j++)
            {
                Vector3 m2 = mps[j];
                float dist = Vector3.Distance(m1, m2);

                if (dist > greatestDistance)
                {
                    greatestDistance = dist;
                }
            }
        }

        Debug.Log("Greatest Distance: " + greatestDistance);
        float diskSize = greatestDistance / 7.50f;

        // select and flood fill the first level using a Poisson distribution
        List<Vector3> l1Points = new List<Vector3>();
        List<Vector2> l1Uvs = new List<Vector2>();
        selectPoints(triangles, verts, uvs, ref l1Uvs, ref l1Points, diskSize, 1);

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

        // Same process for second level of seed points, except here we take all of level one's points as well
        List<Vector3> l2Points = new List<Vector3>();
        List<Vector2> l2Uvs = new List<Vector2>();
        for (int i=0; i<l1Points.Count(); i++) {
            l2Points.Add(l1Points[i]);
            l2Uvs.Add(l1Uvs[i]);
        }

        diskSize /= 2.0f;
        
        selectPoints(triangles, verts, uvs, ref l2Uvs, ref l2Points, diskSize, 2);

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

        // same idea for the third set of seed points
        List<Vector3> l3Points = new List<Vector3>();
        List<Vector2> l3Uvs = new List<Vector2>();
        for (int i=0; i<l2Points.Count(); i++) {
            l3Points.Add(l2Points[i]);
            l3Uvs.Add(l2Uvs[i]);
        }

        diskSize /= 2.0f;
        
        selectPoints(triangles, verts, uvs, ref l3Uvs, ref l3Points, diskSize, 3);

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

        // Convert into colours, I wish I had made a function for this
        List<Vector3> floored = new List<Vector3>();
        List<Vector2> coordSet1 = new List<Vector2>();
        List<Vector3> coloursToUse = new List<Vector3>();

        for (int i =0; i<l1Points.Count; i++) {

            Vector2 pt = l1Uvs[i];
            coordSet1.Add(pt);

            Vector3 floor = (l1Points[i]);

            floor *= 100;

            floor.x = Mathf.Floor(floor.x);
            floor.y = Mathf.Floor(floor.y);
            floor.z = Mathf.Floor(floor.z);

            floored.Add(floor);

            Vector3 colour = l1Points[i];
            colour.x = (colour.x * 0.5f) + 0.5f;
            colour.y = (colour.y * 0.5f) + 0.5f;
            colour.z = (colour.z * 0.5f) + 0.5f;
            coloursToUse.Add(colour);
        }

        List<Vector3> floored2 = new List<Vector3>();
        List<Vector2> coordSet2 = new List<Vector2>();
        List<Vector3> coloursToUse2 = new List<Vector3>();

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
                colour.x = (colour.x * 0.5f) + 0.5f;
                colour.y = (colour.y * 0.5f) + 0.5f;
                colour.z = (colour.z * 0.5f) + 0.5f;
                coloursToUse2.Add(colour);
            }
        }

        List<Vector3> floored3 = new List<Vector3>();
        List<Vector3> coloursToUse3 = new List<Vector3>();

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
                floored3.Add(floor);

                Vector3 colour = (l3Points[i]);
                colour.x = (colour.x * 0.5f) + 0.5f;
                colour.y = (colour.y * 0.5f) + 0.5f;
                colour.z = (colour.z * 0.5f) + 0.5f;
                coloursToUse3.Add(colour);
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

        // SET KNOWN PIXELS
        for (var i=0; i<floored.Count; i++) {
            
            int x = (int) Mathf.Floor(l1Uvs[i].x * 100);
            int y = (int) Mathf.Floor(l1Uvs[i].y * 100);

            layer1[x, y] = new Color(coloursToUse[i].x, coloursToUse[i].y, coloursToUse[i].z, 1f);
            tex.SetPixel(x, y, new Color(coloursToUse[i].x, coloursToUse[i].y, coloursToUse[i].z, 1f));
        }

        tex.Apply();
       
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/../BUNNY-InitialSavedTex--LAYER1.png", bytes);

        // same for layer 2
        tex = TextureBlackout(tex, 100, 100);

        // LAYER 2
        for (var i=0; i<floored2.Count; i++) {
            int x = (int) Mathf.Floor(l2Uvs[i].x * 100);
            int y = (int) Mathf.Floor(l2Uvs[i].y * 100);
            
            layer2[x, y] = new Color(coloursToUse2[i].x, coloursToUse2[i].y, coloursToUse2[i].z, 1f);
            tex.SetPixel(x, y, new Color(coloursToUse2[i].x, coloursToUse2[i].y, coloursToUse2[i].z, 1f));
        }

        tex.Apply();
       
        bytes = tex.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/../BUNNY-InitialSavedTex--LAYER2.png", bytes);

        // same for layer 3
        tex = TextureBlackout(tex, 100, 100);
        // LAYER 3
        for (var i=0; i<floored3.Count; i++) {
           
            int x = (int) Mathf.Floor(l3Uvs[i].x * 100);
            int y = (int) Mathf.Floor(l3Uvs[i].y * 100);

            layer3[x, y] = new Color(coloursToUse3[i].x, coloursToUse3[i].y, coloursToUse3[i].z, 1f);
            tex.SetPixel(x, y, new Color(coloursToUse3[i].x, coloursToUse3[i].y, coloursToUse3[i].z, 1f));
        }

        tex.Apply();
        normalTexture.Apply();
       
        bytes = tex.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/../BUNNY-InitialSavedTex--LAYER3.png", bytes);

        // flood fill
        floodFillColours(ref layer1, 100);
        floodFillColours(ref layer2, 100);
        floodFillColours(ref layer3, 100);
        
        // PRINT
        _level1 = new Texture2D(100, 100);
        _level2 = new Texture2D(100, 100);
        _level3 = new Texture2D(100, 100);

        for (int i=0; i<100; i++) {
            for (int j=0; j<100; j++) {
                _level1.SetPixel(i, j, layer1[i, j]);
                _level2.SetPixel(i, j, layer2[i,j]);
                _level3.SetPixel(i, j, layer3[i,j]);
            }
        }

        _level1.Apply();
        _level2.Apply();
        _level3.Apply();

        bytes = _level1.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/../LEVEL1.png", bytes);

        bytes = _level2.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/../LEVEL2.png", bytes);

        bytes = _level3.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/../LEVEL3.png", bytes);

        bytes = sphereNormalMap.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/../SPHERENORMS.png", bytes);

        bytes = spherePositionMap.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/../SPHEREPOS.png", bytes);

        jitterTex = new Texture2D(100, 100);
        for (int x=0; x<100; x++)
        {
            for (int y = 0; y<100; y++)
            {
                Color c = Color.white;

                float a = Random.Range(0.0f, 1.0f);
                float b = Random.Range(0.0f, 1.0f);

                c.r = a;
                c.g = b;

                jitterTex.SetPixel(x, y, c);
            }
        }
        jitterTex.Apply();


        bytes = jitterTex.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/../JitterTex.png", bytes);

        // Set everything and send it to the shader!
        GameObject r = GameObject.Find("gdef");
        target = r;
        Mesh m = r.GetComponent<MeshFilter>().mesh;
        m.SetIndices(m.GetIndices(0), MeshTopology.Triangles, 0);
        r.GetComponent<Renderer>().material.SetTexture("_TargetPositionMap", targetPositionData);
        r.GetComponent<Renderer>().material.SetTexture("_TargetNormalMapFull", targetNormData);
        r.GetComponent<Renderer>().material.SetTexture("_SphereNormalMap", sphereNormalMap);
        r.GetComponent<Renderer>().material.SetTexture("_SpherePosMap", spherePositionMap);
        r.GetComponent<Renderer>().material.SetVectorArray("_SourceNormals", sourceNormals);
        r.GetComponent<Renderer>().material.SetVectorArray("_SourcePos", sourcePositions);
        r.GetComponent<Renderer>().material.SetVectorArray("_SourceUvs", sourceUvs);

        r.GetComponent<Renderer>().material.SetVectorArray("_TarL1PT", l1ArrPoints.ToArray());
        r.GetComponent<Renderer>().material.SetVectorArray("_TarL1UV", l1ArrUv.ToArray());

        r.GetComponent<Renderer>().material.SetVectorArray("_TarL2PT", l2ArrPoints.ToArray());
        r.GetComponent<Renderer>().material.SetVectorArray("_TarL2UV", l2ArrUv.ToArray());

        r.GetComponent<Renderer>().material.SetVectorArray("_TarL3PT", l3ArrPoints.ToArray());
        r.GetComponent<Renderer>().material.SetVectorArray("_TarL3UV", l3ArrUv.ToArray());

        r.GetComponent<Renderer>().material.SetTexture("_Jitter", jitterTex);

        r.GetComponent<Renderer>().material.SetTexture("_Level1", _level1);
        r.GetComponent<Renderer>().material.SetTexture("_Level2", _level2);
        r.GetComponent<Renderer>().material.SetTexture("_Level3", _level3);
    }

    // Update is called once per frame
    void Update()
    {
        // EXTRA SETTING
        GameObject r = GameObject.Find("gdef");
        target = r;
        Mesh m = r.GetComponent<MeshFilter>().mesh;
        MeshRenderer sphereRenderer = sphere.GetComponent<MeshRenderer > ();
        Material mat = sphereRenderer.material;
        m.SetIndices(m.GetIndices(0), MeshTopology.Triangles, 0);
        r.GetComponent<Renderer>().material.SetTexture("_TargetPositionMap", targetPositionData);
        r.GetComponent<Renderer>().material.SetTexture("_TargetNormalMapFull", targetNormData);
        r.GetComponent<Renderer>().material.SetTexture("_SphereNormalMap", sphereNormalMap);
        r.GetComponent<Renderer>().material.SetTexture("_SpherePosMap", spherePositionMap);
        r.GetComponent<Renderer>().material.SetVectorArray("_SourceNormals", sourceNormals);
        r.GetComponent<Renderer>().material.SetVectorArray("_SourcePos", sourcePositions);
        r.GetComponent<Renderer>().material.SetVectorArray("_SourceUvs", sourceUvs);

        r.GetComponent<Renderer>().material.SetVectorArray("_TarL1PT", l1ArrPoints.ToArray());
        r.GetComponent<Renderer>().material.SetVectorArray("_TarL1UV", l1ArrUv.ToArray());

        r.GetComponent<Renderer>().material.SetVectorArray("_TarL2PT", l2ArrPoints.ToArray());
        r.GetComponent<Renderer>().material.SetVectorArray("_TarL2UV", l2ArrUv.ToArray());

        r.GetComponent<Renderer>().material.SetVectorArray("_TarL3PT", l3ArrPoints.ToArray());
        r.GetComponent<Renderer>().material.SetVectorArray("_TarL3UV", l3ArrUv.ToArray());

        r.GetComponent<Renderer>().material.SetTexture("_Jitter", jitterTex);

        r.GetComponent<Renderer>().material.SetTexture("_Level1", _level1);
        r.GetComponent<Renderer>().material.SetTexture("_Level2", _level2);
        r.GetComponent<Renderer>().material.SetTexture("_Level3", _level3);
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

    void selectPoints(int[] faces, Vector3[] verts, Vector2[] uvs, ref List<Vector2> uvPointsToUse, ref List<Vector3> selected, float radius, int level) {
        // for each of the faces, check if it's too close to one of the selected
        System.Random random = new System.Random();
        for (int i=0; i < faces.Length; i+=3) {
            Vector3 p1 = verts[faces[i]];
            Vector3 p2 = verts[faces[i+1]];
            Vector3 p3 = verts[faces[i+2]];

            Vector3 A = p1 - p2;
            Vector3 B = p1 - p3;

            Vector3 mid = p1 + (0.5f * A) + (0.5f * B);

            // if the point is too close to some other point, then don't select it
            bool choosing = true;
            for (int j=0; j<selected.Count(); j++) {
                float dist = Vector3.Distance(mid, selected[j]);

                if (dist < radius) {
                    choosing = false;
                    break;
                }

            }

            if (!choosing)
            {
                continue;
            }

            // seelect 2 random doubles to jitter the point on the face
            float r1 = (float) (random.NextDouble());
            float r2 = (float) (random.NextDouble());
    
            // make sure that these points are NOT more than 1 - otherwise they may not be on the model
            while (r1 + r2 > 1.00) {
                r1 = (float) random.NextDouble();
                r2 = (float) random.NextDouble();
            }

            mid = p1 + (r1 * A) + (r2 * B);

            // Create spheres at the selected points, with colour based on level
            //GameObject pointSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //pointSphere.transform.position = mid;
            //pointSphere.transform.localScale *= 0.01f;

            //if (level == 1)
            //{
            //    pointSphere.GetComponent<MeshRenderer>().material.color = new Color(1.0f, 0.0f, 0.0f);
            //    //pointSphere.GetComponent<MeshRenderer>().material.SetColor("Albedo", new Color(1.0f, 0.0f, 0.0f));
            //} else if (level == 2)
            //{
            //    pointSphere.GetComponent<MeshRenderer>().material.color = new Color(0.0f, 1.0f, 0.0f);
            //   //pointSphere.GetComponent<MeshRenderer>().material.SetColor("Albedo", new Color(0.0f, 1.0f, 0.0f));
            //} else
            //{
            //    pointSphere.GetComponent<MeshRenderer>().material.color = new Color(0.0f, 0.0f, 1.0f);
            //   //pointSphere.GetComponent<MeshRenderer>().material.SetColor("Albedo", new Color(0.0f, 0.0f, 1.0f));
            //}

            // find the midpoint for the UV coordinates
            Vector2 uv1 = uvs[faces[i]];
            Vector2 uv2 = uvs[faces[i + 1]];
            Vector2 uv3 = uvs[faces[i + 2]];

            Vector2 UVA = uv1 - uv2;
            Vector2 UVB = uv1 - uv3;

            Vector2 uvMid = uv1 + (r1 * UVA) + (r2 * UVB);

            // add to the relevant lists
            uvPointsToUse.Add(uvMid);
            selected.Add(mid);
        }
    }

    void floodFillColours (ref Color[,] colArray, int dim)
    {
        bool[,] flooded = new bool[dim, dim];
        bool remainingBlack = true;
        int interations = 0;
        int bound = dim - 1;
        Color col = Color.white;

        while (interations < 75 && remainingBlack)
        {
            remainingBlack = false;

            for (int i = 0; i < dim; i++)
            {
                for (int j = 0; j < dim; j++)
                {
                    col = colArray[i, j];

                    if (col == Color.black)
                    {
                        remainingBlack = true;
                        continue;
                    }

                    if (flooded[i, j] == true)
                    {
                        continue;
                    }

                    if (i > 0)
                    {
                        if (j < bound && colArray[i - 1, j + 1] == Color.black)
                        {
                            colArray[i - 1, j + 1] = col;
                            flooded[i - 1, j + 1] = true;
                        }
                        if (j > 0 && colArray[i - 1, j - 1] == Color.black)
                        {
                            colArray[i - 1, j - 1] = col;
                            flooded[i - 1, j - 1] = true;
                        }
                        if (colArray[i - 1, j] == Color.black)
                        {
                            colArray[i - 1, j] = col;
                            flooded[i - 1, j] = true;
                        }
                    }

                    if (i < bound)
                    {
                        if (j < bound && colArray[i + 1, j + 1] == Color.black)
                        {
                            colArray[i + 1, j + 1] = col;
                            flooded[i + 1, j + 1] = true;
                        }
                        if (j > 0 && colArray[i + 1, j - 1] == Color.black)
                        {
                            colArray[i + 1, j - 1] = col;
                            flooded[i + 1, j - 1] = true;
                        }
                        if (colArray[i + 1, j] == Color.black)
                        {
                            colArray[i + 1, j] = col;
                            flooded[i + 1, j] = true;
                        }
                    }

                    if (j < bound && colArray[i, j + 1] == Color.black)
                    {
                        colArray[i, j + 1] = col;
                        flooded[i, j + 1] = true;
                    }
                    if (j > 0 && colArray[i, j - 1] == Color.black)
                    {
                        colArray[i, j - 1] = col;
                        flooded[i, j - 1] = true;
                    }

                    flooded[i, j] = true;
                }
            }

            for (int i = 0; i < dim; i++)
            {
                for (int j = 0; j < dim; j++)
                {
                    flooded[i, j] = false;
                }
            }

            interations++;
        }
    }

}