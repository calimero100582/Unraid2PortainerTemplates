using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Unraid2PortainerTemplates.models;
using Unraid2PortainerTemplates.models.portainer;

namespace Unraid2PortainerTemplates.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UnraidConvertController : ControllerBase
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<UnraidConvertController> _logger;

        public UnraidConvertController(ILogger<UnraidConvertController> logger, IMemoryCache memoryCache)
        {
            _logger = logger;
            _memoryCache = memoryCache;
        }

        [HttpGet]
        public portainer_template Portainer()
        {
            return _memoryCache.GetOrCreate("PortainerTemplates", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6);
                using (HttpClient client = new HttpClient())
                {
                    var unraid_data_string = client.GetStringAsync("https://raw.githubusercontent.com/Squidly271/AppFeed/master/applicationFeed.json").GetAwaiter().GetResult();
                    var unraid_data = System.Text.Json.JsonDocument.Parse(unraid_data_string);

                    var result = new portainer_template
                    {
                        version = "2",
                        templates = unraid_data.RootElement.GetProperty("applist").EnumerateArray()
                            .Where(app => app.TryGetProperty("Repository", out JsonElement dummy))
                            .Where(app =>
                            {

                                if (app.TryGetProperty("Blacklist", out JsonElement Blacklist))
                                {
                                    if (Blacklist.GetBoolean())
                                    {
                                        return false;
                                    }
                                }
                                if (app.TryGetProperty("CABlacklist", out JsonElement CABlacklist))
                                {
                                    if (CABlacklist.GetBoolean())
                                    {
                                        return false;
                                    }
                                }
                                if (app.TryGetProperty("Deprecated", out JsonElement Deprecated))
                                {
                                    if (Deprecated.GetBoolean())
                                    {
                                        return false;
                                    }
                                }

                                return true;
                            })
                            .Select(app =>
                            {
                                var result = new models.portainer.application
                                {
                                    type = 1,
                                    platform = platform_enum.linux.ToString(),
                                };

                                StringBuilder note_builder = new();

                                note_builder.AppendLine("Credits to <a href='https://forums.unraid.net/topic/38582-plug-in-community-applications/' target='_blank'>Unraid Community Apps</a><br />");

                                foreach (var property in app.EnumerateObject())
                                {
                                    switch (property.Name)
                                    {
                                        case "Name":
                                            result.title = property.Value.GetString().ToLowerInvariant();
                                            result.name = property.Value.GetString();
                                            break;
                                        case "Overview":
                                            result.description = property.Value.GetString();
                                            break;
                                        case "Repository":
                                            result.image = property.Value.GetString();
                                            break;
                                        case "Icon":
                                            result.logo = property.Value.GetString();
                                            break;
                                        /*case "Registry":
                                            result.registry = property.Value.GetString();
                                            break;*/
                                        case "Privileged":
                                            result.privileged = property.Value.GetString() == "true";
                                            break;
                                        case "CategoryList":
                                            result.categories = property.Value.EnumerateArray().Select(c => c.GetString()).ToList();
                                            break;
                                        case "Config":
                                            IEnumerable<JsonElement> config = property.Value.ValueKind == JsonValueKind.Object ? new JsonElement[] { property.Value } : property.Value.EnumerateArray();

                                            result.env.AddRange(config.Where(c => c.GetProperty("@attributes").GetProperty("Type").GetString() == "Variable").Select(c => new env_variable
                                            {
                                                name = c.GetProperty("@attributes").GetProperty("Target").GetString(),
                                                label = c.GetProperty("@attributes").GetProperty("Name").GetString(),
                                                value = string.IsNullOrEmpty(c.GetProperty("@attributes").GetProperty("Default").GetString()) ? c.GetProperty("value").GetString() : c.GetProperty("@attributes").GetProperty("Default").GetString(),
                                                description = c.GetProperty("@attributes").GetProperty("Description").GetString(),
                                            }));

                                            result.volumes.AddRange(config.Where(c => c.GetProperty("@attributes").GetProperty("Type").GetString() == "Path").Select(c => new volume
                                            {
                                                container = c.GetProperty("@attributes").GetProperty("Target").GetString(),
                                                bind = string.IsNullOrEmpty(c.GetProperty("@attributes").GetProperty("Default").GetString()) ? c.GetProperty("value").GetString() : c.GetProperty("@attributes").GetProperty("Default").GetString(),
                                                read = c.GetProperty("@attributes").GetProperty("Mode").GetString() != "rw",
                                            }));

                                            result.ports.AddRange(config.Where(c => c.GetProperty("@attributes").GetProperty("Type").GetString() == "Port").Select(c =>
                                            {
                                                var host = string.IsNullOrEmpty(c.GetProperty("@attributes").GetProperty("Default").GetString()) ? c.GetProperty("value").GetString() : c.GetProperty("@attributes").GetProperty("Default").GetString();
                                                return $@"{host}:{c.GetProperty("@attributes").GetProperty("Target").GetString()}/{c.GetProperty("@attributes").GetProperty("Mode").GetString()}";
                                            }));
                                            break;
                                        case "Environment":
                                            IEnumerable<JsonElement> environments = property.Value.ValueKind == JsonValueKind.Array ? property.Value.EnumerateArray() : new JsonElement[] { property.Value };

                                            foreach (var env in environments)
                                            {
                                                if (env.ValueKind == JsonValueKind.Object && env.TryGetProperty("Variable", out JsonElement variable))
                                                {
                                                    IEnumerable<JsonElement> variables = variable.ValueKind == JsonValueKind.Object ? new JsonElement[] { variable } : variable.EnumerateArray();

                                                    result.env.AddRange(variables.Select(c => new env_variable
                                                    {
                                                        name = c.GetProperty("Name").GetString(),
                                                        label = c.GetProperty("Name").GetString(),
                                                        value = c.GetProperty("Value").GetString()
                                                    }));
                                                }
                                            }
                                            break;
                                        case "Network":
                                            if (property.Value.ValueKind == JsonValueKind.Object && property.Value.TryGetProperty("@attributes", out JsonElement attributes))
                                            {
                                                JsonElement defaultValue;
                                                if (!attributes.TryGetProperty("Default", out defaultValue))
                                                {
                                                    defaultValue = attributes.GetProperty("default");
                                                }

                                                result.network = string.IsNullOrEmpty(defaultValue.GetString()) ? property.Value.GetProperty("value").GetString() : defaultValue.GetString();
                                            }
                                            else
                                            {
                                                result.network = property.Value.GetString();
                                            }

                                            break;
                                        case "Networking":
                                            if (property.Value.ValueKind == JsonValueKind.Object)
                                            {
                                                if (property.Value.TryGetProperty("Publish", out JsonElement publish) && publish.ValueKind == JsonValueKind.Object)
                                                {
                                                    var port = publish.GetProperty("Port");

                                                    if (port.ValueKind == JsonValueKind.Object)
                                                    {
                                                        StringBuilder port_builder = new();

                                                        if (port.TryGetProperty("HostPort", out JsonElement hostport))
                                                        {
                                                            port_builder.Append($@"{port.GetProperty("HostPort").GetString()}:");
                                                        }

                                                        port_builder.Append($@"{port.GetProperty("ContainerPort").GetString()}/{port.GetProperty("Protocol").GetString()}");

                                                        result.ports.Add(port_builder.ToString());
                                                    }
                                                    else
                                                    {
                                                        result.ports.AddRange(port.EnumerateArray().Select(c =>
                                                        {
                                                            StringBuilder port_builder = new();

                                                            if (c.TryGetProperty("HostPort", out JsonElement hostport))
                                                            {
                                                                port_builder.Append($@"{hostport.GetString()}:");
                                                            }

                                                            port_builder.Append($@"{c.GetProperty("ContainerPort").GetString()}/{c.GetProperty("Protocol").GetString()}");

                                                            return port_builder.ToString();
                                                        }));
                                                    }
                                                }
                                                result.network = property.Value.GetProperty("Mode").GetString();
                                            }
                                            break;
                                        case "Data":
                                            if (property.Value.ValueKind == JsonValueKind.Object)
                                            {
                                                var volume = property.Value.GetProperty("Volume");
                                                IEnumerable<JsonElement> volumes = volume.ValueKind == JsonValueKind.Object ? new JsonElement[] { volume } : volume.EnumerateArray();

                                                result.volumes.AddRange(volumes.Select(c => new volume
                                                {
                                                    container = c.GetProperty("ContainerDir").GetString(),
                                                    bind = c.GetProperty("HostDir").GetString(),
                                                    read = c.GetProperty("Mode").GetString() != "rw",
                                                }));
                                            }
                                            break;

                                        case "downloads":
                                        case "stars":
                                        case "trending":
                                        case "trends":
                                        case "trendsDate":
                                        case "downloadtrend":
                                        case "LastUpdateScan":
                                        case "LastUpdate":
                                        case "FirstSeen":
                                        case "topTrending":
                                        case "topPerforming":
                                        case "BindTime":
                                            break;
                                        case "Registry":
                                            var registryUrl = new Uri(property.Value.GetString());
                                            if (!registryUrl.Host.ToLowerInvariant().Contains("hub.docker.com"))
                                            {
                                                //result.registry = $"{registryUrl.Host}:{registryUrl.Port}";
                                            }

                                            note_builder.AppendLine($@"{property.Name}: <a href='{property.Value.GetString()}' target='_blank'>{property.Value.GetString()}</a><br />");
                                            break;
                                        default:
                                            if (property.Value.ValueKind == JsonValueKind.String && property.Value.GetString().StartsWith("http"))
                                            {
                                                note_builder.AppendLine($@"{property.Name}: <a href='{property.Value.GetString()}' target='_blank'>{property.Value.GetString()}</a><br />");

                                            }
                                            else
                                            {
                                                note_builder.AppendLine($@"{property.Name}: {property.Value.GetRawText()}<br />");
                                            }
                                            break;
                                    }
                                }

                                result.note = note_builder.ToString();

                                return result;
                            }).ToList()
                    };

                    return result;
                }
            });

        }
    }
}
