using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sly
{
    public class SlyScriptComponent : MonoBehaviour
    {
        public SlyScript Script = null;
        public SlyInstance instance = null;
        public SlyInstance runtimeInstance = null;
        public bool hasCompiled = false;

        private void Start()
        {
            runtimeInstance = new SlyInstance(instance);
        }
    }
}
