using UnityEngine;
using UnityEngine.Rendering;

namespace Bosmo
{
    public class Bosmo : MonoBehaviour, IHittable
    {
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private bool reverseDirection;
        [SerializeField] private bool debug;
        [SerializeField] private bool run;
        private DiscoverEffect discoverMesh;
        private GetTriangle closestTriangle;
        private SeparateTriangleHeight triangleHeight;

        private Mesh mesh;
    
        void Awake()
        {
            mesh = meshFilter.sharedMesh;
            mesh = MeshExtensions.CopyMesh(mesh);
            meshFilter.sharedMesh = mesh;

            mesh.uv4 = new Vector2[mesh.vertexCount];
        
            mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
            mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
            mesh.indexFormat = IndexFormat.UInt32;

            MeshExtensions.AddVertexAttribute(mesh, new VertexAttributeDescriptor(VertexAttribute.TexCoord3, VertexAttributeFormat.Float32, 2, 0));
            MeshExtensions.AddVertexAttribute(mesh, new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2, 1));
        
            discoverMesh = new DiscoverEffect(mesh);
            discoverMesh.DiscoverAllAdjacentTriangle();
            closestTriangle = new GetTriangle(mesh, meshFilter.transform);
            triangleHeight = new SeparateTriangleHeight(mesh);
        }

        void FixedUpdate()
        {
            if (Input.GetKeyDown(KeyCode.F) || run)
            {
                discoverMesh.IncrementTriangles();
                discoverMesh.DecayMesh();
                triangleHeight.UpdateTriangles();
            }
            
        }
        public void Hit(Vector3 hitPos)
        {
            int closestTriangleID = closestTriangle.GetClosestTriangle(hitPos);
            if (!debug)
            {
                discoverMesh.FirstTriangleToCheck(closestTriangleID, reverseDirection);
            }
        }
    }
}
