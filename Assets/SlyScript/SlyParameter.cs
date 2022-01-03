using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sly
{
    public class SlyParameter
    {
        public string name;
        public string value;
        public bool isVariable = false;
        //IF isVariable == false
        public SlyObjectType type;
        public SlyParameter(SlyVariable var, string value)
        {
            name = var.name;
            type = var.type;
            this.value = value;
        }
    }
}