namespace Blog.Tests.EndToEnd;

[TestFixture]
public class IndexTests : BlogPageTest
{
  [Test]
  public async Task Index_WhenNavigatedTo_ItShouldDisplayMessage()
  {
    await Page.GotoAsync("/");
    var message = Page.GetByRole(AriaRole.Heading, new () { Name = "Hello, world!" });
    await Expect(message).ToBeVisibleAsync();
  }
}