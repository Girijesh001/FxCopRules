﻿using Microsoft.FxCop.Sdk;
using System;
using System.Collections.Generic;
using System.Text;
namespace MyRules
{
    public class RemoveEmptyMethods : BaseIntrospectionRule
    {
        public RemoveEmptyMethods() :
            base("RemoveEmptyMethods", "MyRules.Connection",
          typeof(RemoveEmptyMethods).Assembly)
        {
        }
        public override ProblemCollection Check(Member member)
        {
            // Method is a subtype of Member.
            Method method = member as Method;
            if (method == null)
                return base.Check(member);
            //
            // Number of instructions in the method. Method’s name.
            //
            InstructionCollection ops = method.Instructions;
            string methodName = method.Name.Name.ToUpper();
            //
            // If the method doesn’t have any opcodes, no analysis is needed.
            //
            if (ops.Count.Equals(0))
                return base.Check(member);
            //
            // Dont bother with default constructors.
            // Default constructor does nothing except  call the constructor
            // in  its base class. It is usually inserted into an assembly by
            // the compiler.
            //
            if (methodName == ".CTOR" && IsDefaultCtor(method))
                return base.Check(member);
            //if (isEmptyFunctionOrGetProperty(method))
            //   return base.Check(member);
            //
            // Check for empty methods (Subs), functions, properties.
            //
            if (isEmptyVoidMethodOrSetProperty(method) || isEmptyNonVoidMethod(method))
            {
                // For testing purposes, uncomment to print out opcodes.
                //string opcodes == “”;
                //for (int i = 0;  i <  ops.Length; i++)
                //{
                //    opcodes += ops[i].OpCode.ToString();
                //}
                //base.Problems.Add(new Problem(GetResolution(
                //      method.Name.FullName,
                //      method.FullName + ” ops:” + ops.Length.ToString() + ” opcodes: ” + opcodes),
                //      method));
                base.Problems.Add(new Problem(GetResolution(method.FullName), method));
            }
            return base.Problems;
        }
        //
        // See if the method is a default constructor – constructor that does nothing
        // except call the constructor in its base class, usually inserted into your
        // assemblies by the compiler.
        //
        private bool IsDefaultCtor(Method method)
        {
            InstructionCollection ops = method.Instructions;
            if (ops.Count.Equals(4))
            {
                //
                // Filter out default ctor generated by the compiler.
                //
                LocalCollection localList = ops[0].Value as LocalCollection;
                if (localList != null && localList.Count != 0) return false;
                if (ops[1].OpCode != OpCode.Ldarg_0) return false;
                if (ops[2].OpCode != OpCode.Call) return false;
                InstanceInitializer init = ops[2].Value as InstanceInitializer;
                if (init == null) return false;
                if (ops[3].OpCode != OpCode.Ret) return false;
                return true;
            }
            return false;
        }
        //
        // Checks if the method is an method or ‘set’ property.
        // See examples in header comment.
        //
        private bool isEmptyVoidMethodOrSetProperty(Method method)
        {
            InstructionCollection ops = method.Instructions;
            if (!ops.Count.Equals(3))
                return false;
            if ((ops[0].OpCode == OpCode._Locals) &&
            (ops[1].OpCode == OpCode.Nop) &&
            (ops[2].OpCode == OpCode.Ret))
            {
                return true;
            }
            return false;
        }
        //
        // Checks if the method is an empty function. See examples
        // in header comment.
        //
        private bool isEmptyNonVoidMethod(Method method)
        {
            InstructionCollection ops = method.Instructions;
            if (!ops.Count.Equals(4))
                return false;
            if ((ops[0].OpCode == OpCode._Locals) &&
            (ops[1].OpCode == OpCode.Nop) &&
            (ops[2].OpCode == OpCode.Ldloc_0) &&
            (ops[3].OpCode == OpCode.Ret))
            {
                return true;
            }
            return false;
        }
        //
        // Checks if the method is an empty function. See examples
        // in header comment.
        //
        private bool isEmptyFunctionOrGetProperty(Method method)
        {
            InstructionCollection ops = method.Instructions;
            if (!ops.Count.Equals(7))
                return false;
            //
            // Don’t care about ops[2].OpCode which will be some
            // type of load instruction, e.g. OpCode.ldxxx.
            //
            // Opcode 5 returns a value so there is no way of knowing
            // if this is a valid function or not.
            //
            if ((ops[0].OpCode == OpCode._Locals) &&
            (ops[1].OpCode == OpCode.Nop) &&
            (ops[3].OpCode == OpCode.Stloc_0) &&
            (ops[4].OpCode == OpCode.Br_S) &&
            (ops[5].OpCode == OpCode.Ldloc_0) &&
            (ops[6].OpCode == OpCode.Ret))
            {
                return true;
            }
            return false;
        }
    }
}