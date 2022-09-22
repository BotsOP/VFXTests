using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SciFiSpawnHelper : MonoBehaviour
{
    [SerializeField] private Material shader;
    [SerializeField] private Transform spawnPoint;
    private static readonly int SpawnPoint = Shader.PropertyToID("_SpawnPoint");
    
    void Update()
    {
        shader.SetVector(SpawnPoint, spawnPoint.position);
    }

    private void OnDrawGizmos()
    {
        shader.SetVector(SpawnPoint, spawnPoint.position);
    }
}
