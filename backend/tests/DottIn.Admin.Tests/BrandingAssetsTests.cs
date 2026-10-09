using System.Xml.Linq;

namespace DottIn.Admin.Tests;

public sealed class BrandingAssetsTests
{
    [Fact]
    public void SvgKitIsLocalOutlinedAndContainsNoExecutableOrExternalContent()
    {
        var root = BackendRoot();
        var assets = Directory.GetFiles(Path.Combine(root, "clients/DottIn.Admin/wwwroot/branding"), "*.svg");
        Assert.Equal(8, assets.Length);
        foreach (var asset in assets)
        {
            var svg = XDocument.Load(asset);
            Assert.Equal("svg", svg.Root!.Name.LocalName);
            Assert.NotNull(svg.Root.Attribute("viewBox"));
            Assert.DoesNotContain(svg.Descendants(), element =>
                element.Name.LocalName is "script" or "foreignObject" or "image" or "text");
            Assert.DoesNotContain(svg.Descendants().Attributes(), attribute =>
                attribute.Name.LocalName.StartsWith("on", StringComparison.OrdinalIgnoreCase) ||
                attribute.Name.LocalName == "href");
        }
        var white = XDocument.Load(Path.Combine(root, "clients/DottIn.Admin/wwwroot/branding/DottIn_White.svg"));
        Assert.DoesNotContain(white.Descendants(), element => element.Name.LocalName == "rect");
        Assert.Contains(white.Descendants().Attributes("fill"), attribute => attribute.Value == "#FFFFFF");
    }

    [Fact]
    public void MobilePackagesTheSameVectorKitAndUsesBrandedNativeIcon()
    {
        var root = BackendRoot();
        var project = File.ReadAllText(Path.Combine(root, "clients/DottIn.Mobile/DottIn.Mobile.csproj"));
        Assert.Contains("DottIn.Admin\\wwwroot\\branding\\*.svg", project);
        Assert.Contains("ForegroundScale=\"0.65\"", project);
        var native = File.ReadAllText(Path.Combine(root, "clients/DottIn.Mobile/Resources/AppIcon/appiconfg.svg"));
        Assert.Contains("#14B8A6", native);
        Assert.Contains("#FFFFFF", native);
        var layout = File.ReadAllText(Path.Combine(root, "clients/DottIn.Admin/Layout/MainLayout.razor"));
        Assert.Contains("<DottInLogo Dark=\"@State.IsDarkMode\"", layout);
    }

    private static string BackendRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (Directory.Exists(Path.Combine(directory.FullName, "clients/DottIn.Admin/wwwroot/branding")))
                return directory.FullName;
        throw new DirectoryNotFoundException("Backend source directory not found.");
    }
}
