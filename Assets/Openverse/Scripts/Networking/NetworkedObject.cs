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
    public Guid myID;
    public MessageSendMode mode = MessageSendMode.unreliable;
    public static Dictionary<Guid, NetworkedObject> NetworkedObjects = new Dictionary<Guid, NetworkedObject>();
    public List<PropertyInfo> toNetworkQueue = new List<PropertyInfo>();
    public Dictionary<PropertyInfo, string> NetworkedVariables = new Dictionary<PropertyInfo, string>();
    public bool isFinished;


    private Dictionary<string, Component> NetworkedComponents = new Dictionary<string, Component>();
    private bool createdCreateMessage = false;
    private Message myCreateMessage;
    static MethodInfo propertyChangeMethod = SymbolExtensions.GetMethodInfo((object o) => NetworkedObject.onPropertyChange(o));

    public static List<string> patched = new List<string>();

    public HashSet<Type> networkedPropertyTypes = new HashSet<Type>
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

    private void Awake()
    {
        myID = Guid.NewGuid();
        NetworkedObjects.Add(myID,this);
    }

    private void FixedUpdate()
    {
        //Network all properties in the toNetworkQueue
        for(int i = toNetworkQueue.Count - 1; i >= 0; i--)
        {
            Message propertyUpdateMessage = Message.Create(mode, ServerToClientId.updateVariable);
            propertyUpdateMessage.Add(myID.ToString());
            PropertyInfo prop = toNetworkQueue[i];
            propertyUpdateMessage.Add(NetworkedVariables[prop]); //add the infos
            bool success = false;
            //Couldn't make this a switch but would have loved to do so
            if (prop.PropertyType == typeof(string))
            {
                propertyUpdateMessage.Add((ushort)0);
                propertyUpdateMessage.Add((string)prop.GetValue(NetworkedComponents[NetworkedVariables[prop]], null));
                success = true;
            }
            if (prop.PropertyType == typeof(float))
            {
                propertyUpdateMessage.Add((ushort)1);
                propertyUpdateMessage.Add((float)prop.GetValue(NetworkedComponents[NetworkedVariables[prop]], null));
                success = true;
            }
            if (prop.PropertyType == typeof(int))
            {
                propertyUpdateMessage.Add((ushort)2);
                propertyUpdateMessage.Add((int)prop.GetValue(NetworkedComponents[NetworkedVariables[prop]], null));
                success = true;
            }
            if (prop.PropertyType == typeof(bool))
            {
                propertyUpdateMessage.Add((ushort)3);
                propertyUpdateMessage.Add((bool)prop.GetValue(NetworkedComponents[NetworkedVariables[prop]], null));
                success = true;
            }
            if (prop.PropertyType == typeof(Vector2))
            {
                propertyUpdateMessage.Add((ushort)4);
                propertyUpdateMessage.Add((Vector2)prop.GetValue(NetworkedComponents[NetworkedVariables[prop]], null));
                success = true;
            }
            if (prop.PropertyType == typeof(Vector3))
            {
                propertyUpdateMessage.Add((ushort)5);
                propertyUpdateMessage.Add((Vector3)prop.GetValue(NetworkedComponents[NetworkedVariables[prop]], null));
                success = true;
            }
            if (prop.PropertyType == typeof(Quaternion))
            {
                propertyUpdateMessage.Add((ushort)6);
                propertyUpdateMessage.Add((Quaternion)prop.GetValue(NetworkedComponents[NetworkedVariables[prop]], null));
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
            toNetworkQueue.RemoveAt(i);
        }
    }

    public void SendtoPlayer(PlayerConnection p)
    {
        isFinished = false;
        if(!createdCreateMessage)
        {
            myCreateMessage = CreateCreateMessage();
            createdCreateMessage = true;
        }
        Metaserver.Instance.server.Send(myCreateMessage, p.Id);
        isFinished = true;
    }

    public Message CreateCreateMessage()
    {
        Message createMessage = Message.Create(MessageSendMode.reliable, ServerToClientId.spawnObject);
        createMessage.Add(myID.ToString());
        createMessage.Add(transform.position);
        createMessage.Add(transform.rotation);
        createMessage.Add(transform.localScale);
        createMessage.Add(gameObject.name);
        Component[] myComponents = gameObject.GetComponents<Component>();
        int nonNetworkedComps = 0;
        for (int i = 0; i < myComponents.Length; i++)
        {
            if (!AllowedComponents.allowedTypes.Contains(myComponents[i].GetType()) || myComponents[i].GetType() == typeof(Transform))
            {
                nonNetworkedComps++;
            }
        }
        createMessage.Add(myComponents.Length - nonNetworkedComps);
        for (int i = 0; i < myComponents.Length; i++)
        {
            if (AllowedComponents.allowedTypes.Contains(myComponents[i].GetType()) && myComponents[i].GetType() != typeof(Transform))
            {
                createMessage.Add(AllowedComponents.allowedTypesList.IndexOf(myComponents[i].GetType()));
                foreach (var prop in myComponents[i].GetType().GetProperties())
                {
                    if (networkedPropertyTypes.Contains(prop.PropertyType) && prop.CanWrite)
                    {
                        bool success = false;
                        //Couldn't make this a switch but would have loved to do so
                        if (prop.PropertyType == typeof(string))
                        {
                            createMessage.Add(true);
                            createMessage.Add(prop.Name);
                            createMessage.Add((ushort)0);
                            createMessage.Add((string)prop.GetValue(myComponents[i], null));
                            success = true;
                        }
                        if (prop.PropertyType == typeof(float))
                        {
                            createMessage.Add(true);
                            createMessage.Add(prop.Name);
                            createMessage.Add((ushort)1);
                            createMessage.Add((float)prop.GetValue(myComponents[i], null));
                            success = true;
                        }
                        if (prop.PropertyType == typeof(int))
                        {
                            createMessage.Add(true);
                            createMessage.Add(prop.Name);
                            createMessage.Add((ushort)2);
                            createMessage.Add((int)prop.GetValue(myComponents[i], null));
                            success = true;
                        }
                        if (prop.PropertyType == typeof(bool))
                        {
                            createMessage.Add(true);
                            createMessage.Add(prop.Name);
                            createMessage.Add((ushort)3);
                            createMessage.Add((bool)prop.GetValue(myComponents[i], null));
                            success = true;
                        }
                        if (prop.PropertyType == typeof(Vector2))
                        {
                            createMessage.Add(true);
                            createMessage.Add(prop.Name);
                            createMessage.Add((ushort)4);
                            createMessage.Add((Vector2)prop.GetValue(myComponents[i], null));
                            success = true;
                        }
                        if (prop.PropertyType == typeof(Vector3))
                        {
                            createMessage.Add(true);
                            createMessage.Add(prop.Name);
                            createMessage.Add((ushort)5);
                            createMessage.Add((Vector3)prop.GetValue(myComponents[i], null));
                            success = true;
                        }
                        if (prop.PropertyType == typeof(Quaternion))
                        {
                            createMessage.Add(true);
                            createMessage.Add(prop.Name);
                            createMessage.Add((ushort)6);
                            createMessage.Add((Quaternion)prop.GetValue(myComponents[i], null));
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
                                        createMessage.Add(true);
                                        createMessage.Add(prop.Name);
                                        createMessage.Add((ushort)7);
                                        createMessage.Add(name);
                                        PatchProperty(myComponents[i], prop);
                                    }
                                }
                            }
                            catch(Exception e) {
                                Debug.LogException(e);
                            }
                        }
                        else
                        {
                            PatchProperty(myComponents[i], prop);
                        }
                    }
                }
                createMessage.Add(false);
            }
            else
            {
                if (myComponents[i]?.GetType() != typeof(NetworkedObject) && myComponents[i].GetType() != typeof(Transform))
                {
                    Debug.LogWarning("The component of type " + myComponents[i].GetType().Name + " will not be networked to the client!");
                }
            }
        }
        return createMessage;
    }

    private void PatchProperty(Component comp,PropertyInfo prop)
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
                if (setmet.IsDeclaredMember() is true)
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

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (CodeInstruction instruction in instructions)
        {
            if(instruction.opcode == OpCodes.Call)
            {
                yield return instruction;
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Call, propertyChangeMethod);
            } else
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
            if(o.GetType().IsSubclassOf(typeof(Component)))
            {
                cache.TryGetValue(((Component)o), out NetworkedObject netobj);
                if (netobj == null)
                {
                    netobj = ((Component)o).GetComponent<NetworkedObject>();
                    cache.Add(((Component)o), netobj);
                }
                Debug.Log(netobj.myID + "'s " + o.GetType().Name + " changed!");
            }
        }
        //onPropertyChange(f.);
    }
    public static void onPropertyChange(string objectID, string propertyID)
    {
        NetworkedObject obj = Bootloader.Instance.GetNetworkedObject(objectID);
        PropertyInfo theProperty = Bootloader.Instance.GetProperty(propertyID);
        if(obj == null || theProperty == null)
        {
            Debug.LogWarning("A patched variable changed but its not correctly registered!");
            return;
        }
        if(!obj.toNetworkQueue.Contains(theProperty))
        {
            obj.toNetworkQueue.Add(theProperty);
        }
    }
}
