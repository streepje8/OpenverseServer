using Openverse.Core;
using Openverse.Input;
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

    void Update()
    {
        myplayer.RequestInput<float>("Left:Trigger");
    }
}
