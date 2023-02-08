using Openverse.Audio;
using UnityEngine;

public class TestScript : MonoBehaviour
{
    public AudioClip testClip;

    private MetaAudioSource source;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            source = AudioServer.Instance.CreateSource();
            AudioServer.Instance.StreamAudio(source,testClip);
        }
    }
}
