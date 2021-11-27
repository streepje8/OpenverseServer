using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SlyFunction
{
    public string name = "undefined";
    public SlyObjectType returntype = SlyObjectType.TypeUndefined;

    public List<SlyVariable> locals = new List<SlyVariable>();
}
