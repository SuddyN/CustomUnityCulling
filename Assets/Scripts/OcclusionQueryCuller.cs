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
    public List<Occludee> occledeeList;

    ComputeBuffer readBuffer;
    ComputeBuffer writeBuffer;
    ComputeBuffer boundsBuffer;
    ComputeBuffer intersectBuffer;

    Vector4[] elements;
    List<Vector4> vertices;
    Material material;

    Camera cam;
    Plane[] frustumPlanes;

    private void Start()
    {
        cam = gameObject.GetComponent<Camera>();
        cam.depthTextureMode = DepthTextureMode.Depth;
    }

    void Awake()
    {
        occledeeList = new();
        Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        foreach (Renderer renderer in renderers)
        {
            if (renderer.GetComponent<NonOccludee>() != null)
            {
                continue;
            }
            occledeeList.Add(new Occludee()
            {
                gameObject = renderer.gameObject,
                bounds = new(),
                meshRenderers = new()
            });
        }
    }

    Vector4[] GetBounds(GameObject gameObject, int index)
    {
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
        Debug.LogWarning("DID NOT HAVE MESH: " + gameObject.name);
        return new Vector4[0];
    }

    Vector3 GetBoundsCenter(Vector4[] bounds)
    {
        Vector3 total = Vector3.zero;
        foreach (Vector4 v in bounds)
        {
            total += new Vector3(v.x, v.y, v.z);
        }
        return total / bounds.Length;
    }

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
        if (occledeeList == null || occledeeList.Count <= 0)
        {
            return;
        }
        // init global arrays
        elements = new Vector4[occledeeList.Count];

        // create bounds vertices
        vertices = new List<Vector4>();
        for (int i = 0; i < occledeeList.Count; i++)
        {
            occledeeList[i].meshRenderers.Clear();
            foreach (MeshRenderer renderer in occledeeList[i].gameObject.GetComponentsInChildren<MeshRenderer>().ToList())
            {
                occledeeList[i].meshRenderers.Add(renderer);
            }
            Vector4[] boundVertices = GetBounds(occledeeList[i].gameObject, i);
            occledeeList[i].bounds.SetCenter(GetBoundsCenter(boundVertices));
            occledeeList[i].bounds.SetSize(GetBoundsSize(boundVertices));
            vertices.AddRange(boundVertices);
        }

        // set boundsBuffer
        Bounds[] boundsList = new Bounds[occledeeList.Count];
        for (int i = 0; i < occledeeList.Count; i++)
        {
            boundsList[i] = occledeeList[i].bounds;
        }
        boundsBuffer = new ComputeBuffer(boundsList.Length, 24, ComputeBufferType.Default);
        boundsBuffer.SetData(boundsList);
        computeShader.SetBuffer(0, "_BoundsBuffer", boundsBuffer);

        // set intersectBuffer
        intersectBuffer = new ComputeBuffer(1, 4, ComputeBufferType.Default);
        computeShader.SetBuffer(0, "_IntersectBuffer", intersectBuffer);

        // set readBuffer
        readBuffer = new ComputeBuffer(vertices.Count, 16, ComputeBufferType.Default);
        readBuffer.SetData(vertices.ToArray());

        // set writeBuffer
        writeBuffer = new ComputeBuffer(occledeeList.Count, 16, ComputeBufferType.Default);
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
        if (occledeeList.Count == 0 || Time.frameCount % cullingDelay != 0)
        {
            return;
        }

        // perform the culling
        writeBuffer.GetData(elements);
        frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        for (int i = 0; i < occledeeList.Count; i++)
        {
            foreach (MeshRenderer renderer in occledeeList[i].meshRenderers)
            {
                renderer.enabled = true;
                if (shouldFrustumCull && !GeometryUtility.TestPlanesAABB(frustumPlanes, renderer.bounds))
                {
                    renderer.enabled = false;
                }
                if (renderer.enabled && Vector4.Dot(elements[i], elements[i]) <= 0.0f)
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
        if (vertices != null)
        {
            material.SetPass(0);
            Graphics.DrawProceduralNow(MeshTopology.Triangles, vertices.Count, 1);
        }
    }

    void OnDisable()
    {
        for (int i = 0; i < occledeeList.Count; i++)
        {
            foreach (MeshRenderer renderer in occledeeList[i].meshRenderers)
            {
                renderer.enabled = true;
            }
        }
        readBuffer?.Release();
        writeBuffer?.Release();
        boundsBuffer?.Release();
        intersectBuffer?.Release();
    }
}