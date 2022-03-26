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
        boxBoi.center = transform.position;
    }
}
