using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnOnOff : MonoBehaviour
{
    public float cooldown;
    public GameObject gameObjectTo;
    private float time;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time - time > cooldown)
        {
            gameObjectTo.SetActive(false);
            time = Time.time;
            gameObjectTo.SetActive(true);
        }
    }
}
