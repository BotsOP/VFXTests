#if (UNITY_EDITOR)
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace ECE
{
  /// <summary>
  /// Used to draw gizmos for selected / hovered vertices
  /// Gizmos draw significantly faster than handles.
  /// </summary>

  [System.Serializable]
  public class EasyColliderGizmos : MonoBehaviour
  {
    public float DensityScale = 0.0f;
    public bool UseDensityScale = false;

    public float CommonScale = 1.0f;
    /// <summary>
    /// Should all valid vertices be displayed
    /// </summary>
    public bool DisplayAllVertices = false;

    /// <summary>
    /// Color to display all valid vertices with
    /// </summary>
    public Color DisplayVertexColor = Color.blue;

    /// <summary>
    /// List of all valid vertex positions in world space
    /// </summary>
    public HashSet<Vector3> DisplayVertexPositions = new HashSet<Vector3>();

    /// <summary>
    /// Scale to display all valid vertex positions
    /// </summary>
    public float DisplayVertexScale = 0.05f;

    /// <summary>
    /// Should gizmos be drawn
    /// </summary>
    public bool DrawGizmos = true;

    /// <summary>
    /// Type of gizmo use when drawing gizmos
    /// </summary>
    public GIZMO_TYPE GizmoType = GIZMO_TYPE.SPHERE;

    /// <summary>
    /// Color of hovered vertices
    /// </summary>
    public Color HoveredVertexColor = Color.cyan;

    /// <summary>
    /// Set of hovered vertices in world space
    /// </summary>
    public HashSet<Vector3> HoveredVertexPositions = new HashSet<Vector3>();

    /// <summary>
    /// Scale of hovered vertices
    /// </summary>
    public float HoveredVertexScale = 0.1f;

    /// <summary>
    /// Color of overlapped vertices
    /// </summary>
    public Color OverlapVertexColor = Color.red;

    /// <summary>
    /// Scale of overlapped vertices
    /// </summary>
    public float OverlapVertexScale = 0.125f;

    /// <summary>
    /// Color of selected vertices
    /// </summary>
    public Color SelectedVertexColor = Color.green;

    /// <summary>
    /// Set of selected vertices in world space
    /// </summary>
    public HashSet<Vector3> SelectedVertexPositions = new HashSet<Vector3>();

    /// <summary>
    /// Scale of selected vertices
    /// </summary>
    public float SelectedVertexScale = 0.15f;

    /// <summary>
    /// Should HandleUtility.GetHandleSize be used for each vertice to maintain a fixed scale and ignore distance?
    /// </summary>
    public bool UseFixedGizmoScale = true;

    void OnDrawGizmos()
    {
      if (DrawGizmos)
      {
        // Keep track of gizmos color to reset at end
        Color original = Gizmos.color;
        // Selected vertices.
        // size is modified for each vertex if using fixed scaling from handle utility.
        Vector3 size = Vector3.zero;
        // original size is kept track of to make calculations easier.
        Vector3 originalSize = Vector3.zero;
        // scale for spheres.
        float scale = 0.0f;
        float originalScale = 0.0f;
        float handleSize = 0.0f;

        // Display all vertices.
        if (DisplayAllVertices)
        {
          Gizmos.color = DisplayVertexColor;
          originalScale = UseDensityScale ? DensityScale * CommonScale : DisplayVertexScale * CommonScale;
          originalSize = Vector3.one * originalScale;

          foreach (Vector3 vert in DisplayVertexPositions)
          {
            scale = originalScale;
            size = originalSize;
            if (UseFixedGizmoScale)
            {
              handleSize = HandleUtility.GetHandleSize(vert);
              scale *= handleSize;
              size *= handleSize;
            }
            DrawAGizmo(vert, size, scale, GizmoType);
          }
        }

        // Selected vertices
        Gizmos.color = SelectedVertexColor;
        originalScale = UseDensityScale ? DensityScale * CommonScale : SelectedVertexScale * CommonScale;
        originalSize = Vector3.one * originalScale;
        foreach (Vector3 vert in SelectedVertexPositions)
        {
          scale = originalScale;
          size = originalSize;
          if (UseFixedGizmoScale)
          {
            handleSize = HandleUtility.GetHandleSize(vert);
            scale *= handleSize;
            size *= handleSize;
          }
          DrawAGizmo(vert, size, scale, GizmoType);
        }

        // Hover vertices.
        Gizmos.color = HoveredVertexColor;
        originalScale = UseDensityScale ? DensityScale * CommonScale : HoveredVertexScale * CommonScale;
        originalSize = Vector3.one * originalScale;
        float originalScaleOverlap = UseDensityScale ? DensityScale * CommonScale : OverlapVertexScale * CommonScale;
        Vector3 originalSizeOverlap = Vector3.one * originalScaleOverlap;
        foreach (Vector3 vert in HoveredVertexPositions)
        {
          if (SelectedVertexPositions.Contains(vert))
          {
            scale = originalScaleOverlap;
            size = originalSizeOverlap;
            if (UseFixedGizmoScale)
            {
              handleSize = HandleUtility.GetHandleSize(vert);
              scale *= handleSize;
              size *= handleSize;
            }
            Gizmos.color = OverlapVertexColor;
            DrawAGizmo(vert, size, scale, GizmoType);
          }
          else
          {
            scale = originalScale;
            size = originalSize;
            if (UseFixedGizmoScale)
            {
              handleSize = HandleUtility.GetHandleSize(vert);
              scale *= handleSize;
              size *= handleSize;
            }
            Gizmos.color = HoveredVertexColor;
            DrawAGizmo(vert, size, scale, GizmoType);
          }
        }
        Gizmos.color = original;
      }
    }

    /// <summary>
    /// Draws a gizmo of type at position at size or scale.
    /// </summary>
    /// <param name="position">World position to draw at</param>
    /// <param name="size">Size of cube to draw</param>
    /// <param name="scale">Radius of sphere to draw</param>
    /// <param name="gizmoType">Sphere or Cubes?</param>
    private void DrawAGizmo(Vector3 position, Vector3 size, float scale, GIZMO_TYPE gizmoType)
    {
      switch (gizmoType)
      {
        case GIZMO_TYPE.SPHERE:
          Gizmos.DrawSphere(position, scale / 2);
          break;
        case GIZMO_TYPE.CUBE:
          Gizmos.DrawCube(position, size);
          break;
      }
    }

    /// <summary>
    /// Sets the set of selected vertices from a list of selected world vertices
    /// </summary>
    /// <param name="worldVertices">List of world vertex positions that are selected</param>
    public void SetSelectedVertices(List<Vector3> worldVertices)
    {
      SelectedVertexPositions.Clear();
      SelectedVertexPositions.UnionWith(worldVertices);
    }
  }
}
#endif