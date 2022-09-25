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
    [SerializeField] private Transform targetTransform;
    [SerializeField][Range(0, 2f)] private float triangleDist;
    [SerializeField][Range(0, 1)] private float triangleLerp;
    [SerializeField] private bool spawn;
    [SerializeField] private bool reset;
    
    private Mesh charMesh;
    private Transform meshTransform;
    
    private GraphicsBuffer gpuVertices;
    private GraphicsBuffer gpuSkinnedVertices;
    private GraphicsBuffer gpuIndices;
    private GraphicsBuffer triangleVelocity;
    
    private void OnEnable()
    {
        charMesh = meshFilter.sharedMesh;
        meshTransform = meshFilter.transform;
        triangleVelocity = new GraphicsBuffer(GraphicsBuffer.Target.Structured, charMesh.triangles.Length / 3 + 3, 12);

        Vector3[] randomDir = new Vector3[charMesh.triangles.Length / 3 + 3];
        for (int i = 0; i < randomDir.Length; i++)
        {
            randomDir[i] = new Vector3(UnityEngine.Random.Range(-0.1f, 0.1f), UnityEngine.Random.Range(-0.1f, 0.1f), UnityEngine.Random.Range(-0.1f, 0.1f));
        }
        
        triangleVelocity.SetData(randomDir);
        
        SetMesh();
    }
    
    void OnDisable()
    {
        gpuVertices?.Dispose();
        gpuVertices = null;
        gpuIndices?.Dispose();
        gpuIndices = null;
        triangleVelocity?.Dispose();
        triangleVelocity = null;
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

        Vector4 target = meshTransform.worldToLocalMatrix.MultiplyPoint3x4(targetTransform.position);
        
        computeShader.SetFloat("triDist", triangleDist);
        computeShader.SetFloat("triLerp", triangleLerp);
        computeShader.SetFloat("time", Time.time);
        computeShader.SetFloat("deltaTime", Time.deltaTime);
        computeShader.SetBool("spawn", spawn);
        computeShader.SetBool("reset", reset);
        computeShader.SetVector("target", target);
        
        computeShader.SetBuffer(0, "bufSkinnedVertices", gpuSkinnedVertices);
        computeShader.SetBuffer(0, "bufVertices", gpuVertices);
        computeShader.SetBuffer(0, "bufIndices", gpuIndices);
        computeShader.SetBuffer(0, "triangleVelocity", triangleVelocity);
        
        computeShader.Dispatch(0, (charMesh.triangles.Length / 3 - 63) / 64 + 2, 1, 1);
        
        // Vertex0[] vertex0 = new Vertex0[charMesh.vertexCount];
        // gpuSkinnedVertices.GetData(vertex0);
        //charMesh.SetVertices(vertex0.Select(vertex => vertex.position).ToArray());
        // UInt32[] index = new uint[charMesh.triangles.Length];
        // gpuIndices.GetData(index);
    }
    
    private void SetMesh()
    {
        charMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        charMesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
        skinnedMeshRenderer.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
    }
}



struct Vertex0
{
    public Vector3 position;
    public Vector3 normal;
    public Vector4 tangent;
}

