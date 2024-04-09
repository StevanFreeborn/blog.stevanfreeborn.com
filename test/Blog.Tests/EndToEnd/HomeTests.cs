namespace Blog.Tests.EndToEnd;

[TestFixture]
public class HomeTests : BlogTest
{
  [Test]
  public async Task Home_WhenNavigatedTo_ItShouldDisplayCorrectPageTitle()
  {
    await Page.GotoAsync("/");
    var title = await Page.TitleAsync();
    title.Should().Be("journal - A blog by Stevan Freeborn");
  }

  [Test]
  public async Task Home_WhenNavigatedTo_ItShouldCorrectSiteTitle()
  {
    await Page.GotoAsync("/");
    var message = Page.GetByRole(AriaRole.Heading, new () { Name = "journal" });
    await Expect(message).ToBeVisibleAsync();
  }

  [Test]
  public async Task Home_WhenNavigatedTo_ItShouldDisplayWrittenByAttribution()
  {
    await Page.GotoAsync("/");
    var message = Page.GetByText("by");
    var image = Page.GetByAltText("Stevan Freeborn");

    await Expect(message).ToBeVisibleAsync();
    await Expect(image).ToBeVisibleAsync();
  }
}