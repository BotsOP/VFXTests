using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class SciFiSpawn2 : MonoBehaviour
{
    [SerializeField] private ComputeShader computeShader;
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private float triangleDist;
    [SerializeField] private Transform targetTransform;
    [SerializeField][Range(0, 1)] private float triangleLerp;
    
    private Mesh charMesh;
    
    private GraphicsBuffer gpuVertices;
    private GraphicsBuffer gpuSkinnedVertices;
    private GraphicsBuffer gpuOldVertices;
    private GraphicsBuffer gpuIndices;
    
    // Transform tOwner;
    // [SerializeField] SkinnedMeshRenderer skinnedMeshRenderer;
    // List<Vector3> meshVertices = new List<Vector3>();
    // public void GetVertices()
    // {
    //     Mesh mesh = new Mesh();
    //     skinnedMeshRenderer.BakeMesh(mesh, true);
    //     mesh.GetVertices(meshVertices);
    //     tOwner = skinnedMeshRenderer.transform;
    // }
    // public Vector3 GetPositionFromVertex(int i)
    // {
    //     Vector3 worldPosVertex = tOwner.localToWorldMatrix.MultiplyPoint3x4(meshVertices[i]);
    //     Matrix4x4 test = tOwner.localToWorldMatrix;
    //     return worldPosVertex;
    // }
    
    private void OnEnable()
    {
        charMesh = meshFilter.sharedMesh;
        gpuOldVertices = new GraphicsBuffer(GraphicsBuffer.Target.Structured, charMesh.vertexCount, 12);
        SetMesh();
    }
    
    void OnDisable()
    {
        gpuVertices?.Dispose();
        gpuVertices = null;
        gpuOldVertices?.Dispose();
        gpuOldVertices = null;
        gpuIndices?.Dispose();
        gpuIndices = null;
    }

    void Update()
    {
        if (Time.time > 1)
        {
            UpdateChar();
        }
        
        // for (int i = 0; i < 10; i++)
        // {
        //     Debug.Log($"{i} = {GetPositionFromVertex(i * 1000)}");
        // }
    }

    private void UpdateChar()
    {
        gpuSkinnedVertices ??= skinnedMeshRenderer.GetVertexBuffer();
        gpuVertices ??= charMesh.GetVertexBuffer(0);
        gpuIndices ??= charMesh.GetIndexBuffer();
        
        computeShader.SetFloat("triDist", triangleDist);
        computeShader.SetFloat("trilerp", triangleLerp);
        computeShader.SetFloat("time", Time.time);
        computeShader.SetVector("target", targetTransform.position);
        
        computeShader.SetBuffer(0, "bufSkinnedVertices", gpuSkinnedVertices);
        computeShader.SetBuffer(0, "bufVertices", gpuVertices);
        computeShader.SetBuffer(0, "bufOldVertices", gpuOldVertices);
        computeShader.SetBuffer(0, "bufIndices", gpuIndices);
        
        computeShader.Dispatch(0, (charMesh.vertexCount - 63) / 64 + 64, 1, 1);
        
        Vertex0[] vertex0 = new Vertex0[charMesh.vertexCount];
        gpuSkinnedVertices.GetData(vertex0);
        //charMesh.SetVertices(vertex0.Select(vertex => vertex.position).ToArray());
        // UInt32[] index = new uint[charMesh.triangles.Length];
        // gpuIndices.GetData(index);
    }
    
    private void SetMesh()
    {
        charMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        charMesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
        skinnedMeshRenderer.vertexBufferTarget |= GraphicsBuffer.Target.Raw;

        List<Vector3> vertices = new List<Vector3>();
        charMesh.GetVertices(vertices);
        gpuOldVertices.SetData(vertices);
    }
}

struct Vertex0
{
    public Vector3 position;
    public Vector3 normal;
    public Vector4 tangent;
}

