using UnityEngine;

namespace Bosmo
{
    public class SeparateTriangleHeight
    {
        private ComputeShader separateTriangleHeightShader;
        private Mesh mesh;
        
        private GraphicsBuffer gpuVertices;
        private GraphicsBuffer gpuUVs;
        private GraphicsBuffer gpuIndices;
        private ComputeBuffer gpuOldVertices;
        
        private int kernelID;
        private int threadGroupSize;
        private int amountTriangles => mesh.triangles.Length / 3;


        public SeparateTriangleHeight(Mesh mesh)
        {
            this.mesh = mesh;

            separateTriangleHeightShader = (ComputeShader)Resources.Load("MeshEffects/SeparateTriangleHeight");
            
            kernelID = separateTriangleHeightShader.FindKernel("SeperateTriangleHeight");
            separateTriangleHeightShader.GetKernelThreadGroupSizes(kernelID, out uint threadGroupSizeX, out _, out _);
            threadGroupSize = Mathf.CeilToInt(amountTriangles / (float)threadGroupSizeX);

            gpuOldVertices = new ComputeBuffer(mesh.vertexCount, sizeof(float) * 3, ComputeBufferType.Structured);
            gpuOldVertices.SetData(mesh.vertices);
        }
        ~SeparateTriangleHeight()
        {
            gpuVertices?.Dispose();
            gpuVertices = null;
            gpuIndices?.Dispose();
            gpuIndices = null;
        }

        public void UpdateTriangles()
        {
            gpuVertices ??= mesh.GetVertexBuffer(0);
            gpuUVs ??= mesh.GetVertexBuffer(1);
            gpuIndices ??= mesh.GetIndexBuffer();

            separateTriangleHeightShader.SetBuffer(kernelID,"gpuVertices", gpuVertices);
            separateTriangleHeightShader.SetBuffer(kernelID,"gpuIndices", gpuIndices);
            separateTriangleHeightShader.SetBuffer(kernelID,"gpuOldVertices", gpuOldVertices);
            separateTriangleHeightShader.SetBuffer(kernelID,"gpuUVs", gpuUVs);
            separateTriangleHeightShader.SetInt("amountTriangles", amountTriangles);
            separateTriangleHeightShader.SetInt("vertexStride", mesh.GetVertexBufferStride(0));
            separateTriangleHeightShader.SetInt("uvStride", mesh.GetVertexBufferStride(1));
            separateTriangleHeightShader.Dispatch(kernelID, threadGroupSize, 1, 1);
        }
    }
}
