using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using Microsoft.FxCop.Sdk;

namespace MyRules
{
    sealed class DocComments
    {
        public static string GetMemberID(Member member)
        {
            char ch;
            TypeNode declaringType = member.DeclaringType;
            List<TypeNode> parentTypes = new List<TypeNode>();
            List<TypeNode> typeTemplateParameters = new List<TypeNode>();
            List<TypeNode> memberTemplateParameters = new List<TypeNode>();
            StringBuilder sb = new StringBuilder();

            if (member == null)
                throw new ArgumentNullException("member");

            // Determine prefix character.

            switch (member.NodeType)
            {
                case NodeType.Class:
                case NodeType.Interface:
                case NodeType.Struct:
                case NodeType.EnumNode:
                case NodeType.DelegateNode:
                    ch = 'T';
                    break;
                case NodeType.Field:
                    ch = 'F';
                    break;
                case NodeType.Property:
                    ch = 'P';
                    break;
                case NodeType.Method:
                case NodeType.InstanceInitializer:
                case NodeType.StaticInitializer:
                    ch = 'M';
                    break;
                case NodeType.Event:
                    ch = 'E';
                    break;
                default:
                    throw new ArgumentException("Unsupported NodeType.", "member");
            }

            // Determine all parent types of this potentially nested type.

            for (TypeNode current = declaringType; current != null; current = current.DeclaringType)
            {
                parentTypes.Add(current);
            }
            parentTypes.Reverse();

            // Collect all template parameters for the types.

            foreach (TypeNode type in parentTypes)
            {
                if (type.TemplateParameters != null)
                {
                    typeTemplateParameters.AddRange(type.TemplateParameters);
                }
            }

            // Collect all template parameters for the method.

            switch (member.NodeType)
            {
                case NodeType.Method:
                case NodeType.InstanceInitializer:
                case NodeType.StaticInitializer:
                    Method method = (Method)member;

                    if (method.TemplateParameters != null)
                    {
                        memberTemplateParameters.AddRange(method.TemplateParameters);
                    }
                    break;
            }

            // Output full method name.

            sb.Append(ch);
            sb.Append(':');
            if (declaringType == null)
            {
                TypeNode type = member as TypeNode;
                if (type != null)
                {
                    if (type.Namespace.Name.Length != 0)
                    {
                        sb.Append(type.Namespace.Name);
                        sb.Append('.');
                    }
                }
            }
            else
            {
                sb.Append(declaringType.FullName.Replace('+', '.'));
                sb.Append('.');
            }
            sb.Append(member.Name.Name.Replace('.', '#'));

            // Output number of template parameters.

            if (memberTemplateParameters.Count != 0)
            {
                // Undocumented: based on output from MS compilers.
                sb.AppendFormat(CultureInfo.InvariantCulture, "``{0}", memberTemplateParameters.Count);
            }

            // Output parameters.

            ParameterCollection parameters;

            switch (member.NodeType)
            {
                case NodeType.Property:
                    parameters = ((PropertyNode)member).Parameters;
                    break;
                case NodeType.Method:
                case NodeType.InstanceInitializer:
                case NodeType.StaticInitializer:
                    parameters = ((Method)member).Parameters;
                    break;
                default:
                    parameters = null;
                    break;
            }

            if (parameters != null && parameters.Count != 0)
            {
                bool comma = false;
                sb.Append('(');
                foreach (Parameter parameter in parameters)
                {
                    if (comma)
                    {
                        sb.Append(',');
                    }
                    sb.Append(GetStringForTypeNode(parameter.Type,
                      typeTemplateParameters, memberTemplateParameters));
                    comma = true;
                }
                sb.Append(')');
            }

            // Output return type (for conversion operators).

            if (member.NodeType == NodeType.Method && member.IsSpecialName &&
              (member.Name.Name == "op_Explicit" || member.Name.Name == "op_Implicit"))
            {
                Method convOperator = (Method)member;

                sb.Append('~');
                sb.Append(GetStringForTypeNode(convOperator.ReturnType,
                  typeTemplateParameters, memberTemplateParameters));
            }

            return sb.ToString();
        }

        private static string GetStringForTypeNode(TypeNode type,
          List<TypeNode> typeTemplateParameters, List<TypeNode> memberTemplateParameters)
        {
            StringBuilder sb = new StringBuilder();

            switch (type.NodeType)
            {
                /* Ordinary types */

                case NodeType.Class:
                case NodeType.Interface:
                case NodeType.Struct:
                case NodeType.EnumNode:
                case NodeType.DelegateNode:
                    if (type.DeclaringType == null)
                    {
                        if (type.Namespace.Name.Length != 0)
                        {
                            sb.Append(type.Namespace.Name);
                            sb.Append('.');
                        }
                    }
                    else
                    {
                        sb.Append(GetStringForTypeNode(type.DeclaringType,
                          typeTemplateParameters, memberTemplateParameters));
                        sb.Append('.');
                    }

                    if (type.IsGeneric)
                    {
                        String templateName = type.Template.Name.Name.Replace('+', '.');
                        int pos = templateName.LastIndexOf('`');
                        if (pos != -1)
                        {
                            sb.Append(templateName.Substring(0, pos));
                        }
                        else
                        {
                            sb.Append(templateName);
                        }
                    }
                    else
                    {
                        sb.Append(type.Name.Name.Replace('+', '.'));
                    }
                    break;

                /* Simple pointer / reference types */

                case NodeType.Reference:
                    sb.Append(GetStringForTypeNode(((Reference)type).ElementType,
                      typeTemplateParameters, memberTemplateParameters));
                    sb.Append('@');
                    break;
                case NodeType.Pointer:
                    sb.Append(GetStringForTypeNode(((Pointer)type).ElementType,
                      typeTemplateParameters, memberTemplateParameters));
                    sb.Append('*');
                    break;

                /* Generic parameters */

                case NodeType.ClassParameter:
                case NodeType.TypeParameter:
                    int index;
                    if ((index = typeTemplateParameters.IndexOf(type)) != -1)
                    {
                        sb.AppendFormat(CultureInfo.InvariantCulture, "`{0}", index);
                    }
                    else if ((index = memberTemplateParameters.IndexOf(type)) != -1)
                    {
                        // Undocumented: based on output from MS compilers.
                        sb.AppendFormat(CultureInfo.InvariantCulture, "``{0}", index);
                    }
                    else
                    {
                        throw new InvalidOperationException("Unable to resolve TypeParameter to a type argument.");
                    }
                    break;

                /* Arrays */

                case NodeType.ArrayType:
                    ArrayType array = ((ArrayType)type);
                    sb.Append(GetStringForTypeNode(array.ElementType,
                      typeTemplateParameters, memberTemplateParameters));
                    if (array.IsSzArray())
                    {
                        sb.Append("[]");
                    }
                    else
                    {
                        // This case handles true multidimensional arrays.
                        // For example, in C#: string[,] myArray
                        sb.Append('[');
                        for (int i = 0; i < array.Rank; i++)
                        {
                            if (i != 0)
                            {
                                sb.Append(',');
                            }

                            // The following appears to be consistent with MS C# compiler output.
                            sb.AppendFormat(CultureInfo.InvariantCulture, "{0}:", array.GetLowerBound(i));
                            if (array.GetSize(i) != 0)
                            {
                                sb.AppendFormat(CultureInfo.InvariantCulture, "{0}", array.GetSize(i));
                            }
                        }
                        sb.Append(']');
                    }
                    break;

                /* Strange types (typically from C++/CLI) */

                case NodeType.FunctionPointer:
                    FunctionPointer funcPointer = (FunctionPointer)type;
                    sb.Append("=FUNC:");
                    sb.Append(GetStringForTypeNode(funcPointer.ReturnType,
                      typeTemplateParameters, memberTemplateParameters));
                    if (funcPointer.ParameterTypes.Count != 0)
                    {
                        bool comma = false;
                        sb.Append('(');
                        foreach (TypeNode parameterType in funcPointer.ParameterTypes)
                        {
                            if (comma)
                            {
                                sb.Append(',');
                            }
                            sb.Append(GetStringForTypeNode(parameterType,
                              typeTemplateParameters, memberTemplateParameters));
                            comma = true;
                        }
                        sb.Append(')');
                    }
                    else
                    {
                        // Inconsistent with documentation: based on MS C++ compiler output.
                        sb.Append("(System.Void)");
                    }
                    break;
                case NodeType.RequiredModifier:
                    RequiredModifier reqModifier = (RequiredModifier)type;
                    sb.Append(GetStringForTypeNode(reqModifier.ModifiedType,
                      typeTemplateParameters, memberTemplateParameters));
                    sb.Append("|");
                    sb.Append(GetStringForTypeNode(reqModifier.Modifier,
                      typeTemplateParameters, memberTemplateParameters));
                    break;
                case NodeType.OptionalModifier:
                    OptionalModifier optModifier = (OptionalModifier)type;
                    sb.Append(GetStringForTypeNode(optModifier.ModifiedType,
                      typeTemplateParameters, memberTemplateParameters));
                    sb.Append("!");
                    sb.Append(GetStringForTypeNode(optModifier.Modifier,
                      typeTemplateParameters, memberTemplateParameters));
                    break;

                default:
                    throw new ArgumentException("Unsupported NodeType.", "type");
            }

            if (type.IsGeneric && type.TemplateArguments.Count != 0)
            {
                // Undocumented: based on output from MS compilers.
                sb.Append('{');
                bool comma = false;
                foreach (TypeNode templateArgumentType in type.TemplateArguments)
                {
                    if (comma)
                    {
                        sb.Append(',');
                    }
                    sb.Append(GetStringForTypeNode(templateArgumentType,
                      typeTemplateParameters, memberTemplateParameters));
                    comma = true;
                }
                sb.Append('}');
            }

            return sb.ToString();
        }

        // Prevent instantiation of this class; all members are static.
        private DocComments()
        {
        }
    }
}