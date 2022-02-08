using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class DissolveController : MonoBehaviour
{
    public Animator animator;
    public SkinnedMeshRenderer skinnedMeshRenderer;
    public VisualEffect VFXGraph;
    public float dissolveRate = 0.02f;
    public float refreshRate = 0.05f;
    public float dieDelay = 0.2f;

    private Material[] dissolveMaterials;
    void Start()
    {
        if (VFXGraph != null)
        {
            VFXGraph.Stop();
            VFXGraph.gameObject.SetActive(false);
        }

        if (skinnedMeshRenderer != null)
        {
            dissolveMaterials = skinnedMeshRenderer.materials;
        }
        
        StartCoroutine(Dissolve());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(Dissolve());
        }
    }

    IEnumerator Dissolve()
    {
        yield return new WaitForSeconds(3f);
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }

        yield return new WaitForSeconds(dieDelay);

        if (VFXGraph != null)
        {
            VFXGraph.gameObject.SetActive(true);
            VFXGraph.Play();
        }
        
        float counter = 0;

        if (dissolveMaterials.Length > 0)
        {
            while (dissolveMaterials[0].GetFloat("DissolveAmount_") < 1)
            {
                counter += dissolveRate;
                for (int i = 0; i < dissolveMaterials.Length; i++)
                {
                    dissolveMaterials[i].SetFloat("DissolveAmount_", counter);
                }

                yield return new WaitForSeconds(refreshRate);
            }
        }
        
        Destroy(gameObject, 1);
    }
}
