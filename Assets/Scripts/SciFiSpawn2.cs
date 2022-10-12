using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class SciFiSpawn2 : MonoBehaviour
{
    [SerializeField] private ComputeShader computeShader;
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshFilter underMeshFilter;
    [SerializeField] private Material[] meshMats;
    [SerializeField] private Transform animTransform;
    [SerializeField] private Transform charMeshTransform;
    [SerializeField] private Transform underCharMeshTransform;
    [SerializeField][Range(0, 0.5f)] private float triangleDist;
    [SerializeField][Range(0, 1)] private float triangleLerp;
    [SerializeField][Range(0, 1)] private float addDist;
    [SerializeField][Range(0, 10)] private float triangleTotalTime;

    private Mesh charMesh;
    private Mesh underCharMesh;
    private Transform meshTransform;
    private Camera mainCam;

    private GraphicsBuffer gpuSkinnedVertices;
    private GraphicsBuffer gpuUnderVertices;
    private GraphicsBuffer gpuVertices;
    private GraphicsBuffer gpuIndices;

    private void OnEnable()
    {
        charMesh = meshFilter.sharedMesh;
        underCharMesh = underMeshFilter.sharedMesh;
        meshTransform = meshFilter.transform;

        mainCam = Camera.main;
        
        SetMesh();
    }
    
    void OnDisable()
    {
        gpuSkinnedVertices?.Dispose();
        gpuSkinnedVertices = null;
        gpuVertices?.Dispose();
        gpuVertices = null;
        gpuIndices?.Dispose();
        gpuIndices = null;
        gpuUnderVertices?.Dispose();
        gpuUnderVertices = null;
    }

    void Update()
    {
        if (Time.time > 0.5f)
        {
            UpdateChar();
        }

        charMeshTransform.position = animTransform.position;
        charMeshTransform.rotation = animTransform.rotation;
        
        underCharMeshTransform.position = animTransform.position + new Vector3(1, 0 , 0) * addDist;
        underCharMeshTransform.rotation = animTransform.rotation;
    }


    private void UpdateChar()
    {
        foreach (var meshMat in meshMats)
        {
            meshMat.SetFloat("_TotalTime", triangleTotalTime);
        }
        
        gpuSkinnedVertices ??= skinnedMeshRenderer.GetVertexBuffer();
        gpuUnderVertices ??= underCharMesh.GetVertexBuffer(0);
        gpuVertices ??= charMesh.GetVertexBuffer(0);
        gpuIndices ??= charMesh.GetIndexBuffer();

        computeShader.SetFloat("triDist", triangleDist);
        computeShader.SetFloat("triLerp", triangleLerp);
        computeShader.SetFloat("time", Time.time);
        computeShader.SetFloat("totalTime", triangleTotalTime);
        computeShader.SetVector("worldPos", transform.position);
        computeShader.SetMatrix("objToWorld", meshTransform.localToWorldMatrix);
        computeShader.SetMatrix("worldToObj", meshTransform.worldToLocalMatrix);
        
        computeShader.SetBuffer(0, "bufSkinnedVertices", gpuSkinnedVertices);
        computeShader.SetBuffer(0, "bufUnderVertices", gpuUnderVertices);
        computeShader.SetBuffer(0, "bufVertices", gpuVertices);
        computeShader.SetBuffer(0, "bufIndices", gpuIndices);
        
        computeShader.Dispatch(0, (charMesh.triangles.Length / 3 - 63) / 64 + 2, 1, 1);
        
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log($"fired");
            
            RaycastHit hit;
            Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        
            if (Physics.Raycast(ray, out hit))
            {
                Vector4 newTarget = meshTransform.worldToLocalMatrix.MultiplyPoint3x4(hit.point);
                computeShader.SetVector("target", newTarget);
                computeShader.SetFloat("triLerp", triangleLerp);
                computeShader.SetFloat("time", Time.time);
                computeShader.SetBuffer(1, "bufSkinnedVertices", gpuSkinnedVertices);
                computeShader.SetBuffer(1, "bufUnderVertices", gpuUnderVertices);

                computeShader.SetBuffer(1, "bufVertices", gpuVertices);
                computeShader.SetBuffer(1, "bufIndices", gpuIndices);
                
                computeShader.Dispatch(1, (charMesh.triangles.Length / 3 - 63) / 64 + 2, 1, 1);
                
                StartCoroutine(waitTest());
            }
        }
        
        computeShader.SetBuffer(2, "bufVertices", gpuVertices);
        computeShader.SetBuffer(2, "bufIndices", gpuIndices);
        computeShader.SetBuffer(2, "bufUnderVertices", gpuUnderVertices);


        computeShader.Dispatch(2, (charMesh.triangles.Length / 3 - 63) / 64 + 2, 1, 1);

        // Vertex0[] vertex0 = new Vertex0[charMesh.vertexCount];
        // gpuSkinnedVertices.GetData(vertex0);
        //charMesh.SetVertices(vertex0.Select(vertex => vertex.position).ToArray());
        // UInt32[] index = new uint[charMesh.triangles.Length];
        // gpuIndices.GetData(index);
    }

    IEnumerator waitTest()
    {
        yield return new WaitForSeconds(0.1f);
        Vertex0[] vertex0 = new Vertex0[charMesh.vertexCount];
        gpuVertices.GetData(vertex0);
    }
    
    private void SetMesh()
    {
        charMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        underCharMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        charMesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
        skinnedMeshRenderer.vertexBufferTarget |= GraphicsBuffer.Target.Raw;

        VertexAttributeDescriptor[] attrib = new VertexAttributeDescriptor[10];
        charMesh.GetVertexAttributes(attrib);
        for (int i = 0; i < 10; i++)
        {
            Debug.Log(attrib[i]);
        }
        
        charMesh.SetVertexBufferParams(charMesh.vertexCount, 
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, dimension:3,stream:0), 
                        new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, dimension:3,stream:0),
                        new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float32, dimension:4,stream:0),
                        new VertexAttributeDescriptor(VertexAttribute.TexCoord4, VertexAttributeFormat.Float32, dimension:3,stream:0),
                        new VertexAttributeDescriptor(VertexAttribute.TexCoord5, VertexAttributeFormat.Float32, dimension:3,stream:0),
                        new VertexAttributeDescriptor(VertexAttribute.TexCoord6, VertexAttributeFormat.Float32, dimension:2,stream:0),
                        new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, dimension:2,stream:1),
                        new VertexAttributeDescriptor(VertexAttribute.BlendWeight, VertexAttributeFormat.Float32, dimension:4,stream:2),
                        new VertexAttributeDescriptor(VertexAttribute.BlendIndices, VertexAttributeFormat.UInt32, dimension:4,stream:2)
            );
        
        underCharMesh.SetVertexBufferParams(charMesh.vertexCount, 
                                       new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, dimension:3,stream:0), 
                                       new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, dimension:3,stream:0),
                                       new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float32, dimension:4,stream:0),
                                       new VertexAttributeDescriptor(VertexAttribute.TexCoord2, VertexAttributeFormat.Float32, dimension:2,stream:0),
                                       new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, dimension:2,stream:1),
                                       new VertexAttributeDescriptor(VertexAttribute.BlendWeight, VertexAttributeFormat.Float32, dimension:4,stream:2),
                                       new VertexAttributeDescriptor(VertexAttribute.BlendIndices, VertexAttributeFormat.UInt32, dimension:4,stream:2)
        );
        
        // als het opeens niet werkt check dit
        // charMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        // charMesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
        // skinnedMeshRenderer.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        // underCharMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
    }
}

struct Vertex0
{
    public Vector3 position;
    public Vector3 normal;
    public Vector4 tangent;
    public Vector3 texcoord4;
    public Vector3 texcoord5;
    public Vector2 texcoord6;
}