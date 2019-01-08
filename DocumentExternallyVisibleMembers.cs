using Microsoft.FxCop.Sdk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace MyRules
{
    class DocumentExternallyVisibleMembers : BaseIntrospectionRule
    {
        private Dictionary<String, bool> m_ProcessedAssemblies =
         new Dictionary<String, bool>();

        private Dictionary<String, XmlElement> m_DocumentedMemberIds =
          new Dictionary<String, XmlElement>();
        public DocumentExternallyVisibleMembers() :
            base("DocumentExternallyVisibleMembers", "MyRules.Connection", typeof(DocumentExternallyVisibleMembers).Assembly)
        {
        }
        public override TargetVisibilities TargetVisibility
        {
            get
            {
                return TargetVisibilities.ExternallyVisible;
            }
        }

        public override ProblemCollection Check(Member member)
        {
            AssemblyNode assembly = member.DeclaringType.DeclaringModule.ContainingAssembly;
            bool assemblyHasDocComments;

            // Does not apply to special members (such as get_ and set_ members),
            // with the exception of constructors.
            if (member.IsSpecialName && member.NodeType != NodeType.InstanceInitializer)
            {
                return this.Problems;
            }

            // Have we loaded the XML for this assembly yet?

            if (!m_ProcessedAssemblies.TryGetValue(assembly.Name, out assemblyHasDocComments))
            {
                // XML not loaded yet for this assembly. Determine
                // the path to the possible XML documentation file.

                string path = Path.Combine(Path.GetDirectoryName(assembly.Location),
                  Path.GetFileNameWithoutExtension(assembly.Location)) + ".xml";

                // If the file exists, load the XML.

                if (File.Exists(path))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(path);

                    // Put the member ID strings of the documented members
                    // into the dictionary.

                    foreach (XmlElement element in doc.SelectNodes("/doc/members/member"))
                    {
                        m_DocumentedMemberIds.Add(element.Attributes["name"].Value, element);
                    }

                    assemblyHasDocComments = true;
                }
                else
                {
                    assemblyHasDocComments = false;
                }

                m_ProcessedAssemblies.Add(assembly.Name, assemblyHasDocComments);
            }

            // If assembly has an XML documentation file present, then its members are
            // eligible for checking to see if they have documentation.

            if (assemblyHasDocComments)
            {
                string memberId =DocComments.GetMemberID(member);

                if (!m_DocumentedMemberIds.ContainsKey(memberId))
                {
                    this.Problems.Add(new Problem(this.GetResolution()));
                }
            }

            return this.Problems;
        }
    }
}
