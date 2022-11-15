using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


//Rigidbody is for the raycast to easily get the parent from any collider on the body
[RequireComponent(typeof(Rigidbody))]
public class TriangleDestruction : MonoBehaviour, IHittable
{
    [SerializeField] private ComputeShader computeShader;
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private Material[] meshMats;
    [SerializeField] private Gradient triangleOutlineGradient;
    private Gradient triangleOutlineGradientCheck;
    [SerializeField][Range(0, 0.5f)] private float explosionRadius = 0.1f;
    [SerializeField][Range(0, 3)] private float explosionDistance = 0.5f;
    [SerializeField][Range(0, 10)] private float triangleTotalTime = 5;
    [SerializeField][Range(0, 5)] private float triangleBeginTransition = 0.2f;
    [SerializeField][Range(0, 5)] private float triangleEndTransition = 0.5f;
    
    private Texture2D gradientTexture;

    private Transform meshTransform;
    private MeshFilter underMeshFiler;
    private MeshRenderer underMeshRend;

    private Mesh mesh;
    private Mesh underMesh;
    
    private GraphicsBuffer gpuOrgVertices;
    private GraphicsBuffer gpuUnderVertices;
    private GraphicsBuffer gpuVertices;
    private GraphicsBuffer gpuIndices;
    
    private void OnValidate()
    {
        if (triangleOutlineGradientCheck == null)
        {
            triangleOutlineGradientCheck = new Gradient();
        }
        
        if (!triangleOutlineGradient.colorKeys.Equals(triangleOutlineGradientCheck.colorKeys) || !triangleOutlineGradient.alphaKeys.Equals(triangleOutlineGradientCheck.alphaKeys))
        {
            triangleOutlineGradientCheck.colorKeys = triangleOutlineGradient.colorKeys;
            triangleOutlineGradientCheck.alphaKeys = triangleOutlineGradient.alphaKeys;

            if (gradientTexture == null)
            {
                gradientTexture = new Texture2D(256, 1);
                gradientTexture.wrapMode = TextureWrapMode.Repeat;
            }
            
            for (int x = 0; x < 256; x++)
            {
                Color color = triangleOutlineGradient.Evaluate(0 + (x / (float)256));
                gradientTexture.SetPixel(x, 0, color);
                gradientTexture.SetPixel(x, 1, color);
            }
            
            gradientTexture.Apply();
            
            foreach (var meshMat in meshMats)
            {
                meshMat.SetTexture("_OutlineGradient", gradientTexture);
            }
        }
    }
    
    private void Awake()
    {
        meshTransform = meshFilter.transform;
        GameObject meshObject = new GameObject();
        
        meshObject.name = "UnderMesh";
        meshObject.transform.parent = meshTransform;
        underMeshFiler = meshObject.AddComponent<MeshFilter>();
        underMeshRend = meshObject.AddComponent<MeshRenderer>();
        underMeshRend.materials = meshMats;
    }

    private void OnEnable()
    {
        SetUnderMesh();
    }
    
    void OnDisable()
    {
        gpuOrgVertices?.Dispose();
        gpuOrgVertices = null;
        gpuVertices?.Dispose();
        gpuVertices = null;
        gpuIndices?.Dispose();
        gpuIndices = null;
        gpuUnderVertices?.Dispose();
        gpuUnderVertices = null;
    }

    void Update()
    {
        foreach (var meshMat in meshMats)
        {
            meshMat.SetFloat("_TotalTime", triangleTotalTime);
        }
        
        gpuUnderVertices ??= underMesh.GetVertexBuffer(3);
        gpuVertices ??= mesh.GetVertexBuffer(0);
        gpuIndices ??= mesh.GetIndexBuffer();

        if (gpuVertices != null && gpuIndices != null && gpuUnderVertices != null)
        {
            computeShader.SetInt("UNDER_VERTEX_STRIDE", gpuUnderVertices.stride);
            computeShader.SetInt("VERTEX_STRIDE", gpuVertices.stride);

            computeShader.SetBuffer(1, "bufVertices", gpuVertices);
            computeShader.SetBuffer(1, "bufIndices", gpuIndices);
            computeShader.SetBuffer(1, "bufUnderVertices", gpuUnderVertices);
            computeShader.SetBuffer(1, "bufOrgVertices", gpuOrgVertices);

            computeShader.Dispatch(1, (mesh.triangles.Length / 3 - 63) / 64 + 2, 1, 1);
        }
    }
    
    private void SetUnderMesh()
    {
        underMesh = new Mesh();
        underMesh.name = "mesh";
        mesh = meshFilter.sharedMesh;
        
        underMesh.indexFormat = IndexFormat.UInt32;
        underMesh.vertices = mesh.vertices;
        underMesh.normals = mesh.normals;
        underMesh.tangents = mesh.tangents;
        underMesh.uv = mesh.uv;
        underMesh.uv2 = mesh.uv2;
        underMesh.uv3 = mesh.uv3;
        underMesh.subMeshCount = mesh.subMeshCount;

        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            underMesh.SetIndices(mesh.GetIndices(i), MeshTopology.Triangles, i, true, 0);
            underMesh.SetSubMesh(i,mesh.GetSubMesh(i));
        }

        underMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
        mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;

        //Set vertex attributes of mesh
        int vertexAttribCount = MeshExtensions.GetVertexAttribCount(mesh) + 3;
        VertexAttributeDescriptor[] meshAttrib = new VertexAttributeDescriptor[vertexAttribCount];
        mesh.GetVertexAttributes(meshAttrib);

        for (int i = 3; i < meshAttrib.Length - 3; i++)
        {
            meshAttrib[i].stream = 1;
        }

        meshAttrib[^3] = new VertexAttributeDescriptor(VertexAttribute.TexCoord4, VertexAttributeFormat.Float32, dimension: 3, stream: 0);
        meshAttrib[^2] = new VertexAttributeDescriptor(VertexAttribute.TexCoord5, VertexAttributeFormat.Float32, dimension:3,  stream: 0);
        meshAttrib[^1] = new VertexAttributeDescriptor(VertexAttribute.TexCoord6, VertexAttributeFormat.Float32, dimension: 2, stream: 0);

        mesh.SetVertexBufferParams(mesh.vertexCount, meshAttrib);

        // foreach (var attribute in meshAttrib)
        // {
        //     Debug.Log($"{attribute}");
        // }

        //Set vertex attributes of skinned mesh
        if (!underMesh.HasVertexAttribute(VertexAttribute.TexCoord3))
        {
            int skinnedVertexAttribCount = MeshExtensions.GetVertexAttribCount(underMesh) + 1;
            VertexAttributeDescriptor[] skinnedMeshAttrib = new VertexAttributeDescriptor[skinnedVertexAttribCount];
            underMesh.GetVertexAttributes(skinnedMeshAttrib);
        
            skinnedMeshAttrib[^1] = new VertexAttributeDescriptor(VertexAttribute.TexCoord3, VertexAttributeFormat.Float32, dimension: 2, stream: 3);
        
            underMesh.SetVertexBufferParams(mesh.vertexCount, skinnedMeshAttrib);
        }

        gpuOrgVertices = new GraphicsBuffer(GraphicsBuffer.Target.Raw, mesh.vertexCount, 40);
        Vertex[] orgVertices = new Vertex[mesh.vertexCount];
        for (int i = 0; i < orgVertices.Length; i++)
        {
            orgVertices[i].pos = mesh.vertices[i];
            orgVertices[i].nor = mesh.normals[i];
            orgVertices[i].tang = mesh.tangents[i];
        }
        gpuOrgVertices.SetData(orgVertices);

        underMeshFiler.sharedMesh = underMesh;
        underMeshRend.materials = meshMats;
    }
    
    public void Hit(Vector3 hitPos)
    {
        Debug.Log($"hoi");
        Vector4 newTarget = meshTransform.worldToLocalMatrix.MultiplyPoint3x4(hitPos);
        computeShader.SetVector("target", newTarget);
        computeShader.SetFloat("time", Time.time);
        
        computeShader.SetBuffer(0, "bufVertices", gpuVertices);
        computeShader.SetBuffer(0, "bufIndices", gpuIndices);
        computeShader.SetBuffer(0, "bufOrgVertices", gpuOrgVertices);
                    
        computeShader.Dispatch(0, (mesh.triangles.Length / 3 - 63) / 64 + 2, 1, 1);
    }

    struct Vertex
    {
        public Vector3 pos;
        public Vector3 nor;
        public Vector4 tang;
    }
}
