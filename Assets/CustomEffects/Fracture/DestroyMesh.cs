using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyMesh : MonoBehaviour
{
    public float fractureAmount = 0.5f;

    void Start()
    {
        ComputeBuffer dynamicBuffer = new ComputeBuffer(10, sizeof(float), ComputeBufferType.Structured, ComputeBufferMode.Dynamic);
    }
}
