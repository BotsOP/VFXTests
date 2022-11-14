using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class BasicLitCustomInspector : ShaderGUI {

    public enum SurfaceType {
        Opaque, TransparentBlend, TransparentCutout
    }
    
    public enum FaceRenderingMode {
        FrontOnly, NoCulling, DoubleSided
    }

    public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader) {
        base.AssignNewShaderToMaterial(material, oldShader, newShader);

        if(newShader.name == "NedMakesGames/MyLit") {
            UpdateSurfaceType(material);
        }
    }

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties) {

        Material material = materialEditor.target as Material;
        var surfaceProp = BaseShaderGUI.FindProperty("_SurfaceType", properties, true);
        var faceProp = BaseShaderGUI.FindProperty("_FaceRenderingMode", properties, true);
        
        EditorGUI.BeginChangeCheck();
        surfaceProp.floatValue = (int)(SurfaceType)EditorGUILayout.EnumPopup("Surface type", (SurfaceType)surfaceProp.floatValue);
        faceProp.floatValue = (int)(FaceRenderingMode)EditorGUILayout.EnumPopup("Face rendering mode", (FaceRenderingMode)faceProp.floatValue);
        if(EditorGUI.EndChangeCheck()) {
            UpdateSurfaceType(material);
        }

        base.OnGUI(materialEditor, properties);
    }

    private void UpdateSurfaceType(Material material) {
        SurfaceType surface = (SurfaceType)material.GetFloat("_SurfaceType");
        switch(surface) {
        case SurfaceType.Opaque:
            material.renderQueue = (int)RenderQueue.Geometry;
            material.SetOverrideTag("RenderType", "Opaque");
            break;
        case SurfaceType.TransparentCutout:
            material.renderQueue = (int)RenderQueue.AlphaTest;
            material.SetOverrideTag("RenderType", "TransparentCutout");
            break;
        case SurfaceType.TransparentBlend:
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetOverrideTag("RenderType", "Transparent");
            break;
        }

        switch(surface) {
        case SurfaceType.Opaque:
        case SurfaceType.TransparentCutout:
            material.SetInt("_SourceBlend", (int)BlendMode.One);
            material.SetInt("_DestBlend", (int)BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            break;
        case SurfaceType.TransparentBlend:
            material.SetInt("_SourceBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DestBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            break;
        }

        if(surface == SurfaceType.TransparentCutout) {
            material.EnableKeyword("_ALPHA_CUTOUT");
        } else {
            material.DisableKeyword("_ALPHA_CUTOUT");
        }

        material.SetShaderPassEnabled("ShadowCaster", surface != SurfaceType.TransparentBlend);
        
        FaceRenderingMode faceRenderingMode = (FaceRenderingMode)material.GetFloat("_FaceRenderingMode");
        if(faceRenderingMode == FaceRenderingMode.FrontOnly) {
            material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Back);
        } else {
            material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        }

        if(faceRenderingMode == FaceRenderingMode.DoubleSided) {
            material.EnableKeyword("_DOUBLE_SIDED_NORMALS");
        } else {
            material.DisableKeyword("_DOUBLE_SIDED_NORMALS");
        }
    }
}