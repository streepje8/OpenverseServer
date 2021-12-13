using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sly
{
    [Serializable]
    public class SlyFunction
    {
        public string name = "undefined";
        public SlyObjectType returntype = SlyObjectType.TypeUndefined;

        public List<SlyVariable> locals = new List<SlyVariable>();
        public SlyVariable[] parameters;
    }
}
