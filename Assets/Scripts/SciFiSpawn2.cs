using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class SciFiSpawn2 : MonoBehaviour
{
    [SerializeField] private ComputeShader computeShader;
    [SerializeField] private Material meshMat;
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private Transform targetTransform;
    [SerializeField] private Transform animTransform;
    [SerializeField] private Transform charMeshTransform;
    [SerializeField][Range(0, 2f)] private float triangleDist;
    [SerializeField][Range(0, 1)] private float triangleLerp;
    [SerializeField][Range(0, 1)] private float cutOffDist;
    [SerializeField] private bool spawn;
    [SerializeField] private bool reset;
    [SerializeField] private int kernelIndex;
    
    private Mesh charMesh;
    private Transform meshTransform;
    private Camera mainCam;
    private int hitCount;
    private Queue<GraphicsBuffer> charBufferHit = new Queue<GraphicsBuffer>();

    private GraphicsBuffer gpuVertices;
    private GraphicsBuffer gpuSkinnedVertices;
    private GraphicsBuffer gpuIndices;
    private GraphicsBuffer triangleVelocity;

    private void OnDrawGizmos()
    {
        Vector4 target = meshFilter.transform.worldToLocalMatrix.MultiplyPoint3x4(targetTransform.position);
        
        meshMat.SetVector("_targetPos", target);
        meshMat.SetFloat("_dist", cutOffDist);
    }

    private void OnEnable()
    {
        charMesh = meshFilter.sharedMesh;
        meshTransform = meshFilter.transform;
        triangleVelocity = new GraphicsBuffer(GraphicsBuffer.Target.Structured, charMesh.triangles.Length / 3 + 3, 12);

        Vector3[] randomDir = new Vector3[charMesh.triangles.Length / 3 + 3];
        for (int i = 0; i < randomDir.Length; i++)
        {
            randomDir[i] = new Vector3(UnityEngine.Random.Range(-0.1f, 0.1f), UnityEngine.Random.Range(-0.1f, 0.1f), UnityEngine.Random.Range(-0.1f, 0.1f));
        }
        
        triangleVelocity.SetData(randomDir);

        mainCam = Camera.main;
        
        SetMesh();
    }
    
    void OnDisable()
    {
        gpuSkinnedVertices?.Dispose();
        gpuSkinnedVertices = null;
        gpuVertices?.Dispose();
        gpuVertices = null;
        gpuIndices?.Dispose();
        gpuIndices = null;
        triangleVelocity?.Dispose();
        triangleVelocity = null;
    }

    void Update()
    {
        UpdateChar();

        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log($"fired");
            
            RaycastHit hit;
            Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        
            if (Physics.Raycast(ray, out hit)) {
                HitChar(hit.point);
            }
        }

        charMeshTransform.position = animTransform.position;
        charMeshTransform.rotation = animTransform.rotation;
    }
    
    //TODO
    /*
     * skinned to mesh shader
     *
     * enemy gets hit
     *      create a new buffer and add it to the queue and hitCount +1
     *      loop through queue and send buffer to each dispatch
     * 
     * enemy triangle hit shader
     *      write to buffer which triangles are effected
     *      write to buffer where corresponding triangles should go
     *      edit texcoord4 to tell other shaders triangles are seperated
     * 
     * seperated triangle update shader
     *      read buffer which triangles should move where
     *      rotate triangle around a bit
     *      move triangles back after some time
     *
     * enemy triangle hit cleanup
     *      delete corresponding buffer
     *      write to texcoord4 they are no longer seperated
     */

    private void HitChar(Vector3 hitPos)
    {
        hitCount++;
        GraphicsBuffer newHit = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 100, 24);
        charBufferHit.Enqueue(newHit);
        
        computeShader.SetBuffer(kernelIndex, "bufVertices", gpuVertices);

        computeShader.Dispatch(1, (charMesh.triangles.Length / 3 - 63) / 64 + 2, 1, 1);

    }

    private void UpdateChar()
    {
        gpuSkinnedVertices ??= skinnedMeshRenderer.GetVertexBuffer();
        gpuVertices ??= charMesh.GetVertexBuffer(0);
        gpuIndices ??= charMesh.GetIndexBuffer();

        Vector4 target = meshTransform.worldToLocalMatrix.MultiplyPoint3x4(targetTransform.position);
        
        meshMat.SetVector("_targetPos", target);
        
        computeShader.SetFloat("triDist", triangleDist);
        computeShader.SetFloat("triLerp", triangleLerp);
        computeShader.SetFloat("time", Time.time);
        computeShader.SetFloat("deltaTime", Time.deltaTime);
        computeShader.SetFloat("VERTEX_STRIDE", 44);
        computeShader.SetBool("spawn", spawn);
        computeShader.SetBool("reset", reset);
        computeShader.SetVector("target", target);
        
        computeShader.SetBuffer(kernelIndex, "bufSkinnedVertices", gpuSkinnedVertices);
        computeShader.SetBuffer(kernelIndex, "bufVertices", gpuVertices);
        computeShader.SetBuffer(kernelIndex, "bufIndices", gpuIndices);
        //computeShader.SetBuffer(0, "triangleVelocity", triangleVelocity);
        
        
        computeShader.Dispatch(kernelIndex, (charMesh.triangles.Length / 3 - 63) / 64 + 2, 1, 1);
        
        // Vertex0[] vertex0 = new Vertex0[charMesh.vertexCount];
        // gpuSkinnedVertices.GetData(vertex0);
        //charMesh.SetVertices(vertex0.Select(vertex => vertex.position).ToArray());
        // UInt32[] index = new uint[charMesh.triangles.Length];
        // gpuIndices.GetData(index);
    }
    
    private void SetMesh()
    {
        charMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        charMesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
        skinnedMeshRenderer.vertexBufferTarget |= GraphicsBuffer.Target.Raw;

        VertexAttributeDescriptor[] attrib = new VertexAttributeDescriptor[10];
        charMesh.GetVertexAttributes(attrib);
        for (int i = 0; i < 10; i++)
        {
            Debug.Log(attrib[i]);
        }
        
        
        charMesh.SetVertexBufferParams(charMesh.vertexCount, 
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, dimension:3,stream:0), 
                        new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, dimension:3,stream:0),
                        new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float32, dimension:4,stream:0),
                        new VertexAttributeDescriptor(VertexAttribute.TexCoord4, VertexAttributeFormat.Float32, dimension:2,stream:0),
                        new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, dimension:2,stream:1),
                        new VertexAttributeDescriptor(VertexAttribute.BlendWeight, VertexAttributeFormat.Float32, dimension:4,stream:2),
                        new VertexAttributeDescriptor(VertexAttribute.BlendIndices, VertexAttributeFormat.UInt32, dimension:4,stream:2)
            );
    }
}

struct Vertex0
{
    public Vector3 position;
    public Vector3 normal;
    public Vector4 tangent;
}

struct TriangleHit
{
    public int index1;
    public int index2;
    public int index3;

    public Vector3 targetPos;
}

