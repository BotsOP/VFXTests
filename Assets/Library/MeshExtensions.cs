using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class MeshExtensions
{
    public static Mesh CopyMesh(Mesh mesh)
    {
        Mesh newMesh = new Mesh();
        newMesh.name = "copy of " + mesh.name;

        newMesh.indexFormat = mesh.indexFormat;
        newMesh.vertices = mesh.vertices;
        newMesh.normals = mesh.normals;
        newMesh.tangents = mesh.tangents;
        newMesh.bounds = mesh.bounds;
        newMesh.bindposes = mesh.bindposes;
        newMesh.indexBufferTarget = mesh.indexBufferTarget;
        newMesh.colors = mesh.colors;
        newMesh.colors32 = mesh.colors32;
        newMesh.boneWeights = mesh.boneWeights;
        newMesh.uv = mesh.uv;
        newMesh.uv2 = mesh.uv2;
        newMesh.uv3 = mesh.uv3;
        newMesh.uv4 = mesh.uv4;
        newMesh.uv5 = mesh.uv5;
        newMesh.uv6 = mesh.uv6;
        newMesh.uv7 = mesh.uv7;
        newMesh.uv8 = mesh.uv8;
        newMesh.subMeshCount = mesh.subMeshCount;

        for (int i = 0; i < newMesh.subMeshCount; i++)
        {
            newMesh.SetIndices(mesh.GetIndices(i), MeshTopology.Triangles, i, true, 0);
            newMesh.SetSubMesh(i,mesh.GetSubMesh(i));
        }
        return newMesh;
    }

    #region EditVertexAttribute
    public static void EditVertexAttribute(Mesh mesh, VertexAttribute attrib, int dimension)
    {
        if (mesh.HasVertexAttribute(attrib))
        {
            int vertexAttribCount = GetVertexAttribCount(mesh);
            VertexAttributeDescriptor[] meshAttrib = new VertexAttributeDescriptor[vertexAttribCount];
            mesh.GetVertexAttributes(meshAttrib);

            for (int i = 0; i < vertexAttribCount; i++)
            {
                if (meshAttrib[i].attribute == attrib)
                {
                    meshAttrib[i].dimension = dimension;
                }
            }
        }
        else
        {
            Debug.LogError($"Mesh: {mesh.name} doesnt have vertex attribute {attrib} but you are trying to edit it");
        }
    }
    public static void EditVertexAttribute(Mesh mesh, VertexAttribute attrib, int dimension, VertexAttributeFormat format)
    {
        if (mesh.HasVertexAttribute(attrib))
        {
            int vertexAttribCount = GetVertexAttribCount(mesh);
            VertexAttributeDescriptor[] meshAttrib = new VertexAttributeDescriptor[vertexAttribCount];
            mesh.GetVertexAttributes(meshAttrib);

            for (int i = 0; i < vertexAttribCount; i++)
            {
                if (meshAttrib[i].attribute == attrib)
                {
                    meshAttrib[i].dimension = dimension;
                    meshAttrib[i].format = format;
                }
            }
        }
        else
        {
            Debug.LogError($"Mesh: {mesh.name} doesnt have vertex attribute {attrib} but you are trying to edit it");
        }
    }
    public static void EditVertexAttribute(Mesh mesh, VertexAttribute attrib, int dimension, VertexAttributeFormat format, uint stream)
    {
        if (mesh.HasVertexAttribute(attrib))
        {
            int vertexAttribCount = GetVertexAttribCount(mesh);
            VertexAttributeDescriptor[] meshAttrib = new VertexAttributeDescriptor[vertexAttribCount];
            mesh.GetVertexAttributes(meshAttrib);

            for (int i = 0; i < vertexAttribCount; i++)
            {
                if (meshAttrib[i].attribute == attrib)
                {
                    meshAttrib[i].dimension = dimension;
                    meshAttrib[i].format = format;
                    meshAttrib[i].stream = (int)stream;
                }
            }
        }
        else
        {
            Debug.LogError($"Mesh: {mesh.name} doesnt have vertex attribute {attrib} but you are trying to edit it");
        }
    }
    public static void EditVertexAttribute(Mesh mesh, VertexAttribute attrib, int dimension, uint stream)
    {
        if (mesh.HasVertexAttribute(attrib))
        {
            int vertexAttribCount = GetVertexAttribCount(mesh);
            VertexAttributeDescriptor[] meshAttrib = new VertexAttributeDescriptor[vertexAttribCount];
            mesh.GetVertexAttributes(meshAttrib);

            for (int i = 0; i < vertexAttribCount; i++)
            {
                if (meshAttrib[i].attribute == attrib)
                {
                    meshAttrib[i].dimension = dimension;
                    meshAttrib[i].stream = (int)stream;
                }
            }
        }
        else
        {
            Debug.LogError($"Mesh: {mesh.name} doesnt have vertex attribute {attrib} but you are trying to edit it");
        }
    }
    public static void EditVertexAttribute(Mesh mesh, VertexAttribute attrib, VertexAttributeFormat format)
    {
        if (mesh.HasVertexAttribute(attrib))
        {
            int vertexAttribCount = GetVertexAttribCount(mesh);
            VertexAttributeDescriptor[] meshAttrib = new VertexAttributeDescriptor[vertexAttribCount];
            mesh.GetVertexAttributes(meshAttrib);

            for (int i = 0; i < vertexAttribCount; i++)
            {
                if (meshAttrib[i].attribute == attrib)
                {
                    meshAttrib[i].format = format;
                }
            }
        }
        else
        {
            Debug.LogError($"Mesh: {mesh.name} doesnt have vertex attribute {attrib} but you are trying to edit it");
        }
    }
    public static void EditVertexAttribute(Mesh mesh, VertexAttribute attrib, uint stream)
    {
        if (mesh.HasVertexAttribute(attrib))
        {
            int vertexAttribCount = GetVertexAttribCount(mesh);
            VertexAttributeDescriptor[] meshAttrib = new VertexAttributeDescriptor[vertexAttribCount];
            mesh.GetVertexAttributes(meshAttrib);

            for (int i = 0; i < vertexAttribCount; i++)
            {
                if (meshAttrib[i].attribute == attrib)
                {

                    meshAttrib[i].stream = (int)stream;
                }
            }
        }
        else
        {
            Debug.LogError($"Mesh: {mesh.name} doesnt have vertex attribute {attrib} but you are trying to edit it");
        }
    }
    public static void EditVertexAttribute(Mesh mesh, VertexAttribute attrib, VertexAttributeFormat format, uint stream)
    {
        if (mesh.HasVertexAttribute(attrib))
        {
            int vertexAttribCount = GetVertexAttribCount(mesh);
            VertexAttributeDescriptor[] meshAttrib = new VertexAttributeDescriptor[vertexAttribCount];
            mesh.GetVertexAttributes(meshAttrib);

            for (int i = 0; i < vertexAttribCount; i++)
            {
                if (meshAttrib[i].attribute == attrib)
                {
                    meshAttrib[i].format = format;
                    meshAttrib[i].stream = (int)stream;
                }
            }
        }
        else
        {
            Debug.LogError($"Mesh: {mesh.name} doesnt have vertex attribute {attrib} but you are trying to edit it");
        }
    }
    #endregion

    public static VertexAttributeDescriptor GetVertexAttribute(Mesh mesh, VertexAttribute attrib)
    {
        if (mesh.HasVertexAttribute(attrib))
        {
            int vertexAttribCount = GetVertexAttribCount(mesh);
            VertexAttributeDescriptor[] meshAttrib = new VertexAttributeDescriptor[vertexAttribCount];
            mesh.GetVertexAttributes(meshAttrib);

            for (int i = 0; i < vertexAttribCount; i++)
            {
                if (meshAttrib[i].attribute == attrib)
                {
                    return meshAttrib[i];
                }
            }
        }
        else
        {
            Debug.LogError($"Mesh: {mesh.name} doesnt have vertex attribute {attrib}");
        }
        return new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float32, dimension: 3, stream: 0);
    }
    
    public static void AddVertexAttribute(Mesh mesh, VertexAttributeDescriptor attrib)
    {
        int vertexAttribCount = GetVertexAttribCount(mesh) + 1;
        VertexAttributeDescriptor[] meshAttrib = new VertexAttributeDescriptor[vertexAttribCount];
        mesh.GetVertexAttributes(meshAttrib);
        meshAttrib[^1] = attrib;
        mesh.SetVertexBufferParams(mesh.vertexCount, meshAttrib);
    }

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
