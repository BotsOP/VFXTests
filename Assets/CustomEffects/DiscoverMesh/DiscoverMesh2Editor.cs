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
            for (int i = 0; i < discoverMesh.whichTriangleToColor.Length; i++)
            {
                discoverMesh.FirstTriangleToCheck(discoverMesh.whichTriangleToColor[i]);
            }
            
        }
        if(GUILayout.Button("Find adjacent triange"))
        {
            discoverMesh.IncrementTriangles();
            discoverMesh.DecayMesh();
        }
        if(GUILayout.Button("Color triangles"))
        {
            discoverMesh.ColorTriangles();
        }
    }
}
