using System.Text.Json;
using WebBundler.Core;

namespace WebBundler.Configuration;

public sealed class BundleConfigurationLoader
{
    private readonly JsonSerializerOptions serializerOptions;

    public BundleConfigurationLoader()
    {
        serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Disallow,
            AllowTrailingCommas = false
        };
        serializerOptions.Converters.Add(new BundleTypeJsonConverter());
    }

    public BundleConfigurationLoadResult Load(string path)
    {
        var messages = new List<BuildMessage>();

        if (!File.Exists(path))
        {
            return new BundleConfigurationLoadResult(
                null,
                [new BuildMessage(BuildSeverity.Error, $"Configuration file '{path}' was not found.", Path: path)]);
        }

        try
        {
            var json = File.ReadAllText(path);
            var configuration = JsonSerializer.Deserialize<BundleConfigurationDocument>(json, serializerOptions);

            if (configuration is null)
            {
                messages.Add(new BuildMessage(BuildSeverity.Error, "The configuration file could not be parsed."));
            }

            return new BundleConfigurationLoadResult(configuration, messages);
        }
        catch (JsonException ex)
        {
            messages.Add(new BuildMessage(BuildSeverity.Error, $"Invalid JSON in '{path}': {ex.Message}", Path: path));
            return new BundleConfigurationLoadResult(null, messages);
        }
        catch (IOException ex)
        {
            messages.Add(new BuildMessage(BuildSeverity.Error, $"Unable to read '{path}': {ex.Message}", Path: path));
            return new BundleConfigurationLoadResult(null, messages);
        }
    }
}
