using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Sly
{
    public class SlyInvocation
    {
        public bool isSystemInvocation;

        //IF isSystemInvocation == true
        public SlyClass executionClass;
        public SlyFunction executionFunction;

        //IF isSystemInvocation == false
        public string name;
        public SlySystemInvocation invocation;
        public SlyParameter[] Myparameters;

        public SlyInvocation(SlyClass sclass, SlyFunction func)
        {
            executionClass = sclass;
            executionFunction = func;
            isSystemInvocation = false;
        }
        public SlyInvocation(string invocation)
        {
            isSystemInvocation = true;
            name = invocation.Substring(0, invocation.IndexOf('('));
            string temp = invocation.Substring(invocation.IndexOf('(') + 1);
            temp = temp.Replace(")", "");
            temp = temp.Replace("\"", "-QUOTE-");
            temp = Regex.Replace(temp, @"[^\w., -]", ""); //Sanitize the string
            temp = temp.Replace("-QUOTE-", "\"");
            Debug.Log(temp);
            string[] foundParameters = { };
            if (temp.Length > 0)
            {
                foundParameters = temp.Split(',');
            }
            name = Regex.Replace(name, @"[^\w\.@-]", ""); //Sanitize the string
            if(SlySystemInvocations.getSystemInvocations().ContainsKey(name))
            {
                isSystemInvocation = true;
                this.invocation = SlySystemInvocations.getSystemInvocations()[name];
                if(this.invocation.expectedParameters.Keys.Count == foundParameters.Length)
                {

                } else
                {
                    Debug.LogError("[SLY/ERROR] " + name + " expects " + this.invocation.expectedParameters.Keys.Count + " parameters!");
                }
            } else
            {
                Debug.LogError("[SLY/ERROR] " + name + " is not a valid system function and not found in any compiled class");
            }
        }

        public void Run(SlyParameter[] parameters, List<SlyVariable> locals, GameObject runner)
        {
            if(!isSystemInvocation)
            {
                executionFunction.Run(runner,parameters);
            } else
            {
                invocation.Invoke(runner, parameters);
            }
        }
    }
}