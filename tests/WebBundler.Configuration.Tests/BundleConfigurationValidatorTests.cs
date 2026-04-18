using WebBundler.Configuration;
using WebBundler.Core;
using Xunit;

namespace WebBundler.Configuration.Tests;

public sealed class BundleConfigurationValidatorTests
{
    [Fact]
    public void AcceptsAValidConfiguration()
    {
        var validator = new BundleConfigurationValidator();
        var result = validator.Validate(new BundleConfigurationDocument
        {
            Version = 1,
            Bundles =
            [
                new AssetBundleDefinition
                {
                    Output = "wwwroot/dist/site.min.css",
                    Inputs = ["wwwroot/css/site.css"],
                    Type = BundleType.Css
                }
            ]
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void RejectsDuplicateOutputs()
    {
        var validator = new BundleConfigurationValidator();
        var result = validator.Validate(new BundleConfigurationDocument
        {
            Version = 1,
            Bundles =
            [
                new AssetBundleDefinition
                {
                    Output = "wwwroot/dist/site.min.css",
                    Inputs = ["wwwroot/css/site.css"],
                    Type = BundleType.Css
                },
                new AssetBundleDefinition
                {
                    Output = "wwwroot/dist/site.min.css",
                    Inputs = ["wwwroot/css/theme.css"],
                    Type = BundleType.Css
                }
            ]
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Messages, message => message.Severity == BuildSeverity.Error);
    }
}
