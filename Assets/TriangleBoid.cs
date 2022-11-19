using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriangleBoid : MonoBehaviour
{
    [SerializeField] private ComputeShader updateTriangles;
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private int amountTriangles = 33446;
    private Mesh mesh;
    private void Awake()
    {
        mesh = new Mesh();
        mesh.name = "triangle boid";

        Vector3[] verts = new Vector3[amountTriangles * 3];
        Vector3 v1 = new Vector3(-0.5f, 0, 0);
        Vector3 v2 = new Vector3(0, 1, 0);
        Vector3 v3 = new Vector3(0.5f, 0, 0);
        int[] indices = new int[amountTriangles * 3];

        for (int i = 0; i < amountTriangles; i++)
        {
            indices[i * 3] = i * 3;
            indices[i * 3 + 1] = i * 3 + 1;
            indices[i * 3 + 2] = i * 3 + 2;
            verts[i * 3] = v1;
            verts[i * 3 + 1] = v2;
            verts[i * 3 + 2] = v3;
        }
        mesh.vertices = verts;
        mesh.triangles = indices;
        mesh.RecalculateNormals();

        meshFilter.sharedMesh = mesh;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
