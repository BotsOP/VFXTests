using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SciFiSpawnHelper : MonoBehaviour
{
    [SerializeField] private Material shader;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private AnimationCurve curve;
    [SerializeField][Range(0, 1)] private float lerpValue;
    [SerializeField] private float lerpSpeed = 1;
    private static readonly int SpawnPoint = Shader.PropertyToID("_TargetPos");
    
    void Update()
    {
        shader.SetVector(SpawnPoint, spawnPoint.position);
        float t = ((float)Math.Sin(Time.time * lerpSpeed) + 1) / 2;
        shader.SetFloat("_Lerp", curve.Evaluate(t));
    }

    private void OnDrawGizmos()
    {
        shader.SetVector(SpawnPoint, spawnPoint.position);
        shader.SetFloat("_Lerp", curve.Evaluate(lerpValue));
    }
}
