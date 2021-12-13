using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Sly
{

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
            functionReady,
            functionBody
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
            List<SlyFunction> functions = new List<SlyFunction>();
            string currentToken = "";
            string slyObjName = "";
            SlyObjectType currentType = SlyObjectType.TypeUndefined;
            SlyObjectType parameterType = SlyObjectType.TypeUndefined;
            string fieldname = "";
            CompilerScope scope = CompilerScope.SlyObject;
            bool inString = false;
            char[] compileAbleCodeArray = compileAbleCode.ToCharArray();
            string errorReason = "";
            SlyFunction currentFunction = null;
            for (int index = 0; index < compileAbleCodeArray.Length; index++)
            {
                char c = compileAbleCodeArray[index];
                if (c == '"')
                {
                    if (!currentToken.EndsWith("\\"))
                    {
                        inString = !inString;
                        c = (char)0;
                    }
                }
                if (c != ' ' || inString)
                {
                    if (c != 0)
                    {
                        currentToken += c;
                    }
                }
                if (currentToken.ToLower().Equals("sly") && state == CompileState.nothing)
                {
                    currentToken = "";
                    state = CompileState.slyStart;
                }
                if (c.Equals('{') && state == CompileState.slyStart)
                {
                    slyObjName = currentToken.Replace(" ", "");
                    currentToken = "";
                    state = CompileState.slyBody;
                }
                if (c.Equals('{') && state == CompileState.functionReady)
                {
                    currentToken = "";
                    state = CompileState.functionBody;
                    scope = CompilerScope.Local;
                }
                if (c.Equals('}') && state == CompileState.slyBody)
                {
                    state = CompileState.SUCCESS;
                    break;
                }
                if (c.Equals('}') && state == CompileState.functionBody)
                {
                    currentFunction.locals = locals;
                    currentFunction.parameters = parameters.ToArray();
                    functions.Add(currentFunction);
                    locals = new List<SlyVariable>();
                    state = CompileState.slyBody;
                    scope = CompilerScope.SlyObject;
                }
                if (Enum.IsDefined(typeof(SlyObjectType), RemoveSpecialCharacters(("Type" + currentToken))) && state == CompileState.slyBody)
                {
                    currentType = (SlyObjectType)Enum.Parse(typeof(SlyObjectType), RemoveSpecialCharacters(("Type" + currentToken)));
                    currentToken = "";
                    state = CompileState.typeDef;
                }
                if (Enum.IsDefined(typeof(SlyObjectType), RemoveSpecialCharacters(("Type" + currentToken))) && state == CompileState.parametersStart)
                {
                    parameterType = (SlyObjectType)Enum.Parse(typeof(SlyObjectType), RemoveSpecialCharacters(("Type" + currentToken)));
                    currentToken = "";
                    state = CompileState.varibleAssignment;
                }
                if (Enum.IsDefined(typeof(SlyObjectType), RemoveSpecialCharacters(("Type" + currentToken))) && state == CompileState.functionBody)
                {
                    currentType = (SlyObjectType)Enum.Parse(typeof(SlyObjectType), RemoveSpecialCharacters(("Type" + currentToken)));
                    currentToken = "";
                    state = CompileState.typeDef;
                }
                if (c.Equals('=') && state == CompileState.typeDef)
                {
                    fieldname = currentToken.Replace("=", "");
                    currentToken = "";
                    state = CompileState.varibleAssignment;
                }
                if (c.Equals('(') && state == CompileState.typeDef)
                {
                    fieldname = currentToken.Replace("(", "");
                    currentToken = "";
                    parameters = new List<SlyVariable>();
                    state = CompileState.parametersStart;
                    scope = CompilerScope.Parameter;
                }
                if(c.Equals(')') && (state == CompileState.parametersStart || (state == CompileState.varibleAssignment && scope == CompilerScope.Parameter)))
                {
                    if(state == CompileState.varibleAssignment) { 
                        SlyVariable slyVar = new SlyVariable();
                        slyVar.type = parameterType;
                        slyVar.name = currentToken.Replace(")", "");
                        parameters.Add(slyVar);
                    }
                    currentFunction = new SlyFunction();
                    currentFunction.name = fieldname;
                    currentFunction.returntype = currentType;
                    state = CompileState.functionReady;
                }
                if (c.Equals(';'))
                {
                    switch (state)
                    {
                        case CompileState.varibleAssignment:
                            SlyVariable slyVar = new SlyVariable();
                            slyVar.name = fieldname;
                            slyVar.type = currentType;
                            switch (scope)
                            {
                                case CompilerScope.SlyObject:
                                    slyVar.value = currentToken.Replace(";", "");
                                    prevariables.Add(slyVar);
                                    state = CompileState.slyBody;
                                    break;
                                case CompilerScope.Parameter:
                                    slyVar.name = currentToken.Replace(";", "");
                                    slyVar.type = parameterType;
                                    parameters.Add(slyVar);
                                    state = CompileState.parametersStart;
                                    break;
                                case CompilerScope.Local:
                                    slyVar.value = currentToken.Replace(";", "");
                                    locals.Add(slyVar);
                                    state = CompileState.functionBody;
                                    break;
                            }
                            currentToken = "";
                            break;
                        case CompileState.functionBody:

                            break;
                        default:
                            //Do literally nothing
                            break;
                    }
                }
                if (state == CompileState.ERROR)
                {
                    break;
                }
            }
            if (RemoveSpecialCharacters(currentToken).Length > 0)
            {
                state = CompileState.ERROR;
                errorReason = "Trailing code found!";
            }
            if (state == CompileState.ERROR)
            {
                Debug.LogError("Error: " + errorReason);
                Debug.LogWarning("Error is presumibly near: " + currentToken.Substring(0, Mathf.Clamp(currentToken.Length, 0, 10)));
            }
            else
            {
                state = CompileState.SUCCESS;
                if (compiledClass == null)
                {
                    compiledClass = new SlyClass();
                }
                compiledClass.name = slyObjName.Replace("{", "");
                compiledClass.variables = prevariables;
                compiledClass.functions = functions;
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
        public List<SlyFunction> functions = new List<SlyFunction>();
    }

    [Serializable]
    public class SlyInstance
    {
        public SlyClass type;
        public List<SlyVariable> variables = new List<SlyVariable>();
        public List<SlyFunction> functions = new List<SlyFunction>();
        public SlyInstance(SlyClass type)
        {
            this.type = type;
            variables = new List<SlyVariable>();
            foreach (SlyVariable var in type.variables)
            {
                SlyVariable copy = new SlyVariable();
                copy.name = var.name;
                copy.type = var.type;
                copy.value = var.value;
                variables.Add(copy);
            }
            this.functions = type.functions;
        }

        public SlyInstance(SlyInstance copyInstance)
        {
            type = copyInstance.type;
            variables = new List<SlyVariable>();
            foreach (SlyVariable var in copyInstance.variables)
            {
                SlyVariable copy = new SlyVariable();
                copy.name = var.name;
                copy.type = var.type;
                copy.value = var.value;
                variables.Add(copy);
            }
            this.functions = type.functions;
        }

        public void setVariable(int index, string value)
        {
            this.variables[index].value = value;
        }

        public List<SlyVariable> getVariables()
        {
            return this.variables;
        }

        public void recompile(SlyClass newType)
        {
            type = newType;
            List<SlyVariable> oldvariables = variables;
            variables = new List<SlyVariable>();
            foreach (SlyVariable var in newType.variables)
            {
                SlyVariable copy = new SlyVariable();
                copy.name = var.name;
                copy.type = var.type;
                copy.value = var.value;
                variables.Add(copy);
            }
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
}