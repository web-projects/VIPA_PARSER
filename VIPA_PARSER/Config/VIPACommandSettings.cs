using System;
using System.Collections.Generic;

namespace VIPA_PARSER.Config
{
    [Serializable]
    public class VIPACommandSettings
    {
        public List<string> VIPADataGroup { get; internal set; } = new List<string>();
    }
}
