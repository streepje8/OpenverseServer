using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScriptBoi : MonoBehaviour
{
    BoxCollider boxBoi;
    void Start()
    {
        boxBoi = GetComponent<BoxCollider>();
    }

    // Update is called once per frame
    void Update()
    {
        boxBoi.size = new Vector3((Mathf.Sin(Time.time) + 1) / 2f * 10f, (Mathf.Sin(Time.time * 2) + 1) / 2f * 10f, (Mathf.Sin(Time.time * 4) + 1) / 2f * 10f);
    }
}
