using System;
using UnityEngine;


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
}

