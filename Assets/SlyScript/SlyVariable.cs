using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlyVariable
{
    public enum SlyObjectType
    {
        String,
        Integer,
        SlyObject,
        Undefined
    }

    public string name = "undefined";
    public SlyObjectType type = SlyObjectType.Undefined;
    public string value = null;
}
