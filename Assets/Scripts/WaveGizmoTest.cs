using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveGizmoTest : MonoBehaviour
{
    [SerializeField] private float steepness, wavelength, speed;
    private void OnDrawGizmos()
    {
        for (int i = 0; i < 10; i++)
        {
            Gizmos.color = Color.black;
            Vector3 anchorPos = new Vector3(transform.position.x + (1 * i), transform.position.y, transform.position.z);
            Gizmos.DrawSphere(anchorPos, 0.1f);

            float k = 2 * Mathf.PI / wavelength;
            float f = k * (anchorPos.x - speed * Time.time);
            float a = steepness / k;
            
            float x = a * (float)Math.Sin(f);
            float y = a * (float)Math.Cos(f);
            
            Vector3 tangent = Vector3.Normalize(new Vector3(
                (float)(steepness * Math.Cos(f)),
                (float)(1 - steepness * Math.Sin(f)),
                0
                ));
            Vector3 normal = new Vector3(-tangent.x, tangent.y, 0);

            Gizmos.color = Color.cyan;
            Vector3 wavePos = new Vector3(x + anchorPos.x, y + anchorPos.y, anchorPos.z);
            Gizmos.DrawSphere(wavePos, 0.1f);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(anchorPos, wavePos);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(wavePos, tangent + wavePos);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(wavePos, normal + wavePos);
        }
        
        for (int i = 0; i < 10; i++)
        {
            Gizmos.color = Color.black;
            Vector3 anchorPos = new Vector3(transform.position.x + (1 * i) + 10, transform.position.y, transform.position.z);
            Gizmos.DrawSphere(anchorPos, 0.1f);

            float k = 2 * Mathf.PI / wavelength;
            float f = k * (anchorPos.x - speed * Time.time);
            float a = steepness / k;
            
            float x = a * (float)Math.Sin(f);
            float y = a * (float)Math.Cos(f);
            
            Vector3 tangent = Vector3.Normalize(new Vector3(
                                                    (float)(steepness * Math.Cos(f)),
                                                    (float)(1 - steepness * Math.Sin(f)),
                                                    0
                                                ));
            Vector3 normal = new Vector3(-tangent.x, tangent.y, 0);

            Gizmos.color = Color.cyan;
            Vector3 wavePos = new Vector3(x + anchorPos.x, y + anchorPos.y, anchorPos.z);
            Gizmos.DrawSphere(wavePos, 0.1f);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(anchorPos, wavePos);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(wavePos, tangent + wavePos);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(wavePos, normal + wavePos);
        }
    }
    
    
    // float3 GerstnerWave (
    //     float4 wave, float3 p, inout float3 tangent, inout float3 binormal
    // )
    // {
    //     float steepness = wave.z;
    //     float wavelength = wave.w;
    //     float k = 2 * PI / wavelength;
    //     float c = sqrt(9.8 / k);
    //     float2 d = normalize(wave.xy);
    //     float f = k * (dot(d, p.xz) - c * (_Time.y / _TimeScale));
    //     float a = steepness / k;
				//
    //     //p.x += d.x * (a * cos(f));
    //     //p.y = a * sin(f);
    //     //p.z += d.y * (a * cos(f));
    //
    //     tangent += float3(
    //         -d.x * d.x * (steepness * sin(f)),
    //         d.x * (steepness * cos(f)),
    //         -d.x * d.y * (steepness * sin(f))
    //     );
    //     binormal += float3(
    //         -d.x * d.y * (steepness * sin(f)),
    //         d.y * (steepness * cos(f)),
    //         -d.y * d.y * (steepness * sin(f))
    //     );
    //     return float3(
    //         d.x * (a * cos(f)),
    //         a * sin(f),
    //         d.y * (a * cos(f))
    //     );
    // }
}
