using Microsoft.FxCop.Sdk;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace MyRules
{
    class SpellCheckStringResources : BaseIntrospectionRule
    {
        public SpellCheckStringResources() :
            base("SpellCheckStringResources", "MyRules.Connection",
          typeof(SpellCheckStringResources).Assembly)
        {
        }

        public override ProblemCollection Check(Resource resource)
        {
            using (MemoryStream mstream = new MemoryStream(resource.Data, false))
            {
                using (ResourceReader reader = new ResourceReader(mstream))
                {
                    foreach (DictionaryEntry entry in reader)
                    {
                        string s = entry.Value as string;
                        if (s != null)
                        {
                            foreach (String word in WordParser.Parse(s, WordParserOptions.None))
                            {
                                if (NamingService.DefaultNamingService.CheckSpelling(word) !=
                                  WordSpelling.SpelledCorrectly)
                                {
                                    this.Problems.Add(new Problem(this.GetResolution(word), word));
                                }
                            }
                        }
                    }
                }
            }

            return this.Problems;
        }
    }
}
