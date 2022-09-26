#if (UNITY_EDITOR)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
#if UNITY_2019_1_OR_NEWER
using Unity.Collections;
#endif
using System;
namespace ECE
{
  [System.Serializable]
  public class EasyColliderAutoSkinned : ScriptableObject
  {
    /// <summary>
    /// List of transforms that children of are excluded from having colliders generated on.
    /// </summary>
    public List<Transform> ExcludeChildrenOf = new List<Transform>();

    /// <summary>
    /// List of transforms that are specifically excluded from having colliders generated on them.
    /// </summary>
    public List<Transform> ExcludeTransforms = new List<Transform>();

    /// <summary>
    /// Class to hold data for a skinned meshes' bones
    /// Used because the bones transform array can be empty in cases where optimize gameobjects is used on import
    /// The bindbose index of a bone, is the same as a bone's boneindex, so boneweight's can still be used.
    /// </summary>
    private class SkinnedMeshBone
    {
      public SkinnedMeshBone(Matrix4x4 bp, int index, Transform t)
      {
        BoneIndex = index;
        BindPose = bp;
        Transform = t;
      }

      /// <summary>
      /// Bind pose of the bone
      /// </summary>
      public Matrix4x4 BindPose;

      /// <summary>
      /// Bind pose index is the bone index as well.
      /// </summary>
      public int BoneIndex;

      /// <summary>
      /// Transform of the bone
      /// </summary>
      public Transform Transform;

      /// <summary>
      /// List of vertices in world space for this bo
      /// </summary>
      /// <typeparam name="Vector3"></typeparam>
      /// <returns></returns>
      public List<Vector3> WorldSpaceVertices = new List<Vector3>();

      public List<int> ChildIndexs = new List<int>();
    }

    /// <summary>
    /// (Undoable) Creates a gameobject as a child of parent, with it's forward axis pointed towards the child, and its up axis at the cross of the new forward and the parent's up direction.
    /// </summary>
    /// <param name="parent">parent transform</param>
    /// <param name="child">child transform</param>
    /// <returns>Gameobject at parent's location with it's forward axis pointing towards child.</returns>
    public GameObject CreateRealignedObject(Transform parent, Transform child)
    {
      // realign with a rotated gameboject.
      GameObject obj = new GameObject(parent.transform.name + "_aligned");
      Vector3 childDir = child.position - parent.position;
      // use either the bone's right or the the bone's forward, whichever isn't the same as the child's direction in the calculation.
      Vector3 right = parent.transform.right;
      if (childDir == right) { right = parent.transform.forward; }
      Vector3 up = Vector3.Cross(childDir, right);
      obj.transform.rotation = Quaternion.LookRotation(childDir, up);
      obj.transform.SetParent(parent.transform);
      obj.layer = obj.transform.parent.gameObject.layer;
      obj.transform.localPosition = Vector3.zero;
      Undo.RegisterCreatedObjectUndo(obj, "Create realign bone object");
      return obj;
    }

    /// <summary>
    /// Creates and attaches a collider of given type
    /// </summary>
    /// <param name="colliderType">type of collider</param>
    /// <param name="properties">properties of collider</param>
    /// <param name="s">data to create collider with</param>
    /// <param name="savePath">save path for mesh colliders</param>
    private Collider GenerateCollider(SKINNED_MESH_COLLIDER_TYPE colliderType, EasyColliderProperties properties, SkinnedMeshBone s, string savePath)
    {
      EasyColliderCreator ecc = new EasyColliderCreator();
      switch (colliderType)
      {
        case SKINNED_MESH_COLLIDER_TYPE.Box:
          return ecc.CreateBoxCollider(s.WorldSpaceVertices, properties);
        case SKINNED_MESH_COLLIDER_TYPE.Capsule:
          return ecc.CreateCapsuleCollider_MinMax(s.WorldSpaceVertices, properties, CAPSULE_COLLIDER_METHOD.MinMax);
        case SKINNED_MESH_COLLIDER_TYPE.Sphere:
          return ecc.CreateSphereCollider_MinMax(s.WorldSpaceVertices, properties);
        case SKINNED_MESH_COLLIDER_TYPE.Convex_Mesh:
          // TODO: switch between quickhull and messyhull
          // messy hull:
          // Mesh m = ecc.CreateAndSaveMesh(savePath, s.WorldSpaceVertices, s.Transform.gameObject);
          Mesh m = ecc.CreateMesh_QuickHull(s.WorldSpaceVertices, s.Transform.gameObject);
          if (m != null) { return ecc.CreateConvexMeshCollider(m, s.Transform.gameObject, properties); }
          break;
      }
      return null;
    }

    /// <summary>
    /// Generates colliders along a bone chain of a skinned mesh renderer.
    /// </summary>
    /// <param name="skinnedMesh">Skinned mesh renderer</param>
    /// <param name="colliderType">Type of colliders to use</param>
    /// <param name="properties">Parameters to set on created colliders</param>
    /// <param name="minBoneWeight">Minimum bone weight to include a vertex in a bones collider</param>
    /// <param name="realignBones">Should realigning colliders be performed?</param>
    /// <param name="minRealignAngle">When the minimum angle of all of a bone's axis (up, down, left, right, forward, back) and the vector to the next bone in the chain is >= minRealignAngle, realigning is performed if enabled.</param>
    /// <param name="savePath">Full path to save mesh's when colliderType is a Convex Mesh. Ie: C:/UnityProjects/ProjectName/Assets/ConvexHulls/BaseHullName</param>
    public List<Collider> GenerateSkinnedMeshColliders(SkinnedMeshRenderer skinnedMesh, SKINNED_MESH_COLLIDER_TYPE colliderType, EasyColliderProperties properties, float minBoneWeight, bool realignBones = false, float minRealignAngle = 20f, string savePath = "Assets/")
    {
      List<Collider> generatedColliders = new List<Collider>();
      // bake the mesh to get world space vertices as this works in all cases (bone transforms are valid, bind poses are valid, or the allow deoptimization is used)
      // also works for incorrectly rotated roots / mesh renderers etc.
      Mesh m = new Mesh();
      Vector3 prevScale = skinnedMesh.transform.localScale;
      skinnedMesh.transform.localScale = Vector3.one;
      skinnedMesh.BakeMesh(m);
      Vector3[] vertices = m.vertices;
      for (int i = 0; i < vertices.Length; i++)
      {
        vertices[i] = skinnedMesh.transform.TransformPoint(vertices[i]);
      }
      skinnedMesh.transform.localScale = prevScale;
      // get skinned mesh bones
      SkinnedMeshBone[] smbs = GetSkinnedMeshBones(skinnedMesh);
      if (smbs == null)
      {
        return null;
      }
      // set the world vertex for each bone.
#if UNITY_2019_1_OR_NEWER
      SetWorldVertices(skinnedMesh, smbs, vertices, minBoneWeight);
#else
      SetWorldVertices(smbs, skinnedMesh.sharedMesh.boneWeights, vertices, minBoneWeight);
#endif
      Transform[] bones = skinnedMesh.bones;
      foreach (SkinnedMeshBone s in smbs)
      {
        // ignore excluded, null, and bones with no vertices.
        if (s == null || s.WorldSpaceVertices.Count == 0) continue;
        if (ExcludeTransforms.Contains(s.Transform)) continue;
        bool isExcludeChild = false;
        // exclude children but not the transform itself of exclude children of
        foreach (Transform t in ExcludeChildrenOf)
        {
          if (s.Transform.IsChildOf(t) && s.Transform != t)
          {
            isExcludeChild = true;
          }
        }
        if (isExcludeChild) continue;
        // the attach to is the skinned bones transform's gameobject.
        properties.AttachTo = s.Transform.gameObject;
        // when the mesh isn't optimized, the bones transform is filled
        if (bones.Length > 0)
        {
          // if realigning is enabled, and its not convex meshes
          if (realignBones && colliderType != SKINNED_MESH_COLLIDER_TYPE.Convex_Mesh)
          {
            // try realigning by finding the single child bone, comparing the angles, and creating a properly aligned transform
            Transform child = GetChildBone(s.Transform, bones);
            if (child != null)
            {
              float minAngle = GetMinimumChildAngle(s.Transform, child);
              if (minAngle >= minRealignAngle)
              {
                properties.AttachTo = CreateRealignedObject(s.Transform, child);
              }
            }
          }
        }
        // finally, create the collider.
        Collider createdCollider = GenerateCollider(colliderType, properties, s, savePath);
        if (createdCollider != null)
        {
          generatedColliders.Add(createdCollider);
        }
      }
      return generatedColliders;
    }

    /// <summary>
    /// Gets the child bone of a transform if it has a single valid child bone.
    /// </summary>
    /// <param name="bone">Bone to get child of</param>
    /// <param name="bones">Array of bones</param>
    /// <returns>Transform of child bone if found, otherwise null</returns>
    private Transform GetChildBone(Transform bone, Transform[] bones)
    {
      int boneChildCount = bone.childCount;
      Transform childBone = null;
      Transform currentChildTransform = null;
      int totalValidChildBones = 0;
      for (int j = 0; j < boneChildCount; j++)
      {
        currentChildTransform = bone.GetChild(j);
        int index = Array.IndexOf(bones, currentChildTransform);
        if (index >= 0)
        {
          totalValidChildBones += 1;
          childBone = currentChildTransform;
        }
      }
      if (totalValidChildBones == 1)
      {
        return childBone;
      }
      return null;
    }

    /// <summary>
    /// Gets the minimum angle between all of transform's axis and the direction from transform to child.
    /// </summary>
    /// <param name="transform">transform to use axis from</param>
    /// <param name="child">child to get minimum angle to</param>
    /// <returns>Minimum angle from all of transform's axis and the direction from transform to child.</returns>
    private float GetMinimumChildAngle(Transform transform, Transform child)
    {
      Vector3 childDir = child.position - transform.position;
      float minAngle = Mathf.Infinity;
      float angle = Vector3.Angle(transform.right, childDir);
      minAngle = minAngle > angle ? angle : minAngle;
      angle = Vector3.Angle(-transform.right, childDir);
      minAngle = minAngle > angle ? angle : minAngle;
      angle = Vector3.Angle(transform.forward, childDir);
      minAngle = minAngle > angle ? angle : minAngle;
      angle = Vector3.Angle(-transform.forward, childDir);
      minAngle = minAngle > angle ? angle : minAngle;
      angle = Vector3.Angle(transform.up, childDir);
      minAngle = minAngle > angle ? angle : minAngle;
      angle = Vector3.Angle(-transform.up, childDir);
      minAngle = minAngle > angle ? angle : minAngle;
      return minAngle;
    }

    /// <summary>
    /// Gets all the skinned mesh bones for the current skinned mesh renderer.
    /// </summary>
    /// <param name="skinnedMesh"></param>
    /// <returns>Array of skinned mesh bones.</returns>
    private SkinnedMeshBone[] GetSkinnedMeshBones(SkinnedMeshRenderer skinnedMesh)
    {
      int validBonesFound = 0;
      SkinnedMeshBone[] smbs = null;
      Transform root = skinnedMesh.transform;
      Animator a = root.GetComponent<Animator>();
      //without bone transforms, we need to find the animator and use that transform.
      while (root.parent != null && a == null)
      {
        root = root.parent;
        a = root.GetComponent<Animator>();
      }
      // first, if there are transform in the bones array, we use that to get everything
      if (skinnedMesh.bones.Length > 0)
      {
        smbs = new SkinnedMeshBone[skinnedMesh.bones.Length];
        // try to match based on bones
        Transform[] bones = skinnedMesh.bones;
        for (int i = 0; i < bones.Length; i++)
        {
          smbs[i] = new SkinnedMeshBone(bones[i].localToWorldMatrix, i, bones[i]);
          validBonesFound++;
        }
      }
      if (validBonesFound == 0)
      {
        Debug.LogWarning("Easy Collider Editor: Unable to find any valid bones. This occurs when optimized gameobject is enabled on the mesh's rig import settings. The recommendation is to temporarily disable optimization, generate colliders, then renable optimization. Colliders on exposed transforms when optimized should be correctly transferred.");
      }
      return smbs;
    }

    /// <summary>
    /// Checks if the transform is a valid transform to stop skinned mesh auto generation at.
    /// </summary>
    /// <param name="stopAt">Transform to check validity</param>
    /// <param name="selectedTransform">Current selected transform / root transform</param>
    /// <returns>true if valid, false otherwise</returns>
    public bool IsValidToExclude(Transform stopAt, Transform selectedTransform)
    {
      bool isValid = true;
      if (stopAt != null)
      {
        if (EditorUtility.IsPersistent(stopAt.gameObject) // only allow scene objects
          || selectedTransform == null // not-null selected transform
          || !stopAt.IsChildOf(selectedTransform) // has to be a child of selected transform.
        )
        {
          isValid = false;
        }
      }
      return isValid;
    }


    // native array and boneweight1 functionality are 2019.1+
#if UNITY_2019_1_OR_NEWER
    /// <summary>
    /// Sets the skinned mesh bone's world vertices list.
    /// </summary>
    /// <param name="skinnedMesh">Skinned mesh we are trying to set world vertices for</param>
    /// <param name="skinnedMeshBones">Array of all skinned mesh bones</param>
    /// <param name="worldVertices">Array of all vertices in world space</param>
    /// <param name="minBoneWeight">Minimum bone weight to include a vertex in a bone's vertex list.</param>
    private void SetWorldVertices(SkinnedMeshRenderer skinnedMesh, SkinnedMeshBone[] skinnedMeshBones, Vector3[] worldVertices, float minBoneWeight)
    {
      NativeArray<BoneWeight1> boneWeights = skinnedMesh.sharedMesh.GetAllBoneWeights();
      NativeArray<byte> bonesPerVertex = skinnedMesh.sharedMesh.GetBonesPerVertex();
      int boneIndex = 0;
      for (int i = 0; i < worldVertices.Length; i++)
      {
        int numBonesForVertex = bonesPerVertex[i];
        for (int j = 0; j < numBonesForVertex; j++)
        {
          BoneWeight1 boneWeight = boneWeights[boneIndex];
          if (boneWeight.weight >= minBoneWeight && boneWeight.boneIndex < skinnedMeshBones.Length)
          {
            if (skinnedMeshBones[boneWeight.boneIndex] != null)
            {
              skinnedMeshBones[boneWeight.boneIndex].WorldSpaceVertices.Add(worldVertices[i]);
            }
          }
          boneIndex++;
        }
      }
    }
#endif

    /// <summary>
    /// Sets the skinned mesh bone's world vertices list.
    /// </summary>
    /// <param name="skinnedMeshBones">Array of all skinned mesh bones</param>
    /// <param name="boneWeights">Array of all bone weights</param>
    /// <param name="worldVertices">Array of all vertices in world space</param>
    /// <param name="minBoneWeight">Minimum bone weight to include a vertex in a bone's vertex list.</param>
    private void SetWorldVertices(SkinnedMeshBone[] skinnedMeshBones, BoneWeight[] boneWeights, Vector3[] worldVertices, float minBoneWeight)
    {
      for (int i = 0; i < boneWeights.Length; i++)
      {
        // make sure the weight is above the minimum weight.
        if (boneWeights[i].weight0 >= minBoneWeight && skinnedMeshBones[boneWeights[i].boneIndex0] != null)
        {
          // add the vertex to that bone's vertex list
          skinnedMeshBones[boneWeights[i].boneIndex0].WorldSpaceVertices.Add(worldVertices[i]);
          // check to see if it's a child bone.
          // child bones will have indexs 1 above it's parent.
          if (boneWeights[i].boneIndex1 - boneWeights[i].boneIndex0 == 1)
          {
            skinnedMeshBones[boneWeights[i].boneIndex0].ChildIndexs.Add(boneWeights[i].boneIndex1);
          }
          if (boneWeights[i].boneIndex2 - boneWeights[i].boneIndex0 == 1)
          {
            skinnedMeshBones[boneWeights[i].boneIndex0].ChildIndexs.Add(boneWeights[i].boneIndex2);
          }
          if (boneWeights[i].boneIndex3 - boneWeights[i].boneIndex0 == 1)
          {
            skinnedMeshBones[boneWeights[i].boneIndex0].ChildIndexs.Add(boneWeights[i].boneIndex3);
          }
        }
        // repeat the above for all 4 possible weights/indexs
        if (boneWeights[i].weight1 >= minBoneWeight && skinnedMeshBones[boneWeights[i].boneIndex1] != null)
        {
          skinnedMeshBones[boneWeights[i].boneIndex1].WorldSpaceVertices.Add(worldVertices[i]);
          if (boneWeights[i].boneIndex0 - boneWeights[i].boneIndex1 == 1)
          {
            skinnedMeshBones[boneWeights[i].boneIndex1].ChildIndexs.Add(boneWeights[i].boneIndex0);
          }
          if (boneWeights[i].boneIndex2 - boneWeights[i].boneIndex1 == 1)
          {
            skinnedMeshBones[boneWeights[i].boneIndex1].ChildIndexs.Add(boneWeights[i].boneIndex2);
          }
          if (boneWeights[i].boneIndex3 - boneWeights[i].boneIndex1 == 1)
          {
            skinnedMeshBones[boneWeights[i].boneIndex1].ChildIndexs.Add(boneWeights[i].boneIndex3);
          }
        }
        if (boneWeights[i].weight2 >= minBoneWeight && skinnedMeshBones[boneWeights[i].boneIndex2] != null)
        {
          skinnedMeshBones[boneWeights[i].boneIndex2].WorldSpaceVertices.Add(worldVertices[i]);
          if (boneWeights[i].boneIndex0 - boneWeights[i].boneIndex2 == 1)
          {
            skinnedMeshBones[boneWeights[i].boneIndex2].ChildIndexs.Add(boneWeights[i].boneIndex0);
          }
          if (boneWeights[i].boneIndex1 - boneWeights[i].boneIndex2 == 1)
          {
            skinnedMeshBones[boneWeights[i].boneIndex2].ChildIndexs.Add(boneWeights[i].boneIndex1);
          }
          if (boneWeights[i].boneIndex3 - boneWeights[i].boneIndex2 == 1)
          {
            skinnedMeshBones[boneWeights[i].boneIndex2].ChildIndexs.Add(boneWeights[i].boneIndex3);
          }
        }
        if (boneWeights[i].weight3 >= minBoneWeight && skinnedMeshBones[boneWeights[i].boneIndex3] != null)
        {
          skinnedMeshBones[boneWeights[i].boneIndex3].WorldSpaceVertices.Add(worldVertices[i]);
          if (boneWeights[i].boneIndex0 - boneWeights[i].boneIndex3 == 1)
          {
            skinnedMeshBones[boneWeights[i].boneIndex3].ChildIndexs.Add(boneWeights[i].boneIndex0);
          }
          if (boneWeights[i].boneIndex1 - boneWeights[i].boneIndex3 == 1)
          {
            skinnedMeshBones[boneWeights[i].boneIndex3].ChildIndexs.Add(boneWeights[i].boneIndex1);
          }
          if (boneWeights[i].boneIndex2 - boneWeights[i].boneIndex3 == 1)
          {
            skinnedMeshBones[boneWeights[i].boneIndex3].ChildIndexs.Add(boneWeights[i].boneIndex2);
          }
        }
      }
    }


  }
}
#endif