using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSlyScript", menuName = "SlyScript/Sly Script", order = 1)]
public class SlyScript : ScriptableObject
{
    public string sourceCode;
    private List<SlyVariable> variables = new List<SlyVariable>();

    public List<SlyVariable> GetVariables()
    {
        return this.variables;
    }

    public enum CompileState
    {
        nothing,
        ERROR,
        SUCCESS,
        slyStart,
        slyBody,
        typeDef,
        varibleAssignment,
        parametersStart
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
        string currentToken = "";
        string slyObjName = "";
        SlyVariable.SlyObjectType currentType = SlyVariable.SlyObjectType.Undefined;
        string fieldname = "";
        CompilerScope scope = CompilerScope.SlyObject;
        bool inString = false;
        char[] compileAbleCodeArray = compileAbleCode.ToCharArray();
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
            if(Enum.GetNames(typeof(SlyVariable.SlyObjectType)).Any(x => x.ToLower().Trim().Equals(currentToken.ToLower().Trim(), StringComparison.OrdinalIgnoreCase)) && state == CompileState.slyBody)
            {
                currentType = (SlyVariable.SlyObjectType) Enum.Parse(typeof(SlyVariable.SlyObjectType),currentToken);
                currentToken = "";
                state = CompileState.typeDef;
            }
            if(c.Equals('=') && state == CompileState.typeDef)
            {
                fieldname = currentToken.Replace("=","");
                currentToken = "";
                state = CompileState.varibleAssignment;
            }
            if (c.Equals('(') && state == CompileState.typeDef)
            {
                fieldname = currentToken;
                currentToken = "";
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
                        slyVar.value = currentToken.Replace(";","");
                        switch(scope)
                        {
                            case CompilerScope.Local:
                                locals.Add(slyVar);
                                break;
                            case CompilerScope.SlyObject:
                                prevariables.Add(slyVar);
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
        if(state == CompileState.ERROR)
        {

        } else
        {
            state = CompileState.SUCCESS;
            this.variables = prevariables;
            
        }
    }

}

class SlyObject
{
    public SlyObject(string content)
    {
        
    }
}