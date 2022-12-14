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
    [SerializeField] private int speedWhenReverse;
    [SerializeField] private float timeUntilReverse = 30;
    [SerializeField] private float timeDecayBeforeReverse = 20;
    [SerializeField] [Range(0.01f, 0.1f)] private float decaySpeed;
    [SerializeField] private bool start;
    [SerializeField] private float distThreshold;
    [SerializeField] private Transform ball;
    [SerializeField] private bool reverseDirection;
    [SerializeField] private bool test;
    [SerializeField] private bool debug;
    [SerializeField] private bool decay;
    public int[] whichTriangleToColor;
    
    private GraphicsBuffer gpuVertices;
    private GraphicsBuffer gpuIndices;
    private ComputeBuffer gpuAdjacentTriangle;
    private ComputeBuffer gpuTrianglesShouldCheck;
    private ComputeBuffer gpuAmountTrianglesToCheck;

    private List<ComputeBuffer> gpuAdjacentTrianglesIndexList;
    private List<ComputeBuffer> gpuTrianglesShouldCheckAppendList;
    private List<int> amountTrianglesToCheckList;
    private List<bool> reverseDirectionList;
    private List<float> timeUntilReverseList;
    private List<float> timeToDecayUntilReverseList;
    private List<float> startTimeList;

    private ComputeBuffer gpuAmountTrianglesInProximity;
    private ComputeBuffer gpuTrianglesInProximity;
    
    private Mesh targetMesh;
    private bool firstTimeStart;
    private int kernelID;
    private int threadGroupSize;
    private int checkedIndicesCache;
    private bool doneDiscover;
    private int counter;
    private int[] whichTrianglesToCheck;
    private int[] emptyArray;
    private AdjacentTriangles[] adjacentTrianglesArray;

    private int amountTriangles => targetMesh.triangles.Length / 3;
    private int amountEffectsRunning => gpuAdjacentTrianglesIndexList.Count;
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
        gpuAdjacentTrianglesIndexList = new List<ComputeBuffer>();
        gpuTrianglesShouldCheckAppendList = new List<ComputeBuffer>();
        reverseDirectionList = new List<bool>();
        timeUntilReverseList = new List<float>();
        timeToDecayUntilReverseList = new List<float>();
        startTimeList = new List<float>();
        
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

        gpuAdjacentTriangle = new ComputeBuffer(amountTriangles, sizeof(int) * 3, ComputeBufferType.Structured);
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
        counter = 0;
        
        ComputeBuffer adjacentTrianglesIndex = new ComputeBuffer(amountTriangles, sizeof(int), ComputeBufferType.Structured);
        adjacentTrianglesIndex.SetData(new int[amountTriangles]);
        gpuAdjacentTrianglesIndexList.Add(adjacentTrianglesIndex);
        
        ComputeBuffer gpuTrianglesShouldCheckAppends = new ComputeBuffer(amountTriangles / 2, sizeof(int), ComputeBufferType.Append);
        gpuTrianglesShouldCheckAppendList.Add(gpuTrianglesShouldCheckAppends);
        
        whichTrianglesToCheck = new int[1];
        whichTrianglesToCheck[0] = triangleToStart;
        gpuTrianglesShouldCheckAppendList[amountEffectsRunning - 1].SetData(whichTrianglesToCheck);
        amountTrianglesToCheckList.Add(1);
        
        reverseDirectionList.Add(reverseDirection);
        timeUntilReverseList.Add(timeUntilReverse);
        timeToDecayUntilReverseList.Add(timeDecayBeforeReverse);
        startTimeList.Add(Time.time);
    }

    public void IncrementTriangles()
    {
        gpuVertices ??= targetMesh.GetVertexBuffer(0);
        gpuIndices ??= targetMesh.GetIndexBuffer();
        gpuAmountTrianglesToCheck.SetData(emptyArray);
        
        for (int i = 0; i < amountEffectsRunning; i++)
        {
            IncrementTriangle(i);
        }
    }

    private void IncrementTriangle(int i)
    {
        bool shouldReverseDirection = false;
        int incrementSpeed = speed;
        if (reverseDirectionList[i] && Time.time > startTimeList[i] + timeUntilReverseList[i])
        {
            if (Time.time > startTimeList[i] + timeToDecayUntilReverseList[i] + timeUntilReverseList[i])
            {
                incrementSpeed = speedWhenReverse;
                shouldReverseDirection = true;
            }
            else
            {
                return;
            }
        }
        for (int j = 0; j < incrementSpeed; j++)
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

            //const
            discoverMeshShader.SetBuffer(kernelID, "gpuVertices", gpuVertices);
            discoverMeshShader.SetBuffer(kernelID, "gpuIndices", gpuIndices);
            discoverMeshShader.SetBuffer(kernelID, "gpuAdjacentTriangle", gpuAdjacentTriangle);
            discoverMeshShader.SetInt("amountTriangles", amountTriangles);
            //changes
            discoverMeshShader.SetBuffer(kernelID, "gpuTrianglesShouldCheck", gpuTrianglesShouldCheck);
            discoverMeshShader.SetBuffer(kernelID, "gpuTrianglesShouldCheckAppend", gpuTrianglesShouldCheckAppendList[i]);
            discoverMeshShader.SetBuffer(kernelID, "gpuAmountTrianglesToCheck", gpuAmountTrianglesToCheck);
            discoverMeshShader.SetBuffer(kernelID, "gpuAdjacentTrianglesIndex", gpuAdjacentTrianglesIndexList[i]);
            discoverMeshShader.SetInt("amountTrianglesToCheck", amountTrianglesToCheckList[i]);
            discoverMeshShader.SetBool("reverseDirection", shouldReverseDirection);
            gpuTrianglesShouldCheckAppendList[i].SetCounterValue(0);
            discoverMeshShader.Dispatch(kernelID, threadGroupSize, 1, 1);

            int[] amountTrianglesToCheckArray = new int[1];
            gpuAmountTrianglesToCheck.GetData(amountTrianglesToCheckArray);
            amountTrianglesToCheckList[i] = amountTrianglesToCheckArray[0];
            gpuAmountTrianglesToCheck.SetData(emptyArray);

            if (amountTrianglesToCheckList[i] == 0)
            {
                gpuAdjacentTrianglesIndexList[i]?.Release();
                gpuAdjacentTrianglesIndexList.RemoveAt(i);
                gpuTrianglesShouldCheckAppendList[i]?.Release();
                gpuTrianglesShouldCheckAppendList.RemoveAt(i);
                amountTrianglesToCheckList.RemoveAt(i);
                reverseDirectionList.RemoveAt(i);
                timeUntilReverseList.RemoveAt(i);
                timeToDecayUntilReverseList.RemoveAt(i);
                startTimeList.RemoveAt(i);
                return;
            }
        }
    }

    private bool ShouldReverse(int i)
    {
        if (reverseDirectionList[i])
        {
            
        }
        return false;
    }

    public void DecayMesh()
    {
        kernelID = discoverMeshShader.FindKernel("UpdateMesh");
        discoverMeshShader.GetKernelThreadGroupSizes(kernelID, out uint threadGroupSizeX, out _, out _);
        threadGroupSize = Mathf.CeilToInt(targetMesh.vertexCount / (float)threadGroupSizeX);

        //const
        discoverMeshShader.SetBuffer(kernelID, "gpuVertices", gpuVertices);
        discoverMeshShader.SetBuffer(kernelID, "gpuIndices", gpuIndices);
        discoverMeshShader.SetInt("amountVerts", targetMesh.vertexCount);
        //changes
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
        int lowestTri = (int)lowestDistTri.id;
        
        if (!debug)
        {
            FirstTriangleToCheck(lowestTri);
            Debug.Log($"{lowestTri}");
        }
        else
        {
            int[] indices = targetMesh.triangles;
            Vertex[] vertices = new Vertex[targetMesh.vertexCount];
            gpuVertices ??= targetMesh.GetVertexBuffer(0);
            gpuVertices.GetData(vertices);
            
            int index1 = indices[lowestTri * 3];
            int index2 = indices[lowestTri * 3 + 1];
            int index3 = indices[lowestTri * 3 + 2];
            
            // adjacentTrianglesArray = new AdjacentTriangles[amountTriangles];
            // gpuAdjacentTrianglesIndexList[0].GetData(adjacentTrianglesArray);

            // AdjacentTriangles adjTri = adjacentTrianglesArray[lowestTri];

            Debug.Log($"ID: {(int)lowestDistTri.id} UVS: {vertices[index1].uv.x}, {vertices[index2].uv.x}, {vertices[index3].uv.x} " +
                      $"Tri: ");
        }
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
        // counter++;
        //
        // if (counter > reverseTime && amountEffectsRunning > 0)
        // {
        //     if (counter > reverseTime + beforeReverseWait)
        //     {
        //         reverseDirection = true;
        //         speed = 4;
        //         decaySpeed = 0.5f;
        //         IncrementTriangles();
        //         DecayMesh();
        //         return;
        //     }
        //     
        //     DecayMesh();
        //     return;
        // }
        IncrementTriangles();
        DecayMesh();
        
        // if (start)
        // {
        //     IncrementTriangles();
        // }
        // if (decay)
        // {
        //     DecayMesh();
        // }
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
}

struct Vertex
{
    public Vector3 pos;
    public Vector3 nor;
    public Vector4 tang;
    public Vector2 uv;
}
