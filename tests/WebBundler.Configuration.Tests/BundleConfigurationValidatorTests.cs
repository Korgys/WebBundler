using WebBundler.Configuration;
using WebBundler.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebBundler.Configuration.Tests;

[TestClass]
public sealed class BundleConfigurationValidatorTests
{
    [TestMethod]
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

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
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

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Messages.Any(message => message.Severity == BuildSeverity.Error));
    }

    [TestMethod]
    public void RejectsEmptyBundleCollections()
    {
        var validator = new BundleConfigurationValidator();
        var result = validator.Validate(new BundleConfigurationDocument
        {
            Version = 1,
            Bundles = []
        });

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Messages.Any(message => message.Severity == BuildSeverity.Error));
    }
}
