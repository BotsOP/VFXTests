
namespace ECE
{
  /// <summary>
  /// Capsule method to use when creating a capsule
  /// </summary>
  public enum CAPSULE_COLLIDER_METHOD
  {
    BestFit,
    MinMax,
    MinMaxPlusRadius,
    MinMaxPlusDiameter,
  }

  /// <summary>
  /// Type of collider to create
  /// </summary>
  public enum CREATE_COLLIDER_TYPE
  {
    BOX,
    ROTATED_BOX,
    SPHERE,
    CAPSULE,
    ROTATED_CAPSULE,
    CONVEX_MESH,
    CYLINDER,
  }

  /// <summary>
  /// Orientation of collider
  /// </summary>
  public enum COLLIDER_ORIENTATION
  {
    NORMAL,
    ROTATED,
  }

  /// <summary>
  /// Enum for spheres ot cubes when drawing gizmos
  /// </summary>
  public enum GIZMO_TYPE
  {
    CUBE,
    SPHERE,
  }

  /// <summary>
  /// enum for Shaders or Gizmos to draw vertices with.
  /// </summary>
  public enum RENDER_POINT_TYPE
  {
    SHADER,
    GIZMOS,
  }

  /// <summary>
  /// Collider type to use when automatically generating colliders along a bone chain for a skinned mesh renderer.
  /// </summary>
  public enum SKINNED_MESH_COLLIDER_TYPE
  {
    Box,
    Capsule,
    Sphere,
    Convex_Mesh,
  }

  /// <summary>
  /// Sphere method to use when creating a sphere
  /// </summary>
  public enum SPHERE_COLLIDER_METHOD
  {
    BestFit,
    Distance,
    MinMax,
  }

  public enum MESH_COLLIDER_METHOD
  {
    QuickHull,
    MessyHull
  }


  public enum VERTEX_SNAP_METHOD
  {
    Add,
    Remove,
    Both,
  }

  /// <summary>
  /// Method used to attach mesh colliders to the attach to object.
  /// </summary>
  public enum VHACD_RESULT_METHOD
  {
    AttachTo,
    ChildObject,
    IndividualChildObjects
  }

  /// <summary>
  /// Current window tab selected in the editor window.
  /// </summary>
  public enum ECE_WINDOW_TAB
  {
    None = -1,
    Creation = 0,
    Editing = 1,
    VHACD = 2,
    AutoSkinned = 3,
  }
}
