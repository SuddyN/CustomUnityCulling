using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableNativeCulling : MonoBehaviour
{
    Camera cam;
    OcclusionQueryCuller culler;
    bool wasEnabledLast = false;

    private void Start()
    {
        cam = gameObject.GetComponent<Camera>();
        culler = gameObject.GetComponent<OcclusionQueryCuller>();
        wasEnabledLast = culler.enabled;
    }

    private void Update()
    {
        if (culler.enabled)
        {
            if (!wasEnabledLast)
            {
                cam.ResetCullingMatrix();
            }
        }
        else
        {
            cam.cullingMatrix = Matrix4x4.Ortho(-99999, 99999, -99999, 99999, 0.001f, 99999) *
                        Matrix4x4.Translate(Vector3.forward * -99999 / 2f) *
                        cam.worldToCameraMatrix;
        }
        wasEnabledLast = culler.enabled;
    }
}
