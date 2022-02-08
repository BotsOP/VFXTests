using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class Leaves2Controller : MonoBehaviour
{
    public VisualEffect VFXGraph;
    void Start()
    {
        StartCoroutine("StartLeaves");
    }

    private IEnumerator StartLeaves()
    {
        yield return new WaitForSeconds(5f);
        VFXGraph.SetBool("Dance", true);
    }
}
