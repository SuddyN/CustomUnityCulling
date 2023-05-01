using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class FrustumCuller : MonoBehaviour
{
    Camera cam;
    Plane[] frustumPlanes;
    Vector2Int screenSize;

    public bool shouldFrustumCull = true;

    private void Start()
    {
        screenSize = new Vector2Int(Screen.width, Screen.height);
        this.cam = gameObject.GetComponent<Camera>();
        this.cam.depthTextureMode = DepthTextureMode.Depth;
    }

    private void Update()
    {
        frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        Renderer[] renderers = Object.FindObjectsByType<MeshRenderer>(UnityEngine.FindObjectsSortMode.None);
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = !shouldFrustumCull || GeometryUtility.TestPlanesAABB(frustumPlanes, renderer.bounds);
        }

        // disables native frustum culling
        this.cam.cullingMatrix = Matrix4x4.Ortho(-99999, 99999, -99999, 99999, 0.001f, 99999) *
                    Matrix4x4.Translate(Vector3.forward * -99999 / 2f) *
                    this.cam.worldToCameraMatrix;
    }

    private void OnDisable()
    {
        this.cam.ResetCullingMatrix();
    }
}
