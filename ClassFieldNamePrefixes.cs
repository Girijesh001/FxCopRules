using Microsoft.FxCop.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyRules
{
    class ClassFieldNamePrefixes : BaseIntrospectionRule
    {
        public ClassFieldNamePrefixes() :
            base("ClassFieldNamePrefixes", "MyRules.Connection", typeof(ClassFieldNamePrefixes).Assembly)
        {
        }

        public override ProblemCollection Check(Member member)
        {
            if (!(member.DeclaringType is ClassNode))
                return this.Problems;

            Field fld = member as Field;
            if (fld == null)
                return this.Problems;

            if (fld.IsStatic && !fld.Name.Name.StartsWith("g_", StringComparison.Ordinal) &&
              fld.Name.Name != "__ENCList")
            {
                this.Problems.Add(new Problem(this.GetNamedResolution("Static", fld.Name.Name)));
            }
            else if (!fld.Name.Name.StartsWith("m_", StringComparison.Ordinal) &&
              fld.Name.Name != "__ENCList" && fld.Name.Name != "components")
            {
                this.Problems.Add(new Problem(this.GetNamedResolution("Instance", fld.Name.Name)));
            }

            return this.Problems;
        }
    }
}
