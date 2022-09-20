using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Unraid2PortainerTemplates.models.unraid
{
    public class application
    {
        [DefaultValue(null)]
        public string type { get; set; } = "1";
        public string title { get; set; }
        public string description { get; set; }
        public string image { get; set; }
        [DefaultValue(false)]
        public bool administrator_only { get; set; } = true;
        public string name { get; set; }
        public string logo { get; set; }
        public string registry { get; set; }
        public string command { get; set; }
        public IEnumerable<env_variable> env { get; set; }
        public string network { get; set; }
        public IEnumerable<volume> volumes { get; set; }
        public IEnumerable<string> ports { get; set; }
        public IEnumerable<KeyValuePair<string, string>> labels { get; set; }
        public bool privileged { get; set; }
        public bool interactive { get; set; }
        public restart_policy_enum restart_policy { get; set; } = restart_policy_enum.always;
        public string hostname { get; set; }
        public string note { get; set; }
        public platform_enum platform { get; set; }
        public IEnumerable<string> categories { get; set; }
    }

    public enum platform_enum
    {
        empty,
        linux,
        windows
    }

    public enum restart_policy_enum
    {
        always,
        no,
        [EnumMember(Value = "on-failure")]
        onfailure,
        [EnumMember(Value = "unless-stopped")]
        unlessstopped
    }

    public class volume
    {
        public string container { get; set; }
        public string bind { get; set; }
        [JsonPropertyName("readonly")]
        public bool read { get; set; }
    }

    public class env_variable
    {
        public string name { get; set; }
        public string label { get; set; }
        public string description { get; set; }
        [JsonPropertyName("default")]
        public string value { get; set; }
        public bool preset { get; set; }
        public IEnumerable<env_variable_choice> select { get; set; }
    }

    public class env_variable_choice
    {
        public string text { get; set; }
        public string value { get; set; }
        [JsonPropertyName("default")]
        public bool selected { get; set; }
    }
}
