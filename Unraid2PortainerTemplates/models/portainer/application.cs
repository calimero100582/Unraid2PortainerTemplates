using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Unraid2PortainerTemplates.models.portainer
{
    public class application
    {
        [DefaultValue(null)]
        public int type { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string image { get; set; }
        [DefaultValue(false)]
        public bool administrator_only { get; set; } = true;
        public string name { get; set; }
        public string logo { get; set; }
        public string registry { get; set; }
        public string command { get; set; }
        public List<env_variable> env { get; set; } = new List<env_variable>();
        public string network { get; set; }
        public List<volume> volumes { get; set; } = new List<volume>();
        public List<string> ports { get; set; } = new List<string>();
        public List<KeyValuePair<string, string>> labels { get; set; } = new List<KeyValuePair<string, string>>();
        public bool privileged { get; set; }
        public bool interactive { get; set; }
        public string restart_policy { get; set; } = restart_policy_enum.always.ToString();
        public string hostname { get; set; }
        public string note { get; set; }
        public string platform { get; set; }
        public List<string> categories { get; set; } = new List<string>();
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
        public bool? read { get; set; }
    }

    public class env_variable
    {
        public string name { get; set; }
        public string label { get; set; }
        public string description { get; set; }
        [JsonPropertyName("default")]
        public string value {get;set;}
    }
}
