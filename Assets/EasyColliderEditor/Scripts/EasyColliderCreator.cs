
using System.Collections.Generic;
using UnityEngine;
#if (UNITY_EDITOR)
using UnityEditor;
#endif
using System.Linq;
using System.IO;
namespace ECE
{
  /// <summary>
  /// Class to calculate and add colliders
  /// </summary>
  public class EasyColliderCreator
  {

#if (UNITY_EDITOR)

    /// <summary>
    /// Just an easy way to get an instance of preferences to create colliders with.
    /// </summary>
    /// <value></value>
    private EasyColliderPreferences ECEPreferences
    {
      get { return EasyColliderPreferences.Preferences; }
    }
#endif
    /// <summary>
    /// Data struct from calculating a best fit sphere
    /// </summary>
    private struct BestFitSphere
    {
      /// <summary>
      /// Center of the sphere
      /// </summary>
      public Vector3 Center;

      /// <summary>
      /// Radius of the sphere
      /// </summary>
      public float Radius;

      /// <summary>
      /// Best Fit Sphere
      /// </summary>
      /// <param name="center">Center of the sphere</param>
      /// <param name="radius">Radius of the sphere</param>
      public BestFitSphere(Vector3 center, float radius)
      {
        this.Center = center;
        this.Radius = radius;
      }
    }


    // merge colliders are editor-only.
#if (UNITY_EDITOR)

    #region MergeColliders



    /// <summary>
    /// Merges all colliders in the list to a single resultant collider and returns it.
    /// </summary>
    /// <param name="collidersToMerge">List of colliders to merge</param>
    /// <param name="result">Type of collider we want the colliders merged into</param>
    /// <param name="properties">Properties to set on the new collider</param>
    /// <returns>Single merged collider.</returns>
    public Collider MergeColliders(List<Collider> collidersToMerge, CREATE_COLLIDER_TYPE result, EasyColliderProperties properties)
    {
      if (properties.Orientation == COLLIDER_ORIENTATION.ROTATED)
      {
        properties.AttachTo = GetFirstNonNullTransform(collidersToMerge).gameObject;
      }
      EasyColliderData data = MergeCollidersPreview(collidersToMerge, result, properties.AttachTo.transform);
      if (result == CREATE_COLLIDER_TYPE.BOX || result == CREATE_COLLIDER_TYPE.ROTATED_BOX)
      {
        if (result == CREATE_COLLIDER_TYPE.ROTATED_BOX)
        {
          properties.AttachTo = GetFirstNonNullTransform(collidersToMerge).gameObject;
        }
        return CreateBoxCollider(data as BoxColliderData, properties);
      }
      else if (result == CREATE_COLLIDER_TYPE.CAPSULE || result == CREATE_COLLIDER_TYPE.ROTATED_CAPSULE)
      {
        if (result == CREATE_COLLIDER_TYPE.ROTATED_CAPSULE)
        {
          properties.AttachTo = GetFirstNonNullTransform(collidersToMerge).gameObject;
        }
        return CreateCapsuleCollider(data as CapsuleColliderData, properties);
      }
      else if (result == CREATE_COLLIDER_TYPE.SPHERE)
      {
        return CreateSphereCollider(data as SphereColliderData, properties);
      }
      else if (result == CREATE_COLLIDER_TYPE.CONVEX_MESH || result == CREATE_COLLIDER_TYPE.CYLINDER)
      {
        MeshColliderData d = data as MeshColliderData;
        if (ECEPreferences.SaveConvexHullAsAsset)
        {
          EasyColliderSaving.CreateAndSaveMeshAsset(d.ConvexMesh, properties.AttachTo);
        }
        return CreateConvexMeshCollider(d.ConvexMesh, properties.AttachTo, properties);
      }
      return null;
    }

    /// <summary>
    /// Returns the first transform of a non-null collider
    /// </summary>
    /// <param name="collidersToMerge">list of colliders</param>
    /// <returns>first non-null collider's transform, null if no non-null colliders</returns>
    private Transform GetFirstNonNullTransform(List<Collider> collidersList)
    {
      foreach (Collider c in collidersList)
      {
        if (c != null)
        {
          return c.transform;
        }
      }
      return null;
    }

    /// <summary>
    /// Calculates the preview data for merged colliders.
    /// </summary>
    /// <param name="collidersToMerge"></param>
    /// <param name="result"></param>
    /// <param name="properties"></param>
    /// <returns></returns>
    public EasyColliderData MergeCollidersPreview(List<Collider> collidersToMerge, CREATE_COLLIDER_TYPE result, Transform attachTo)
    {
      List<Vector3> worldVertices = new List<Vector3>();
      foreach (Collider col in collidersToMerge)
      {
        // Get world vertices for mesh collider.
        MeshCollider mc = col as MeshCollider;
        if (mc != null)
        {
          AddWorldVerts(mc, worldVertices);
          continue;
        }
        BoxCollider box = col as BoxCollider;
        if (box != null)
        {
          AddWorldVerts(box, worldVertices);
          continue;
        }
        CapsuleCollider capsule = col as CapsuleCollider;
        if (capsule != null)
        {
          AddWorldVerts(capsule, worldVertices);
          continue;
        }
        SphereCollider sphere = col as SphereCollider;
        if (sphere != null)
        {
          AddWorldVerts(sphere, worldVertices);
          continue;
        }
      }
      EasyColliderData d = new EasyColliderData();
      if (result == CREATE_COLLIDER_TYPE.CONVEX_MESH)
      {
        return EasyColliderQuickHull.CalculateHullData(worldVertices, attachTo);
      }
      else if (result == CREATE_COLLIDER_TYPE.BOX || result == CREATE_COLLIDER_TYPE.ROTATED_BOX)
      {
        if (result == CREATE_COLLIDER_TYPE.ROTATED_BOX)
        {
          attachTo = GetFirstNonNullTransform(collidersToMerge);
        }
        return CalculateBox(worldVertices, attachTo, false);
      }
      else if (result == CREATE_COLLIDER_TYPE.SPHERE)
      {
        // does it make sense to allow different sphere methods -> or just min-max method.
        if (ECEPreferences.SphereColliderMethod == SPHERE_COLLIDER_METHOD.MinMax)
        {
          return CalculateSphereMinMax(worldVertices, attachTo);
        }
        else if (ECEPreferences.SphereColliderMethod == SPHERE_COLLIDER_METHOD.Distance)
        {
          return CalculateSphereDistance(worldVertices, attachTo);
        }
        else if (ECEPreferences.SphereColliderMethod == SPHERE_COLLIDER_METHOD.BestFit)
        {
          return CalculateSphereBestFit(worldVertices, attachTo);
        }
      }
      else if (result == CREATE_COLLIDER_TYPE.CAPSULE || result == CREATE_COLLIDER_TYPE.ROTATED_CAPSULE)
      {
        if (result == CREATE_COLLIDER_TYPE.ROTATED_CAPSULE)
        {
          attachTo = GetFirstNonNullTransform(collidersToMerge);
        }
        // does it make sense to allow capsule methods? -> can use various min max + dia, radius etc.
        if (ECEPreferences.CapsuleColliderMethod == CAPSULE_COLLIDER_METHOD.BestFit)
        {
          return CalculateCapsuleBestFit(worldVertices, attachTo, false);
        }
        else
        {
          return CalculateCapsuleMinMax(worldVertices, attachTo, ECEPreferences.CapsuleColliderMethod, false);
        }
      }
      else if (result == CREATE_COLLIDER_TYPE.CYLINDER)
      {
        return CalculateCylinderCollider(worldVertices, attachTo.transform);
      }
      return d;
    }

    /// <summary>
    /// Adds the vertices of a mesh collider to the world vertices list
    /// </summary>
    /// <param name="meshCollider">mesh collider</param>
    /// <param name="worldVertices">world vertices list</param>
    private void AddWorldVerts(MeshCollider meshCollider, List<Vector3> worldVertices)
    {
      Vector3[] vertices = meshCollider.sharedMesh.vertices;
      Transform t = meshCollider.transform;
      for (int i = 0; i < vertices.Length; i++)
      {
        vertices[i] = t.TransformPoint(vertices[i]);
      }
      worldVertices.AddRange(vertices);
    }

    /// <summary>
    /// Adds the vertices of a box collider to the world vertices list
    /// </summary>
    /// <param name="boxCollider">box collider</param>
    /// <param name="worldVertices">world vertices list</param>
    private void AddWorldVerts(BoxCollider boxCollider, List<Vector3> worldVertices)
    {
      Vector3 halfSize = boxCollider.size / 2;
      Vector3 center = boxCollider.center;
      Vector3[] vertices = new Vector3[8]{
        center + halfSize, //0
        center + new Vector3(halfSize.x, halfSize.y, -halfSize.z), //1
        center + new Vector3(halfSize.x, -halfSize.y, halfSize.z), //2
        center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z), //3
        center + new Vector3(-halfSize.x, halfSize.y, halfSize.z), //4 
        center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z), //5
        center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z), //6
        center - halfSize, //7
      };
      int triangleOffset = worldVertices.Count;
      Transform t = boxCollider.transform;
      for (int i = 0; i < vertices.Length; i++)
      {
        vertices[i] = t.TransformPoint(vertices[i]);
      }
      // add triangles and verts
      worldVertices.AddRange(vertices);
    }

    /// <summary>
    /// Adds the vertices of a sphere collider to the world vertices list
    /// </summary>
    /// <param name="sphereCollider">sphere collider</param>
    /// <param name="worldVertices">world vertices list</param>
    private void AddWorldVerts(SphereCollider sphereCollider, List<Vector3> worldVertices)
    {
      AddWorldVertsSphere(sphereCollider.transform, sphereCollider.center, sphereCollider.radius, worldVertices);
    }

    /// <summary>
    /// Adds the vertices of a capsule collider to the world vertices list
    /// </summary>
    /// <param name="capsuleCollider">capsule collider</param>
    /// <param name="worldVertices">world vertices list</param>
    private void AddWorldVerts(CapsuleCollider capsuleCollider, List<Vector3> worldVertices)
    {
      Vector3 top = Vector3.zero;
      Vector3 bottom = Vector3.zero;
      if (capsuleCollider.direction == 0) //x
      {
        top = capsuleCollider.center + Vector3.right * ((capsuleCollider.height - capsuleCollider.radius * 2) / 2);
        bottom = capsuleCollider.center - Vector3.right * ((capsuleCollider.height - capsuleCollider.radius * 2) / 2);
      }
      else if (capsuleCollider.direction == 1) //y
      {
        top = capsuleCollider.center + Vector3.up * ((capsuleCollider.height - capsuleCollider.radius * 2) / 2);
        bottom = capsuleCollider.center - Vector3.up * ((capsuleCollider.height - capsuleCollider.radius * 2) / 2);
      }
      else if (capsuleCollider.direction == 2) //z
      {
        top = capsuleCollider.center + Vector3.forward * ((capsuleCollider.height - capsuleCollider.radius * 2) / 2);
        bottom = capsuleCollider.center - Vector3.forward * ((capsuleCollider.height - capsuleCollider.radius * 2) / 2);
      }
      // Easiest to just add the full top and bottom spheres as the result is the same for any collider.
      // as they contain the min and max values of collider, and the middle section is all on the same plane as the halfsphere's base for convex meshes.
      // we could write a seperate method to only add the half-spheres, but there's no need.
      // top sphere
      AddWorldVertsSphere(capsuleCollider.transform, top, capsuleCollider.radius, worldVertices);
      // bottom sphere
      AddWorldVertsSphere(capsuleCollider.transform, bottom, capsuleCollider.radius, worldVertices);
    }

    /// <summary>
    /// Adds world space points around a sphere
    /// </summary>
    /// <param name="t">transform of the collider</param>
    /// <param name="center">center of the sphere</param>
    /// <param name="baseRadius">radius of the sphere</param>
    /// <param name="worldVertices"></param>
    private void AddWorldVertsSphere(Transform t, Vector3 center, float radius, List<Vector3> worldVertices)
    {
      int accuracy = ECEPreferences.MergeCollidersRoundnessAccuracy;
      // 360 degrees in radians.
      float sin, cos = sin = 0.0f;
      for (int i = 1; i < accuracy; i++)
      {
        // center shifted to the z-axis
        float h = (i / (float)accuracy) * radius * 2;
        Vector3 centerX = center - (radius - (i / (float)accuracy) * radius * 2) * Vector3.right;
        Vector3 centerY = center - (radius - (i / (float)accuracy) * radius * 2) * Vector3.up;
        Vector3 centerZ = center - (radius - (i / (float)accuracy) * radius * 2) * Vector3.forward;
        float newRadius = Mathf.Sqrt(radius * 2 * h - Mathf.Pow(h, 2));
        for (int j = 0; j <= accuracy; j++)
        {
          float angleStep = ((j / (float)accuracy) * 360f) * Mathf.Deg2Rad;
          sin = Mathf.Sin(angleStep);
          cos = Mathf.Cos(angleStep);
          // constant z.
          float xZ = centerZ.x + newRadius * sin;
          float yZ = centerZ.y + newRadius * cos;
          // constant x.
          float yX = centerX.y + newRadius * sin;
          float zX = centerX.z + newRadius * cos;
          // constant y.
          float zY = centerY.z + (newRadius * sin);
          float xY = centerY.x + (newRadius * cos);
          // 
          worldVertices.Add(t.TransformPoint(new Vector3(centerX.x, yX, zX)));
          worldVertices.Add(t.TransformPoint(new Vector3(xY, centerY.y, zY)));
          worldVertices.Add(t.TransformPoint(new Vector3(xZ, yZ, centerZ.z)));
        }
      }
    }

    #endregion

#endif

    #region ColliderDataCalculation

    /// <summary>
    /// Calculates the best fit sphere for a series of points. Providing a larger list of points increases accuracy.
    /// </summary>
    /// <param name="localVertices">Local space vertices</param>
    /// <returns>The best fit sphere</returns>
    private BestFitSphere CalculateBestFitSphere(List<Vector3> localVertices)
    {
      // # of points.
      int n = localVertices.Count;
      // Calculate average x, y, and z value of vertices.
      float xAvg, yAvg, zAvg = xAvg = yAvg = 0.0f;
      foreach (Vector3 vertex in localVertices)
      {
        xAvg += vertex.x;
        yAvg += vertex.y;
        zAvg += vertex.z;
      }
      xAvg = xAvg * (1.0f / n);
      yAvg = yAvg * (1.0f / n);
      zAvg = zAvg * (1.0f / n);
      // Do some fun math with matrices
      // B Vector.
      Vector3 B = Vector3.zero;
      // Can use a 4x4 as a 3x3 with the 4x4 as 0,0,0,1 in the last row/column.
      Matrix4x4 AM = new Matrix4x4(Vector4.zero, Vector4.zero, Vector4.zero, new Vector4(0, 0, 0, 1));
      float x2, y2, z2 = x2 = y2 = 0.0f;
      foreach (Vector3 vertex in localVertices)
      {
        AM[0, 0] += 2 * (vertex.x * (vertex.x - xAvg)) / n;
        AM[0, 1] += 2 * (vertex.x * (vertex.y - yAvg)) / n;
        AM[0, 2] += 2 * (vertex.x * (vertex.z - zAvg)) / n;
        AM[1, 0] += 2 * (vertex.y * (vertex.x - xAvg)) / n;
        AM[1, 1] += 2 * (vertex.y * (vertex.y - yAvg)) / n;
        AM[1, 2] += 2 * (vertex.y * (vertex.z - zAvg)) / n;
        AM[2, 0] += 2 * (vertex.z * (vertex.x - xAvg)) / n;
        AM[2, 1] += 2 * (vertex.z * (vertex.y - yAvg)) / n;
        AM[2, 2] += 2 * (vertex.z * (vertex.z - zAvg)) / n;
        x2 = vertex.x * vertex.x;
        y2 = vertex.y * vertex.y;
        z2 = vertex.z * vertex.z;
        B.x += ((x2 + y2 + z2) * (vertex.x - xAvg)) / n;
        B.y += ((x2 + y2 + z2) * (vertex.y - yAvg)) / n;
        B.z += ((x2 + y2 + z2) * (vertex.z - zAvg)) / n;
      }
      // Calculate the center of the best-fit sphere.
      Vector3 center = (AM.transpose * AM).inverse * AM.transpose * B;
      // Calculate radius.
      float radius = 0.0f;
      foreach (Vector3 vertex in localVertices)
      {
        radius += Mathf.Pow((vertex.x - center.x), 2) + Mathf.Pow(vertex.y - center.y, 2) + Mathf.Pow(vertex.z - center.z, 2);
      }
      radius = Mathf.Sqrt(radius / localVertices.Count);
      BestFitSphere bfs = new BestFitSphere(center, radius);
      return bfs;
    }

    /// <summary>
    /// Calculates a box's data from the given values
    /// </summary>
    /// <param name="worldVertices">list of vertices in world space</param>
    /// <param name="attachTo">transform the box will be attached to</param>
    /// <param name="isRotated">are we creating a rotated box?</param>
    /// <returns>Data appropriate variables set for a box collider</returns>
    public BoxColliderData CalculateBox(List<Vector3> worldVertices, Transform attachTo, bool isRotated)
    {
      if (isRotated && worldVertices.Count < 3)
      {
        return new BoxColliderData();
      }
      else if (worldVertices.Count < 2)
      {
        return new BoxColliderData();
      }
      Quaternion q = Quaternion.identity;
      Matrix4x4 m;
      List<Vector3> localVertices = new List<Vector3>();
      if (isRotated && worldVertices.Count >= 3)
      {
        // for rotated colliders we also re-calculate the to local even though the transform is changed.
        // this better handles scale-shearing.
        Vector3 forward = worldVertices[1] - worldVertices[0];
        Vector3 up = Vector3.Cross(forward, worldVertices[2] - worldVertices[1]);
        q = Quaternion.LookRotation(forward, up);
        m = Matrix4x4.TRS(attachTo.position, q, Vector3.one);
        for (int i = 0; i < worldVertices.Count; i++)
        {
          localVertices.Add(m.inverse.MultiplyPoint3x4(worldVertices[i]));
        }
      }
      else
      {
        localVertices = ToLocalVerts(attachTo, worldVertices);
        m = attachTo.localToWorldMatrix;
      }
      BoxColliderData data = CalculateBoxLocal(localVertices);
      data.ColliderType = isRotated ? CREATE_COLLIDER_TYPE.ROTATED_BOX : CREATE_COLLIDER_TYPE.BOX;
      data.Matrix = m;
      return data;
    }

    /// <summary>
    /// Calculates box collider data for a list of local space vertices
    /// </summary>
    /// <param name="vertices">list of local space vertices</param>
    /// <returns>box collider data with center and size set</returns>
    public BoxColliderData CalculateBoxLocal(List<Vector3> vertices)
    {
      float xMin, yMin, zMin = xMin = yMin = Mathf.Infinity;
      float xMax, yMax, zMax = xMax = yMax = -Mathf.Infinity;
      foreach (Vector3 vertex in vertices)
      {
        //x min & max.
        xMin = (vertex.x < xMin) ? vertex.x : xMin;
        xMax = (vertex.x > xMax) ? vertex.x : xMax;
        //y min & max
        yMin = (vertex.y < yMin) ? vertex.y : yMin;
        yMax = (vertex.y > yMax) ? vertex.y : yMax;
        //z min & max
        zMin = (vertex.z < zMin) ? vertex.z : zMin;
        zMax = (vertex.z > zMax) ? vertex.z : zMax;
      }
      Vector3 max = new Vector3(xMax, yMax, zMax);
      Vector3 min = new Vector3(xMin, yMin, zMin);
      Vector3 size = max - min;
      Vector3 center = (max + min) / 2;
      // set data from calculated values
      BoxColliderData data = new BoxColliderData();
      data.Center = center;
      data.ColliderType = CREATE_COLLIDER_TYPE.BOX;
      data.IsValid = true;
      data.Size = size;
      return data;
    }

    /// <summary>
    /// Calculates a capsule's data from the given values using the best fit method
    /// </summary>
    /// <param name="worldVertices">list of vertices in world space</param>
    /// <param name="attachTo">transform the capsule will be attached to</param>
    /// <param name="isRotated">are we creating a rotated capsule?</param>
    /// <returns>Data with appropriate variables set for a capsule collider</returns>
    public CapsuleColliderData CalculateCapsuleBestFit(List<Vector3> worldVertices, Transform attachTo, bool isRotated)
    {
      if (worldVertices.Count >= 3)
      {
        Quaternion q = Quaternion.identity;
        Matrix4x4 m;
        List<Vector3> localVertices = new List<Vector3>();
        if (isRotated)
        {
          // for rotated colliders we also re-calculate the to local even though the transform is changed.
          // this better handles scale-shearing.
          Vector3 forward = worldVertices[1] - worldVertices[0];
          Vector3 up = Vector3.Cross(forward, worldVertices[2] - worldVertices[1]);
          q = Quaternion.LookRotation(forward, up);
          m = Matrix4x4.TRS(attachTo.position, q, Vector3.one);
          for (int i = 0; i < worldVertices.Count; i++)
          {
            localVertices.Add(m.inverse.MultiplyPoint3x4(worldVertices[i]));
          }
        }
        else
        {
          localVertices = ToLocalVerts(attachTo, worldVertices);
          m = attachTo.localToWorldMatrix;
        }
        CapsuleColliderData data = CalculateCapsuleBestFitLocal(localVertices);
        data.ColliderType = isRotated ? CREATE_COLLIDER_TYPE.ROTATED_CAPSULE : CREATE_COLLIDER_TYPE.CAPSULE;
        data.Matrix = m;
        return data;
      }
      return new CapsuleColliderData();
    }

    /// <summary>
    /// Calculates a best-fit capsule collider from a list of local space vertices
    /// </summary>
    /// <param name="localVertices">local space vertices</param>
    /// <returns>Capsule collider data with center, direction and height</returns>
    public CapsuleColliderData CalculateCapsuleBestFitLocal(List<Vector3> localVertices)
    {
      if (localVertices.Count < 3)
      {
        Debug.LogWarning("EasyColliderCreator: Too few vertices passed to calculate a best fit capsule collider.");
        return new CapsuleColliderData();
      }
      // height from first 2 verts selected.
      Vector3 v0 = localVertices[0];
      Vector3 v1 = localVertices[1];
      float height = Vector3.Distance(v0, v1);
      float dX = Mathf.Abs(v1.x - v0.x);
      float dY = Mathf.Abs(v1.y - v0.y);
      float dZ = Mathf.Abs(v1.z - v0.z);
      localVertices.RemoveAt(1);
      localVertices.RemoveAt(0);
      BestFitSphere bfs = CalculateBestFitSphere(localVertices);
      Vector3 center = bfs.Center;
      int direction = 0;
      if (dX > dY && dX > dZ)
      {
        direction = 0;
        center.x = (v1.x + v0.x) / 2;
      }
      else if (dY > dX && dY > dZ)
      {
        direction = 1;
        center.y = (v1.y + v0.y) / 2;
      }
      else
      {
        direction = 2;
        center.z = (v1.z + v0.z) / 2;
      }
      CapsuleColliderData data = new CapsuleColliderData();
      data.Center = center;
      data.ColliderType = CREATE_COLLIDER_TYPE.CAPSULE;
      data.Direction = direction;
      data.Height = height;
      data.IsValid = true;
      data.Radius = bfs.Radius;
      return data;
    }

    /// <summary>
    /// Calculates a capsule's data from the given values using the min max method
    /// </summary>
    /// <param name="worldVertices">list of vertices in world space</param>
    /// <param name="attachTo">transform the capsule will be attached to</param>
    /// <param name="method">method we are using to create the capsule (ie MinMaxPlusRadius)</param>
    /// <param name="isRotated">are we creating a rotated capsule?</param>
    /// <returns>Data with appropriate variables set for a capsule collider</returns>
    public CapsuleColliderData CalculateCapsuleMinMax(List<Vector3> worldVertices, Transform attachTo, CAPSULE_COLLIDER_METHOD method, bool isRotated)
    {
      if (isRotated && worldVertices.Count < 3)
      {
        return new CapsuleColliderData();
      }
      else if (worldVertices.Count < 2)
      {
        return new CapsuleColliderData();
      }
      List<Vector3> localVertices = new List<Vector3>();
      Matrix4x4 m;
      Quaternion q;
      if (isRotated && worldVertices.Count >= 3)
      {
        Vector3 forward = worldVertices[1] - worldVertices[0];
        Vector3 up = Vector3.Cross(forward, worldVertices[2] - worldVertices[1]);
        q = Quaternion.LookRotation(forward, up);
        m = Matrix4x4.TRS(attachTo.position, q, Vector3.one);
        for (int i = 0; i < worldVertices.Count; i++)
        {
          localVertices.Add(m.inverse.MultiplyPoint3x4(worldVertices[i]));
        }
      }
      else
      {
        localVertices = ToLocalVerts(attachTo.transform, worldVertices);
        m = attachTo.localToWorldMatrix;
      }
      CapsuleColliderData data = CalculateCapsuleMinMaxLocal(localVertices, method);
      data.ColliderType = isRotated ? CREATE_COLLIDER_TYPE.ROTATED_CAPSULE : CREATE_COLLIDER_TYPE.CAPSULE;
      data.Matrix = m;
      return data;
    }

    /// <summary>
    /// Calculates a capsule collider from a list of local space vertices
    /// </summary>
    /// <param name="localVertices">List of local space vertices</param>
    /// <param name="method">method to use when calculating (used to add radius or diameter to height of capsule)</param>
    /// <returns>Capsule collider data with center, direction, and height</returns>
    public CapsuleColliderData CalculateCapsuleMinMaxLocal(List<Vector3> localVertices, CAPSULE_COLLIDER_METHOD method)
    {
      // calculate min and max points from vertices.
      Vector3 min = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
      Vector3 max = new Vector3(-Mathf.Infinity, -Mathf.Infinity, -Mathf.Infinity);
      foreach (Vector3 vertex in localVertices)
      {
        // Calc minimums
        min.x = vertex.x < min.x ? vertex.x : min.x;
        min.y = vertex.y < min.y ? vertex.y : min.y;
        min.z = vertex.z < min.z ? vertex.z : min.z;
        // Calc maximums
        max.x = vertex.x > max.x ? vertex.x : max.x;
        max.y = vertex.y > max.y ? vertex.y : max.y;
        max.z = vertex.z > max.z ? vertex.z : max.z;
      }
      // Deltas for max-min
      float dX = max.x - min.x;
      float dY = max.y - min.y;
      float dZ = max.z - min.z;
      // center is between min and max values.
      Vector3 center = (max + min) / 2;
      int direction = 0;
      float height = 0;
      // set direction and height.
      if (dX > dY && dX > dZ) // direction is x
      {
        direction = 0;
        // height is the max difference in x.
        height = dX;
      }
      else if (dY > dX && dY > dZ) // direction is y
      {
        direction = 1;
        height = dY;
      }
      else // direction is z.
      {
        direction = 2;
        height = dZ;
      }
      // Calculate radius, makes sure that all vertices are within the radius.
      // Esentially to points on plane defined by direction axis, and find the furthest distance.
      float maxRadius = -Mathf.Infinity;
      Vector3 current = Vector3.zero;
      foreach (Vector3 vertex in localVertices)
      {
        current = vertex;
        if (direction == 0)
        {
          current.x = center.x;
        }
        else if (direction == 1)
        {
          current.y = center.y;
        }
        else if (direction == 2)
        {
          current.z = center.z;
        }
        float d = Vector3.Distance(current, center);
        if (d > maxRadius)
        {
          maxRadius = d;
        }
      }
      // method add radius / diameter
      if (method == CAPSULE_COLLIDER_METHOD.MinMaxPlusRadius)
      {
        height += maxRadius;
      }
      else if (method == CAPSULE_COLLIDER_METHOD.MinMaxPlusDiameter)
      {
        height += maxRadius * 2;
      }
      CapsuleColliderData data = new CapsuleColliderData();
      data.Center = center;
      data.ColliderType = CREATE_COLLIDER_TYPE.CAPSULE;
      data.Direction = direction;
      data.Height = height;
      data.IsValid = true;
      data.Radius = maxRadius;
      return data;
    }


    //TODO: Do a local-only method for cylinders.

    /// <summary>
    /// Calculates the data needed to create a cylinder shaped convex mesh collider using a list of world space vertices
    /// </summary>
    /// <param name="worldVertices">list of selected world space vertices</param>
    /// <param name="attachTo">transform the collider will be attached to</param>
    /// <param name="numberOfSides">number of sides on the cylinder</param>
    /// <returns>Data to create a a cylinder collider with type, convex mesh, validity, and matrix set</returns>
    public MeshColliderData CalculateCylinderCollider(List<Vector3> worldVertices, Transform attachTo, int numberOfSides = 12)
    {
      MeshColliderData data = new MeshColliderData();
#if (UNITY_EDITOR)
      List<Vector3> cylinderLocalPoints = CalculateLocalCylinderPoints(worldVertices, attachTo, ECEPreferences.CylinderNumberOfSides);
#else
      List<Vector3> cylinderLocalPoints = CalculateLocalCylinderPoints(worldVertices, attachTo, numberOfSides);
#endif
      // Mesh mesh = CreateMesh_QuickHull(cylinderLocalPoints, attachTo.gameObject, true);
      EasyColliderQuickHull qh = EasyColliderQuickHull.CalculateHull(cylinderLocalPoints);
      data.ColliderType = CREATE_COLLIDER_TYPE.CONVEX_MESH;
      data.ConvexMesh = qh.Result;
      if (qh.Result != null)
      {
        data.IsValid = true;
      }
      data.Matrix = attachTo.transform.localToWorldMatrix;
      return data;
    }

    /// <summary>
    /// Calculates the mesh collider data for a cylinder shaped convex mesh collider using a list of local space vertices
    /// </summary>
    /// <param name="vertices">list of local space vertices</param>
    /// <param name="numberOfSides">number of sides on the cylinder</param>
    /// <returns>Mesh collider data with convex mesh set</returns>
    public MeshColliderData CalculateCylinderColliderLocal(List<Vector3> vertices, int numberOfSides = 12)
    {
      MeshColliderData data = new MeshColliderData();
#if (UNITY_EDITOR)
      List<Vector3> cylinderLocalPoints = CalculateLocalCylinderPoints(vertices, null, ECEPreferences.CylinderNumberOfSides);
#else
      List<Vector3> cylinderLocalPoints = CalculateLocalCylinderPoints(vertices, null, numberOfSides);
#endif
      EasyColliderQuickHull qh = EasyColliderQuickHull.CalculateHull(cylinderLocalPoints);
      data.ColliderType = CREATE_COLLIDER_TYPE.CONVEX_MESH;
      data.ConvexMesh = qh.Result;
      if (qh.Result != null)
      {
        data.IsValid = true;
      }
      data.Matrix = new Matrix4x4();
      return data;
    }

    /// <summary>
    /// Calculates mesh collider data for a list of world space vertices
    /// </summary>
    /// <param name="vertices">list of world space vertices</param>
    /// <param name="attachTo">transform the mesh collider will be attached to</param>
    /// <returns>Mesh collider data</returns>
    public MeshColliderData CalculateMeshColliderQuickHull(List<Vector3> vertices, Transform attachTo)
    {
      List<Vector3> localVertices = ToLocalVerts(attachTo.transform, vertices);
      MeshColliderData data = CalculateMeshColliderQuickHullLocal(localVertices);
      data.Matrix = attachTo.localToWorldMatrix;
      return data;
    }

    /// <summary>
    /// Calculates mesh collider data for a list of local space vertices
    /// </summary>
    /// <param name="localVertices">list of local space vertices</param>
    /// <returns>Mesh collider data with convex mesh set</returns>
    public MeshColliderData CalculateMeshColliderQuickHullLocal(List<Vector3> localVertices)
    {
      MeshColliderData data = new MeshColliderData();
      EasyColliderQuickHull qh = EasyColliderQuickHull.CalculateHull(localVertices);
      data.ConvexMesh = qh.Result;
      if (qh.Result != null)
      {
        data.IsValid = true;
      }
      return data;
    }


    /// <summary>
    /// Calculates a sphere using the best fit method
    /// </summary>
    /// <param name="worldVertices">list of vertex positions in world space</param>
    /// <param name="attachTo">transform sphere would be attached to</param>
    /// <returns>Data with appropriate variables set for a sphere collider</returns>
    public SphereColliderData CalculateSphereBestFit(List<Vector3> worldVertices, Transform attachTo)
    {
      if (worldVertices.Count < 2)
      {
        return new SphereColliderData();
      }
      List<Vector3> localVertices = ToLocalVerts(attachTo, worldVertices);
      // set data from values
      SphereColliderData data = CalculateSphereBestFitLocal(localVertices);
      data.Matrix = attachTo.localToWorldMatrix;
      return data;
    }

    /// <summary>
    /// Calculates a best fit sphere collider using a list of local space vertices
    /// </summary>
    /// <param name="localVertices">list of local space vertices</param>
    /// <returns>Sphere collider data with center and radius set</returns>
    public SphereColliderData CalculateSphereBestFitLocal(List<Vector3> localVertices)
    {
      BestFitSphere bfs = CalculateBestFitSphere(localVertices);
      // set data from values
      SphereColliderData data = new SphereColliderData();
      data.Center = bfs.Center;
      data.ColliderType = CREATE_COLLIDER_TYPE.SPHERE;
      data.IsValid = true;
      data.Radius = bfs.Radius;
      return data;
    }


    // distance sphere is editor-only for now
    /// <summary>
    /// Calculates a sphere using the distance method
    /// </summary>
    /// <param name="worldVertices">list of vertex positions in world space</param>
    /// <param name="attachTo">transform sphere would be attached to</param>
    /// <returns>Data with appropriate variables set for a sphere collider</returns>
    public SphereColliderData CalculateSphereDistance(List<Vector3> worldVertices, Transform attachTo)
    {
      if (worldVertices.Count < 2)
      {
        return new SphereColliderData();
      }
      List<Vector3> localVertices = ToLocalVerts(attachTo, worldVertices);
      // set data from values
      SphereColliderData data = CalculateSphereDistanceLocal(localVertices);
      data.Matrix = attachTo.localToWorldMatrix;
      return data;
    }

    /// <summary>
    /// Calculates a sphere collider using a list of local space vertices
    /// </summary>
    /// <param name="localVertices">list of local space vertices</param>
    /// <returns>Sphere collider data with center and radius</returns>
    public SphereColliderData CalculateSphereDistanceLocal(List<Vector3> localVertices)
    {
      // if calculations take to long, it switches to a faster less accurate algorithm using the mean.
      bool switchToFasterAlgorithm = false;
#if (UNITY_EDITOR)
      double startTime = EditorApplication.timeSinceStartup;
#else
      double startTime = Time.realtimeSinceStartup;
#endif
      double maxTime = 0.1f;
      Vector3 distanceVert1 = Vector3.zero;
      Vector3 distanceVert2 = Vector3.zero;
      float maxDistance = -Mathf.Infinity;
      float distance = 0;
      for (int i = 0; i < localVertices.Count; i++)
      {
        for (int j = i + 1; j < localVertices.Count; j++)
        {
          distance = Vector3.Distance(localVertices[i], localVertices[j]);
          if (distance > maxDistance)
          {
            maxDistance = distance;
            distanceVert1 = localVertices[i];
            distanceVert2 = localVertices[j];
          }
        }
#if (UNITY_EDITOR)
        if (EditorApplication.timeSinceStartup - startTime > maxTime)
        {
          switchToFasterAlgorithm = true;
          break;
        }
#else
        if (Time.realtimeSinceStartup - startTime > maxTime)
        {
          switchToFasterAlgorithm = true;
          break;
        }
#endif
      }
      if (switchToFasterAlgorithm)
      {
        // use a significantly faster algorithm that is less accurate for a large # of points.
        Vector3 mean = Vector3.zero;
        foreach (Vector3 vertex in localVertices)
        {
          mean += vertex;
        }
        mean = mean / localVertices.Count;
        foreach (Vector3 vertex in localVertices)
        {
          distance = Vector3.Distance(vertex, mean);
          if (distance > maxDistance)
          {
            distanceVert1 = vertex;
            maxDistance = distance;
          }
        }
        maxDistance = -Mathf.Infinity;
        foreach (Vector3 vertex in localVertices)
        {
          distance = Vector3.Distance(vertex, distanceVert1);
          if (distance > maxDistance)
          {
            maxDistance = distance;
            distanceVert2 = vertex;
          }
        }
      }
      // set data from values
      SphereColliderData data = new SphereColliderData();
      data.Center = (distanceVert1 + distanceVert2) / 2;
      data.ColliderType = CREATE_COLLIDER_TYPE.SPHERE;
      data.IsValid = true;
      data.Radius = maxDistance / 2;
      return data;
    }

    /// <summary>
    /// Calculates a sphere using the min max method
    /// </summary>
    /// <param name="worldVertices">list of vertex positions in world space</param>
    /// <param name="attachTo">transform sphere would be attached to</param>
    /// <returns>Data with appropriate variables set for a sphere collider</returns>
    public SphereColliderData CalculateSphereMinMax(List<Vector3> worldVertices, Transform attachTo)
    {
      if (worldVertices.Count < 2)
      {
        return new SphereColliderData();
      }
      // use local space verts.
      List<Vector3> localVertices = ToLocalVerts(attachTo, worldVertices);
      SphereColliderData data = CalculateSphereMinMaxLocal(localVertices);
      data.Matrix = attachTo.localToWorldMatrix;
      return data;
    }

    /// <summary>
    /// Calculates a sphere collider using a list of local space vertices
    /// </summary>
    /// <param name="localVertices">local space vertices</param>
    /// <returns>Sphere collider data with center and radius set</returns>
    public SphereColliderData CalculateSphereMinMaxLocal(List<Vector3> localVertices)
    {
      float xMin, yMin, zMin = xMin = yMin = Mathf.Infinity;
      float xMax, yMax, zMax = xMax = yMax = -Mathf.Infinity;
      for (int i = 0; i < localVertices.Count; i++)
      {
        //x min & max.
        xMin = (localVertices[i].x < xMin) ? localVertices[i].x : xMin;
        xMax = (localVertices[i].x > xMax) ? localVertices[i].x : xMax;
        //y min & max
        yMin = (localVertices[i].y < yMin) ? localVertices[i].y : yMin;
        yMax = (localVertices[i].y > yMax) ? localVertices[i].y : yMax;
        //z min & max
        zMin = (localVertices[i].z < zMin) ? localVertices[i].z : zMin;
        zMax = (localVertices[i].z > zMax) ? localVertices[i].z : zMax;
      }
      // calculate center
      Vector3 center = (new Vector3(xMin, yMin, zMin) + new Vector3(xMax, yMax, zMax)) / 2;
      // calculate radius to contain all points
      float maxDistance = 0.0f;
      float distance = 0.0f;
      foreach (Vector3 vertex in localVertices)
      {
        distance = Vector3.Distance(vertex, center);
        if (distance > maxDistance)
        {
          maxDistance = distance;
        }
      }
      SphereColliderData data = new SphereColliderData();
      data.Center = center;
      data.ColliderType = CREATE_COLLIDER_TYPE.SPHERE;
      data.IsValid = true;
      data.Radius = maxDistance;
      return data;
    }

    #endregion

    // editor only
    #region CreateMeshColliders
#if (UNITY_EDITOR)
    /// <summary>
    /// Create and saves a mesh with a minimum number of triangles that includes all selected vertices. Editor only.
    /// </summary>
    /// <param name="SavePath">Full path to save location including a base object name ie: "C:/UnityProjects/ProjectName/Assets/ConvexHulls/SaveNameBase"</param>
    /// <param name="worldSpaceVertices">Vertices to create the mesh with in world space</param>
    /// <param name="attachTo">Gameobject the mesh will be attached to</param>
    /// <returns>The created mesh</returns>
    public Mesh CreateMesh_Messy(List<Vector3> worldSpaceVertices, GameObject attachTo)
    {
      // use vertices to make a useable mesh that contains all the selected points.
      // The mesh is only used to generate the convex hull
      Mesh mesh = new Mesh();
      // get all vertices in world space and convert to local space.
      List<Vector3> localVertices = worldSpaceVertices.Select(vertex => attachTo.transform.InverseTransformPoint(vertex)).ToList();
      while (localVertices.Count % 3 != 0)
      {
        localVertices.Add(localVertices[localVertices.Count % 3]);
      }
      // attempt to deal with degenerate triangles (so if user changes the mesh collider flags manually, no crashes will occur)
      Vector3 p0, p1, p2 = p1 = p0 = Vector3.zero;
      Vector3 s1, s2 = s1 = Vector3.zero;
      List<Vector3> verts = new List<Vector3>();
      int index = localVertices.Count - 1;
      while (index >= 0) // need to make sure we include the last vertex.
      {
        p0 = localVertices[index];
        p1 = localVertices[(index - 1 >= 0) ? index - 1 : localVertices.Count - 1];
        p2 = localVertices[(index - 2 >= 0) ? index - 2 : localVertices.Count - 2];
        s1 = (p0 - p1).normalized;
        s2 = (p0 - p2).normalized;
        int degenIndex = localVertices.Count; // so we can automatically re-use the last vertices if needed.
        bool degenFixed = false;
        while (s1 == s2 || -s1 == s2 || (s2 == Vector3.zero && s1 != Vector3.zero))
        {
          degenFixed = true;
          degenIndex--;
          if (degenIndex < 0)
          {
            Debug.LogError("Easy Collider Editor: Unable to generate a valid mesh collider from the selected points. This happens when all points are in a straight line.");
            return null;
          }
          p2 = localVertices[degenIndex];
          s2 = (p0 - p2).normalized;
        }
        // if we fixed a degenerate we still need to do the last vertex, so only move back 2 indexs' in that case.
        index -= degenFixed ? 2 : 3;
        verts.Add(p0);
        verts.Add(p1);
        verts.Add(p2);
      }

      int[] triangles = new int[verts.Count];
      for (int i = 0; i < verts.Count; i++)
      {
        triangles[i] = i;
      }
      // mesh.vertices = vertices;
      mesh.vertices = verts.ToArray();
      mesh.triangles = triangles;
      // the mesh has to be saved somewhere so it can actually be used (although this is still just optional)
      try
      {
        EasyColliderSaving.CreateAndSaveMeshAsset(mesh, attachTo);
        return mesh;
      }
      catch
      {
        Debug.LogError("EasyColliderEditor: Error saving mesh at path:" + EasyColliderSaving.GetValidConvexHullPath(attachTo));
        return null;
      }
    }

    /// <summary>
    /// Creates and saves (if set in preferences) a convex mesh collider using QuickHull. Editor only.
    /// </summary>
    /// <param name="vertices">Local or world space vertices</param>
    /// <param name="attachTo">Gameobject the collider will be attached to</param>
    /// <param name="isLocal">are the vertices already in local space?</param>
    /// <returns></returns>
    public Mesh CreateMesh_QuickHull(List<Vector3> vertices, GameObject attachTo, bool isLocal = false)
    {
      List<Vector3> localVerts = isLocal ? vertices : ToLocalVerts(attachTo.transform, vertices);
      EasyColliderQuickHull qh = EasyColliderQuickHull.CalculateHull(localVerts);
      if (ECEPreferences.SaveConvexHullAsAsset)
      {
        EasyColliderSaving.CreateAndSaveMeshAsset(qh.Result, attachTo);
      }
      return qh.Result;
    }
#endif

    #endregion

    // creating colliders uses undos, the data itself can be used during runtime.
    #region CreatePrimitiveColliders

    /// <summary>
    /// Creates a Box collider
    /// </summary>
    /// <param name="data">data to create box from</param>
    /// <param name="properties">properties to set on collider</param>
    /// <returns>Created collider</returns>
    private BoxCollider CreateBoxCollider(BoxColliderData data, EasyColliderProperties properties)
    {
#if (UNITY_EDITOR)
      BoxCollider boxCollider = Undo.AddComponent<BoxCollider>(properties.AttachTo);
#else
      BoxCollider boxCollider = properties.AttachTo.AddComponent<BoxCollider>();
#endif
      boxCollider.size = data.Size;
      boxCollider.center = data.Center;
      SetPropertiesOnCollider(boxCollider, properties);
      return boxCollider;
    }

    /// <summary>
    /// Creates a box collider by calculating the min and max x, y, and z.
    /// </summary>
    /// <param name="worldVertices">List of world space vertices</param>
    /// <param name="properties">Properties of collider</param>
    /// <returns></returns>
    public BoxCollider CreateBoxCollider(List<Vector3> worldVertices, EasyColliderProperties properties)
    {
      if (worldVertices.Count >= 2)
      {
        BoxColliderData data;
        if (properties.Orientation == COLLIDER_ORIENTATION.ROTATED)
        {
          if (worldVertices.Count >= 3)
          {
            GameObject obj = CreateGameObjectOrientation(worldVertices, properties.AttachTo, "Rotated Box Collider");
            // still want to recalculate using the transform matrix, as it better handles uneven scale / shearing across multiple children
            if (obj != null)
            {
              obj.layer = properties.Layer;
              properties.AttachTo = obj;
            }
            data = CalculateBox(worldVertices, properties.AttachTo.transform, true);
          }
          else
          {
            Debug.LogWarning("Easy Collider Editor: Creating a Rotated Box Collider requires at least 3 points to be selected.");
            return null;
          }
        }
        else
        {
          data = CalculateBox(worldVertices, properties.AttachTo.transform, false);
        }
        return CreateBoxCollider(data, properties);
      }
      return null;
    }

    /// <summary>
    /// Creates a capsule collider (editor undoable)
    /// </summary>
    /// <param name="data">data to create capsule from</param>
    /// <param name="properties">properties to set on collider</param>
    /// <returns>created capsule collider</returns>
    private CapsuleCollider CreateCapsuleCollider(CapsuleColliderData data, EasyColliderProperties properties)
    {
#if (UNITY_EDITOR)
      CapsuleCollider capsuleCollider = Undo.AddComponent<CapsuleCollider>(properties.AttachTo);
#else
      CapsuleCollider capsuleCollider = properties.AttachTo.AddComponent<CapsuleCollider>();
#endif
      capsuleCollider.direction = data.Direction;
      capsuleCollider.height = data.Height;
      capsuleCollider.center = data.Center;
      capsuleCollider.radius = data.Radius;
      // set properties
      SetPropertiesOnCollider(capsuleCollider, properties);
      return capsuleCollider;
    }

    /// <summary>
    /// Creates a capsule collider using the height from first 2 vertices, and then getting radius from the best fit sphere algorithm.
    /// </summary>
    /// <param name="worldVertices">List of world vertices</param>
    /// <param name="properties">Properties of collider</param>
    /// <returns></returns>
    public CapsuleCollider CreateCapsuleCollider_BestFit(List<Vector3> worldVertices, EasyColliderProperties properties)
    {
      if (worldVertices.Count >= 3)
      {
        CapsuleColliderData data = new CapsuleColliderData();
        if (properties.Orientation == COLLIDER_ORIENTATION.ROTATED)
        {
          GameObject obj = CreateGameObjectOrientation(worldVertices, properties.AttachTo, "Rotated Capsule Collider");
          if (obj != null)
          {
            properties.AttachTo = obj;
            obj.layer = properties.Layer;
          }
          data = CalculateCapsuleBestFit(worldVertices, properties.AttachTo.transform, true);
        }
        else
        {
          data = CalculateCapsuleBestFit(worldVertices, properties.AttachTo.transform, false);
        }
        return CreateCapsuleCollider(data, properties);
      }
      return null;
    }

    /// <summary>
    /// Creates a capsule collider using the Min-Max method
    /// </summary>
    /// <param name="worldVertices">List of world space vertices</param>
    /// <param name="properties">Properties to set on collider</param>
    /// <param name="method">Min-Max method to use to add radius' to height.</param>
    /// <returns></returns>
    public CapsuleCollider CreateCapsuleCollider_MinMax(List<Vector3> worldVertices, EasyColliderProperties properties, CAPSULE_COLLIDER_METHOD method)
    {
      CapsuleColliderData data;
      if (properties.Orientation == COLLIDER_ORIENTATION.ROTATED && worldVertices.Count >= 3)
      {
        GameObject obj = CreateGameObjectOrientation(worldVertices, properties.AttachTo, "Rotated Capsule Collider");
        if (obj != null)
        {
          properties.AttachTo = obj;
          obj.layer = properties.AttachTo.layer;
        }
        data = CalculateCapsuleMinMax(worldVertices, properties.AttachTo.transform, method, true);
      }
      else
      {
        data = CalculateCapsuleMinMax(worldVertices, properties.AttachTo.transform, method, false);
      }
      return CreateCapsuleCollider(data, properties);
    }

    /// <summary>
    /// Creates a convex mesh collider component from the mesh using all cooking options, so mesh does not have to be "valid"
    /// </summary>
    /// <param name="mesh">Mesh to make a convex hull from</param>
    /// <param name="attachToObject">Gameobject the convex hull will be attached to</param>
    /// <param name="properties">Parameters to set on created collider</param>
    public MeshCollider CreateConvexMeshCollider(Mesh mesh, GameObject attachToObject, EasyColliderProperties properties)
    {
      // Create a mesh collider
#if (UNITY_EDITOR)
      MeshCollider createdCollider = Undo.AddComponent<MeshCollider>(attachToObject);
#else
      MeshCollider createdCollider = attachToObject.AddComponent<MeshCollider>();
#endif
      createdCollider.sharedMesh = mesh;
      // Auto inflate mesh to the minimum amount
#if UNITY_2018_3_OR_NEWER
      createdCollider.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation | MeshColliderCookingOptions.EnableMeshCleaning | MeshColliderCookingOptions.WeldColocatedVertices;
#elif UNITY_2017_3_OR_NEWER
      createdCollider.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation | MeshColliderCookingOptions.InflateConvexMesh | MeshColliderCookingOptions.EnableMeshCleaning | MeshColliderCookingOptions.WeldColocatedVertices;
      createdCollider.skinWidth = 0.000001f;
#else
      createdCollider.inflateMesh = true;
      createdCollider.skinWidth = 0.000001f;
#endif
      // Would be nice if we could do a try/catch on the baking to only inflate if we have to, but that doesn't work.
      createdCollider.convex = true;
      SetPropertiesOnCollider(createdCollider, properties);
      return createdCollider;
    }

    /// <summary>
    /// Creates a sphere collider, editor undo-able
    /// </summary>
    /// <param name="data">data to create the sphere collider from</param>
    /// <param name="properties">properties to set on the collider</param>
    /// <returns>the created sphere collider</returns>
    private SphereCollider CreateSphereCollider(SphereColliderData data, EasyColliderProperties properties)
    {
#if (UNITY_EDITOR)
      SphereCollider sphereCollider = Undo.AddComponent<SphereCollider>(properties.AttachTo);
#else
      SphereCollider sphereCollider = properties.AttachTo.AddComponent<SphereCollider>();
#endif
      sphereCollider.radius = data.Radius;
      sphereCollider.center = data.Center;
      SetPropertiesOnCollider(sphereCollider, properties);
      return sphereCollider;
    }

    /// <summary>
    /// Creates a sphere collider using the best fit sphere algorithm.
    /// </summary>
    /// <param name="worldVertices">List of world space vertices</param>
    /// <param name="properties">Properties of collider</param>
    /// <returns></returns>
    public SphereCollider CreateSphereCollider_BestFit(List<Vector3> worldVertices, EasyColliderProperties properties)
    {
      if (worldVertices.Count >= 2)
      {
        // Convert to local space.
        SphereColliderData data = CalculateSphereBestFit(worldVertices, properties.AttachTo.transform);
        return CreateSphereCollider(data, properties);
      }
      return null;
    }

    /// <summary>
    /// Creates a Sphere Collider by finding the 2 points with a maximum distance between them.
    /// </summary>
    /// <param name="worldVertices">List of world space vertices</param>
    /// <param name="properties">Properties of collider</param>
    /// <returns></returns>
    public SphereCollider CreateSphereCollider_Distance(List<Vector3> worldVertices, EasyColliderProperties properties)
    {
      if (worldVertices.Count >= 2)
      {
        SphereColliderData data = CalculateSphereDistance(worldVertices, properties.AttachTo.transform);
        return CreateSphereCollider(data, properties);
      }
      return null;
    }

    /// <summary>
    /// Creates a sphere collider by calculating the min and max in x, y, and z.
    /// </summary>
    /// <param name="worldVertices">List of world space vertices</param>
    /// <param name="properties">Properties of collider</param>
    /// <returns></returns>
    public SphereCollider CreateSphereCollider_MinMax(List<Vector3> worldVertices, EasyColliderProperties properties)
    {
      if (worldVertices.Count >= 2)
      {
        SphereColliderData data = CalculateSphereMinMax(worldVertices, properties.AttachTo.transform);
        return CreateSphereCollider(data, properties);
      }
      return null;
    }
    #endregion


    #region OtherHelperMethods

    /// <summary>
    /// Calculates points around for a cylinder shaped convex-mesh collider.
    /// </summary>
    /// <param name="vertices">vertices currently selected</param>
    /// <param name="attachTo">Transform the collider will be attached, null if verts are already local</param>
    /// <param name="numberOfSides">number of subdivisons on the circle. Clamped between 3 and 64.</param>
    /// <returns>List of local space points for creating a cylinder with quickhull</returns>
    private List<Vector3> CalculateLocalCylinderPoints(List<Vector3> vertices, Transform attachTo, int numberOfSides)
    {
      // convex mesh colliders can cause errors with > 256 verts or triangles in some version of unity
      // so max number of sides would be roughly 256 = 2(n-2) + 2n, n = 64 which works out pretty well.
      // clamp to min/max number of sides.
      numberOfSides = Mathf.Clamp(numberOfSides, 3, 64);
      // calculate the data for a capsule, min max + diameter means the cylinder section will contain
      // all of the vertices passed in, perfect!
      CapsuleColliderData capsuleData;
      if (attachTo != null)
      {
        capsuleData = CalculateCapsuleMinMax(vertices, attachTo, CAPSULE_COLLIDER_METHOD.MinMaxPlusDiameter, false);
      }
      else
      {
        capsuleData = CalculateCapsuleMinMaxLocal(vertices, CAPSULE_COLLIDER_METHOD.MinMaxPlusDiameter);
      }
      return CalculateLocalCylinderPoints(capsuleData, numberOfSides);
    }

    /// <summary>
    /// Calculates points around for a cylinder shaped convex-mesh collider.
    /// </summary>
    /// <param name="capsuleData">capsule data to create the poitns from</param>
    /// <param name="numberOfSides">number of sides on the cylinder</param>
    /// <returns>list of local space points to create a cylinder collider with</returns>
    private List<Vector3> CalculateLocalCylinderPoints(CapsuleColliderData capsuleData, int numberOfSides)
    {
      List<Vector3> localPoints = new List<Vector3>();
      // angle increase for each calculation.
      float angleIncrement = 360f / numberOfSides;
      // offset points for our top and bottom circle.
      Vector3 top, bottom = top = capsuleData.Center;
      // height includes the half spheres so subtract the radius.
      if (capsuleData.Direction == 0)
      {
        top.x += (capsuleData.Height / 2) - capsuleData.Radius;
        bottom.x -= (capsuleData.Height / 2) - capsuleData.Radius;
      }
      else if (capsuleData.Direction == 1)
      {
        top.y += (capsuleData.Height / 2) - capsuleData.Radius;
        bottom.y -= (capsuleData.Height / 2) - capsuleData.Radius;
      }
      else if (capsuleData.Direction == 2)
      {
        top.z += (capsuleData.Height / 2) - capsuleData.Radius;
        bottom.z -= (capsuleData.Height / 2) - capsuleData.Radius;
      }
      for (float a = 0; a < 360f; a += angleIncrement)
      {
        // doesn't really matter if b or c is used for x or y or z, as long as both are not used
        float b = capsuleData.Radius * Mathf.Sin(a * Mathf.Deg2Rad);
        float c = capsuleData.Radius * Mathf.Cos(a * Mathf.Deg2Rad);
        if (capsuleData.Direction == 0)
        {
          // position + offset by center
          top.y = b + capsuleData.Center.y;
          bottom.y = b + capsuleData.Center.y;
          top.z = c + capsuleData.Center.z;
          bottom.z = c + capsuleData.Center.z;
        }
        else if (capsuleData.Direction == 1)
        {
          top.x = b + capsuleData.Center.x;
          bottom.x = b + capsuleData.Center.x;
          top.z = c + capsuleData.Center.z;
          bottom.z = c + capsuleData.Center.z;
        }
        else if (capsuleData.Direction == 2)
        {
          top.x = b + capsuleData.Center.x;
          bottom.x = b + capsuleData.Center.x;
          top.y = c + capsuleData.Center.y;
          bottom.y = c + capsuleData.Center.y;
        }
        localPoints.Add(top);
        localPoints.Add(bottom);
      }
      return localPoints;
    }


    /// <summary>
    /// Creates a gameobject attach to parent with it's local position at zero, and it's up direction oriented in the direction of the first 2 world vertices.
    /// </summary>
    /// <param name="worldVertices">List of world space vertices</param>
    /// <param name="parent">Parent to attach gameobject to</param>
    /// <param name="name">Name of gameobject to create</param>
    /// <returns></returns>
    private GameObject CreateGameObjectOrientation(List<Vector3> worldVertices, GameObject parent, string name)
    {
      GameObject obj = new GameObject(name);
      if (worldVertices.Count >= 3)
      {
        // calculate forward and up.
        Vector3 forward = worldVertices[1] - worldVertices[0];
        Vector3 up = Vector3.Cross(forward, worldVertices[2] - worldVertices[1]);
        obj.transform.rotation = Quaternion.LookRotation(forward, up);
        obj.transform.SetParent(parent.transform);
        obj.transform.localPosition = Vector3.zero;
#if (UNITY_EDITOR) // undo for unity editor.
        Undo.RegisterCreatedObjectUndo(obj, "Create Rotated GameObject");
#endif
        return obj;
      }
      return null;
    }


    /// <summary>
    /// Just a helper method to draw a point in world space
    /// </summary>
    /// <param name="worldLoc"></param>
    /// <param name="color"></param>
    private void DebugDrawPoint(Vector3 worldLoc, Color color)
    {
      Debug.DrawLine(worldLoc - Vector3.up * 0.01f, worldLoc + Vector3.up * 0.01f, color, 10f, false);
      Debug.DrawLine(worldLoc - Vector3.left * 0.01f, worldLoc + Vector3.left * 0.01f, color, 10f, false);
      Debug.DrawLine(worldLoc - Vector3.forward * 0.01f, worldLoc + Vector3.forward * 0.01f, color, 10f, false);
    }

    /// <summary>
    /// Sets the collider properties isTrigger and physicMaterial.
    /// </summary>
    /// <param name="collider">Collider to set properties on</param>
    /// <param name="properties">Properties object with the properties you want to set</param>
    private void SetPropertiesOnCollider(Collider collider, EasyColliderProperties properties)
    {
      if (collider != null)
      {
        collider.isTrigger = properties.IsTrigger;
        collider.sharedMaterial = properties.PhysicMaterial;
      }
    }

    /// <summary>
    /// Converts the list of world vertices to local positions
    /// </summary>
    /// <param name="transform">Transform to use for local space</param>
    /// <param name="worldVertices">World space position of vertices</param>
    /// <returns>Localspace position w.r.t transform of worldVertices</returns>
    private List<Vector3> ToLocalVerts(Transform transform, List<Vector3> worldVertices)
    {
      List<Vector3> localVerts = new List<Vector3>(worldVertices.Count);
      foreach (Vector3 v in worldVertices)
      {
        localVerts.Add(transform.InverseTransformPoint(v));
      }
      return localVerts;
    }

    #endregion
  }
}
