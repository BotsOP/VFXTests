using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DiscoverMesh2))]
public class DiscoverMesh2Editor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DiscoverMesh2 discoverMesh = (DiscoverMesh2)target;
        if(GUILayout.Button("First Triangle"))
        {
            discoverMesh.FirstTriangleToCheck(0);
        }
        if(GUILayout.Button("Find adjacent triange"))
        {
            discoverMesh.IncrementTriangles();
        }
        if(GUILayout.Button("Color triangles"))
        {
            discoverMesh.ColorTriangles();
        }
    }
}
