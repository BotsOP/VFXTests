using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldPosFragHelper : MonoBehaviour
{
    [SerializeField] private Material shader;

    private void Update()
    {
        Shader.SetGlobalMatrix("_InverseView", Camera.main.cameraToWorldMatrix);
    }

    private void OnDrawGizmos()
    {
        Shader.SetGlobalMatrix("_InverseView", Camera.main.cameraToWorldMatrix);
    }
}
