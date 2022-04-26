using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScript : MonoBehaviour
{
    public VirtualPlayer myplayer;
    void Start()
    {
        myplayer.AddInputListner<float>("Left:Trigger", (InputValue value) => {
            Debug.Log("Left Trigger is equal to: " + value.Get<float>());
        });
    }

    // Update is called once per frame
    void Update()
    {
        myplayer.RequestInput<float>("Left:Trigger");
    }
}
