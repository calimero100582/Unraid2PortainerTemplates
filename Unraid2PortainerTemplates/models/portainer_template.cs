using System.Collections.Generic;

namespace Unraid2PortainerTemplates.models
{
    public class portainer_template
    {
        public string version { get; set; }
        public List<models.portainer.application> templates { get; set; }
    }
}
