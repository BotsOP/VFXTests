using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Bosmo : MonoBehaviour, IHittable
{
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private bool reverseDirection;
    private DiscoverEffect discoverMesh;
    private GetTriangle closestTriangle;

    private Mesh mesh;
    
    void Awake()
    {
        mesh = meshFilter.sharedMesh;
        mesh = MeshExtensions.CopyMesh(mesh);
        meshFilter.sharedMesh = mesh;

        mesh.uv4 = new Vector2[mesh.vertexCount];
        
        mesh.vertexBufferTarget |= GraphicsBuffer.Target.Structured;
        mesh.indexBufferTarget |= GraphicsBuffer.Target.Structured;

        MeshExtensions.AddVertexAttribute(mesh, new VertexAttributeDescriptor(VertexAttribute.TexCoord3, VertexAttributeFormat.Float32, 2, 0));
        MeshExtensions.AddVertexAttribute(mesh, new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2, 1));
        
        discoverMesh = new DiscoverEffect(mesh);
        discoverMesh.DiscoverAllAdjacentTriangle();
        closestTriangle = new GetTriangle(mesh, meshFilter.transform);
    }

    void FixedUpdate()
    {
        discoverMesh.IncrementTriangles();
        discoverMesh.DecayMesh();
    }
    public void Hit(Vector3 hitPos)
    {
        int closestTriangleID = closestTriangle.GetClosestTriangle(hitPos);
        discoverMesh.FirstTriangleToCheck(closestTriangleID, reverseDirection);
    }
}
