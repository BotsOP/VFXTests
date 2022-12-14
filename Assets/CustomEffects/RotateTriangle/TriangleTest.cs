using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriangleTest : MonoBehaviour
{
    [SerializeField] private Transform point1;
    [SerializeField] private Transform point2;
    [SerializeField] private Transform point3;
    [SerializeField] private float angle;
    [SerializeField] private float angleLength;
    private void OnDrawGizmos()
    {
        Vector3 pos1 = point1.position;
        Vector3 pos2 = point2.position;
        Vector3 pos3 = point3.position;
        Gizmos.DrawSphere(pos1, 0.1f);
        Gizmos.DrawSphere(pos2, 0.1f);
        Gizmos.DrawSphere(pos3, 0.1f);
        Gizmos.DrawLine(pos1, pos2);
        Gizmos.DrawLine(pos2, pos3);
        Gizmos.DrawLine(pos1, pos3);

        Gizmos.color = Color.blue;
        Vector3 basePos = (pos1 + pos2) / 2;
        Gizmos.DrawSphere(basePos, 0.1f);
        Vector3 newPos3 = pos3 - basePos;
        float length = newPos3.magnitude;
        newPos3 = new Vector3(0, (float)Math.Sin(angle) * angleLength, (float)Math.Cos(angle) * angleLength - pos3.z);
        newPos3 = newPos3.normalized * length;
        newPos3 += basePos;
        Gizmos.DrawSphere(newPos3, 0.1f);
        Gizmos.DrawLine(basePos, newPos3);
    }
}
