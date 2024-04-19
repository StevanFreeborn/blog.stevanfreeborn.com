namespace Blog.Tests.EndToEnd;

[TestFixture]
public class RssTests : BlogTest
{
  [Test]
  public async Task Rss_WhenFetched_ItShouldReturnRssFeed()
  {
    var response = await Page.APIRequest.GetAsync("/rss");
    response.Status.Should().Be((int)HttpStatusCode.OK);

    var xml = await response.BodyAsync();

    var streamReader = new StreamReader(
      new MemoryStream(xml),
      Encoding.UTF8
    );


    var settings = new XmlReaderSettings
    {
      Async = true
    };

    using var xmlReader = XmlReader.Create(streamReader, settings);
    var feed = SyndicationFeed.Load(xmlReader);

    feed.Title.Text.Should().Be("journal");
    feed.Description.Text.Should().Be("A blog by Stevan Freeborn");
    feed.Items.Should().HaveCount(2);
  }
}