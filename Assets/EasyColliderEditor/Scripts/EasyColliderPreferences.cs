#if (UNITY_EDITOR)
using UnityEngine;
using UnityEditor;
namespace ECE
{
  [System.Serializable]
  public class EasyColliderPreferences : ScriptableObject
  {

#if (!UNITY_EDITOR_LINUX)
    /// <summary>
    /// Currently set vhacd parameters.
    /// </summary>
    private VHACDParameters _VHACDParameters;
    [SerializeField]
    public VHACDParameters VHACDParameters
    {
      get
      {
        if (_VHACDParameters == null)
        {
          _VHACDParameters = new VHACDParameters();
        }
        return _VHACDParameters;
      }
      set { _VHACDParameters = value; }
    }

    /// <summary>
    /// Float to convert to vhacd parameters resolution using 2^Value for UI Slider when advanced parameters is expanded
    /// </summary>
    [SerializeField] public float VHACDResFloat = 12.97f;

    /// <summary>
    /// Resets vhacd parameters to default.
    /// </summary>
    public void VHACDSetDefaultParameters()
    {
      VHACDParameters = new VHACDParameters();
    }

#endif

    /// <summary>
    /// Auto include child skinned meshes
    /// </summary>
    [SerializeField] public bool AutoIncludeChildSkinnedMeshes;

    /// <summary>
    /// Key to hold before box selection to only add vertices in the box.
    /// </summary>
    [SerializeField] public KeyCode BoxSelectPlusKey;

    /// <summary>
    /// Key to hold before box selection to only remove vertices in the box.
    /// </summary>
    [SerializeField] public KeyCode BoxSelectMinusKey;

    /// <summary>
    /// Capsule collider generation method to use when creating a capsule collider.
    /// </summary>
    [SerializeField] public CAPSULE_COLLIDER_METHOD CapsuleColliderMethod;

    /// <summary>
    /// A helpful common multiplier for all scales when using any scaling method.
    /// </summary>
    [SerializeField] public float CommonScalingMultiplier = 1.0f;

    /// <summary>
    /// Created colliders automatically get disabled to make vertex selection easier.
    /// </summary>
    [SerializeField] public bool CreatedColliderDisabled;

    /// <summary>
    /// key to press to create from the current preview.
    /// </summary>
    [SerializeField] public KeyCode CreateFromPreviewKey;

    /// <summary>
    /// number of sides when creating a cylinder collider.
    /// </summary>
    [SerializeField] public int CylinderNumberOfSides = 16;

    /// <summary>
    /// Should tips be displayed?
    /// </summary>
    [SerializeField] public bool DisplayTips;

    /// <summary>
    /// Display vertices colour
    /// </summary>
    [SerializeField] public Color DisplayVerticesColour;

    /// <summary>
    /// Display vertices scaling size
    /// </summary>
    [SerializeField] public float DisplayVerticesScaling;

    /// <summary>
    /// Should the scene be focused during selection?
    /// </summary>
    [SerializeField] public bool ForceFocusScene;

    /// <summary>
    /// Type of gizmos to use when drawing gizmos for vertices
    /// </summary>
    public GIZMO_TYPE GizmoType;

    /// <summary>
    /// Hover vertices scaling colour
    /// </summary>
    [SerializeField] public Color HoverVertColour;

    /// <summary>
    /// Hover vertices scaling size
    /// </summary>
    [SerializeField] public float HoverVertScaling;

    // [SerializeField] public CREATE_COLLIDER_TYPE MergeCollidersTo;

    /// <summary>
    /// Number of points to generate around a rounded portion of a collider like sphere or capsules
    /// </summary>
    [SerializeField] public int MergeCollidersRoundnessAccuracy = 10;

    /// <summary>
    /// Method to use when generating mesh colliders
    /// </summary>
    [SerializeField] public MESH_COLLIDER_METHOD MeshColliderMethod;

    /// <summary>
    /// Overlapped vertice scaling colour
    /// </summary>
    [SerializeField] public Color OverlapSelectedVertColour;

    /// <summary>
    /// Overlapped selected vertex scale
    /// </summary>
    [SerializeField] public float OverlapSelectedVertScale;

    /// <summary>
    /// Key used to select points (any point on a mesh that isn't a vertex)
    /// </summary>
    [SerializeField] public KeyCode PointSelectKeyCode;

    /// <summary>
    /// Collider type we want to preview
    /// </summary>
    [SerializeField] public CREATE_COLLIDER_TYPE PreviewColliderType;

    /// <summary>
    /// Color of lines to draw previewed colliders with.
    /// </summary>
    [SerializeField] public Color PreviewDrawColor;

    /// <summary>
    /// Raycast delay time, ie only check / select at increments of this time.
    /// </summary>
    [SerializeField] public float RaycastDelayTime;

    /// <summary>
    /// Render point method
    /// </summary>
    [SerializeField] public RENDER_POINT_TYPE RenderPointType;


    [SerializeField] public bool RemoveMergedColliders;

    /// <summary>
    /// If true, puts rotated colliders on the same layer as the selected gameobject.
    /// </summary>
    [SerializeField] public bool RotatedOnSelectedLayer;

    /// <summary>
    /// When true, meshes created from creating convex hulls are saved as assets.
    /// </summary>
    [SerializeField] public bool SaveConvexHullAsAsset;

    /// <summary>
    /// Saves convex hull's mesh at the same path as the selected gameobject if true
    /// </summary>
    [SerializeField] public bool SaveConvexHullMeshAtSelected;

    /// <summary>
    /// if SaveConvexHullMeshAtSelected is false, saves at the path specified.
    /// </summary>
    [SerializeField] public string SaveConvexHullPath;

    /// <summary>
    /// Suffix with which to save convex hulls.
    /// </summary>
    [SerializeField] public string SaveConvexHullSuffix;

    /// <summary>
    /// Selected vertice scaling colour
    /// </summary>
    [SerializeField] public Color SelectedVertColour;

    /// <summary>
    /// Selected vertice scaling size
    /// </summary>
    [SerializeField] public float SelectedVertScaling;

    /// <summary>
    /// Type of collider to use when auto generating skinned mesh colliders along a bone chain.
    /// </summary>
    [SerializeField] public SKINNED_MESH_COLLIDER_TYPE SkinnedMeshColliderType;

    /// <summary>
    /// Sphere method to use when creating a sphere collider.
    /// </summary>
    public SPHERE_COLLIDER_METHOD SphereColliderMethod;

    /// <summary>
    /// Should gizmos and shaders used to draw vertices be scaled by density of vertices on the selected meshes?
    /// </summary>
    [SerializeField] public bool UseDensityScale;

    /// <summary>
    /// Should HandleUtility.GetHandleSize be used when using gizmos to draw to keep gizmo size constant regardless of distance to camera?
    /// </summary>
    [SerializeField] public bool UseFixedGizmoScale;

    /// <summary>
    /// Enables using left click to select vertices, and right click to select points.
    /// </summary>
    [SerializeField] public bool UseMouseClickSelection;

    /// <summary>
    /// Method used when raycasting for closest vertices, add (only snap to unselected verts), remove (only snap to selected verts), both (default)
    /// </summary>
    [SerializeField] public VERTEX_SNAP_METHOD VertexSnapMethod;

    /// <summary>
    /// Key used to select vertices.
    /// </summary>
    [SerializeField] public KeyCode VertSelectKeyCode;

    /// <summary>
    /// Should we update the VHACD calculation and preview as parameters change?
    /// </summary>
    [SerializeField] public bool VHACDPreview;

    /// <summary>
    /// Sets all values to default values.
    /// </summary>  
    public void SetDefaultValues()
    {

#if (!UNITY_EDITOR_LINUX)
      VHACDParameters = new VHACDParameters();
      VHACDPreview = true;
#endif
      AutoIncludeChildSkinnedMeshes = true;

      // shifts do not work.
      BoxSelectMinusKey = KeyCode.S;
      BoxSelectPlusKey = KeyCode.A;

      CapsuleColliderMethod = CAPSULE_COLLIDER_METHOD.MinMax;

      CommonScalingMultiplier = 1.0f;

      CreatedColliderDisabled = true;

      CylinderNumberOfSides = 16;

      DisplayTips = true;

      DisplayVerticesColour = Color.blue;
      DisplayVerticesScaling = 0.05f;

      CreateFromPreviewKey = KeyCode.BackQuote;

      GizmoType = GIZMO_TYPE.SPHERE;

      ForceFocusScene = false;

      HoverVertColour = Color.cyan;
      HoverVertScaling = 0.1F;

      // MergeCollidersTo = CREATE_COLLIDER_TYPE.Box;
      MeshColliderMethod = MESH_COLLIDER_METHOD.QuickHull;

      OverlapSelectedVertColour = Color.red;
      OverlapSelectedVertScale = 0.1f;

      PointSelectKeyCode = KeyCode.B;
      PreviewDrawColor = Color.cyan;

      RaycastDelayTime = 0.1f;

      RemoveMergedColliders = true;

      if (SystemInfo.graphicsShaderLevel < 45)
      {
        RenderPointType = RENDER_POINT_TYPE.GIZMOS;
      }
      else
      {
        RenderPointType = RENDER_POINT_TYPE.SHADER;
      }

      RotatedOnSelectedLayer = true;

      SaveConvexHullMeshAtSelected = true;
      SaveConvexHullPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
      SaveConvexHullPath = SaveConvexHullPath.Remove(SaveConvexHullPath.LastIndexOf("/")) + "/";
      SaveConvexHullAsAsset = true;
      SaveConvexHullSuffix = "_ConvexHull_";

      SelectedVertColour = Color.green;
      SelectedVertScaling = 0.1f;

      SkinnedMeshColliderType = SKINNED_MESH_COLLIDER_TYPE.Capsule;

      SphereColliderMethod = SPHERE_COLLIDER_METHOD.MinMax;

      UseFixedGizmoScale = true;
      UseMouseClickSelection = false;
      UseDensityScale = true;

      VertSelectKeyCode = KeyCode.V;
    }

    private static EasyColliderPreferences _Prefereneces;
    public static EasyColliderPreferences Preferences
    {
      get
      {
        if (_Prefereneces == null)
        {
          _Prefereneces = FindOrCreatePreferences();
        }
        return _Prefereneces;
      }
    }
    private static EasyColliderPreferences FindOrCreatePreferences()
    {
      EasyColliderPreferences preferences;
      string[] ecp = AssetDatabase.FindAssets("EasyColliderPreferences t:ScriptableObject");
      string assetPath = "";
      if (ecp.Length > 0)
      {
        assetPath = AssetDatabase.GUIDToAssetPath(ecp[0]);
        if (ecp.Length > 1)
        {
          Debug.LogWarning("Easy Collider Editor has found multiple preferences files. Using the one located at " + assetPath);
        }
        preferences = AssetDatabase.LoadAssetAtPath(assetPath, typeof(EasyColliderPreferences)) as EasyColliderPreferences;
      }
      else
      {
        ecp = AssetDatabase.FindAssets("EasyColliderWindow t:script");
        if (ecp.Length > 0)
        {
          assetPath = AssetDatabase.GUIDToAssetPath(ecp[0]);
          if (ecp.Length > 1)
          {
            Debug.LogWarning("Easy Collider Editor has found multiple preferences files. Using the one located at " + assetPath);
          }
        }
        // preferences = AssetDatabase.LoadAssetAtPath(assetPath, typeof(EasyColliderPreferences)) as EasyColliderPreferences;
        // Create a new preferences file.

        string prefPath = assetPath.Remove(assetPath.Length - 21) + "EasyColliderPreferences.asset";
        preferences = CreateInstance<EasyColliderPreferences>();
        preferences.SetDefaultValues();
        AssetDatabase.CreateAsset(preferences, prefPath);
        AssetDatabase.SaveAssets();
        Debug.LogWarning("Easy Collider Editor did not find a preferences file, new preferences file created at " + prefPath);
      }
      return preferences;
    }
  }
}
#endif