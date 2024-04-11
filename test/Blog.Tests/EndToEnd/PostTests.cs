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
  public async Task Post_WhenNavigatedToAndPostDoesNotExist_ItShouldDisplay404()
  {
    await Page.GotoAsync("/non-existent-post");
    Page.Url.Should().Contain("/Error/404");
  }
}