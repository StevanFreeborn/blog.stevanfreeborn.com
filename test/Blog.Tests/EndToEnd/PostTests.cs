namespace Blog.Tests.EndToEnd;

[TestFixture]
public class PostTests : BlogTest
{
  private const string TestPostSlug = "test-blog";
  private const string TestPostTitle = "Test Blog";
  private const string TestPostDate = "January 1, 2021";

  [Test]
  public async Task Post_WhenNavigatedTo_ItShouldDisplayCorrectPageTitle()
  {
    await Page.GotoAsync($"/{TestPostSlug}");
    var title = await Page.TitleAsync();
    title.Should().Be($"{TestPostTitle} - journal");
  }

  [Test]
  public async Task Post_WhenNavigatedTo_ItShouldPostTitle()
  {
    await Page.GotoAsync($"/{TestPostSlug}");
    var heading = Page.GetByRole(AriaRole.Heading, new() { Name = TestPostTitle });
    await Expect(heading).ToBeVisibleAsync();
  }

  [Test]
  public async Task Post_WhenNavigatedTo_ItShouldDisplayPostDate()
  {
    await Page.GotoAsync($"/{TestPostSlug}");
    var date = Page.GetByText(TestPostDate);
    await Expect(date).ToBeVisibleAsync();
  }

  [Test]
  public async Task Post_WhenNavigatedTo_ItShouldDisplayContent()
  {
    await Page.GotoAsync($"/{TestPostSlug}");
    var content = Page.GetByRole(AriaRole.Article);
    await Expect(content).ToBeVisibleAsync();
  }

  [Test]
  public async Task Post_WhenNavigatedToAndPostContainsCodeBlock_ItShouldDisplayCopyButton()
  {
    await Page.Context.GrantPermissionsAsync(["clipboard-read"]);

    await Page.GotoAsync($"/{TestPostSlug}");
    var copyButton = Page.GetByRole(AriaRole.Button, new() { Name = "Copy" });
    await Expect(copyButton).ToBeVisibleAsync();

    await copyButton.ClickAsync();
    var clipboard = await Page.EvaluateAsync<string>("navigator.clipboard.readText()");
    clipboard.Should().Be(
      """
      def greet(name):
        print(f"Hello, {name}!")

      greet("World")
      """
    );

    await Page.Context.ClearPermissionsAsync();
  }

  [Test]
  public async Task Post_WhenNavigatedToAndPostContainsHeadings_ItShouldDisplayLinkToHeading()
  {
    await Page.GotoAsync($"/{TestPostSlug}");
    var heading = Page.GetByRole(AriaRole.Heading, new() { Name = "Text Formatting" });
    await Expect(heading).ToBeVisibleAsync();

    var link = heading.GetByRole(AriaRole.Link);
    await Expect(link).ToBeVisibleAsync();

    await link.ClickAsync();
    Page.Url.Should().EndWith("#text-formatting");
  }

  [Test]
  public async Task Post_WhenNavigatedToAndPostDoesNotExist_ItShouldDisplay404()
  {
    await Page.GotoAsync("/non-existent-post");
    Page.Url.Should().Contain("/Error/404");
  }
}