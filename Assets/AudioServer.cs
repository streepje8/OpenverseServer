using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Openverse.Audio
{
    using System;
    using System.Collections.Generic;
    using NetCode;
    using RiptideNetworking;
    using UnityEngine;

    public struct MetaAudioSource
    {
        public Vector3 position;
        public float volume;
        public int streamingIndex;
        public Guid ID { get; private set; }

        public MetaAudioSource(int streamingIndex = 0) //since a struct constructor requires parameters...
        {
            position = Vector3.zero;
            volume = 1f;
            ID = Guid.NewGuid();
            this.streamingIndex = streamingIndex;
        }
    }

    public struct AudioClient
    {
        public MetaAudioSource source;
        public ushort clientID;

        public AudioClient(ushort clientID)
        {
            this.clientID = clientID;
            source = new MetaAudioSource(0);
        }
    }

    public class AudioServer : Singleton<AudioServer>
    {
        public bool useProximityChat = true;
        public Dictionary<ushort, AudioClient> connectedClients = new Dictionary<ushort, AudioClient>();
        public Action<AudioClient> OnClientConnect;
        public Action<AudioClient> OnClientDisonnect;
        public List<MetaAudioSource> sources = new List<MetaAudioSource>();


        public MetaAudioSource CreateSource()
        {
            MetaAudioSource s = new MetaAudioSource(0);
            //Network the creation
            Message m = Message.Create(MessageSendMode.reliable, ServerToClientId.createStreamSource);
            m.Add(s.ID.ToString());
            m.Add(s.position);
            m.Add(s.volume);
            Metaserver.Instance.server.SendToAll(m);
            return s;
        }

        public void StreamAudio(MetaAudioSource source, AudioClip clip)
        {
            clip = SetSampleRate(clip, 11025);
            float[] audioData = new float[clip.samples * clip.channels];
            clip.GetData(audioData, 0);
            byte[] data = new byte[audioData.Length * sizeof(float)];
            Buffer.BlockCopy(audioData, 0, data, 0, Buffer.ByteLength(audioData));
            StreamAudio(source, data);
        }
        
        

        public async void StreamAudio(MetaAudioSource source, byte[] data)
        {
            int chunkSize = 1024; //1kb of data per packet
            int lastIndex = 0;
            foreach (var chunk in SplitArray(data,chunkSize))
            {
                byte[] compressed = AudioUtility.Compress(chunk.ToArray());
                Message m = Message.Create(MessageSendMode.unreliable, ServerToClientId.streamAudioData);
                m.Add(source.ID.ToByteArray(), true);
                m.Add(source.streamingIndex);
                m.Add(compressed, true, true);
                source.streamingIndex++;
                Metaserver.Instance.server.SendToAll(m);
                await Task.Delay(44100/4/(chunkSize/2));
            }
            /*
            for (int i = chunkSize;
                 i < data.Length;
                 i = i + chunkSize < data.Length ? i + chunkSize : data.Length)
            {
                byte[] chunk = new byte[i - lastIndex];
                lastIndex = i;
                Array.Copy(data, i - (i - lastIndex), chunk, 0, chunkSize);
                byte[] compressed = AudioUtility.Compress(chunk);
                Message m = Message.Create(MessageSendMode.unreliable, ServerToClientId.streamAudioData);
                m.Add(source.ID.ToByteArray(), true);
                m.Add(source.streamingIndex);
                m.Add(compressed, true, true);
                source.streamingIndex++;
                Metaserver.Instance.server.SendToAll(m);
                await Task.Delay(44100/4/(chunkSize/2));
            }
            */
        }
        
        public IEnumerable<IEnumerable<T>> SplitArray<T>(T[] array, int size)
        {
            for (var i = 0; i < (float)array.Length / size; i++)
            {
                yield return array.Skip(i * size).Take(size);
            }
        }

        public void DeleteSource(MetaAudioSource source)
        {
            if (sources.Contains(source)) sources.Remove(source);
            Message m = Message.Create(MessageSendMode.reliable, ServerToClientId.deleteStreamSource);
            m.Add(source.ID.ToByteArray(), true);
            Metaserver.Instance.server.SendToAll(m);
        }


        void Awake()
        {
            Instance = this;
            if (useProximityChat)
            {
                OnClientConnect += ProximtyConnectHandler;
                OnClientDisonnect += ProximtyDisonnectHandler;
            }
        }

        private void ProximtyConnectHandler(AudioClient audioClient)
        {
            //Make proximity chat
        }

        private void ProximtyDisonnectHandler(AudioClient audioClient)
        {
            //Make proximity chat
        }

        public void DisconnectAudioClient(ushort clientID)
        {
            if (Instance.connectedClients.ContainsKey(clientID))
            {
                Instance.OnClientDisonnect.Invoke(Instance.connectedClients[clientID]);
                Instance.connectedClients.Remove(clientID);
            }
            else Debug.LogError("(AudioServer) An audio client tried to disconnect while it was already disconnected!");
        }

        [MessageHandler((ushort)ClientToServerId.audioClientConnect)]
        private static void AudioClientConnect(ushort fromClientId, Message message)
        {
            if (!Instance.connectedClients.ContainsKey(fromClientId))
            {
                AudioClient a = new AudioClient(fromClientId);
                Instance.connectedClients.Add(fromClientId, a);
                Instance.OnClientConnect.Invoke(a);
            }
            else Debug.LogError("(AudioServer) An audio client tried to connect twice!");
        }

        [MessageHandler((ushort)ClientToServerId.audioClientDisconnect)]
        private static void AudioClientDisconnect(ushort fromClientId, Message message)
        {
            if (Instance.connectedClients.ContainsKey(fromClientId))
            {
                Instance.OnClientDisonnect.Invoke(Instance.connectedClients[fromClientId]);
                Instance.connectedClients.Remove(fromClientId);
            }
            else Debug.LogError("(AudioServer) An audio client tried to disconnect while it was already disconnected!");
        }

        [MessageHandler((ushort)ClientToServerId.audioData)]
        private static void AudioData(ushort fromClientId, Message message)
        {
            //Make this call a action/delegate and make proximity chat work with it
        }
        
        
        
        public static AudioClip SetSampleRate(AudioClip clip, int frequency)
{
    if (clip.frequency == frequency) return clip;
    if (clip.channels != 1 && clip.channels != 2) return clip;

    var samples = new float[clip.samples * clip.channels];

    clip.GetData(samples, 0);

    var samplesNewLength = (int) (frequency * clip.length) * clip.channels;
    var clipNew = AudioClip.Create(clip.name + "_" + frequency, samplesNewLength, clip.channels, frequency, false);

    var channelsOriginal = new List<float[]>();
    var channelsNew = new List<float[]>();

    if (clip.channels == 1)
    {
        channelsOriginal.Add(samples);
        channelsNew.Add(new float[(int) (frequency * clip.length)]);
    }
    else
    {
        channelsOriginal.Add(new float[clip.samples]);
        channelsOriginal.Add(new float[clip.samples]);

        channelsNew.Add(new float[(int) (frequency * clip.length)]);
        channelsNew.Add(new float[(int) (frequency * clip.length)]);

        for (var i = 0; i < samples.Length; i++)
        {
            channelsOriginal[i % 2][i / 2] = samples[i];
        }
    }

    for (var c = 0; c < clip.channels; c++)
    {
        var index = 0;
        var sum = 0f;
        var count = 0;
        var channelSamples = channelsOriginal[c];

        for (var i = 0; i < channelSamples.Length; i++)
        {
            var index_ = (int) ((float) i / channelSamples.Length * channelsNew[c].Length);

            if (index_ == index)
            {
                sum += channelSamples[i];
                count++;
            }
            else
            {
                channelsNew[c][index] = sum / count;
                index = index_;
                sum = channelSamples[i];
                count = 1;
            }
        }
    }

    float[] samplesNew;

    if (clip.channels == 1)
    {
        samplesNew = channelsNew[0];
    }
    else
    {
        samplesNew = new float[channelsNew[0].Length + channelsNew[1].Length];

        for (var i = 0; i < samplesNew.Length; i++)
        {
            samplesNew[i] = channelsNew[i % 2][i / 2];
        }
    }

    clipNew.SetData(samplesNew, 0);

    return clipNew;
}
    }
}