using UnityEngine;

namespace Bosmo
{
    public class GetTriangle
    {
        private ComputeShader closestTriangleShader;
        private float distThreshold;

        private GraphicsBuffer gpuVertices;
        private GraphicsBuffer gpuIndices;

        private ComputeBuffer gpuAmountTrianglesInProximity;
        private ComputeBuffer gpuTrianglesInProximity;
    
        private Mesh mesh;
        private Transform meshTransform;
        private int kernelID;
        private int threadGroupSize;
        private int vertexStride;

        private int amountTriangles => mesh.triangles.Length / 3;

        

        public GetTriangle(Mesh mesh, Transform meshTransform, float distThreshold = 0.1f)
        {
            this.mesh = mesh;
            this.meshTransform = meshTransform;
            this.distThreshold = distThreshold;
        
            closestTriangleShader = (ComputeShader)Resources.Load("MeshEffects/GetClosestTriangle");
        
            gpuAmountTrianglesInProximity = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Structured);
            gpuTrianglesInProximity = new ComputeBuffer(amountTriangles, sizeof(int) * 2, ComputeBufferType.Append);
            TriangleInProximity[] emptyTri = new TriangleInProximity[amountTriangles];
            gpuTrianglesInProximity.SetData(emptyTri);

            vertexStride = mesh.GetVertexBufferStride(0);
            
            kernelID = closestTriangleShader.FindKernel("FindClosestTriangle");
            closestTriangleShader.GetKernelThreadGroupSizes(kernelID, out uint threadGroupSizeX, out _, out _);
            threadGroupSize = Mathf.CeilToInt(amountTriangles / (float)threadGroupSizeX);
        }
    
        ~GetTriangle()
        {
            gpuVertices?.Dispose();
            gpuVertices = null;
            gpuIndices?.Dispose();
            gpuIndices = null;
            gpuAmountTrianglesInProximity?.Dispose();
            gpuAmountTrianglesInProximity = null;
            gpuTrianglesInProximity?.Dispose();
            gpuTrianglesInProximity = null;
        }
    
        public int GetClosestTriangle(Vector3 worldHitPos)
        {
            TriangleInProximity[] trisEmpty = new TriangleInProximity[amountTriangles];
            gpuTrianglesInProximity.SetData(trisEmpty);
            int[] empty = new int[1];
            gpuAmountTrianglesInProximity.SetData(empty);
    
            gpuVertices ??= mesh.GetVertexBuffer(0);
            gpuIndices ??= mesh.GetIndexBuffer();
            
            vertexStride = mesh.GetVertexBufferStride(0);

            Vector3 pos = meshTransform.worldToLocalMatrix.MultiplyPoint3x4(worldHitPos);
    
            gpuTrianglesInProximity.SetCounterValue(0);
            closestTriangleShader.SetBuffer(kernelID,"gpuVertices", gpuVertices);
            closestTriangleShader.SetBuffer(kernelID,"gpuIndices", gpuIndices);
            closestTriangleShader.SetBuffer(kernelID,"gpuAmountTrianglesInProximity", gpuAmountTrianglesInProximity);
            closestTriangleShader.SetBuffer(kernelID,"gpuTrianglesInProximity", gpuTrianglesInProximity);
            closestTriangleShader.SetFloat("distThreshold", distThreshold);
            closestTriangleShader.SetVector("hitPos", pos);
            closestTriangleShader.SetInt("amountTriangles", amountTriangles);
            closestTriangleShader.SetInt("vertexStride", vertexStride);
            closestTriangleShader.Dispatch(kernelID, threadGroupSize, 1, 1);

            int[] amountTrianglesInProximityArray = new int[1];
            gpuAmountTrianglesInProximity.GetData(amountTrianglesInProximityArray);
            int amountTrianglesInProximity = amountTrianglesInProximityArray[0];
    
            TriangleInProximity[] tris = new TriangleInProximity[amountTrianglesInProximity];
            gpuTrianglesInProximity.GetData(tris);
            TriangleInProximity lowestDistTri = new TriangleInProximity(0, 99999);
            foreach (TriangleInProximity tri in tris)
            {
                if (tri.dist < lowestDistTri.dist)
                {
                    lowestDistTri = tri;
                }
            }
            int lowestTri = (int)lowestDistTri.id;
            
            return lowestTri;
        }
    }
}
