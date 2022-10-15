using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

public class MotionVectorTest : MonoBehaviour
{
    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct InputVertex
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector2 uv;
    }

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct InputTriangle
    {
        public InputVertex vertex0;
        public InputVertex vertex1;
        public InputVertex vertex2;
    }

    [SerializeField] private ComputeShader shellTextureCS;

    private int kernelID;
    private int threadGroupSize;

    private int[] indirectArgs = new[] { 0, 1, 0, 0 };

    private List<InputTriangle> inputTriangles;
    private ComputeBuffer drawTrianglesBuffer;
    private ComputeBuffer indirectArgsBuffer;

    private GraphicsBuffer inputVertexBuffer;
    private GraphicsBuffer inputPreviousVertexBuffer;
    private GraphicsBuffer inputUVBuffer;
    private GraphicsBuffer inputIndexBuffer;
    
    private const int DRAWTRIANGLES_STRIDE = (3 * (3 + 3 + 2 + 4)) * sizeof(float);
    private const int INDIRECTARGS_STRIDE = 4 * sizeof(int);

    [SerializeField] private Mesh mesh;
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
    [SerializeField] private float test;
    private int vertexCount;
    private bool initialized = false;
    private Vector3 startParenOffset;

    private void OnEnable()
    {
        // mesh = GetComponent<SkinnedMeshRenderer>().sharedMesh;
        // skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
        vertexCount = mesh.triangles.Length;
        
        SetupBuffers();
    }

    private void OnDisable()
    {
        ReleaseBuffers();
    }

    private void OnValidate()
    {
        SetupData();
        GenerateGeometry();
    }

    private void SetupData()
    {
        if (mesh == null)
        {
            return;
        }

        skinnedMeshRenderer.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        skinnedMeshRenderer.skinnedMotionVectors = true;
        mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
        
        mesh.SetVertexBufferParams(mesh.vertexCount, 
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, dimension:3,stream:0), 
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, dimension:3,stream:0),
            new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float32, dimension:4,stream:0),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord3, VertexAttributeFormat.Float32, dimension:3,stream:1),
            new VertexAttributeDescriptor(VertexAttribute.BlendWeight, VertexAttributeFormat.Float32, dimension:4,stream:2),
            new VertexAttributeDescriptor(VertexAttribute.BlendIndices, VertexAttributeFormat.UInt32, dimension:4,stream:2)
        );
    }

    private void GenerateGeometry()
    {
        if (mesh == null || shellTextureCS == null)
        {
            return;
        }

        inputPreviousVertexBuffer ??= skinnedMeshRenderer.GetPreviousVertexBuffer();
        inputVertexBuffer ??= skinnedMeshRenderer.GetVertexBuffer();
        inputUVBuffer ??= mesh.GetVertexBuffer(1);
        inputIndexBuffer ??= mesh.GetIndexBuffer();

        kernelID = shellTextureCS.FindKernel("MotionVector");
        shellTextureCS.GetKernelThreadGroupSizes(kernelID, out uint threadGroupSizeX, out _, out _);
        threadGroupSize = Mathf.CeilToInt((float)vertexCount / threadGroupSizeX);
        
        shellTextureCS.SetBuffer(kernelID, "_InputVertexBuffer", inputVertexBuffer);
        shellTextureCS.SetBuffer(kernelID, "_InputPreviousVertexBuffer", inputPreviousVertexBuffer);
        shellTextureCS.SetBuffer(kernelID, "_InputUVBuffer", inputUVBuffer);
        shellTextureCS.SetBuffer(kernelID, "_InputIndexBuffer", inputIndexBuffer);
        
        Debug.Log($"{kernelID} + {mesh.vertexCount} + {vertexCount} + {threadGroupSizeX}");
        
        shellTextureCS.Dispatch(kernelID, threadGroupSize, 1, 1);

        Vector3[] velocity = new Vector3[inputUVBuffer.count];
        inputUVBuffer.GetData(velocity);

        initialized = true;
    }

    private void Update()
    {
        if (!initialized)
        {
            if (Time.time > 1)
            {
                SetupData();
                GenerateGeometry();
            }
            
            return;
        }

        shellTextureCS.Dispatch(kernelID, threadGroupSize, 1, 1);

        // Vector3[] uv = new Vector3[inputUVBuffer.count];
        // inputUVBuffer.GetData(uv);
    }

    private void SetupBuffers()
    {
        // drawTrianglesBuffer = new ComputeBuffer(vertexCount * layers, DRAWTRIANGLES_STRIDE, ComputeBufferType.Append);
        // indirectArgsBuffer = new ComputeBuffer(1, INDIRECTARGS_STRIDE, ComputeBufferType.IndirectArguments);
    }
    
    private void ReleaseBuffers()
    {
        ReleaseBuffer(drawTrianglesBuffer);
        ReleaseBuffer(indirectArgsBuffer);
        ReleaseBuffer(inputVertexBuffer);
        ReleaseBuffer(inputUVBuffer);
        ReleaseBuffer(inputIndexBuffer);
        ReleaseBuffer(inputPreviousVertexBuffer);
    }
    
    private void ReleaseBuffer(ComputeBuffer buffer)
    {
        if (buffer != null)
        {
            buffer.Release();
            buffer = null;
        }
    }
    
    private void ReleaseBuffer(GraphicsBuffer buffer)
    {
        if (buffer != null)
        {
            buffer.Release();
            buffer = null;
        }
    }
}
