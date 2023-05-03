# Project Details

This project aims to implement Frustum and Occlusion Query Culling in Unity.
Unity already has its own implementation of Frustum and Occlusion Query Culling, which are disabled.

## Rubric

-  5% - Project Submission
- 15% - Render 3D Level
- 15% - Add HUD: top-down PIP view
- 20% - Implement Frustum Culling
- 25% - Implement Occlusion Query Culling
- 20% - Add and handle translucent objects correctly

From this rubric, I have attempted and completed all parts.

## Project setup:

1. Clone this repo
2. Open the `Build` folder
3. Run `CustomUnityCuller.exe`

This .exe was build for the Universal Windows Platform. 
If that fails to run, or if rendering/culling is noticeably bugged (which may be due to differences in GPU):

1. Install/open Unity Hub
2. Install Unity editor version 2021.3.23f1
3. Click the dropdown next to `Open --> Add Project From Disk`
4. Add the project's root directory
5. Open the project in the editor version installed
6. Load the `Playground` scene and press play

# Demonstration

## Frustum Culling

Frustum culling is included in Unity and cannot be "turned off", but we can get around this using a clever hack 
that sets the "culling mesh" (in effect, the frustum) very far behind us with very big range - culling nothing.

Instead, I implement culling as part of the `OcclusionQueryCuller.cs` script:

```cs
        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        for (int i = 0; i < occludeeList.Count; i++)
        {
            foreach (MeshRenderer renderer in occludeeList[i].meshRenderers)
            {
                renderer.enabled = true;
                if (shouldFrustumCull && !GeometryUtility.TestPlanesAABB(frustumPlanes, renderer.bounds))
                {
                    renderer.enabled = false; // the frustum culling happens here
                }
                if (renderer.enabled && elements[i].sqrMagnitude <= 0.0f)
                {
                    renderer.enabled = false;
                }
            }
        }
```

`GeometryUtility` is a handy tool that does our vector math for us. `GeometryUtility.CalculateFrustumPlanes(Camera.main)` returns a set of `Plane`s, 
which `GeometryUtility.TestPlanesAABB(frustumPlanes, renderer.bounds))` uses to check each renderer's bounds against. If this check fails, the renderer is occluded.

https://user-images.githubusercontent.com/33226440/235812383-a84bd211-8998-4098-b450-650d6d1494e9.mp4

## Occlusion Query Culling

Unity also includes occlusion culling, but this is static "baked" occlusion culling, and can easily be toggled per-camera (I have toggled it off).
Instead, this project aims to develop Occlusion Query Culling within Unity.

Unity, unlike projects directly within WebGL, has no direct support for occlusion queries, 
so I couldn't just take some triangles and feed them to WebGL to get a response on whether or not they would be visible. 
I had to implement my own solution to occlusion querying, which I did using the `OcclusionQueryCulling.cs` script, `OcclusionQueryShader.shader` shader,
and the `OcclusionIntersectionCompute.compute` compute shader. These shaders perform the function of creating a CPU-accessible depth buffer!

Every N frames, the shaders are run, and they compute this buffer and write to `_WriteBuffer` and `_IntersectBuffer`. 
Then, `OcclusionQueryCulling.cs` tests objects in the scene against these buffers, and culls them if occluded.

https://user-images.githubusercontent.com/33226440/235813793-a106e5f1-d2aa-4959-a45b-08ac145cab9b.mp4

## Non-Occluders (Transparency & Thin Objects)

Transparent and other non-occluding objects are handled simply - if an object is marked as "Transparent", 
it cannot occlude other objects (even if it is actually opaque).

In the video, the glass and pole are marked as non-occluders.

https://user-images.githubusercontent.com/33226440/235814013-0bff2cba-bf6d-4001-ac9e-7f2d4864368e.mp4

