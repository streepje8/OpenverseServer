using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bootloader : Singleton<Bootloader>
{
    public Harmony harmony;

    private void Awake()
    {
        DontDestroyOnLoad(this);
        Instance = this;
        harmony = new Harmony("com.streep.openversepatch");
        harmony.PatchAll();
    }

    public void log(string v)
    {
        Debug.Log(v);
    }
}
