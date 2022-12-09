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
    [SerializeField] [Range(0.01f, 0.1f)] private float decaySpeed;
    [SerializeField] private bool start;
    [SerializeField] private float distThreshold;
    [SerializeField] private Transform ball;
    public int[] whichTriangleToColor;
    
    private GraphicsBuffer gpuVertices;
    private GraphicsBuffer gpuIndices;
    private ComputeBuffer gpuAdjacentTriangle;
    private ComputeBuffer gpuTrianglesShouldCheck;
    private ComputeBuffer gpuAmountTrianglesToCheck;

    private List<ComputeBuffer> gpuAdjacentTriangleList;
    private List<ComputeBuffer> gpuTrianglesShouldCheckAppendList;
    private List<int> amountTrianglesToCheckList;

    private ComputeBuffer gpuAmountTrianglesInProximity;
    private ComputeBuffer gpuTrianglesInProximity;
    
    private Mesh targetMesh;
    private bool firstTimeStart;
    private int kernelID;
    private int threadGroupSize;
    private int checkedIndicesCache;
    private bool doneDiscover;
    private int[] whichTrianglesToCheck;
    private int[] emptyArray;
    private AdjacentTriangles[] adjacentTrianglesArray;

    private int amountTriangles => targetMesh.triangles.Length / 3;
    private int amountEffectsRunning => gpuAdjacentTriangleList.Count;
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
        gpuAmountTrianglesToCheck?.Dispose();
        gpuAmountTrianglesToCheck = null;
        gpuAmountTrianglesInProximity?.Dispose();
        gpuAmountTrianglesInProximity = null;
        gpuTrianglesInProximity?.Dispose();
        gpuTrianglesInProximity = null;
    }
    private void Awake()
    {
        emptyArray = new int[1];
        amountTrianglesToCheckList = new List<int>();
        gpuAdjacentTriangleList = new List<ComputeBuffer>();
        gpuTrianglesShouldCheckAppendList = new List<ComputeBuffer>();
        
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
        gpuAmountTrianglesToCheck = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Structured);
        
        gpuAmountTrianglesInProximity = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Structured);
        gpuTrianglesInProximity = new ComputeBuffer(amountTriangles, sizeof(int) * 2, ComputeBufferType.Append);
        TriangleInProximity[] emptyTri = new TriangleInProximity[amountTriangles];
        gpuTrianglesInProximity.SetData(emptyTri);

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
        discoverMeshShader.Dispatch(kernelID, threadGroupSize, 1, 1);
        
        adjacentTrianglesArray = new AdjacentTriangles[amountTriangles];
        gpuAdjacentTriangle.GetData(adjacentTrianglesArray);
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

    public void FirstTriangleToCheck(int triangleToStart)
    {
        // gpuVertices ??= targetMesh.GetVertexBuffer(0);
        // gpuIndices ??= targetMesh.GetIndexBuffer();
        // kernelID = discoverMeshShader.FindKernel("ColorOneTriangle");
        // discoverMeshShader.SetBuffer(kernelID,"gpuVertices", gpuVertices);
        // discoverMeshShader.SetBuffer(kernelID,"gpuIndices", gpuIndices);
        // discoverMeshShader.SetInt("triangleIndexToColor", triangleToStart);
        // discoverMeshShader.Dispatch(kernelID, 1, 1, 1);

        ComputeBuffer adjacentTriangles = new ComputeBuffer(amountTriangles, sizeof(int) * 4, ComputeBufferType.Structured);
        adjacentTriangles.SetData(adjacentTrianglesArray);
        gpuAdjacentTriangleList.Add(adjacentTriangles);
        
        ComputeBuffer gpuTrianglesShouldCheckAppends = new ComputeBuffer(amountTriangles / 2, sizeof(int), ComputeBufferType.Append);
        gpuTrianglesShouldCheckAppendList.Add(gpuTrianglesShouldCheckAppends);
        
        whichTrianglesToCheck = new int[1];
        whichTrianglesToCheck[0] = triangleToStart;
        gpuTrianglesShouldCheckAppendList[amountEffectsRunning - 1].SetData(whichTrianglesToCheck);
        amountTrianglesToCheckList.Add(1);
    }

    public void IncrementTriangles()
    {
        gpuVertices ??= targetMesh.GetVertexBuffer(0);
        gpuIndices ??= targetMesh.GetIndexBuffer();
        gpuAmountTrianglesToCheck.SetData(emptyArray);
        
        for (int i = 0; i < amountEffectsRunning; i++)
        {
            gpuTrianglesShouldCheck?.Release();
            gpuTrianglesShouldCheck = new ComputeBuffer(amountTrianglesToCheckList[i], sizeof(int), ComputeBufferType.Structured);
            
            whichTrianglesToCheck = new int[amountTrianglesToCheckList[i]];
            gpuTrianglesShouldCheckAppendList[i].GetData(whichTrianglesToCheck);
            whichTrianglesToCheck = whichTrianglesToCheck.Distinct().ToArray();
            amountTrianglesToCheckList[i] = whichTrianglesToCheck.Length;
            discoverMeshShader.SetInt("amountTrianglesToCheck", amountTrianglesToCheckList[i]);
            gpuTrianglesShouldCheck.SetData(whichTrianglesToCheck);

            kernelID = discoverMeshShader.FindKernel("DiscoverMesh");
            discoverMeshShader.GetKernelThreadGroupSizes(kernelID, out uint threadGroupSizeX, out _, out _);
            threadGroupSize = Mathf.CeilToInt(amountTrianglesToCheckList[i] / (float)threadGroupSizeX);

            gpuTrianglesShouldCheckAppendList[i].SetCounterValue(0);
            discoverMeshShader.SetBuffer(kernelID,"gpuVertices", gpuVertices);
            discoverMeshShader.SetBuffer(kernelID,"gpuIndices", gpuIndices);
            discoverMeshShader.SetBuffer(kernelID,"gpuTrianglesShouldCheck", gpuTrianglesShouldCheck);
            discoverMeshShader.SetBuffer(kernelID,"gpuTrianglesShouldCheckAppend", gpuTrianglesShouldCheckAppendList[i]);
            discoverMeshShader.SetBuffer(kernelID,"gpuAmountTrianglesToCheck", gpuAmountTrianglesToCheck);
            discoverMeshShader.SetBuffer(kernelID,"gpuAdjacentTriangle", gpuAdjacentTriangleList[i]);
            discoverMeshShader.SetInt("amountTriangles", amountTriangles);
            discoverMeshShader.SetInt("amountTrianglesToCheck", amountTrianglesToCheckList[i]);
            discoverMeshShader.Dispatch(kernelID, threadGroupSize, 1, 1);

            int[] amountTrianglesToCheckArray = new int[1];
            gpuAmountTrianglesToCheck.GetData(amountTrianglesToCheckArray);
            gpuAmountTrianglesToCheck.SetData(emptyArray);
            amountTrianglesToCheckList[i] = amountTrianglesToCheckArray[0];
            
            if (amountTrianglesToCheckList[i] == 0)
            {
                gpuAdjacentTriangleList[i]?.Release();
                gpuAdjacentTriangleList.RemoveAt(i);
                gpuTrianglesShouldCheckAppendList[i]?.Release();
                gpuTrianglesShouldCheckAppendList.RemoveAt(i);
                amountTrianglesToCheckList.RemoveAt(i);
            }
        }
    }
    
    public void UpdateMesh()
    {
        kernelID = discoverMeshShader.FindKernel("UpdateMesh");
        discoverMeshShader.GetKernelThreadGroupSizes(kernelID, out uint threadGroupSizeX, out _, out _);
        threadGroupSize = Mathf.CeilToInt(targetMesh.vertexCount / (float)threadGroupSizeX);

        discoverMeshShader.SetBuffer(kernelID, "gpuVertices", gpuVertices);
        discoverMeshShader.SetBuffer(kernelID, "gpuIndices", gpuIndices);
        discoverMeshShader.SetInt("amountVerts", targetMesh.vertexCount);
        discoverMeshShader.SetFloat("decaySpeed", decaySpeed);

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
        FirstTriangleToCheck((int)lowestDistTri.id);
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

    private void FixedUpdate()
    {
        if (start)
        {
            IncrementTriangles();
            UpdateMesh();
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
