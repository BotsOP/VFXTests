using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DiscoverMesh))]
public class DiscoverMeshEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DiscoverMesh discoverMesh = (DiscoverMesh)target;
        if(GUILayout.Button("Discover Triangles"))
        {
            discoverMesh.IncrementTriangles();
        }
    }
}
