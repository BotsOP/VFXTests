using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

[RequireComponent(typeof(SkinnedMeshRenderer))]
public class ShellTexturingAnimated : MonoBehaviour
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
    [SerializeField] private Material renderingMaterial;
    [SerializeField] private Transform originalOffset;
    [SerializeField] private Transform parentOffset;
    [SerializeField] private Transform transformOffset;
    [Min(1)]
    [SerializeField] private int layers = 1;
    [SerializeField] private float heightOffset = 0;
    [SerializeField] private float uvScale = 1;

    private int kernelID;
    private int threadGroupSize;

    private int[] indirectArgs = new[] { 0, 1, 0, 0 };

    private List<InputTriangle> inputTriangles;
    private ComputeBuffer drawTrianglesBuffer;
    private ComputeBuffer indirectArgsBuffer;

    private GraphicsBuffer inputVertexBuffer;
    private GraphicsBuffer inputUVBuffer;
    private GraphicsBuffer inputIndexBuffer;
    
    private const int DRAWTRIANGLES_STRIDE = (3 * (3 + 3 + 2 + 4)) * sizeof(float);
    private const int INDIRECTARGS_STRIDE = 4 * sizeof(int);

    private Mesh mesh;
    private SkinnedMeshRenderer skinnedMeshRenderer;
    private int triangleCount;
    private bool initialized = false;
    private Vector3 startParenOffset;

    private void OnEnable()
    {
        mesh = GetComponent<SkinnedMeshRenderer>().sharedMesh;
        skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
        triangleCount = mesh.triangles.Length / 3;
        startParenOffset = parentOffset.position;
        
        SetupBuffers();
        SetupData();
        GenerateGeometry();
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
        mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
        
        mesh.SetVertexBufferParams(mesh.vertexCount, 
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, dimension:3,stream:0), 
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, dimension:3,stream:0),
            new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float32, dimension:4,stream:0),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, dimension:2,stream:1),
            new VertexAttributeDescriptor(VertexAttribute.BlendWeight, VertexAttributeFormat.Float32, dimension:4,stream:2),
            new VertexAttributeDescriptor(VertexAttribute.BlendIndices, VertexAttributeFormat.UInt32, dimension:4,stream:2)
        );

        drawTrianglesBuffer.SetCounterValue(0);
        indirectArgsBuffer.SetData(indirectArgs);

        initialized = true;
    }

    private void GenerateGeometry()
    {
        if (mesh == null || shellTextureCS == null || renderingMaterial == null)
        {
            return;
        }

        inputVertexBuffer ??= skinnedMeshRenderer.GetVertexBuffer();
        inputUVBuffer ??= mesh.GetVertexBuffer(1);
        inputIndexBuffer ??= mesh.GetIndexBuffer();

        kernelID = shellTextureCS.FindKernel("ShellTextureGeoAnim");
        shellTextureCS.GetKernelThreadGroupSizes(kernelID, out uint threadGroupSizeX, out _, out _);
        threadGroupSize = Mathf.CeilToInt((float)triangleCount / threadGroupSizeX);
        
        shellTextureCS.SetBuffer(kernelID, "_DrawTrianglesBuffer", drawTrianglesBuffer);
        shellTextureCS.SetBuffer(kernelID, "_IndirectArgsBuffer", indirectArgsBuffer);
        shellTextureCS.SetBuffer(kernelID, "_InputVertexBuffer", inputVertexBuffer);
        shellTextureCS.SetBuffer(kernelID, "_InputUVBuffer", inputUVBuffer);
        shellTextureCS.SetBuffer(kernelID, "_InputIndexBuffer", inputIndexBuffer);
        
        shellTextureCS.SetMatrix("_LocalToWorld", transform.localToWorldMatrix);
        shellTextureCS.SetMatrix("_Offset", transformOffset.localToWorldMatrix);
        shellTextureCS.SetInt("_TriangleCount", triangleCount);
        shellTextureCS.SetInt("_Layers", layers);
        shellTextureCS.SetFloat("_HeightOffset", heightOffset);
        shellTextureCS.SetFloat("_UVScale", uvScale);
        
        renderingMaterial.SetBuffer("_DrawTrianglesBuffer", drawTrianglesBuffer);
        
        shellTextureCS.Dispatch(kernelID, threadGroupSize, 1, 1);
    }

    private void Update()
    {
        if (!initialized)
        {
            return;
        }

        transformOffset.position = originalOffset.position - startParenOffset;
        transformOffset.rotation = originalOffset.rotation;
        transformOffset.localScale = originalOffset.localScale;
        
        indirectArgsBuffer.SetData(indirectArgs);
        shellTextureCS.SetMatrix("_Offset", transformOffset.localToWorldMatrix);
        shellTextureCS.Dispatch(kernelID, threadGroupSize, 1, 1);

        Graphics.DrawProceduralIndirect(
            renderingMaterial,
            skinnedMeshRenderer.bounds,
            MeshTopology.Triangles,
            indirectArgsBuffer,
            0,
            null,
            null,
            ShadowCastingMode.On,
            true,
            gameObject.layer
        );
        
        drawTrianglesBuffer.SetCounterValue(0);
    }
    
    private void SetupBuffers()
    {
        drawTrianglesBuffer = new ComputeBuffer(triangleCount * layers, DRAWTRIANGLES_STRIDE, ComputeBufferType.Append);
        indirectArgsBuffer = new ComputeBuffer(1, INDIRECTARGS_STRIDE, ComputeBufferType.IndirectArguments);
    }
    
    private void ReleaseBuffers()
    {
        ReleaseBuffer(drawTrianglesBuffer);
        ReleaseBuffer(indirectArgsBuffer);
        ReleaseBuffer(inputVertexBuffer);
        ReleaseBuffer(inputUVBuffer);
        ReleaseBuffer(inputIndexBuffer);
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















