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
    [SerializeField] private MeshRenderer meshRend;
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
                gradientTexture.wrapMode = TextureWrapMode.Clamp;
            }
            
            for (int x = 0; x < 256; x++)
            {
                Color color = triangleOutlineGradient.Evaluate(0 + (x / (float)256));
                gradientTexture.SetPixel(x, 0, color);
                gradientTexture.SetPixel(x, 1, color);
            }
            
            gradientTexture.Apply();
            
            foreach (var meshMat in meshRend.sharedMaterials)
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
        meshObject.transform.localPosition = Vector3.zero;
        meshObject.transform.localRotation = Quaternion.identity;
        meshObject.transform.localScale = new Vector3(0.99f, 0.99f, 0.99f);
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
        gpuVertices?.Dispose();
        gpuVertices = null;
        gpuIndices?.Dispose();
        gpuIndices = null;
        gpuUnderVertices?.Dispose();
        gpuUnderVertices = null;
    }

    void Update()
    {
        foreach (var meshMat in meshRend.materials)
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
            
            computeShader.SetFloat("triExplosionRadius", explosionRadius);
            computeShader.SetFloat("triExplosionDist", explosionDistance);
            computeShader.SetFloat("time", Time.time);
            computeShader.SetFloat("totalTime", triangleTotalTime);
            computeShader.SetFloat("beginTime", triangleBeginTransition);
            computeShader.SetFloat("endTime", triangleEndTransition);
            computeShader.SetVector("worldPos", transform.position);
            computeShader.SetVector("objScale", meshTransform.localScale);
            computeShader.SetMatrix("objToWorld", meshTransform.localToWorldMatrix);
            computeShader.SetMatrix("worldToObj", meshTransform.worldToLocalMatrix);
            
            computeShader.SetBuffer(2, "bufVertices", gpuVertices);
            computeShader.SetBuffer(2, "bufIndices", gpuIndices);
            computeShader.SetBuffer(2, "bufUnderVertices", gpuUnderVertices);

            computeShader.Dispatch(2, (mesh.triangles.Length - 63) / 64, 1, 1);

            computeShader.SetBuffer(1, "bufVertices", gpuVertices);
            computeShader.SetBuffer(1, "bufIndices", gpuIndices);
            computeShader.SetBuffer(1, "bufUnderVertices", gpuUnderVertices);

            computeShader.Dispatch(1, (mesh.triangles.Length / 3 - 63) / 64 + 2, 1, 1);
            
            VertexTri[] tris = new VertexTri[meshFilter.sharedMesh.vertexCount];
            gpuVertices.GetData(tris);
            
            Vector2[] uv3 = new Vector2[mesh.vertexCount];
            gpuUnderVertices.GetData(uv3);
            underMesh.uv5 = uv3;
            underMesh.uv4 = uv3;
            underMesh.uv3 = uv3;
            underMesh.uv2 = uv3;
            underMesh.uv = uv3;
        }
    }
    
    private void SetUnderMesh()
    {
        underMesh = new Mesh();
        underMesh.name = "undermesh";
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
        int vertexAttribCount = MeshExtensions.GetVertexAttribCount(mesh) + 4;
        VertexAttributeDescriptor[] meshAttrib = new VertexAttributeDescriptor[vertexAttribCount];
        mesh.GetVertexAttributes(meshAttrib);

        for (int i = 3; i < meshAttrib.Length - 4; i++)
        {
            meshAttrib[i].stream = 1;
        }
        underMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
        mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;

        if (!mesh.HasVertexAttribute(VertexAttribute.TexCoord4))
        {
            meshAttrib[^4] = new VertexAttributeDescriptor(VertexAttribute.TexCoord4, VertexAttributeFormat.Float32, dimension: 3, stream: 0);
            meshAttrib[^3] = new VertexAttributeDescriptor(VertexAttribute.TexCoord5, VertexAttributeFormat.Float32, dimension:3,  stream: 0);
            meshAttrib[^2] = new VertexAttributeDescriptor(VertexAttribute.TexCoord6, VertexAttributeFormat.Float32, dimension: 2, stream: 0);
            meshAttrib[^1] = new VertexAttributeDescriptor(VertexAttribute.TexCoord7, VertexAttributeFormat.Float32, dimension: 3, stream: 0);
            
            mesh.SetVertexBufferParams(mesh.vertexCount, meshAttrib);

            mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
            mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
            gpuVertices ??= mesh.GetVertexBuffer(0);
            gpuIndices ??= mesh.GetIndexBuffer();
            
            computeShader.SetBuffer(3, "bufVertices", gpuVertices);
            computeShader.SetBuffer(3, "bufIndices", gpuIndices);
            
            computeShader.Dispatch(3, (meshFilter.sharedMesh.triangles.Length / 3 - 63) / 64 + 2, 1, 1);
        }

        // foreach (var attribute in meshAttrib)
        // {
        //     Debug.Log($"{attribute}");
        // }
        underMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
        mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;

        //Set vertex attributes of skinned mesh
        int skinnedVertexAttribCount = MeshExtensions.GetVertexAttribCount(underMesh) + 1;
        VertexAttributeDescriptor[] underMeshAttrib = new VertexAttributeDescriptor[skinnedVertexAttribCount];
        underMesh.GetVertexAttributes(underMeshAttrib);
        
        if (!underMesh.HasVertexAttribute(VertexAttribute.TexCoord3))
        {
            underMeshAttrib[^1] = new VertexAttributeDescriptor(VertexAttribute.TexCoord3, VertexAttributeFormat.Float32, dimension: 2, stream: 3);
        
            underMesh.SetVertexBufferParams(mesh.vertexCount, underMeshAttrib);
        }
        
        foreach (var attribute in underMeshAttrib)
        {
            Debug.Log($"{attribute}");
        }
        
        underMeshFiler.sharedMesh = underMesh;
        underMeshRend.materials = meshMats;
        
        underMeshFiler.sharedMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
        mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
    }
    
    public void Hit(Vector3 hitPos)
    {
        Debug.Log($"hoi");
        Vector4 newTarget = meshTransform.worldToLocalMatrix.MultiplyPoint3x4(hitPos);
        computeShader.SetVector("target", hitPos);
        
        computeShader.SetBuffer(0, "bufVertices", gpuVertices);
        computeShader.SetBuffer(0, "bufIndices", gpuIndices);
                    
        computeShader.Dispatch(0, (mesh.triangles.Length / 3 - 63) / 64 + 2, 1, 1);
    }

    struct Vertex
    {
        public Vector3 pos;
        public Vector3 nor;
        public Vector4 tang;
    }
    
    struct VertexTri
    {
        public Vector3 pos;
        public Vector3 nor;
        public Vector4 tang;
        public Vector3 start;
        public Vector3 end;
        public Vector2 info;
        public Vector3 center;
    }
}
