using System;
using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
//  [AddComponentMenu("Image Effects/Rendering/Screen Space Ambient Occlusion")]
public class FoW : MonoBehaviour {
   /* public enum FowSamples {
        Low = 0,
        Medium = 1,
        High = 2,
    }*/

    [Range(0.05f, 1.0f)]
    public float m_Radius = 0.4f;
  //  public FowSamples m_SampleCount = FowSamples.Medium;
    [Range(0.5f, 4.0f)]
    public float m_OcclusionIntensity = 1.5f;
   // [Range(0, 4)]
    //public int m_Blur = 2;
    [Range(1, 6)]
    public int m_Downsampling = 2;
    [Range(0.2f, 2.0f)]
    public float m_OcclusionAttenuation = 1.0f;
    [Range(0.00001f, 0.5f)]
    public float m_MinZ = 0.01f;

    public Shader m_FoWShader;
    private Material m_FoWMaterial;

    public RenderTexture m_RandomTexture;

    private bool m_Supported;

    private static Material CreateMaterial(Shader shader) {
        if(!shader)
            return null;
        Material m = new Material(shader);
        m.hideFlags = HideFlags.HideAndDontSave;
        return m;
    }
    private static void DestroyMaterial(Material mat) {
        if(mat) {
            DestroyImmediate(mat);
            mat = null;
        }
    }


    void OnDisable() {
        DestroyMaterial(m_FoWMaterial);
    }

    void Start() {
        if(!SystemInfo.supportsImageEffects || !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth)) {
            m_Supported = false;
            enabled = false;
            return;
        }

        CreateMaterials();
        if(!m_FoWMaterial ) { //|| m_FoWMaterial.passCount != 5) {
            m_Supported = false;
            enabled = false;
            return;
        }

        //CreateRandomTable (26, 0.2f);

        m_Supported = true;
    }

    void OnEnable() {
        Start();
        GetComponent<Camera>().depthTextureMode |= DepthTextureMode.DepthNormals;
    }

    private void CreateMaterials() {
        if(!m_FoWMaterial && m_FoWShader.isSupported) {
            m_FoWMaterial = CreateMaterial(m_FoWShader);
            m_FoWMaterial.SetTexture("_RandomTexture", m_RandomTexture);
        }
    }
    public Camera Cam;

    [ImageEffectOpaque]
    void OnRenderImage(RenderTexture source, RenderTexture destination) {
        if(!m_Supported || !m_FoWShader.isSupported) {
            enabled = false;
            return;
        }
        CreateMaterials();

        m_Downsampling = Mathf.Clamp(m_Downsampling, 1, 6);
        m_Radius = Mathf.Clamp(m_Radius, 0.05f, 1.0f);
        m_MinZ = Mathf.Clamp(m_MinZ, 0.00001f, 0.5f);
        m_OcclusionIntensity = Mathf.Clamp(m_OcclusionIntensity, 0.5f, 4.0f);
        m_OcclusionAttenuation = Mathf.Clamp(m_OcclusionAttenuation, 0.2f, 2.0f);
       // m_Blur = Mathf.Clamp(m_Blur, 0, 4);

        // Render SSAO term into a smaller texture
       // RenderTexture rtAO = RenderTexture.GetTemporary(source.width / m_Downsampling, source.height / m_Downsampling, 0);
        float fovY = GetComponent<Camera>().fieldOfView;
        float far = GetComponent<Camera>().farClipPlane;
        float y = Mathf.Tan(fovY * Mathf.Deg2Rad * 0.5f) * far;
        float x = y * GetComponent<Camera>().aspect;
        m_FoWMaterial.SetVector("_FarCorner", new Vector3(x, y, far));
        int noiseWidth, noiseHeight;
        if(m_RandomTexture) {
            noiseWidth = m_RandomTexture.width;
            noiseHeight = m_RandomTexture.height;
        } else {
            noiseWidth = 1; noiseHeight = 1;
        }

        Camera cam = GetComponent<Camera>();
        Transform camtr = cam.transform;
        float camNear = cam.nearClipPlane;
        float camFar = cam.farClipPlane;
        float camFov = cam.fieldOfView;
        float camAspect = cam.aspect;

        Matrix4x4 frustumCorners = Matrix4x4.identity;

        float fovWHalf = camFov * 0.5f;

        Vector3 toRight = camtr.right * camNear * Mathf.Tan(fovWHalf * Mathf.Deg2Rad) * camAspect;
        Vector3 toTop = camtr.up * camNear * Mathf.Tan(fovWHalf * Mathf.Deg2Rad);

        Vector3 topLeft = (camtr.forward * camNear - toRight + toTop);
        float camScale = topLeft.magnitude * camFar / camNear;

        topLeft.Normalize();
        topLeft *= camScale;

        Vector3 topRight = (camtr.forward * camNear + toRight + toTop);
        topRight.Normalize();
        topRight *= camScale;

        Vector3 bottomRight = (camtr.forward * camNear + toRight - toTop);
        bottomRight.Normalize();
        bottomRight *= camScale;

        Vector3 bottomLeft = (camtr.forward * camNear - toRight - toTop);
        bottomLeft.Normalize();
        bottomLeft *= camScale;

        frustumCorners.SetRow(0, topLeft);
        frustumCorners.SetRow(1, topRight);
        frustumCorners.SetRow(2, bottomRight);
        frustumCorners.SetRow(3, bottomLeft);

        var camPos = camtr.position;
       // float FdotC = camPos.y - height;
        //float paramK = (FdotC <= 0.0f ? 1.0f : 0.0f);
        //float excludeDepth = (excludeFarPixels ? 1.0f : 2.0f);
        m_FoWMaterial.SetMatrix("_FrustumCornersWS", frustumCorners);
        m_FoWMaterial.SetVector("_CameraWS", camPos);


      //  m_FoWMaterial.SetVector("_NoiseScale", new Vector3((float)rtAO.width / noiseWidth, (float)rtAO.height / noiseHeight, 0.0f));
        m_FoWMaterial.SetVector("_Params", new Vector4(
                                                 m_Radius,
                                                 m_MinZ,
                                                 1.0f / m_OcclusionAttenuation,
                                                 m_OcclusionIntensity));
        
        Shader.SetGlobalMatrix ("_Camera2World", GetComponent<Camera>().cameraToWorldMatrix );

        //bool doBlur = m_Blur > 0;
        //doBlur ? null : source
       // Graphics.Blit(null, destination, m_FoWMaterial, 0);

       /* if(doBlur) {
            // Blur SSAO horizontally
            RenderTexture rtBlurX = RenderTexture.GetTemporary(source.width, source.height, 0);
            m_FoWMaterial.SetVector("_TexelOffsetScale",
                                      new Vector4((float)m_Blur / source.width, 0, 0, 0));
            m_FoWMaterial.SetTexture("_Fow", rtAO);
            Graphics.Blit(null, rtBlurX, m_FoWMaterial, 3);
            RenderTexture.ReleaseTemporary(rtAO); // original rtAO not needed anymore

            // Blur SSAO vertically
            RenderTexture rtBlurY = RenderTexture.GetTemporary(source.width, source.height, 0);
            m_FoWMaterial.SetVector("_TexelOffsetScale",
                                      new Vector4(0, (float)m_Blur / source.height, 0, 0));
            m_FoWMaterial.SetTexture("_Fow", rtBlurX);
            Graphics.Blit(source, rtBlurY, m_FoWMaterial, 3);
            RenderTexture.ReleaseTemporary(rtBlurX); // blurX RT not needed anymore

            rtAO = rtBlurY; // AO is the blurred one now
        } */

        // Modulate scene rendering with SSAO
        m_FoWMaterial.SetTexture("_Fow", m_RandomTexture);
       // Graphics.Blit(source, destination, m_FoWMaterial, 0);
        CustomGraphicsBlit(source, destination, m_FoWMaterial, 0);
       // RenderTexture.ReleaseTemporary(rtAO);
    }

    static void CustomGraphicsBlit(RenderTexture source, RenderTexture dest, Material fxMaterial, int passNr) {
        RenderTexture.active = dest;

        fxMaterial.SetTexture("_MainTex", source);

        GL.PushMatrix();
        GL.LoadOrtho();

        fxMaterial.SetPass(passNr);

        GL.Begin(GL.QUADS);

        GL.MultiTexCoord2(0, 0.0f, 0.0f);
        GL.Vertex3(0.0f, 0.0f, 3.0f); // BL

        GL.MultiTexCoord2(0, 1.0f, 0.0f);
        GL.Vertex3(1.0f, 0.0f, 2.0f); // BR

        GL.MultiTexCoord2(0, 1.0f, 1.0f);
        GL.Vertex3(1.0f, 1.0f, 1.0f); // TR

        GL.MultiTexCoord2(0, 0.0f, 1.0f);
        GL.Vertex3(0.0f, 1.0f, 0.0f); // TL

        GL.End();
        GL.PopMatrix();
    }
    /*
    private void CreateRandomTable (int count, float minLength)
    {
        Random.seed = 1337;
        Vector3[] samples = new Vector3[count];
        // initial samples
        for (int i = 0; i < count; ++i)
            samples[i] = Random.onUnitSphere;
        // energy minimization: push samples away from others
        int iterations = 100;
        while (iterations-- > 0) {
            for (int i = 0; i < count; ++i) {
                Vector3 vec = samples[i];
                Vector3 res = Vector3.zero;
                // minimize with other samples
                for (int j = 0; j < count; ++j) {
                    Vector3 force = vec - samples[j];
                    float fac = Vector3.Dot (force, force);
                    if (fac > 0.00001f)
                        res += force * (1.0f / fac);
                }
                samples[i] = (samples[i] + res * 0.5f).normalized;
            }
        }
        // now scale samples between minLength and 1.0
        for (int i = 0; i < count; ++i) {
            samples[i] = samples[i] * Random.Range (minLength, 1.0f);
        }

        string table = string.Format ("#define SAMPLE_COUNT {0}\n", count);
        table += "const float3 RAND_SAMPLES[SAMPLE_COUNT] = {\n";
        for (int i = 0; i < count; ++i) {
            Vector3 v = samples[i];
            table += string.Format("\tfloat3({0},{1},{2}),\n", v.x, v.y, v.z);
        }
        table += "};\n";
        Debug.Log (table);
    }
    */

   // public int Dim = 40;

    public Transform Eye;
    public float Rad = 2;
    public float Step = 0.2f;
    public float D1 =1, D2 = 5;

    public Vector2 Tl = -new Vector2(30,30), Size = new Vector2(60,60);

    struct Cell {
        public byte Col;
        public float D;
    };
    Cell[] Map;


    public class DuplicateKeyComparer<K> : IComparer<K> where K : System.IComparable {
        public int Compare( K x, K y ) {
            int result = x.CompareTo(y);
            if(result == 0) return 1;
            else return -result;  // (-) invert 
        }
    }
    class SearchNode {
        //public float D;
        public int I, X, Y;
        public int L_Di;
        public float Dm = 1;
        public int Dir;
    };
    public bool Regen = false;


    bool sub(  SortedList<float,SearchNode> search, int i, int x, int y, float nk, int di) {
        if(Map[i].Col == 0 && Map[i].D < nk) {
            Map[i].D = nk;
            if(nk > Step) {
                SearchNode sn1 = new SearchNode();
                sn1.I = i;
                sn1.X = x; sn1.Y = y;
                sn1.L_Di = di;
                // sn1.Dm = nDm;
                sn1.Dir = 1 << di;
                search.Add(nk, sn1);
            }
            return true;
        }
        return false;
    }
    void gen() {

        Vector3 sz = Vector3.one *0.8f *Rad;
        Color col = Color.blue;
        Gizmos.color = col;

        /*Vector2 cp = transform.position;
        Vector2 yAx = transform.up;
        Vector2 xAx = transform.right;*/
        Vector2 cp = Tl;
        Vector2 yAx = Vector2.up;
        Vector2 xAx = Vector2.right;

        int lm = 1 << LayerMask.NameToLayer("Map");

        int dx = Mathf.CeilToInt(Size.x/Rad);
        int dy = Mathf.CeilToInt(Size.y/Rad);

        Map = new Cell[dx*dy];
        for(int x = dx; x-- > 0; )
            for(int y = dy; y-- > 0; ) {
                int i = x +y *dx;
                var off = (float)x *xAx + (float)y*yAx;
                var p = cp +off *Rad;
                if(Physics2D.OverlapCircle(p, Rad *0.8f, lm) == null) {
                    Map[i].Col = 0;
                } else {
                    Map[i].Col = 1;
                }
                Map[i].D = 0;
            }

        var ep = ((Vector2)Eye.position - Tl)/Rad;

        int ex = Mathf.RoundToInt(ep.x);
        int ey = Mathf.RoundToInt(ep.y);


        SortedList<float, SearchNode> search = new SortedList<float, SearchNode>(new DuplicateKeyComparer<float>());
        Map[ex +ey*dx].D  =1;
        SearchNode sn = new SearchNode();
        sn.I = ex +ey*dx;
        sn.X = ex; sn.Y = ey;
        sn.L_Di = 8;
        sn.Dir = 1;

        int m1 = -1;
        search.Add(1, sn);
        //unchecked {  //just so i can    (uint)-1
        int[,] da = { { -1, 0 }, { 1, 0 }, { 0, -1 }, { 0, 1 }, { -1, 1 }, { 1, -1 }, { -1, -1 }, { 1, 1 }, };

        float[,] dm = new float[9, 8];

        int[, ,] adj = { { { 3, 4 }, { 2,6 }  } }; 
 
        for(int i1 = 8; i1-- >0; ) {

            for(int i2 = 8; i2-- >0; ) {
                Vector2 v1 = new Vector2(da[i1, 0], da[i1, 1]).normalized,
                    v2 = new Vector2(da[i2, 0], da[i2, 1]).normalized;

                float dt = Vector2.Dot(v1, v2);  //-1 -> 1
                dt =  (dt-1)*-0.5f;   //0 ->  1
               // dt = dt *dt;
                dt = D1 + dt*(D2-D1);
                
                dm[i1, i2] = dt;
                
                Debug.Log(" dt   " +dt+  " v1   " +v1+  " v2   " +v2+  " x   " +(da[i2, 0])+  " y   " + (da[i2, 1]));
            }
            dm[8, i1]=1;
        }

        for(; ; ) {
            var iter = search.GetEnumerator();
            if(!iter.MoveNext()) break;
            var pair = iter.Current;
            var key = pair.Key;
            var cur = pair.Value;
            // if(key > bestD * 1.2f) break;
            search.RemoveAt(0);
            if(key   !=  Map[cur.I ].D)
                continue;

            // Debug.Log("s  " + cur.X +"   y " + cur.Y+"   k " + key);



            for(int di = 4; di-- >0; ) {
                if(((1 << di) & cur.Dir) == 0) continue;
                int x = cur.X + da[di, 0];
                int y = cur.Y + da[di, 1];
                float dm2 = 1;
                //if(di >= 4)
                //    dm2 = 1.42f;
                float nk = key - (Step);//*dm2;

                float nDm = cur.Dm*0.5f + dm[cur.L_Di, di] *0.5f;
                //Debug.Log("    n  " + x +"   y " + y);
                if((uint)x >= (uint)dx || (uint)y >= (uint)dy) continue;
                int i = x +y *dx;
                if( !sub( search, i, x,y,nk,di ) ) continue;


                
                int adjI = 0;
                int ndi = adj[di, adjI, 0];
                int nx = cur.X + da[ndi, 0];
                int ny = cur.Y + da[ndi, 1];
                if((uint)nx >= (uint)dx || (uint)ny >= (uint)dy) continue;
                int ni = nx +ny *dx;
                if(Map[ni].Col != 0) continue;

                int n2x = x + da[ndi, 0];
                int n2y = y + da[ndi, 1];
                sub(search, n2x + n2y * dx, n2x, n2y, nk, 9 );

            }
        }
    // }
    }
    void OnDrawGizmos() {

        if(Map == null || Regen) {
            gen();
            Regen = false;
        }

        Vector3 sz = Vector3.one *0.8f *Rad;
        Color col = Color.blue;
        Gizmos.color = col;

        /*Vector2 cp = transform.position;
        Vector2 yAx = transform.up;
        Vector2 xAx = transform.right;*/
        Vector2 cp = Tl;
        Vector2 yAx = Vector2.up;
        Vector2 xAx = Vector2.right;

        int lm = 1 << LayerMask.NameToLayer("Map");

        uint dx = (uint)Mathf.CeilToInt(Size.x/Rad);
        uint dy = (uint)Mathf.CeilToInt(Size.y/Rad);
        for(uint x = dx; x-- > 0; )
        for(uint y = dy; y-- > 0; ) {
            uint i = x +y *dx;
            var off = (float)x *xAx + (float)y*yAx;
            var p = cp +off *Rad;
            if( Map[i].D > 0 ) {
                Gizmos.color = new Color( 0, Map[i].D ,0 );
            } else if(Map[i].Col == 0 ) {
                Gizmos.color = Color.blue;
            } else {
                Gizmos.color = Color.red;
            }
            Gizmos.DrawWireCube(p, sz);
        }


    }
}

