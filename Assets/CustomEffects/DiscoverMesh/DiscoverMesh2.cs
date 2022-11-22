using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class DiscoverMesh2 : MonoBehaviour, IHittable
{
    [SerializeField] private ComputeShader discoverMeshShader;
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private int speed;
    [SerializeField] private bool start;
    [SerializeField] private int[] whichTriangleToColor;
    [SerializeField] private float distThreshold;
    [SerializeField] private Transform ball;
    
    private GraphicsBuffer gpuVertices;
    private GraphicsBuffer gpuIndices;
    private ComputeBuffer gpuAdjacentTriangle;
    private ComputeBuffer gpuTrianglesShouldCheck;
    private ComputeBuffer gpuTrianglesShouldCheckAppend;
    private ComputeBuffer gpuTrianglesToNotCheck;
    private ComputeBuffer gpuAmountTrianglesToCheck;
    
    private ComputeBuffer gpuAmountTrianglesInProximity;
    private ComputeBuffer gpuTrianglesInProximity;
    
    private ComputeBuffer debug;

    private Mesh targetMesh;
    private bool firstTimeStart;
    private int kernelID;
    private int threadGroupSize;
    private int checkedIndicesCache;
    private bool doneDiscover;
    private int[] whichTrianglesToCheck;
    private int amountTriangles => targetMesh.triangles.Length / 3;
    void OnDisable()
    {
        gpuVertices?.Dispose();
        gpuVertices = null;
        gpuIndices?.Dispose();
        gpuIndices = null;
        gpuAdjacentTriangle?.Dispose();
        gpuAdjacentTriangle = null;
        gpuTrianglesShouldCheck?.Dispose();
        gpuTrianglesShouldCheck = null;
        gpuTrianglesShouldCheckAppend?.Dispose();
        gpuTrianglesShouldCheckAppend = null;
        gpuAmountTrianglesToCheck?.Dispose();
        gpuAmountTrianglesToCheck = null;
        gpuAmountTrianglesInProximity?.Dispose();
        gpuAmountTrianglesInProximity = null;
        gpuTrianglesInProximity?.Dispose();
        gpuTrianglesInProximity = null;
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
        gpuTrianglesShouldCheckAppend = new ComputeBuffer(amountTriangles, sizeof(int), ComputeBufferType.Append);
        gpuTrianglesToNotCheck = new ComputeBuffer(amountTriangles, sizeof(int) * 2, ComputeBufferType.Append);
        gpuAmountTrianglesToCheck = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Structured);
        
        gpuAmountTrianglesInProximity = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Structured);
        gpuTrianglesInProximity = new ComputeBuffer(amountTriangles, sizeof(int) * 2, ComputeBufferType.Append);
        TriangleInProximity[] emptyTri = new TriangleInProximity[amountTriangles];
        gpuTrianglesInProximity.SetData(emptyTri);

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
        //         adjacentTrianglesArray[i].tri2TriangleIndex == -1 ||
        //         adjacentTrianglesArray[i].tri3TriangleIndex == -1)
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
        
        whichTrianglesToCheck = new int[1];
        whichTrianglesToCheck[0] = 0;
        gpuTrianglesShouldCheckAppend.SetData(whichTrianglesToCheck);
    }

    public void IncrementTriangles()
    {
        int[] amountTrianglesToCheckArray = new int[1];
        gpuAmountTrianglesToCheck.GetData(amountTrianglesToCheckArray);
        int amountTrianglesToCheck = amountTrianglesToCheckArray[0];
        if (amountTrianglesToCheck == 0)
        {
            start = false;
            return;
        }

        gpuTrianglesShouldCheck?.Release();
        gpuTrianglesShouldCheck = new ComputeBuffer(amountTrianglesToCheck, sizeof(int), ComputeBufferType.Structured);
        whichTrianglesToCheck = new int[amountTrianglesToCheck];
        gpuTrianglesShouldCheckAppend.GetData(whichTrianglesToCheck);
        whichTrianglesToCheck = whichTrianglesToCheck.Distinct().ToArray();
        amountTrianglesToCheck = whichTrianglesToCheck.Length;
        gpuTrianglesShouldCheck.SetData(whichTrianglesToCheck.Distinct().ToArray());
        
        int[] amountTrianglesToCheckArrayEmpty = new int[1];
        gpuAmountTrianglesToCheck.SetData(amountTrianglesToCheckArrayEmpty);
        int[] whichTrianglesToCheckEmpty = new int[amountTriangles];
        gpuTrianglesShouldCheckAppend.SetData(whichTrianglesToCheckEmpty);
        
        kernelID = discoverMeshShader.FindKernel("DiscoverMesh");
        discoverMeshShader.GetKernelThreadGroupSizes(kernelID, out uint threadGroupSizeX, out _, out _);
        threadGroupSize = Mathf.CeilToInt(amountTrianglesToCheck / (float)threadGroupSizeX);

        gpuVertices ??= targetMesh.GetVertexBuffer(0);
        gpuIndices ??= targetMesh.GetIndexBuffer();
        
        discoverMeshShader.SetBuffer(kernelID,"gpuVertices", gpuVertices);
        discoverMeshShader.SetBuffer(kernelID,"gpuIndices", gpuIndices);
        discoverMeshShader.SetInt("amountTrianglesToCheck", amountTrianglesToCheck);
        discoverMeshShader.SetBuffer(kernelID,"gpuTrianglesShouldCheck", gpuTrianglesShouldCheck);
        discoverMeshShader.SetBuffer(kernelID,"gpuTrianglesShouldCheckAppend", gpuTrianglesShouldCheckAppend);
        discoverMeshShader.SetBuffer(kernelID,"gpuAmountTrianglesToCheck", gpuAmountTrianglesToCheck);
        discoverMeshShader.SetInt("amountTriangles", amountTriangles);
        discoverMeshShader.SetBuffer(kernelID,"gpuAdjacentTriangle", gpuAdjacentTriangle);
        gpuTrianglesShouldCheckAppend.SetCounterValue(0);
        discoverMeshShader.Dispatch(kernelID, threadGroupSize, 1, 1);
    }

    public void Hit(Vector3 hitPos)
    {
        TriangleInProximity[] trisEmpty = new TriangleInProximity[amountTriangles];
        gpuTrianglesInProximity.SetData(trisEmpty);
        int[] empty = new int[1];
        gpuAmountTrianglesInProximity.SetData(empty);
        
        kernelID = discoverMeshShader.FindKernel("FindClosestTriangle");
        discoverMeshShader.GetKernelThreadGroupSizes(kernelID, out uint threadGroupSizeX, out _, out _);
        threadGroupSize = Mathf.CeilToInt(amountTriangles / (float)threadGroupSizeX);
        
        gpuVertices ??= targetMesh.GetVertexBuffer(0);
        gpuIndices ??= targetMesh.GetIndexBuffer();

        Vector3 pos = meshFilter.transform.worldToLocalMatrix.MultiplyPoint3x4(hitPos);
        ball.localPosition = pos;
        
        gpuTrianglesInProximity.SetCounterValue(0);
        discoverMeshShader.SetBuffer(kernelID,"gpuVertices", gpuVertices);
        discoverMeshShader.SetBuffer(kernelID,"gpuIndices", gpuIndices);
        discoverMeshShader.SetBuffer(kernelID,"gpuAmountTrianglesInProximity", gpuAmountTrianglesInProximity);
        discoverMeshShader.SetBuffer(kernelID,"gpuTrianglesInProximity", gpuTrianglesInProximity);
        discoverMeshShader.SetFloat("distThreshold", distThreshold);
        discoverMeshShader.SetVector("hitPos", pos);
        discoverMeshShader.SetInt("amountTriangles", amountTriangles);
        discoverMeshShader.Dispatch(kernelID, threadGroupSize, 1, 1);

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
        Debug.Log($"id: {lowestDistTri.id}");
    }

    public void ColorTriangles()
    {
        targetMesh = meshFilter.sharedMesh;
        int amountTrianglesToColor = whichTriangleToColor.Length;
        int[] indices = targetMesh.triangles;
        Vector2[] uvs = new Vector2[targetMesh.vertexCount];
        Vertex[] vertices = new Vertex[targetMesh.vertexCount];
        gpuVertices ??= targetMesh.GetVertexBuffer(0);
        gpuVertices.GetData(vertices);
        for (int i = 0; i < targetMesh.vertexCount; i++)
        {
            uvs[i] = new Vector2(vertices[i].uv.x, 0);
        }
        
        for (int i = 0; i < amountTrianglesToColor; i++)
        {
            int index1 = indices[whichTriangleToColor[i] * 3];
            int index2 = indices[whichTriangleToColor[i] * 3 + 1];
            int index3 = indices[whichTriangleToColor[i] * 3 + 2];
        
            uvs[index1].y = 1;
            uvs[index2].y = 1;
            uvs[index3].y = 1;
        }
        targetMesh.uv = uvs;
    }

    private void Update()
    {
        if (start && Time.frameCount % speed == 0)
        {
            if (!firstTimeStart)
            {
                FirstTriangleToCheck();
                firstTimeStart = true;
            }
            IncrementTriangles();
        }
    }
}

struct TriangleInProximity
{
    public TriangleInProximity(uint _id, float _dist)
    {
        id = _id;
        dist = _dist;
    }
    
    public uint id;
    public float dist;
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
