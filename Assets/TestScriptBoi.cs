using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScriptBoi : MonoBehaviour
{
    Light lightBoi;
    void Start()
    {
        lightBoi = GetComponent<Light>();
    }

    // Update is called once per frame
    void Update()
    {
        lightBoi.range = (Mathf.Sin(Time.time) + 1) / 2f * 10f;
    }
}
