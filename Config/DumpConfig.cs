namespace UniSentinel.Config;

public class DumpConfig
{
    [JsonPropertyName("excludeDirs")]
    public List<string> ExcludeDirs { get; set; } = [];

    [JsonPropertyName("excludeExtensions")]
    public List<string> ExcludeExtensions { get; set; } = [];
}

[JsonSerializable(typeof(DumpConfig))]
internal sealed partial class DumpConfigJsonContext : JsonSerializerContext { }
