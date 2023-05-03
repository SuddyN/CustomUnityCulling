using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class OcclusionQueryCuller : MonoBehaviour
{

    public struct Occludee
    {
        public GameObject gameObject;
        public Bounds bounds;
        public List<MeshRenderer> meshRenderers;
    }

    public struct Bounds
    {
        private Vector3 center;
        public Vector3 GetCenter() { return center; }
        public void SetCenter(Vector3 value) { center = value; }

        private Vector3 size;
        public Vector3 Getsize() { return size; }
        public void SetSize(Vector3 value) { size = value; }
    };

    public bool shouldFrustumCull = true;
    public uint cullingDelay = 1;
    public Shader shader;
    public ComputeShader computeShader;
    public List<Occludee> occludeeList;

    Vector4[] elements;
    List<Vector4> vertices;
    Material material;
    ComputeBuffer writeBuffer;

    void Awake()
    {
        occludeeList = new();
        Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        foreach (Renderer renderer in renderers)
        {
            // skip objects marked to never be occluded
            if (renderer.GetComponent<NonOccludee>() != null)
            {
                continue;
            }
            // add objects to list of potential occludees
            occludeeList.Add(new Occludee()
            {
                gameObject = renderer.gameObject,
                bounds = new(),
                meshRenderers = new()
            });
        }
    }

    Vector4[] GetBounds(GameObject gameObject, int index)
    {
        // get the bounds from the mesh, if it exist
        if (gameObject.GetComponent<MeshFilter>() != null)
        {
            // create bounds vertices from cube mesh
            Mesh foundMesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
            Vector4[] vs = new Vector4[foundMesh.triangles.Length];
            for (int i = 0; i < vs.Length; i++)
            {
                Vector3 point = gameObject.transform.TransformPoint(foundMesh.vertices[foundMesh.triangles[i]]);
                vs[i] = new Vector4(point.x, point.y, point.z, index);
            }
            return vs;
        }
        // TODO: support objects without a mesh, or craft a less complex mesh
        Debug.LogWarning("DID NOT HAVE MESH: " + gameObject.name);
        return new Vector4[0];
    }

    /// <summary>
    /// Gets the AVERAGE center of a set of positions.
    /// </summary>
    /// <param name="bounds">A set of vertices. The fourth component (index) is unused.</param>
    /// <returns>A Vector3 position: the center.</returns>
    Vector3 GetBoundsCenter(Vector4[] bounds)
    {
        Vector3 total = Vector3.zero;
        foreach (Vector4 v in bounds)
        {
            total += new Vector3(v.x, v.y, v.z);
        }
        return total / bounds.Length;
    }

    /// <summary>
    /// Gets the bounds size of a set of vertices.
    /// Calculated as the (x, y, z) difference between min and max points.
    /// </summary>
    /// <param name="bounds">A set of vertices. The fourth component (index) is unused.</param>
    /// <returns>A Vector3 scale.</returns>
    Vector3 GetBoundsSize(Vector4[] bounds)
    {
        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;
        foreach (Vector4 v in bounds)
        {
            Vector3 point = new Vector3(v.x, v.y, v.z);
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
        }
        return (max - min) * 0.5f;
    }

    void OnEnable()
    {
        if (occludeeList == null || occludeeList.Count <= 0)
        {
            return;
        }
        elements = new Vector4[occludeeList.Count];

        // create bounds vertices
        vertices = new List<Vector4>();
        for (int i = 0; i < occludeeList.Count; i++)
        {
            occludeeList[i].meshRenderers.Clear();
            foreach (MeshRenderer renderer in occludeeList[i].gameObject.GetComponentsInChildren<MeshRenderer>().ToList())
            {
                occludeeList[i].meshRenderers.Add(renderer);
            }
            Vector4[] boundVertices = GetBounds(occludeeList[i].gameObject, i);
            occludeeList[i].bounds.SetCenter(GetBoundsCenter(boundVertices));
            occludeeList[i].bounds.SetSize(GetBoundsSize(boundVertices));
            vertices.AddRange(boundVertices);
        }

        // set boundsBuffer
        Bounds[] boundsList = new Bounds[occludeeList.Count];
        for (int i = 0; i < occludeeList.Count; i++)
        {
            boundsList[i] = occludeeList[i].bounds;
        }
        ComputeBuffer boundsBuffer = new ComputeBuffer(boundsList.Length, 24, ComputeBufferType.Default);
        boundsBuffer.SetData(boundsList);
        computeShader.SetBuffer(0, "_BoundsBuffer", boundsBuffer);

        // set intersectBuffer
        ComputeBuffer intersectBuffer = new ComputeBuffer(1, 4, ComputeBufferType.Default);
        computeShader.SetBuffer(0, "_IntersectBuffer", intersectBuffer);

        // set readBuffer
        ComputeBuffer readBuffer = new ComputeBuffer(vertices.Count, 16, ComputeBufferType.Default);
        readBuffer.SetData(vertices.ToArray());

        // set writeBuffer
        writeBuffer = new ComputeBuffer(occludeeList.Count, 16, ComputeBufferType.Default);
        Graphics.ClearRandomWriteTargets();
        Graphics.SetRandomWriteTarget(1, writeBuffer, false);

        // init the material
        if (material == null)
        {
            material = new Material(shader);
        }
        material.SetBuffer("_ReadBuffer", readBuffer);
        material.SetBuffer("_WriteBuffer", writeBuffer);
    }

    void Update()
    {
        if (occludeeList.Count == 0 || Time.frameCount % cullingDelay != 0)
        {
            return;
        }

        // perform the culling
        writeBuffer.GetData(elements);
        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        for (int i = 0; i < occludeeList.Count; i++)
        {
            foreach (MeshRenderer renderer in occludeeList[i].meshRenderers)
            {
                renderer.enabled = true;
                if (shouldFrustumCull && !GeometryUtility.TestPlanesAABB(frustumPlanes, renderer.bounds))
                {
                    renderer.enabled = false;
                }
                if (renderer.enabled && elements[i].sqrMagnitude <= 0.0f)
                {
                    renderer.enabled = false;
                }
            }
        }

        // clear elements
        System.Array.Clear(elements, 0, elements.Length);
        writeBuffer.SetData(elements);
    }

    void OnPostRender()
    {
        if (vertices != null && Time.frameCount % cullingDelay != 0)
        {
            material.SetPass(0);
            Graphics.DrawProceduralNow(MeshTopology.Triangles, vertices.Count, 1);
        }
    }

    void OnDisable()
    {
        foreach (Occludee occludee in occludeeList)
        {
            foreach (MeshRenderer renderer in occludee.meshRenderers)
            {
                if (!renderer)
                {
                    continue;
                }
                renderer.enabled = true;
            }
        }
    }
}