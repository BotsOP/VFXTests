using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetCameraTexture : MonoBehaviour
{
    public Material mat;
    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        mat.SetTexture("_CamTex", src);
    }
}
