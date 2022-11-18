using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class DiscoverMesh : MonoBehaviour
{
    [SerializeField] private ComputeShader computeShader;
    [SerializeField] private MeshFilter meshFilter;
    
    private GraphicsBuffer gpuVertices;
    private GraphicsBuffer gpuIndices;
    private ComputeBuffer gpuAdjacentTriangle;
    private ComputeBuffer gpuCheckedIndices;
    private ComputeBuffer gpuAdjacentTrianglesCounter;

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
    }
    private void Awake()
    {
        mesh = meshFilter.sharedMesh;
        mesh.vertexBufferTarget |= GraphicsBuffer.Target.Structured;
        mesh.indexBufferTarget |= GraphicsBuffer.Target.Structured;
        
        mesh.uv = new Vector2[mesh.vertexCount];
        
        mesh.SetVertexBufferParams(mesh.vertexCount, 
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3, 0),
            new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float32, 4, 0),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2, 0)
        );
        gpuCheckedIndices = new ComputeBuffer(3, 4, ComputeBufferType.Structured);
        gpuAdjacentTriangle = new ComputeBuffer( mesh.vertexCount, 4, ComputeBufferType.Append);
        
        IncrementTriangles();
    }

    private void IncrementTriangles()
    {
        uint[] newIndices = new uint[mesh.triangles.Length];
        gpuAdjacentTriangle.SetData(newIndices);
        gpuAdjacentTriangle.SetCounterValue(0);
        
        gpuAdjacentTrianglesCounter = new ComputeBuffer(1, 4, ComputeBufferType.Structured);
        uint[] amountAdjecentIndicesArr = new uint[1];
        gpuAdjacentTrianglesCounter.SetData(amountAdjecentIndicesArr);
        
        gpuVertices ??= mesh.GetVertexBuffer(0);
        gpuIndices ??= mesh.GetIndexBuffer();

        kernelID = computeShader.FindKernel("DiscoverMesh");
        computeShader.GetKernelThreadGroupSizes(kernelID, out uint threadGroupSizeX, out _, out _);
        threadGroupSize = Mathf.CeilToInt((float)mesh.triangles.Length / 3 / threadGroupSizeX);
        
        computeShader.SetBuffer(kernelID,"gpuVertices", gpuVertices);
        computeShader.SetBuffer(kernelID,"gpuIndices", gpuIndices);
        computeShader.SetBuffer(kernelID,"gpuAdjacentTriangle", gpuAdjacentTriangle);
        computeShader.SetBuffer(kernelID,"gpuVertices2", gpuCheckedIndices);
        computeShader.SetBuffer(kernelID,"gpuAdjacentTrianglesCounter", gpuAdjacentTrianglesCounter);
        gpuAdjacentTriangle.SetCounterValue(0);
        computeShader.Dispatch(kernelID, threadGroupSize, 1, 1);
        
        gpuAdjacentTrianglesCounter.GetData(amountAdjecentIndicesArr);
        uint amountAdjecentIndices = amountAdjecentIndicesArr[0];

        gpuAdjacentTriangle.GetData(newIndices);
        for (int i = 0; i < amountAdjecentIndices; i++)
        {
            checkedIndices.Add(newIndices[i]);
        }
        gpuCheckedIndices = new ComputeBuffer(checkedIndices.Count, 4, ComputeBufferType.Structured);
        gpuCheckedIndices.SetData(checkedIndices);

        //Vertex[] vertices = new Vertex[mesh.vertexCount];
        //gpuVertices.GetData(vertices);
    }
}

struct Vertex
{
    public Vector3 pos;
    public Vector3 nor;
    public Vector4 tang;
    public Vector2 uv;
}
