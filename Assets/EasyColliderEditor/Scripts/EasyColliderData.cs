using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ECE
{
  /// <summary>
  /// Data holder for collider calculations.
  /// </summary>
  public class EasyColliderData
  {
    /// <summary>
    /// Type of collider data
    /// </summary>
    public CREATE_COLLIDER_TYPE ColliderType;

    /// <summary>
    /// Did the collider calculation complete
    /// </summary>
    public bool IsValid = false;

    /// <summary>
    /// TRS matrix of the attach to object, or TRS matrix of what the rotated collider will have
    /// </summary>
    public Matrix4x4 Matrix;

  }

  /// <summary>
  /// Data for creating a sphere collider
  /// </summary>
  public class SphereColliderData : EasyColliderData
  {
    /// <summary>
    /// Radius of the collider
    /// </summary>
    public float Radius;

    /// <summary>
    /// Center of the collider
    /// </summary>
    public Vector3 Center;
  }

  /// <summary>
  /// Data for creating a capsule collider
  /// </summary>
  public class CapsuleColliderData : SphereColliderData
  {
    /// <summary>
    /// Direction of the capsule collider
    /// </summary>
    public int Direction;

    /// <summary>
    /// Height of the capsule collider
    /// </summary>
    public float Height;
  }

  /// <summary>
  /// Data for creating a box collider
  /// </summary>
  public class BoxColliderData : EasyColliderData
  {
    /// <summary>
    /// Center of the box collider
    /// </summary>
    public Vector3 Center;

    /// <summary>
    /// Size of the box collider
    /// </summary>
    public Vector3 Size;
  }

  /// <summary>
  /// Data for creating a mesh collider
  /// </summary>
  public class MeshColliderData : EasyColliderData
  {
    /// <summary>
    /// Mesh of the convex mesh collider
    /// </summary>
    public Mesh ConvexMesh;
  }
}
