using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class DiscoverMesh : MonoBehaviour
{
    [SerializeField] private ComputeShader discoverMeshShader;
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private int speed;
    [SerializeField] private bool start;
    
    public ComputeBuffer gpuCheckedIndices;
    private GraphicsBuffer gpuVertices;
    private GraphicsBuffer gpuIndices;
    private ComputeBuffer gpuAdjacentTriangle;
    private ComputeBuffer gpuAdjacentTrianglesCounter;
    private ComputeBuffer gpuTrianglesFound;
    private ComputeBuffer debug;

    private Mesh targetMesh;
    private int kernelID;
    private int threadGroupSize;
    private int checkedIndicesCache;
    private bool doneDiscover;
    private List<uint> checkedIndices = new();
    void OnDisable()
    {
        gpuVertices?.Dispose();
        gpuVertices = null;
        gpuIndices?.Dispose();
        gpuIndices = null;
        gpuAdjacentTriangle?.Dispose();
        gpuAdjacentTriangle = null;
        gpuCheckedIndices?.Dispose();
        gpuCheckedIndices = null;
        gpuAdjacentTrianglesCounter?.Dispose();
        gpuAdjacentTrianglesCounter = null;
        gpuTrianglesFound?.Dispose();
        gpuTrianglesFound = null;
        debug?.Dispose();
        debug = null;
    }
    private void Awake()
    {
        targetMesh = meshFilter.sharedMesh;
        targetMesh.vertexBufferTarget |= GraphicsBuffer.Target.Structured;
        targetMesh.indexBufferTarget |= GraphicsBuffer.Target.Structured;
        
        targetMesh.uv = new Vector2[targetMesh.vertexCount];
        
        targetMesh.SetVertexBufferParams(targetMesh.vertexCount, 
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3, 0),
            new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float32, 4, 0),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2, 0)
        );
        gpuCheckedIndices = new ComputeBuffer(3, 4, ComputeBufferType.Structured);
        checkedIndices.Add(0);
        checkedIndices.Add(1);
        checkedIndices.Add(2);
        gpuCheckedIndices.SetData(checkedIndices);
        
        gpuAdjacentTriangle = new ComputeBuffer(targetMesh.triangles.Length, 4, ComputeBufferType.Append);
        
        gpuAdjacentTrianglesCounter = new ComputeBuffer(1, 4, ComputeBufferType.Structured);

        gpuTrianglesFound = new ComputeBuffer(targetMesh.triangles.Length / 3, 16, ComputeBufferType.Append);
        gpuTrianglesFound.SetCounterValue(0);
    }

    private void Update()
    {
        if (Time.frameCount % speed == 0 && start && !doneDiscover)
        {
            IncrementTriangles();
        }
    }

    public void IncrementTriangles()
    {
        uint[] newIndices = new uint[targetMesh.triangles.Length];
        gpuAdjacentTriangle.SetData(newIndices);
        gpuAdjacentTriangle.SetCounterValue(0);
        
        uint[] amountAdjacentIndicesArr = new uint[1];
        gpuAdjacentTrianglesCounter.SetData(amountAdjacentIndicesArr);
        
        gpuVertices ??= targetMesh.GetVertexBuffer(0);
        gpuIndices ??= targetMesh.GetIndexBuffer();

        kernelID = discoverMeshShader.FindKernel("DiscoverMesh");
        discoverMeshShader.GetKernelThreadGroupSizes(kernelID, out uint threadGroupSizeX, out _, out _);
        threadGroupSize = Mathf.CeilToInt((float)targetMesh.triangles.Length / 3 / threadGroupSizeX);
        
        discoverMeshShader.SetBuffer(kernelID,"gpuVertices", gpuVertices);
        discoverMeshShader.SetBuffer(kernelID,"gpuIndices", gpuIndices);
        discoverMeshShader.SetBuffer(kernelID,"gpuAdjacentTriangle", gpuAdjacentTriangle);
        discoverMeshShader.SetBuffer(kernelID,"gpuCheckedIndices", gpuCheckedIndices);
        discoverMeshShader.SetBuffer(kernelID,"gpuAdjacentTrianglesCounter", gpuAdjacentTrianglesCounter);
        discoverMeshShader.SetBuffer(kernelID,"gpuTrianglesFound", gpuTrianglesFound);
        discoverMeshShader.SetFloat("currentTime", Time.timeSinceLevelLoad);
        discoverMeshShader.Dispatch(kernelID, threadGroupSize, 1, 1);

        gpuAdjacentTrianglesCounter.GetData(amountAdjacentIndicesArr);
        uint amountAdjecentIndices = amountAdjacentIndicesArr[0];

        gpuAdjacentTriangle.GetData(newIndices);
        for (int i = 0; i < amountAdjecentIndices; i++)
        {
            checkedIndices.Add(newIndices[i]);
        }
        checkedIndices = checkedIndices.Distinct().ToList();
        
        CheckedTriangle[] trisFound = new CheckedTriangle[targetMesh.triangles.Length / 3];
        gpuTrianglesFound.GetData(trisFound);

        Debug.Log(checkedIndices.Count);
        if (checkedIndicesCache == checkedIndices.Count)
        {
            Debug.Log($"done discover");
            doneDiscover = true;
            return;
        }
        checkedIndicesCache = checkedIndices.Count;

        
        
        //Try having one big buffer instead of constantly creating smaller buffers
        gpuCheckedIndices.Release();
        gpuCheckedIndices = new ComputeBuffer(checkedIndices.Count, 4, ComputeBufferType.Structured);
        gpuCheckedIndices.SetData(checkedIndices);
    }

    private void FinishTriangleDiscover()
    {
        if (!doneDiscover)
            return;
        
        
    }
}

struct CheckedTriangle
{
    uint index1;
    uint index2;
    uint index3;
    float time;
};

struct Vertex
{
    public Vector3 pos;
    public Vector3 nor;
    public Vector4 tang;
    public Vector2 uv;
}
