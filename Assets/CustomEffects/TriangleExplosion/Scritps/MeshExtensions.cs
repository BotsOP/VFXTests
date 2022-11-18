using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class MeshExtensions
{
    public static int GetVertexAttribCount(Mesh mesh)
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
