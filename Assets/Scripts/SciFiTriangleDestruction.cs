using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class SciFiTriangleDestruction : MonoBehaviour
{
    [SerializeField] private ComputeShader computeShader;
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
    [SerializeField] private Material[] meshMats;
    [SerializeField] private Transform animTransform;
    [SerializeField][Range(0, 0.5f)] private float triangleDist;
    [SerializeField][Range(0, 1)] private float triangleLerp;
    [SerializeField][Range(0, 1)] private float addDist;
    [SerializeField][Range(0, 10)] private float triangleTotalTime;

    private Mesh charMesh;
    private Camera mainCam;
    private MeshFilter charMeshFilter;

    private Transform charMeshTransform;
    private GraphicsBuffer gpuSkinnedVertices;
    private GraphicsBuffer gpuUnderVertices;
    private GraphicsBuffer gpuVertices;
    private GraphicsBuffer gpuIndices;

    private void Awake()
    {
        GameObject meshObject = new GameObject();
        meshObject.name = "Mesh";
        meshObject.transform.parent = transform;
        charMeshFilter = meshObject.AddComponent<MeshFilter>();
        MeshRenderer meshRend = meshObject.AddComponent<MeshRenderer>();
        meshRend.materials = meshMats;
        charMeshTransform = meshObject.transform;
    }

    private void OnEnable()
    {
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
        UpdateChar();

        charMeshTransform.position = animTransform.position;
        charMeshTransform.rotation = animTransform.rotation;
        
    }


    private void UpdateChar()
    {
        // foreach (var meshMat in meshMats)
        // {
        //     meshMat.SetFloat("_TotalTime", triangleTotalTime);
        // }
        
        gpuSkinnedVertices ??= skinnedMeshRenderer.GetVertexBuffer();
        gpuUnderVertices ??= skinnedMeshRenderer.sharedMesh.GetVertexBuffer(3);
        gpuVertices ??= charMesh.GetVertexBuffer(0);
        gpuIndices ??= charMesh.GetIndexBuffer();

        if (gpuSkinnedVertices != null && gpuVertices != null && gpuIndices != null)
        {
            computeShader.SetFloat("triDist", triangleDist);
            computeShader.SetFloat("triLerp", triangleLerp);
            computeShader.SetFloat("time", Time.time);
            computeShader.SetFloat("totalTime", triangleTotalTime);
            computeShader.SetVector("worldPos", transform.position);
            computeShader.SetMatrix("objToWorld", charMeshTransform.localToWorldMatrix);
            computeShader.SetMatrix("worldToObj", charMeshTransform.worldToLocalMatrix);
            
            computeShader.SetBuffer(0, "bufSkinnedVertices", gpuSkinnedVertices);
            computeShader.SetBuffer(0, "bufVertices", gpuVertices);
            computeShader.SetBuffer(0, "bufIndices", gpuIndices);
            computeShader.SetBuffer(0, "bufUnderVertices", gpuUnderVertices);
            
            computeShader.Dispatch(0, (charMesh.triangles.Length / 3 - 63) / 64 + 2, 1, 1);
            
            Vector2[] uv3 = new Vector2[charMesh.vertexCount];
            gpuUnderVertices.GetData(uv3);
            
            if (Input.GetMouseButtonDown(0))
            {
                Debug.Log($"fired");
                
                RaycastHit hit;
                Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
            
                if (Physics.Raycast(ray, out hit))
                {
                    Vector4 newTarget = charMeshTransform.worldToLocalMatrix.MultiplyPoint3x4(hit.point);
                    computeShader.SetVector("target", newTarget);
                    computeShader.SetFloat("triLerp", triangleLerp);
                    computeShader.SetFloat("time", Time.time);
                    computeShader.SetBuffer(1, "bufSkinnedVertices", gpuSkinnedVertices);

                    computeShader.SetBuffer(1, "bufVertices", gpuVertices);
                    computeShader.SetBuffer(1, "bufIndices", gpuIndices);
                    
                    computeShader.Dispatch(1, (charMesh.triangles.Length / 3 - 63) / 64 + 2, 1, 1);
                }
            }
            
            computeShader.SetBuffer(2, "bufVertices", gpuVertices);
            computeShader.SetBuffer(2, "bufIndices", gpuIndices);
            computeShader.SetBuffer(2, "bufUnderVertices", gpuUnderVertices);

            computeShader.Dispatch(2, (charMesh.triangles.Length / 3 - 63) / 64 + 2, 1, 1);
        }
        
    }

    private void SetMesh()
    {
        charMesh = new Mesh();
        charMesh.name = "mesh";
        Mesh animatedMesh = skinnedMeshRenderer.sharedMesh;
        
        charMesh.indexFormat = IndexFormat.UInt32;
        charMesh.vertices = animatedMesh.vertices;
        charMesh.normals = animatedMesh.normals;
        charMesh.tangents = animatedMesh.tangents;
        charMesh.uv = animatedMesh.uv;
        charMesh.uv2 = animatedMesh.uv2;
        charMesh.subMeshCount = 2;
        
        charMesh.SetIndices(animatedMesh.GetIndices(0), MeshTopology.Triangles, 0, true, 0);
        charMesh.SetIndices(animatedMesh.GetIndices(1), MeshTopology.Triangles, 1, true, 0);
        charMesh.SetSubMesh(0,animatedMesh.GetSubMesh(0));
        charMesh.SetSubMesh(1,animatedMesh.GetSubMesh(1));
        
        charMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        charMesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
        skinnedMeshRenderer.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        animatedMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;

        //Set vertex attributes of mesh
        int vertexAttribCount = GetVertexAttribCount(animatedMesh) + 3;
        VertexAttributeDescriptor[] meshAttrib = new VertexAttributeDescriptor[vertexAttribCount];
        skinnedMeshRenderer.sharedMesh.GetVertexAttributes(meshAttrib);
        
        meshAttrib[^3] = new VertexAttributeDescriptor(VertexAttribute.TexCoord4, VertexAttributeFormat.Float32, dimension: 3, stream: 0);
        meshAttrib[^2] = new VertexAttributeDescriptor(VertexAttribute.TexCoord5, VertexAttributeFormat.Float32, dimension:3,  stream: 0);
        meshAttrib[^1] = new VertexAttributeDescriptor(VertexAttribute.TexCoord6, VertexAttributeFormat.Float32, dimension: 2, stream: 0);
        
        Debug.Log("Mesh");
        for (int i = 0; i < vertexAttribCount; i++)
        {
            Debug.Log(meshAttrib[i]);
        }
        
        charMesh.SetVertexBufferParams(charMesh.vertexCount, meshAttrib);
        charMeshFilter.sharedMesh = charMesh;
        
        //Set vertex attributes of skinned mesh
        if (!animatedMesh.HasVertexAttribute(VertexAttribute.TexCoord3))
        {
            int skinnedVertexAttribCount = GetVertexAttribCount(animatedMesh) + 1;
            VertexAttributeDescriptor[] skinnedMeshAttrib = new VertexAttributeDescriptor[skinnedVertexAttribCount];
            skinnedMeshRenderer.sharedMesh.GetVertexAttributes(skinnedMeshAttrib);
        
            skinnedMeshAttrib[^1] = new VertexAttributeDescriptor(VertexAttribute.TexCoord3, VertexAttributeFormat.Float32, dimension: 2, stream: 3);
        
            Debug.Log("Skinned Mesh");
            for (int i = 0; i < skinnedVertexAttribCount; i++)
            {
                Debug.Log(skinnedMeshAttrib[i]);
            }
        
            skinnedMeshRenderer.sharedMesh.SetVertexBufferParams(charMesh.vertexCount, skinnedMeshAttrib);
        }

        Debug.Log(animatedMesh.GetVertexBuffer(0).stride);
        Debug.Log(charMesh.GetVertexBuffer(0).stride);
        Debug.Log(animatedMesh.GetVertexBuffer(3).stride);
        computeShader.SetInt("SKINNED_VERTEX_STRIDE", animatedMesh.GetVertexBuffer(0).stride);
        computeShader.SetInt("UNDER_VERTEX_STRIDE", animatedMesh.GetVertexBuffer(3).stride);
        computeShader.SetInt("VERTEX_STRIDE", charMesh.GetVertexBuffer(0).stride);
        
        charMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        charMesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
        skinnedMeshRenderer.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        animatedMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
    }

    private int GetVertexAttribCount(Mesh mesh)
    {
        int count = 0;
        
        if (mesh.HasVertexAttribute(VertexAttribute.Color))
            count++;
        
        if (mesh.HasVertexAttribute(VertexAttribute.Normal))
            count++;
        
        if (mesh.HasVertexAttribute(VertexAttribute.Position))
            count++;
        
        if (mesh.HasVertexAttribute(VertexAttribute.Tangent))
            count++;
        
        if (mesh.HasVertexAttribute(VertexAttribute.BlendIndices))
            count++;
        
        if (mesh.HasVertexAttribute(VertexAttribute.BlendWeight))
            count++;
        
        if (mesh.HasVertexAttribute(VertexAttribute.TexCoord0))
            count++;
        
        if (mesh.HasVertexAttribute(VertexAttribute.TexCoord1))
            count++;
        
        if (mesh.HasVertexAttribute(VertexAttribute.TexCoord2))
            count++;
        
        if (mesh.HasVertexAttribute(VertexAttribute.TexCoord3))
            count++;
        
        if (mesh.HasVertexAttribute(VertexAttribute.TexCoord4))
            count++;
        
        if (mesh.HasVertexAttribute(VertexAttribute.TexCoord5))
            count++;
        
        if (mesh.HasVertexAttribute(VertexAttribute.TexCoord6))
            count++;
        
        if (mesh.HasVertexAttribute(VertexAttribute.TexCoord7))
            count++;

        return count;
    }
}