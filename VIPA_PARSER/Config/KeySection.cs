using System;

namespace VIPA_PARSER.Config
{
    [Serializable]
    public class ConfigurationSection
    {
        public VIPACommandSettings vipaCommandSettings { get; internal set; } = new VIPACommandSettings();
    }
}
