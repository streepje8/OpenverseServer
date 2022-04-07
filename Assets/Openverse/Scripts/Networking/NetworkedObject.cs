using HarmonyLib;
using Openverse.Core;
using Openverse.NetCode;
using RiptideNetworking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using static Openverse.NetCode.NetworkingCommunications;

public class NetworkedObject : MonoBehaviour
{
    public MessageSendMode mode = MessageSendMode.unreliable;
    public Guid myID { get; private set; }
    public bool isFinished { get; private set; }
    public HashSet<Type> networkedPropertyTypes { get; private set; } = new HashSet<Type>
    {
        typeof(string),
        typeof(float),
        typeof(int),
        typeof(bool),
        typeof(Vector3),
        typeof(Vector2),
        typeof(Quaternion),
        typeof(Mesh),
        typeof(Material)
    };
    public bool supressWarnings = true;

    public static Dictionary<Guid, NetworkedObject> NetworkedObjects = new Dictionary<Guid, NetworkedObject>();
    public static List<string> patched = new List<string>();
    private static MethodInfo propertyChangeMethod = SymbolExtensions.GetMethodInfo((object o) => NetworkedObject.onPropertyChange(o));

    private bool createdCreateMessage = false;
    private List<Component> toNetworkQueue = new List<Component>();
    private List<Message> myCreateMessages;
    private Vector3 lastPOS;
    private Quaternion lastRot;
    private Vector3 lastScale;

    private void Awake()
    {
        myID = Guid.NewGuid();
        NetworkedObjects.Add(myID, this);
        lastPOS = transform.position;
        lastRot = transform.rotation;
        lastScale = transform.localScale;
    }

    private void FixedUpdate()
    {
        //Network the transform component
        if (transform.position != lastPOS || transform.rotation != lastRot || transform.localScale != lastScale)
        {
            Message transformUpdateMessage = Message.Create(mode, ServerToClientId.transformObject);
            transformUpdateMessage.Add(myID.ToString());
            transformUpdateMessage.Add(transform.position);
            transformUpdateMessage.Add(transform.rotation);
            transformUpdateMessage.Add(transform.localScale);
            lastPOS = transform.position;
            lastRot = transform.rotation;
            lastScale = transform.localScale;
            Metaserver.Instance.server.SendToAll(transformUpdateMessage);
        }

        //Network all properties in the toNetworkQueue
        for (int i = toNetworkQueue.Count - 1; i >= 0; i--)
        {
            foreach (PropertyInfo prop in toNetworkQueue[i].GetType().GetProperties())
            {
                if (networkedPropertyTypes.Contains(prop.PropertyType))
                {
                    Message propertyUpdateMessage = Message.Create(mode, ServerToClientId.updateVariable);
                    propertyUpdateMessage.Add(myID.ToString());
                    propertyUpdateMessage.Add(toNetworkQueue[i].GetType().Name + "$.$" + prop.Name); //add the infos
                    bool success = false;
                    //Couldn't make this a switch but would have loved to do so
                    if (prop.PropertyType == typeof(string))
                    {
                        propertyUpdateMessage.Add((ushort)0);
                        propertyUpdateMessage.Add((string)prop.GetValue(toNetworkQueue[i], null));
                        success = true;
                    }
                    if (prop.PropertyType == typeof(float))
                    {
                        propertyUpdateMessage.Add((ushort)1);
                        propertyUpdateMessage.Add((float)prop.GetValue(toNetworkQueue[i], null));
                        success = true;
                    }
                    if (prop.PropertyType == typeof(int))
                    {
                        propertyUpdateMessage.Add((ushort)2);
                        propertyUpdateMessage.Add((int)prop.GetValue(toNetworkQueue[i], null));
                        success = true;
                    }
                    if (prop.PropertyType == typeof(bool))
                    {
                        propertyUpdateMessage.Add((ushort)3);
                        propertyUpdateMessage.Add((bool)prop.GetValue(toNetworkQueue[i], null));
                        success = true;
                    }
                    if (prop.PropertyType == typeof(Vector2))
                    {
                        propertyUpdateMessage.Add((ushort)4);
                        propertyUpdateMessage.Add((Vector2)prop.GetValue(toNetworkQueue[i], null));
                        success = true;
                    }
                    if (prop.PropertyType == typeof(Vector3))
                    {
                        propertyUpdateMessage.Add((ushort)5);
                        propertyUpdateMessage.Add((Vector3)prop.GetValue(toNetworkQueue[i], null));
                        success = true;
                    }
                    if (prop.PropertyType == typeof(Quaternion))
                    {
                        propertyUpdateMessage.Add((ushort)6);
                        propertyUpdateMessage.Add((Quaternion)prop.GetValue(toNetworkQueue[i], null));
                        success = true;
                    }
                    if (!success)
                    {
                        Debug.LogWarning("Failed to sync value of variable " + prop.Name + "! Could not add value to packet!");
                    }
                    else
                    {
                        Metaserver.Instance.server.SendToAll(propertyUpdateMessage);
                    }
                }
            }
            toNetworkQueue.RemoveAt(i);
        }
    }

    public void SendtoPlayer(PlayerConnection p)
    {
        isFinished = false;
        if (!createdCreateMessage)
        {
            myCreateMessages = CreateCreateMessages();
            createdCreateMessage = true;
        }
        foreach (Message m in myCreateMessages)
        {
            Metaserver.Instance.server.Send(m, p.Id);
        }
        isFinished = true;
    }

    public List<Message> CreateCreateMessages()
    {
        List<Message> messages = new List<Message>();
        Message createMessage = Message.Create(MessageSendMode.reliable, ServerToClientId.spawnObject);
        createMessage.Add(myID.ToString());
        createMessage.Add(transform.position);
        createMessage.Add(transform.rotation);
        createMessage.Add(transform.localScale);
        createMessage.Add(gameObject.name);
        messages.Add(createMessage);
        Component[] myComponents = gameObject.GetComponents<Component>();
        int nonNetworkedComps = 0;
        for (int i = 0; i < myComponents.Length; i++)
        {
            if (!AllowedComponents.allowedTypes.Contains(myComponents[i].GetType()) || myComponents[i].GetType() == typeof(Transform))
            {
                nonNetworkedComps++;
            }
        }
        //createMessage.Add(myComponents.Length - nonNetworkedComps);
        for (int i = 0; i < myComponents.Length; i++)
        {
            if (AllowedComponents.allowedTypes.Contains(myComponents[i].GetType()) && myComponents[i].GetType() != typeof(Transform))
            {
                Message addCompMessage = Message.Create(MessageSendMode.reliable, ServerToClientId.addComponent);
                addCompMessage.Add(myID.ToString());
                addCompMessage.Add(AllowedComponents.allowedTypesList.IndexOf(myComponents[i].GetType()));
                foreach (var prop in myComponents[i].GetType().GetProperties())
                {
                    if (networkedPropertyTypes.Contains(prop.PropertyType) && prop.CanWrite)
                    {
                        bool success = false;
                        //Couldn't make this a switch but would have loved to do so
                        if (prop.PropertyType == typeof(string))
                        {
                            addCompMessage.Add(true);
                            addCompMessage.Add(prop.Name);
                            addCompMessage.Add((ushort)0);
                            addCompMessage.Add((string)prop.GetValue(myComponents[i], null));
                            success = true;
                        }
                        if (prop.PropertyType == typeof(float))
                        {
                            addCompMessage.Add(true);
                            addCompMessage.Add(prop.Name);
                            addCompMessage.Add((ushort)1);
                            addCompMessage.Add((float)prop.GetValue(myComponents[i], null));
                            success = true;
                        }
                        if (prop.PropertyType == typeof(int))
                        {
                            addCompMessage.Add(true);
                            addCompMessage.Add(prop.Name);
                            addCompMessage.Add((ushort)2);
                            addCompMessage.Add((int)prop.GetValue(myComponents[i], null));
                            success = true;
                        }
                        if (prop.PropertyType == typeof(bool))
                        {
                            addCompMessage.Add(true);
                            addCompMessage.Add(prop.Name);
                            addCompMessage.Add((ushort)3);
                            addCompMessage.Add((bool)prop.GetValue(myComponents[i], null));
                            success = true;
                        }
                        if (prop.PropertyType == typeof(Vector2))
                        {
                            addCompMessage.Add(true);
                            addCompMessage.Add(prop.Name);
                            addCompMessage.Add((ushort)4);
                            addCompMessage.Add((Vector2)prop.GetValue(myComponents[i], null));
                            success = true;
                        }
                        if (prop.PropertyType == typeof(Vector3))
                        {
                            addCompMessage.Add(true);
                            addCompMessage.Add(prop.Name);
                            addCompMessage.Add((ushort)5);
                            addCompMessage.Add((Vector3)prop.GetValue(myComponents[i], null));
                            success = true;
                        }
                        if (prop.PropertyType == typeof(Quaternion))
                        {
                            addCompMessage.Add(true);
                            addCompMessage.Add(prop.Name);
                            addCompMessage.Add((ushort)6);
                            addCompMessage.Add((Quaternion)prop.GetValue(myComponents[i], null));
                            success = true;
                        }
                        if (!success)
                        {
                            object value = prop.GetValue(myComponents[i], null);
                            string name = "null";
                            try
                            {
                                if (value != null && ((UnityEngine.Object)value).name != null)
                                {
                                    name = ((UnityEngine.Object)value).name.Replace(" (Instance)", "");
                                    bool foundInBundle = false;
                                    foreach (UnityEngine.Object obj in Metaserver.Instance.allAssets)
                                    {
                                        if (obj.name == name)
                                        {
                                            foundInBundle = true;
                                        }
                                    }
                                    if (foundInBundle)
                                    {
                                        addCompMessage.Add(true);
                                        addCompMessage.Add(prop.Name);
                                        addCompMessage.Add((ushort)7);
                                        addCompMessage.Add(name);
                                        PatchProperty(myComponents[i], prop);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Debug.LogException(e);
                            }
                        }
                        else
                        {
                            PatchProperty(myComponents[i], prop);
                        }
                    }
                }
                addCompMessage.Add(false);
                messages.Add(addCompMessage);
            }
            else
            {
                if (myComponents[i]?.GetType() != typeof(NetworkedObject) && myComponents[i].GetType() != typeof(Transform))
                {
                    Debug.LogWarning("The component of type " + myComponents[i].GetType().Name + " will not be networked to the client!");
                }
            }
        }
        return messages;
    }

    private void PatchProperty(Component comp, PropertyInfo prop)
    {
        if (!patched.Contains(comp.GetType() + "&&" + prop.Name))
        {
            if (Bootloader.Instance == null || Bootloader.Instance.harmony == null)
            {
                Debug.LogError("Harmony is null! Please ensure the bootloader is present in the scene!");
                return;
            }
            if (prop != null && prop.GetSetMethod() != null)
            {
                MethodInfo setmet = prop.GetSetMethod();
                if (setmet.IsDeclaredMember())
                {
                    try
                    {
                        Bootloader.Instance.harmony.Patch(setmet, transpiler: new HarmonyMethod(typeof(NetworkedObject).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static)));
                        patched.Add(comp.GetType() + "&&" + prop.Name);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("Property " + prop.Name + " can not be networked in realtime, network it manually when making changes. Reason: " + e.Message);
                    }
                }
            }
        }
    }

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Call)
            {
                yield return instruction;
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Call, propertyChangeMethod);
            }
            else
            {
                yield return instruction;
            }
        }
    }

    private static Dictionary<Component, NetworkedObject> cache = new Dictionary<Component, NetworkedObject>();

    public static void onPropertyChange(object o)
    {
        if (o != null)
        {
            if (o.GetType().IsSubclassOf(typeof(Component)))
            {
                cache.TryGetValue(((Component)o), out NetworkedObject netobj);
                if (netobj == null)
                {
                    netobj = ((Component)o).GetComponent<NetworkedObject>();
                    cache.Add((Component)o, netobj);
                }
                if (!netobj.toNetworkQueue.Contains((Component)o))
                {
                    netobj.toNetworkQueue.Add((Component)o);
                }
            }
        }
    }
}
