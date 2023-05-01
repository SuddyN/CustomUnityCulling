using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CameraScript : MonoBehaviour
{

    Plane[] frustumPlanes;
    public bool shouldFrustumCull = true;

    private void Start()
    {
        RenderPipelineManager.beginCameraRendering += RenderPipelineManager_beginCameraRendering;
    }

    private void RenderPipelineManager_beginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        Camera.main.cullingMatrix = Matrix4x4.Ortho(-99999, 99999, -99999, 99999, 0.001f, 99999) *
                            Matrix4x4.Translate(Vector3.forward * -99999 / 2f) *
                            Camera.main.worldToCameraMatrix;
    }

    private bool IsFrustumCulled(Renderer renderer)
    {
        return !GeometryUtility.TestPlanesAABB(frustumPlanes, renderer.bounds);
    }

    private void Update()
    {
        frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        Renderer[] renderers = Object.FindObjectsByType<Renderer>(UnityEngine.FindObjectsSortMode.None);
        foreach (Renderer renderer in renderers)
        {
            if (shouldFrustumCull && IsFrustumCulled(renderer))
            {
                renderer.enabled = false;
                continue;
            }
            renderer.enabled = true;
        }
    }

    private void OnDisable()
    {
        Camera.main.ResetCullingMatrix();
    }
}
