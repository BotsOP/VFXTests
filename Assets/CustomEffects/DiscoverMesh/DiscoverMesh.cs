using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class DiscoverMesh : MonoBehaviour
{
    [SerializeField] private ComputeShader discoverMeshShader;
    [SerializeField] private ComputeShader triangleManagerShader;
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private bool start;
    
    private GraphicsBuffer gpuVertices;
    private GraphicsBuffer gpuIndices;
    private ComputeBuffer gpuAdjacentTriangle;
    private ComputeBuffer gpuCheckedIndices;
    private ComputeBuffer gpuAdjacentTrianglesCounter;
    private ComputeBuffer debug;

    private Mesh targetMesh;
    private Mesh mesh;
    private int kernelID;
    private int threadGroupSize;
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
        checkedIndices.Add(20001);
        checkedIndices.Add(20002);
        checkedIndices.Add(20003);
        gpuCheckedIndices.SetData(checkedIndices);
        
        gpuAdjacentTriangle = new ComputeBuffer(targetMesh.triangles.Length, 4, ComputeBufferType.Append);
        debug = new ComputeBuffer(targetMesh.vertexCount, 4, ComputeBufferType.Append);
        uint[] debugInfo = new uint[targetMesh.vertexCount];
        debug.SetData(debugInfo);
    }

    private void Update()
    {
        if (Time.frameCount % 10 == 0 && start)
        {
            IncrementTriangles();
        }
    }

    public void IncrementTriangles()
    {
        uint[] newIndices = new uint[targetMesh.triangles.Length];
        gpuAdjacentTriangle.SetData(newIndices);
        gpuAdjacentTriangle.SetCounterValue(0);
        
        gpuAdjacentTrianglesCounter = new ComputeBuffer(1, 4, ComputeBufferType.Structured);
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
        discoverMeshShader.SetBuffer(kernelID,"debug", debug);
        debug.SetCounterValue(0);
        gpuAdjacentTriangle.SetCounterValue(0);
        discoverMeshShader.Dispatch(kernelID, threadGroupSize, 1, 1);

        uint[] debugInfo = new uint[targetMesh.vertexCount];
        debug.GetData(debugInfo);
        
        gpuAdjacentTrianglesCounter.GetData(amountAdjacentIndicesArr);
        uint amountAdjecentIndices = amountAdjacentIndicesArr[0];

        gpuAdjacentTriangle.GetData(newIndices);
        for (int i = 0; i < amountAdjecentIndices; i++)
        {
            checkedIndices.Add(newIndices[i]);
        }
        checkedIndices = checkedIndices.Distinct().ToList();
        gpuCheckedIndices = new ComputeBuffer(checkedIndices.Count, 4, ComputeBufferType.Structured);
        gpuCheckedIndices.SetData(checkedIndices);
        
        Debug.Log(checkedIndices.Count);

        //Vertex[] vertices = new Vertex[mesh.vertexCount];
        //gpuVertices.GetData(vertices);
    }
}

struct CheckedVertex
{
    uint index;
    Vector3 pos;
};

struct Vertex
{
    public Vector3 pos;
    public Vector3 nor;
    public Vector4 tang;
    public Vector2 uv;
}
