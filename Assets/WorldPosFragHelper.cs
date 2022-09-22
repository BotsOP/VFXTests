using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldPosFragHelper : MonoBehaviour
{
    [SerializeField] private Material postPrcossMat;
    [SerializeField] private Transform source;
    [SerializeField] private float shockwaveSpeed;
    [SerializeField] private float shockwaveDist;
    [SerializeField] private float shockwavePow;
    private static readonly int PosOrigin = Shader.PropertyToID("_PosOrigin");
    private static readonly int Distance = Shader.PropertyToID("_Distance");

    private void Update()
    {
        postPrcossMat.SetVector(PosOrigin, source.position);
        
        float shockwaveDistance = (float)Math.Pow(Time.time % 1 + 1, shockwavePow) - 1;
        shockwaveDistance *= shockwaveSpeed;
        shockwaveDistance %= shockwaveDist;
        postPrcossMat.SetFloat(Distance, shockwaveDistance);
    }

    private void OnDrawGizmos()
    {
        postPrcossMat.SetVector(PosOrigin, source.position);
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        Graphics.Blit(src, dest, postPrcossMat);
    }
}
