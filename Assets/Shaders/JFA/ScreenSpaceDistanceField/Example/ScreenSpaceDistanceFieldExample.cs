using System;
using UnityEngine;

[ExecuteInEditMode]
public class ScreenSpaceDistanceFieldExample : MonoBehaviour {

  public Material compositeMat;
  public JumpFlood jumpFlood;
  public RenderTexture texA;
  public RenderTexture texB;
  public RenderTexture texC;

  // private void OnRenderImage(RenderTexture source, RenderTexture destination) {
  //   var distanceTex = jumpFlood.BuildDistanceField(source);
  //
  //   Graphics.Blit(source, destination);
  //
  //   RenderTexture.ReleaseTemporary(distanceTex);
  // }

  private void Update()
  {
    var jfa = jumpFlood.BuildDistanceField(texA);
    Graphics.Blit(jfa, texB);
  }

}
