using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class DiscoverMesh2 : MonoBehaviour
{
    [SerializeField] private ComputeShader discoverMeshShader;
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private int speed;
    [SerializeField] private bool start;
    [SerializeField] private int[] whichTriangleToColor;
    
    private GraphicsBuffer gpuVertices;
    private GraphicsBuffer gpuIndices;
    private ComputeBuffer gpuAdjacentTriangle;
    private ComputeBuffer gpuTrianglesShouldCheck;
    private ComputeBuffer gpuTrianglesShouldCheckAppend;
    private ComputeBuffer gpuAmountTrianglesToCheck;
    private ComputeBuffer debug;

    private Mesh targetMesh;
    private bool firstTimeStart;
    private int kernelID;
    private int threadGroupSize;
    private int checkedIndicesCache;
    private bool doneDiscover;
    private int amountTriangles => targetMesh.triangles.Length / 3;
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

        gpuAdjacentTriangle = new ComputeBuffer(amountTriangles, sizeof(int) * 4, ComputeBufferType.Structured);
        gpuTrianglesShouldCheckAppend = new ComputeBuffer(amountTriangles * 3, sizeof(int), ComputeBufferType.Append);
        gpuAmountTrianglesToCheck = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Structured);

        debug = new ComputeBuffer(amountTriangles, 4, ComputeBufferType.Structured);
        int[] empty = new int[amountTriangles];
        debug.SetData(empty);

        targetMesh.uv = new Vector2[targetMesh.vertexCount];
        
        DiscoverAllAdjacentTriangle();
    }

    private void DiscoverAllAdjacentTriangle()
    {
        gpuVertices ??= targetMesh.GetVertexBuffer(0);
        gpuIndices ??= targetMesh.GetIndexBuffer();
        
        kernelID = discoverMeshShader.FindKernel("FindAdjacentTriangles");
        discoverMeshShader.GetKernelThreadGroupSizes(kernelID, out uint threadGroupSizeX, out _, out _);
        threadGroupSize = Mathf.CeilToInt((float)amountTriangles / threadGroupSizeX);
        
        discoverMeshShader.SetBuffer(kernelID,"gpuVertices", gpuVertices);
        discoverMeshShader.SetBuffer(kernelID,"gpuIndices", gpuIndices);
        discoverMeshShader.SetBuffer(kernelID,"gpuAdjacentTriangle", gpuAdjacentTriangle);
        discoverMeshShader.SetFloat("currentTime", Time.timeSinceLevelLoad);
        discoverMeshShader.SetInt("amountTriangles", amountTriangles);
        debug.SetCounterValue(0);
        discoverMeshShader.Dispatch(kernelID, threadGroupSize, 1, 1);
        
        // AdjacentTriangles[] adjacentTrianglesArray = new AdjacentTriangles[amountTriangles];
        // gpuAdjacentTriangle.GetData(adjacentTrianglesArray);
        // for (int i = 0; i < amountTriangles; i++)
        // {
        //     if (adjacentTrianglesArray[i].tri1TriangleIndex == -1 ||
        //         adjacentTrianglesArray[i].tri2TriangleIndex == -1 || adjacentTrianglesArray[i].tri3TriangleIndex == -1)
        //     {
        //         Debug.Log($"{i} not 3 connecting triangles");
        //     }
        // }
    }

    public void FirstTriangleToCheck()
    {
        int[] amountTrianglesToCheckArray = new int[1];
        amountTrianglesToCheckArray[0] = 1;
        gpuAmountTrianglesToCheck.SetData(amountTrianglesToCheckArray);
        
        int[] whichTrianglesToCheck = new int[1];
        whichTrianglesToCheck[0] = 0;
        gpuTrianglesShouldCheckAppend.SetData(whichTrianglesToCheck);
    }

    public void IncrementTriangles()
    {
        AdjacentTriangles[] adjacentTrianglesArray = new AdjacentTriangles[amountTriangles];
        gpuAdjacentTriangle.GetData(adjacentTrianglesArray);
        
        int[] amountTrianglesToCheckArray = new int[1];
        gpuAmountTrianglesToCheck.GetData(amountTrianglesToCheckArray);
        int amountTrianglesToCheck = amountTrianglesToCheckArray[0];
        if (amountTrianglesToCheck == 0)
        {
            return;
        }
        
        int[] debugInfo = new int[amountTriangles];
        debug.GetData(debugInfo);
        int[] debugInfoEmpty = new int[amountTriangles];
        debug.SetData(debugInfoEmpty);
        
        gpuTrianglesShouldCheck?.Release();
        gpuTrianglesShouldCheck = new ComputeBuffer(amountTrianglesToCheck, sizeof(int), ComputeBufferType.Structured);
        int[] whichTrianglesToCheck = new int[amountTrianglesToCheck];
        gpuTrianglesShouldCheckAppend.GetData(whichTrianglesToCheck);
        whichTrianglesToCheck = whichTrianglesToCheck.Distinct().ToArray();
        amountTrianglesToCheck = whichTrianglesToCheck.Length;
        gpuTrianglesShouldCheck.SetData(whichTrianglesToCheck.Distinct().ToArray());
        
        int[] amountTrianglesToCheckArrayEmpty = new int[1];
        gpuAmountTrianglesToCheck.SetData(amountTrianglesToCheckArrayEmpty);
        int[] whichTrianglesToCheckEmpty = new int[amountTriangles * 3];
        gpuTrianglesShouldCheckAppend.SetData(whichTrianglesToCheckEmpty);

        gpuVertices ??= targetMesh.GetVertexBuffer(0);
        gpuIndices ??= targetMesh.GetIndexBuffer();
        
        kernelID = discoverMeshShader.FindKernel("DiscoverMesh");
        discoverMeshShader.GetKernelThreadGroupSizes(kernelID, out uint threadGroupSizeX, out _, out _);
        threadGroupSize = Mathf.CeilToInt(amountTrianglesToCheck / (int)threadGroupSizeX);
        if (threadGroupSize == 0)
        {
            threadGroupSize = 1;
        }
        
        discoverMeshShader.SetBuffer(kernelID,"gpuVertices", gpuVertices);
        discoverMeshShader.SetBuffer(kernelID,"gpuIndices", gpuIndices);
        discoverMeshShader.SetBuffer(kernelID,"gpuAdjacentTriangle", gpuAdjacentTriangle);
        discoverMeshShader.SetBuffer(kernelID,"gpuTrianglesShouldCheck", gpuTrianglesShouldCheck);
        discoverMeshShader.SetBuffer(kernelID,"gpuTrianglesShouldCheckAppend", gpuTrianglesShouldCheckAppend);
        discoverMeshShader.SetBuffer(kernelID,"gpuAmountTrianglesToCheck", gpuAmountTrianglesToCheck);
        discoverMeshShader.SetBuffer(kernelID,"debug", debug);
        discoverMeshShader.SetFloat("currentTime", Time.timeSinceLevelLoad);
        discoverMeshShader.SetInt("amountTrianglesToCheck", amountTrianglesToCheck);
        discoverMeshShader.SetInt("amountTriangles", amountTriangles);
        gpuTrianglesShouldCheckAppend.SetCounterValue(0);
        discoverMeshShader.Dispatch(kernelID, threadGroupSize, 1, 1);
        
        
    }

    public void ColorTriangles()
    {
        targetMesh = meshFilter.sharedMesh;
        Vector3[] vertices = targetMesh.vertices;
        int[] indices = targetMesh.triangles;
        Vector2[] uvs = new Vector2[targetMesh.vertices.Length];
        for (int i = 0; i < amountTriangles; i++)
        {
            int index1 = indices[i * 3];
            int index2 = indices[i * 3 + 1];
            int index3 = indices[i * 3 + 2];
        
            Vector3 vertex1 = vertices[index1];
            Vector3 vertex2 = vertices[index2];
            Vector3 vertex3 = vertices[index3];
        
            Vector3[] allPos = new Vector3[3];
            allPos[0] = vertices[index1];
            allPos[0] = vertices[index2];
            allPos[0] = vertices[index3];

            for (int j = 0; j < whichTriangleToColor.Length; j++)
            {
                if (i == whichTriangleToColor[j])
                {
                    uvs[index1] = Vector2.up;
                    uvs[index2] = Vector2.up;
                    uvs[index3] = Vector2.up;
                }
            }
        }
        targetMesh.uv = uvs;
    }

    private void Update()
    {
        if (start)
        {
            if (!firstTimeStart)
            {
                FirstTriangleToCheck();
                firstTimeStart = true;
            }
            IncrementTriangles();
        }
    }

    private bool DistanceLessThen(Vector3[] a, Vector3 b, float maxDist)
    {
        for (int i = 0; i < a.Length; i++)
        {
            if (math.abs(a[i].x - b.x) < maxDist && 
                math.abs(a[i].y - b.y) < maxDist && 
                math.abs(a[i].z - b.z) < maxDist)
            {
                return true;
            }
        }

        return false;
    }
}

struct CheckedTriangle
{
    uint index1;
    uint index2;
    uint index3;
    float time;
}

struct AdjacentTriangles
{
    public int tri1TriangleIndex;
    public int tri2TriangleIndex;
    public int tri3TriangleIndex;
    public int hasTriangleBeenVisited;
}

struct Vertex
{
    public Vector3 pos;
    public Vector3 nor;
    public Vector4 tang;
    public Vector2 uv;
}
