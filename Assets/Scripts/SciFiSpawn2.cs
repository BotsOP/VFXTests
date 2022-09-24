using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SciFiSpawn2 : MonoBehaviour
{
    [SerializeField] private ComputeShader computeShader;
    [SerializeField] private SkinnedMeshRenderer meshFilter;
    [SerializeField] private Mesh charMesh;
    [SerializeField] private float triangleDist;
    [SerializeField] private Transform targetTransform;
    [SerializeField][Range(0, 1)] private float triangleLerp;
    
    private GraphicsBuffer gpuVertices;
    private GraphicsBuffer gpuOldVertices;
    private GraphicsBuffer gpuIndices;
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
        UpdateChar();
    }

    private void UpdateChar()
    {
        gpuVertices ??= charMesh.GetVertexBuffer(0);
        gpuIndices ??= charMesh.GetIndexBuffer();
        
        computeShader.SetFloat("triDist", triangleDist);
        computeShader.SetFloat("trilerp", triangleLerp);
        computeShader.SetVector("target", targetTransform.position);
        
        computeShader.SetBuffer(0, "bufVertices", gpuVertices);
        computeShader.SetBuffer(0, "bufOldVertices", gpuOldVertices);
        computeShader.SetBuffer(0, "bufIndices", gpuIndices);
        
        computeShader.Dispatch(0, (charMesh.triangles.Length), 1, 1);
        
        Vertex0[] Vertex0 = new Vertex0[charMesh.vertexCount];
        gpuVertices.GetData(Vertex0);
        UInt32[] index = new uint[charMesh.triangles.Length];
        gpuIndices.GetData(index);
        //9767
    }
    
    private void SetMesh()
    {
        charMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        charMesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;

        List<Vector3> vertices = new List<Vector3>();
        charMesh.GetVertices(vertices);
        gpuOldVertices.SetData(vertices);

        // VertexAttributeDescriptor[] vertexAttributes = new VertexAttributeDescriptor[10];
        // vertexAttributes = charMesh.GetVertexAttributes();
        // for (int i = 0; i < vertexAttributes.Length; i++)
        // {
        //     Debug.Log(vertexAttributes[i]);
        // }
    }
}

struct Vertex0
{
    private Vector3 position;
    private Vector3 normal;
    private Vector4 tangent;
}

