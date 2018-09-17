﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
namespace CefPreter
{
    class Expression
    {
        List<Function.Function> funcs;
        List<Expression> expressions;

        
        public static Expression Parse(List<Token> tokens)
        {
            
            if (!tokens[0].IsReserved() && tokens[0].Type != CefType.Function)
                throw new Exception("Unknown function " + tokens[0].Name);

            Expression expression = tokens.ToExpression();
            return expression;
        }

        public Expression(List<Function.Function> funcs)
        {
            this.funcs = funcs;
        }

        public Expression(List<Expression> exs)
        {
            this.expressions = exs;
        }

        public CefMemory RequiredMemory()
        {
            CefMemory Memory = new CefMemory();
            foreach(var func in funcs.OrEmptyIfNull() )
                Memory.Add(Types.Variable.Create(func.GetType().Name, ""));
            return Memory;
        }

        
        public async Task<ExpressionResult> Execute(Browser Browser, CefMemory Memory, Action log = null)
        {
            Types.Variable result = null;
            ExpressionResult res = ExpressionResult.OK;

            foreach(Expression ex in expressions.OrEmptyIfNull())
            {
                if (res == ExpressionResult.CondFalse)
                    continue;
                res = await ex.Execute(Browser, Memory, log);
                
            }

            foreach(Function.Function func in funcs.OrEmptyIfNull())
            {

                func.Parameters = Memory.UnpackAllVariables(func.Parameters);
                result = await func.Exec(Browser);

                if (func.GetType().Name == "If")
                {
                    if (((Types.Number)result).Value == 0)
                    {
                        res = ExpressionResult.CondFalse;
                    }
                    else
                        res = ExpressionResult.CondTrue;
                }
                

                if (result != null)
                    Memory.Set(result);
                if (log != null)
                    log();
            }
            return res;
        }
    }

    public enum ExpressionResult
    {
        OK,
        Error,
        CondTrue,
        CondFalse
    }


    static class TokensListExtension
    {
        public static IEnumerable<T> OrEmptyIfNull<T>(this IEnumerable<T> source)
        {
            return source ?? Enumerable.Empty<T>();
        }

        public static Expression ToExpression(this List<Token> tokens)
        {
            List<Function.Function> funcs = new List<Function.Function>();
            //List<Token> Params = paramsList(tokens);
            for (int i = tokens.Count - 1; i >= 0; i--)
            {
                if (tokens[i].Type == CefType.Function /*Function.Function.IsFunction(tokens[i].Type)*/)//////////////////////////////////////////
                {
                    Function.Function func = Function.Function.Create(tokens[i]);
                    try
                    {

                        func.Parameters = (tokens.Eject(i + 1, func.ParamsCount));

                        funcs.Add(func);
                    }
                    catch (IndexOutOfRangeException)
                    {
                        throw new WrongParamsCountException();
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        throw new WrongParamsCountException();
                    }
                }
                
            }
            return new Expression(funcs);
        }
        


        public static List<Token> Eject(this List<Token> tokens, int i0, int count)
        {

            List<Token> result = new List<Token>();
            count = count == -1 ? tokens.Count - i0 : count;
            for (int i = 0; i < count; i++)
            {
                result.Add(tokens[i0]);
                tokens.RemoveAt(i0);////////////////////////////////removeit
            }
            return result;
        }

        
        

        
    }

    

    
}
