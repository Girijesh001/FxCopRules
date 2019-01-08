using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.FxCop.Sdk;

namespace MyRules
{
    public class clsCheck : BaseIntrospectionRule
    {
        public clsCheck(): base("clsCheck", "MyRules.Connection", typeof(clsCheck).Assembly)
        {
        }
        
        public override ProblemCollection Check(Member member)
        {
            Method method = member as Method;
            bool boolFoundConnectionOpened = false;
            bool boolFoundConnectionClosed = false;
            Instruction objInstr=null;
            if (method != null)
            {
                for (int i = 0; i < method.Instructions.Count; i++)
                {
                    objInstr = method.Instructions[i];
                    if (objInstr.Value != null)
                    {
                        if (objInstr.Value.ToString().Contains("System.Data.SqlClient.SqlConnection"))
                        {
                            boolFoundConnectionOpened = true;
                        }
                        if (boolFoundConnectionOpened)
                        {
                            if (objInstr.Value.ToString().Contains("System.Data.Common.DbConnection.Close"))
                            {
                                boolFoundConnectionClosed = true;
                            }
                        }
                    }
                }
            }
            if ((boolFoundConnectionOpened) && (boolFoundConnectionClosed ==false))
            {
                Resolution resolu = GetResolution(new string[] { method.ToString() });
                Problems.Add(new Problem(resolu));
            }
            return Problems;
            
        }
    }
}
