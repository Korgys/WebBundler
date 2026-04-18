using WebBundler.Core;

namespace WebBundler.Minification;

public static class DefaultAssetMinifiers
{
    public static IReadOnlyCollection<IAssetMinifier> Create() =>
    [
        new CssAssetMinifier(),
        new JavaScriptAssetMinifier()
    ];
}
