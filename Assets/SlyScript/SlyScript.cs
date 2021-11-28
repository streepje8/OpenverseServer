using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSlyScript", menuName = "SlyScript/Sly Script", order = 1)]
public class SlyScript : ScriptableObject
{
    public string sourceCode;
    public SlyClass compiledClass = null;

    public enum CompileState
    {
        nothing,
        ERROR,
        SUCCESS,
        slyStart,
        slyBody,
        typeDef,
        varibleAssignment,
        parametersStart,
        parameterDef
    }

    public enum CompilerScope
    {
        SlyObject,
        Parameter,
        Local
    }

    public void Compile()
    {
        //Parse Sly Object
        string compileAbleCode = sourceCode.Replace("\n", "").Replace("\t", "     ");
        
        CompileState state = CompileState.nothing;
        List<SlyVariable> prevariables = new List<SlyVariable>();
        List<SlyVariable> locals = new List<SlyVariable>();
        List<SlyVariable> parameters = new List<SlyVariable>();
        string currentToken = "";
        string slyObjName = "";
        SlyObjectType currentType = SlyObjectType.TypeUndefined;
        SlyObjectType parameterType = SlyObjectType.TypeUndefined;
        string fieldname = "";
        CompilerScope scope = CompilerScope.SlyObject;
        bool inString = false;
        char[] compileAbleCodeArray = compileAbleCode.ToCharArray();
        string errorReason = "";
        for(int index = 0; index < compileAbleCodeArray.Length; index++)
        {
            char c = compileAbleCodeArray[index];
            if(c == '"')
            {
                if(!currentToken.EndsWith("\\")) { 
                    inString = !inString;
                    c = (char) 0;
                }
            }
            if(c != ' ' || inString) { 
                if(c != 0) { 
                    currentToken += c;
                }
            }
            if (currentToken.ToLower().Equals("sly") && state == CompileState.nothing)
            {
                currentToken = "";
                state = CompileState.slyStart;
            }
            if(c.Equals('{') && state == CompileState.slyStart)
            {
                slyObjName = currentToken.Replace(" ", "");
                currentToken = "";
                state = CompileState.slyBody;
            }
            if (Enum.IsDefined(typeof(SlyObjectType), RemoveSpecialCharacters(("Type" + currentToken))) && state == CompileState.slyBody)
            {
                currentType = (SlyObjectType) Enum.Parse(typeof(SlyObjectType), RemoveSpecialCharacters(("Type" + currentToken)));
                currentToken = "";
                state = CompileState.typeDef;
            }
            if (Enum.IsDefined(typeof(SlyObjectType), RemoveSpecialCharacters(("Type" + currentToken))) && state == CompileState.parametersStart)
            {
                parameterType = (SlyObjectType)Enum.Parse(typeof(SlyObjectType), RemoveSpecialCharacters(("Type" + currentToken)));
                currentToken = "";
                state = CompileState.parameterDef;
            }
            if (c.Equals('=') && state == CompileState.typeDef)
            {
                fieldname = currentToken.Replace("=","");
                currentToken = "";
                state = CompileState.varibleAssignment;
            }
            if (c.Equals('(') && state == CompileState.typeDef)
            {
                fieldname = currentToken;
                currentToken = "";
                parameters = new List<SlyVariable>();
                state = CompileState.parametersStart;
                scope = CompilerScope.Parameter;
            }
            if(c.Equals(';'))
            {
                switch(state)
                {
                    case CompileState.varibleAssignment:
                        SlyVariable slyVar = new SlyVariable();
                        slyVar.name = fieldname;
                        slyVar.type = currentType;
                        switch(scope)
                        {
                            case CompilerScope.Local:
                                slyVar.value = currentToken.Replace(";", "");
                                locals.Add(slyVar);
                                break;
                            case CompilerScope.SlyObject:
                                slyVar.value = currentToken.Replace(";", "");
                                prevariables.Add(slyVar);
                                break;
                            case CompilerScope.Parameter:
                                slyVar.name = currentToken.Replace(";", "");
                                parameters.Add(slyVar);
                                break;
                        }
                        currentToken = "";
                        state = CompileState.slyBody;
                        break;
                    default:
                        //Do literally nothing
                        break;
                }
            }
            if(state == CompileState.ERROR)
            {
                break;
            }
            if(c.Equals('}') && state == CompileState.slyBody)
            {
                state = CompileState.SUCCESS;
                break;
            }
        }
        if(RemoveSpecialCharacters(currentToken).Length > 0)
        {
            state = CompileState.ERROR;
            errorReason = "Trailing code found!";
        }
        if(state == CompileState.ERROR)
        {
            Debug.LogError("Error: " + errorReason);
            Debug.LogWarning("Error is presumibly near: " + currentToken.Substring(0,Mathf.Clamp(currentToken.Length,0,10)));
        } else
        {
            state = CompileState.SUCCESS;
            if (compiledClass == null)
            {
                compiledClass = new SlyClass();
            }
            compiledClass.name = slyObjName;
            compiledClass.variables = prevariables;
        }
        SlyManager.recompileAllExceptSelf(this);
        EditorUtility.SetDirty(this);
    }

    public static string RemoveSpecialCharacters(string str)
    {
        return Regex.Replace(str, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled);
    }

}

[Serializable]
public class SlyClass
{
    public string name = "Undefined";
    public List<SlyVariable> variables = new List<SlyVariable>();
}

[Serializable]
public class SlyInstance
{
    public SlyClass type;
    public List<SlyVariable> variables = new List<SlyVariable>();
    public SlyInstance(SlyClass type)
    {
        this.type = type;
        variables = type.variables;
    }

    public SlyInstance(SlyInstance copy)
    {
        type = copy.type;
        variables = copy.variables;
    }

    public void recompile(SlyClass newType)
    {
        Debug.Log("Recompiled component");
        type = newType;
        List<SlyVariable> oldvariables = variables;
        variables = type.variables;
        foreach (SlyVariable slyvar in type.variables)
        {
            foreach (SlyVariable oldvar in oldvariables)
            {
                if (oldvar.name.Equals(slyvar.name) && oldvar.type.Equals(slyvar.type))
                {
                    slyvar.value = oldvar.value;
                }
            }
        }
    }
}