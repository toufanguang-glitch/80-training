using System.Text.RegularExpressions;

namespace OrderHub.Tests;

public class OrderListViewTests
{
    [Fact]
    public void OrderListView_SortIndicatorsInHeaders_UseExplicitRazorExpressions()
    {
        var repositoryRoot = FindRepositoryRoot();
        var viewPath = Path.Combine(repositoryRoot, "src", "OrderHub.Web", "Views", "Orders", "Index.cshtml");
        var markup = File.ReadAllText(viewPath);

        Assert.Equal(6, Regex.Matches(markup, @"@\(SortIndicator\(").Count);
        Assert.DoesNotContain("編號@SortIndicator", markup);
        Assert.DoesNotContain("客戶@SortIndicator", markup);
        Assert.DoesNotContain("狀態@SortIndicator", markup);
        Assert.DoesNotContain("金額@SortIndicator", markup);
        Assert.DoesNotContain("品項數@SortIndicator", markup);
        Assert.DoesNotContain("建立時間@SortIndicator", markup);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "OrderHub.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find the repository root.");
    }
}
