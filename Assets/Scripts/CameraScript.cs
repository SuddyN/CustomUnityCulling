using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CameraScript : MonoBehaviour
{

    Camera camera;
    Plane[] frustumPlanes;
    public RenderTexture textureTexture;
    public RenderTexture depthTexture;
    public Texture2D renderTex2d;
    public bool shouldFrustumCull = true;
    public bool shouldOcclusionCull = true;

    private void Start()
    {
        this.camera = gameObject.GetComponent<Camera>();
        RenderPipelineManager.beginCameraRendering += RenderPipelineManager_beginCameraRendering;
        this.camera.depthTextureMode = DepthTextureMode.Depth;
        depthTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.Depth)
        {
            filterMode = FilterMode.Point,
        };
        textureTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.Default)
        {
            filterMode = FilterMode.Point,
        };
        renderTex2d = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
    }

    private void RenderPipelineManager_beginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        this.camera.cullingMatrix = Matrix4x4.Ortho(-99999, 99999, -99999, 99999, 0.001f, 99999) *
                            Matrix4x4.Translate(Vector3.forward * -99999 / 2f) *
                            this.camera.worldToCameraMatrix;
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

    private void Update()
    {
        SetTex2d();

        frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        Renderer[] renderers = Object.FindObjectsByType<Renderer>(UnityEngine.FindObjectsSortMode.None);
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
        }
    }

    private void OnDisable()
    {
        this.camera.ResetCullingMatrix();
    }
}
