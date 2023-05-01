using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CameraScript : MonoBehaviour
{
    Camera camera;
    Plane[] frustumPlanes;
    Vector2Int screenSize;

    public double[,] depthBuffer;
    public RenderTexture textureTexture;
    public RenderTexture depthTexture;
    public Texture2D renderTex2d;
    public bool shouldFrustumCull = true;
    public bool shouldOcclusionCull = true;

    private void Start()
    {
        screenSize = new Vector2Int(Screen.width, Screen.height);
        this.camera = gameObject.GetComponent<Camera>();
        this.camera.depthTextureMode = DepthTextureMode.Depth;
        depthTexture = new RenderTexture(screenSize.x, screenSize.y, 24, RenderTextureFormat.Depth)
        {
            filterMode = FilterMode.Point,
        };
        textureTexture = new RenderTexture(screenSize.x, screenSize.y, 0, RenderTextureFormat.Default)
        {
            filterMode = FilterMode.Point,
        };
        renderTex2d = new Texture2D(screenSize.x, screenSize.y, TextureFormat.RGB24, false);
        depthBuffer = new double[screenSize.x, screenSize.y];
        InitDepthBuffer();
    }

    private void InitDepthBuffer()
    {
        for (int x = 0; x < screenSize.x; x++)
        {
            for (int y = 0; y < screenSize.y; y++)
            {
                depthBuffer[x, y] = double.MaxValue;
            }
        }
    }

    private bool IsFrustumCulled(Renderer renderer)
    {
        return !GeometryUtility.TestPlanesAABB(frustumPlanes, renderer.bounds);
    }

    private bool IsOcclusionCulled(Renderer renderer)
    {
        //RenderBuffer buffer = RenderTexture.active.depthBuffer;
        return false;
    }

    private void SetTex2d()
    {
        camera.SetTargetBuffers(textureTexture.colorBuffer, depthTexture.depthBuffer);
        var temp = RenderTexture.active;
        RenderTexture.active = depthTexture;
        this.camera.Render();
        renderTex2d.ReadPixels(new Rect(0, 0, RenderTexture.active.width, RenderTexture.active.height), 0, 0);
        renderTex2d.Apply();
        RenderTexture.active = temp;
        camera.targetTexture = null;
    }

    /// <summary>
    /// this method defeats the whole purpose of culling by being extremely expensive
    /// but Unity refuses to give me read access to the actual depth buffer in CPU
    /// and there's no supported way to perform an Occlusion Query.
    /// So, I build my own usable depth buffer here before the Render() call.
    /// </summary>
    /// <param name="renderer">The Renderer component to add to the buffer</param>
    private void AddToDepthBuffer(Renderer renderer)
    {

        //Collider collider = renderer.GetComponent<Collider>();
        //if (collider != null)
        //{
        //    for (int x = 0; x < screenSize.x; x+=10)
        //    {
        //        for (int y = 0; y < screenSize.y; y+=10)
        //        {
        //            RaycastHit hit;
        //            Vector3 screenPos = new Vector3(x, y, 0);
        //            if (!Physics.Raycast(transform.position, camera.ScreenToWorldPoint(screenPos), out hit, camera.farClipPlane))
        //            {
        //                continue;
        //            }
        //            if (hit.distance < depthBuffer[x, y])
        //            {
        //                depthBuffer[x, y] = hit.distance;
        //            }
        //        }
        //    }
        //    return;
        //}

        MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            return;
        }
        Mesh mesh = meshFilter.mesh;
        if (mesh == null)
        {
            return;
        }
    }

    private void Update()
    {

        SetTex2d();

        frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        Renderer[] renderers = Object.FindObjectsByType<MeshRenderer>(UnityEngine.FindObjectsSortMode.None);
        foreach (Renderer renderer in renderers)
        {
            if (shouldFrustumCull && IsFrustumCulled(renderer))
            {
                renderer.enabled = false;
                continue;
            }
            if (shouldOcclusionCull && IsOcclusionCulled(renderer))
            {
                renderer.enabled = false;
                continue;
            }
            renderer.enabled = true;
            AddToDepthBuffer(renderer);
        }

        // disables native frustum culling
        this.camera.cullingMatrix = Matrix4x4.Ortho(-99999, 99999, -99999, 99999, 0.001f, 99999) *
                    Matrix4x4.Translate(Vector3.forward * -99999 / 2f) *
                    this.camera.worldToCameraMatrix;
    }

    private void OnDisable()
    {
        this.camera.ResetCullingMatrix();
    }
}
