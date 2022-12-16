using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CompositeEffects
{
    public ComputeBuffer input1;
    
    private ComputeShader compositeShader;
    private float distThreshold;

    private GraphicsBuffer gpuVertices;
    private GraphicsBuffer gpuIndices;
    private ComputeBuffer startTime;

    private Mesh mesh;
    private Transform meshTransform;
    private int kernelID;
    private int threadGroupSize;
    private int vertexStride;

    private int amountTriangles => mesh.triangles.Length / 3;

    public CompositeEffects(Mesh mesh)
    {
        this.mesh = mesh;

        compositeShader = (ComputeShader)Resources.Load("MeshEffects/CompositeShader");
        
        kernelID = compositeShader.FindKernel("CompositeEffects");
        compositeShader.GetKernelThreadGroupSizes(kernelID, out uint threadGroupSizeX, out _, out _);
        threadGroupSize = Mathf.CeilToInt(mesh.vertexCount / (float)threadGroupSizeX);

        startTime = new ComputeBuffer(mesh.vertexCount, sizeof(float), ComputeBufferType.Structured);
        float[] empty = new float[mesh.vertexCount];
        startTime.SetData(empty);
    }
    
    ~CompositeEffects()
    {
        gpuVertices?.Dispose();
        gpuVertices = null;
        gpuIndices?.Dispose();
        gpuIndices = null;
        startTime?.Dispose();
        startTime = null;
    }

    public void Compositing()
    {
        gpuVertices ??= mesh.GetVertexBuffer(1);
        gpuIndices ??= mesh.GetIndexBuffer();
            
        vertexStride = mesh.GetVertexBufferStride(1);
        
        compositeShader.SetBuffer(kernelID, "gpuVertices", gpuVertices);
        compositeShader.SetBuffer(kernelID, "gpuIndices", gpuIndices);
        compositeShader.SetBuffer(kernelID, "startTime", startTime);
        compositeShader.SetBuffer(kernelID, "input1", input1);
        compositeShader.SetInt("vertexStride", vertexStride);
        compositeShader.SetInt("amountVerts", mesh.vertexCount);
        compositeShader.SetFloat("time", Time.time);
        compositeShader.SetFloat("effectDuration", 0.5f);
        compositeShader.Dispatch(kernelID, threadGroupSize, 1, 1);
    }
}
