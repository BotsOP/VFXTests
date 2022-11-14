using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class AnimateVBOTest : MonoBehaviour
{
    public ComputeShader shader;
    public Mesh mesh;

    private GraphicsBuffer gpuVertices;
    private GraphicsBuffer gpuIndices;

    private void Awake()
    {
        mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        
        if (!mesh.HasVertexAttribute(VertexAttribute.TexCoord3))
        {
            int skinnedVertexAttribCount = GetVertexAttribCount(mesh) + 1;
            VertexAttributeDescriptor[] skinnedMeshAttrib = new VertexAttributeDescriptor[skinnedVertexAttribCount];
            mesh.GetVertexAttributes(skinnedMeshAttrib);
        
            skinnedMeshAttrib[^1] = new VertexAttributeDescriptor(VertexAttribute.TexCoord3, VertexAttributeFormat.Float32, dimension: 2, stream: 3);
        
            Debug.Log("Skinned Mesh");
            for (int i = 0; i < skinnedVertexAttribCount; i++)
            {
                Debug.Log(skinnedMeshAttrib[i]);
            }
        
            mesh.SetVertexBufferParams(mesh.vertexCount, skinnedMeshAttrib);
        }
        
        int t = GetVertexAttribCount(mesh);
        VertexAttributeDescriptor[] a = new VertexAttributeDescriptor[t];
        mesh.GetVertexAttributes(a);
        
        Debug.Log("Skinned Mesh");
        for (int i = 0; i < t; i++)
        {
            Debug.Log(a[i]);
        }
    }

    private void Update()
    {
        gpuVertices ??= mesh.GetVertexBuffer(3);
        gpuIndices ??= mesh.GetIndexBuffer();
        
        shader.SetBuffer(0, "bufVertices", gpuVertices);
        shader.SetBuffer(0, "bufIndices", gpuIndices);
        shader.Dispatch(0, 1, 1, 1);

        Vector2[] uv3 = new Vector2[mesh.vertexCount];
        gpuVertices.GetData(uv3);
        Debug.Log(uv3[0]);
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
