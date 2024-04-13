namespace Blog.Tests.Unit;

[TestFixture]
class FilePostServiceTests
{
  private readonly Mock<IFileSystem> _mockFileSystem = new();
  private readonly Mock<IOptions<FilePostServiceOptions>> _mockOptions = new();
  private readonly Mock<ILogger<FilePostService>> _mockLogger = new();
  private readonly FilePostService _sut;

  public FilePostServiceTests()
  {
    _mockOptions
      .Setup(x => x.Value)
      .Returns(new FilePostServiceOptions { PostsDirectory = "posts" });

    _sut = new FilePostService(
      _mockOptions.Object,
      _mockFileSystem.Object,
      _mockLogger.Object
    );
  }

  [Test]
  public async Task GetPostsAsync_WhenCalledAndPostsDirectoryDoesNotExist_ItShouldReturnEmptyList()
  {
    _mockFileSystem
      .Setup(x => x.Directory.Exists(It.IsAny<string>()))
      .Returns(false);

    var result = await _sut.GetPostsAsync();

    result.Should().BeEmpty();
  }

  [Test]
  public async Task GetPostsAsync_WhenCalledAndPostsDirectoryContainsNoSubDirectories_ItShouldReturnEmptyList()
  {
    _mockFileSystem
      .Setup(x => x.Directory.Exists(It.IsAny<string>()))
      .Returns(true);

    _mockFileSystem
      .Setup(x => x.Directory.GetDirectories(It.IsAny<string>()))
      .Returns([]);

    var result = await _sut.GetPostsAsync();

    result.Should().BeEmpty();
  }

  [Test]
  public async Task GetPostsAsync_WhenCalledAndPostsDirectoryContainsSubDirectoryWithNoIndexFile_ItShouldReturnEmptyList()
  {
    _mockFileSystem
      .Setup(x => x.Directory.Exists(It.IsAny<string>()))
      .Returns(true);

    _mockFileSystem
      .Setup(x => x.Directory.GetDirectories(It.IsAny<string>()))
      .Returns(["subdirectory"]);

    _mockFileSystem
      .Setup(x => x.Path.Combine(It.IsAny<string>(), It.IsAny<string>()))
      .Returns("subdirectory/INDEX.md");

    _mockFileSystem
      .Setup(x => x.File.Exists(It.IsAny<string>()))
      .Returns(false);

    var result = await _sut.GetPostsAsync();

    result.Should().BeEmpty();
  }

  [Test]
  public async Task GetPostsAsync_WhenCalledAndPostIndexFileIsEmpty_ItShouldReturnEmptyList()
  {
    _mockFileSystem
      .Setup(x => x.Directory.Exists(It.IsAny<string>()))
      .Returns(true);

    _mockFileSystem
      .Setup(x => x.Directory.GetDirectories(It.IsAny<string>()))
      .Returns(["subdirectory"]);

    _mockFileSystem
      .Setup(x => x.Path.Combine(It.IsAny<string>(), It.IsAny<string>()))
      .Returns("subdirectory/INDEX.md");

    _mockFileSystem
      .Setup(x => x.File.Exists(It.IsAny<string>()))
      .Returns(true);

    _mockFileSystem
      .Setup(x => x.File.ReadAllTextAsync(It.IsAny<string>(), default))
      .ReturnsAsync(string.Empty);

    var result = await _sut.GetPostsAsync();

    result.Should().BeEmpty();
  }

  [Test]
  public async Task GetPostsAsync_WhenCalledAndPostIndexFileContainsNoMetadata_ItShouldReturnEmptyList()
  {
    _mockFileSystem
      .Setup(x => x.Directory.Exists(It.IsAny<string>()))
      .Returns(true);

    _mockFileSystem
      .Setup(x => x.Directory.GetDirectories(It.IsAny<string>()))
      .Returns(["subdirectory"]);

    _mockFileSystem
      .Setup(x => x.Path.Combine(It.IsAny<string>(), It.IsAny<string>()))
      .Returns("subdirectory/INDEX.md");

    _mockFileSystem
      .Setup(x => x.File.Exists(It.IsAny<string>()))
      .Returns(true);

    _mockFileSystem
      .Setup(x => x.File.ReadAllTextAsync(It.IsAny<string>(), default))
      .ReturnsAsync(
        """
        # Post Title

        Post content
        """
      );

    var result = await _sut.GetPostsAsync();

    result.Should().BeEmpty();
  }

  [Test]
  public async Task GetPostsAsync_WhenCalledAndPostIndexFileOnlyContainsMetadata_ItShouldReturnEmptyList()
  {
    _mockFileSystem
      .Setup(x => x.Directory.Exists(It.IsAny<string>()))
      .Returns(true);

    _mockFileSystem
      .Setup(x => x.Directory.GetDirectories(It.IsAny<string>()))
      .Returns(["subdirectory"]);

    _mockFileSystem
      .Setup(x => x.Path.Combine(It.IsAny<string>(), It.IsAny<string>()))
      .Returns("subdirectory/INDEX.md");

    _mockFileSystem
      .Setup(x => x.File.Exists(It.IsAny<string>()))
      .Returns(true);

    _mockFileSystem
      .Setup(x => x.File.ReadAllTextAsync(It.IsAny<string>(), default))
      .ReturnsAsync(
        """
        ```json meta
        {
          "title": "Post Title",
          "date": "2021-01-01"
        }
        ```
        """
      );

    var result = await _sut.GetPostsAsync();

    result.Should().BeEmpty();
  }

  [Test]
  public async Task GetPostsAsync_WhenCalledAndPostIndexFileHasInvalidMetadata_ItShouldReturnEmptyList()
  {
    _mockFileSystem
      .Setup(x => x.Directory.Exists(It.IsAny<string>()))
      .Returns(true);

    _mockFileSystem
      .Setup(x => x.Directory.GetDirectories(It.IsAny<string>()))
      .Returns(["subdirectory"]);

    _mockFileSystem
      .Setup(x => x.Path.Combine(It.IsAny<string>(), It.IsAny<string>()))
      .Returns("subdirectory/INDEX.md");

    _mockFileSystem
      .Setup(x => x.File.Exists(It.IsAny<string>()))
      .Returns(true);

    _mockFileSystem
      .Setup(x => x.File.ReadAllTextAsync(It.IsAny<string>(), default))
      .ReturnsAsync(
        """
        # Post Title

        ```json meta
        {
          "title": "Post Title",
          "isPublished": "true",
          "date": "2021-01-01"
        }
        ```

        Post content
        """
      );

    var result = await _sut.GetPostsAsync();

    result.Should().BeEmpty();
  }

  [Test]
  public async Task GetPostsAsync_WhenCalledAndPostIndexFileIsNotPublished_ItShouldReturnEmptyList()
  {
    _mockFileSystem
      .Setup(x => x.Directory.Exists(It.IsAny<string>()))
      .Returns(true);

    _mockFileSystem
      .Setup(x => x.Directory.GetDirectories(It.IsAny<string>()))
      .Returns(["subdirectory"]);

    _mockFileSystem
      .Setup(x => x.Path.Combine(It.IsAny<string>(), It.IsAny<string>()))
      .Returns("subdirectory/INDEX.md");

    _mockFileSystem
      .Setup(x => x.File.Exists(It.IsAny<string>()))
      .Returns(true);

    _mockFileSystem
      .Setup(x => x.File.ReadAllTextAsync(It.IsAny<string>(), default))
      .ReturnsAsync(
        """
        # Post Title

        ```json meta
        {
          "title": "Post Title",
          "lead": "Post lead",
          "isPublished": false,
          "publishedAt": "2021-01-01"
        }
        ```

        Post content
        """
      );

    var result = await _sut.GetPostsAsync();

    result.Should().BeEmpty();
  }

  [Test]
  public async Task GetPostsAsync_WhenCalledAndPostIndexFileContainsMetadataAndContent_ItShouldReturnPosts()
  {
    _mockFileSystem
      .Setup(x => x.Directory.Exists(It.IsAny<string>()))
      .Returns(true);

    _mockFileSystem
      .Setup(x => x.Directory.GetDirectories(It.IsAny<string>()))
      .Returns(["subdirectory"]);

    _mockFileSystem
      .Setup(x => x.Path.Combine(It.IsAny<string>(), It.IsAny<string>()))
      .Returns("subdirectory/INDEX.md");

    _mockFileSystem
      .Setup(x => x.File.Exists(It.IsAny<string>()))
      .Returns(true);

    _mockFileSystem
      .Setup(x => x.File.ReadAllTextAsync(It.IsAny<string>(), default))
      .ReturnsAsync(
        """
        # Post Title

        ```json meta
        {
          "title": "Post Title",
          "lead": "Post lead",
          "isPublished": true,
          "publishedAt": "2021-01-01",
          "slug": "subdirectory"
        }
        ```

        Post content
        """
      );

    _mockFileSystem
      .Setup(x => x.Path.GetFileName(It.IsAny<string>()))
      .Returns("subdirectory");

    var result = await _sut.GetPostsAsync();

    result.Should().NotBeEmpty();
    result.Should().HaveCount(1);
    result.Should().BeEquivalentTo([
      new Post
      {
        Title = "Post Title",
        Lead = "Post lead",
        IsPublished = true,
        PublishedAt = new DateTime(2021, 1, 1),
        Slug = "subdirectory",
      }
    ]);
  }

  [Test]
  public async Task GetPostsAsync_WhenCalled_ItShouldValidPostsInDescendingOrder()
  {
    _mockFileSystem
      .Setup(x => x.Directory.Exists(It.IsAny<string>()))
      .Returns(true);

    _mockFileSystem
      .Setup(x => x.Directory.GetDirectories(It.IsAny<string>()))
      .Returns([
        "valid-blog-post",
        "another-valid-blog-post",
        "invalid-blog-post"
      ]);

    _mockFileSystem
      .SetupSequence(x => x.Path.Combine(It.IsAny<string>(), It.IsAny<string>()))
      .Returns("valid-blog-post/INDEX.md")
      .Returns("another-valid-blog-post/INDEX.md")
      .Returns("invalid-blog-post/INDEX.md");

    _mockFileSystem
      .Setup(x => x.File.Exists(It.IsAny<string>()))
      .Returns(true);

    _mockFileSystem
      .SetupSequence(x => x.File.ReadAllTextAsync(It.IsAny<string>(), default))
      .ReturnsAsync(
        """
        # Valid Blog Post

        ```json meta
        {
          "title": "Valid Blog Post",
          "lead": "Valid post lead",
          "isPublished": true,
          "publishedAt": "2021-01-01",
          "slug": "valid-blog-post"
        }
        ```

        Post content
        """
      )
      .ReturnsAsync(
        """
        # Another Valid Blog Post

        ```json meta
        {
          "title": "Another Valid Blog Post",
          "lead": "Another valid post lead",
          "isPublished": true,
          "publishedAt": "2022-01-02",
          "slug": "another-valid-blog-post"
        }
        ```

        Post content
        """
      )
      .ReturnsAsync(
        """
        ```json meta
        {
          "title": "Invalid Blog Post",
          "lead": "Invalid post lead",
          "isPublished": false,
          "publishedAt": "2023-01-03",
          "slug": "invalid-blog-post"
        }
        ```
        """
      );

    _mockFileSystem
      .SetupSequence(x => x.Path.GetFileName(It.IsAny<string>()))
      .Returns("valid-blog-post")
      .Returns("another-valid-blog-post")
      .Returns("invalid-blog-post");

    var result = await _sut.GetPostsAsync();

    result.Should().NotBeEmpty();
    result.Should().HaveCount(2);
    result.Should().BeEquivalentTo([
      new Post
      {
        Title = "Another Valid Blog Post",
        Lead = "Another valid post lead",
        IsPublished = true,
        PublishedAt = new DateTime(2022, 1, 2),
        Slug = "another-valid-blog-post",
      },
      new Post
      {
        Title = "Valid Blog Post",
        Lead = "Valid post lead",
        IsPublished = true,
        PublishedAt = new DateTime(2021, 1, 1),
        Slug = "valid-blog-post",
      }
    ]);
  }

  [Test]
  public async Task GetPostAsync_WhenCalledAndSlugDoesNotExist_ItShouldReturnNull()
  {
    _mockFileSystem
      .Setup(x => x.Directory.Exists(It.IsAny<string>()))
      .Returns(false);

    var result = await _sut.GetPostAsync("slug");

    result.Should().BeNull();
  }

  [Test]
  public async Task GetPostAsync_WhenCalledAndPostExistsButContainsNoMetaData_ItShouldReturnNull()
  {
    _mockFileSystem
      .Setup(x => x.Directory.Exists(It.IsAny<string>()))
      .Returns(true);

    _mockFileSystem
      .Setup(x => x.Path.Combine(It.IsAny<string>(), It.IsAny<string>()))
      .Returns("slug/INDEX.md");

    _mockFileSystem
      .Setup(x => x.File.Exists(It.IsAny<string>()))
      .Returns(true);

    _mockFileSystem
      .Setup(x => x.File.ReadAllTextAsync(It.IsAny<string>(), default))
      .ReturnsAsync("# Post Title");

    var result = await _sut.GetPostAsync("slug");

    result.Should().BeNull();
  }

  [Test]
  public async Task GetPostAsync_WhenCalledAndPostContainsNoContent_ItShouldReturnNull()
  {
    _mockFileSystem
      .Setup(x => x.Directory.Exists(It.IsAny<string>()))
      .Returns(true);

    _mockFileSystem
      .Setup(x => x.Path.Combine(It.IsAny<string>(), It.IsAny<string>()))
      .Returns("slug/INDEX.md");

    _mockFileSystem
      .Setup(x => x.File.Exists(It.IsAny<string>()))
      .Returns(true);

    _mockFileSystem
      .Setup(x => x.File.ReadAllTextAsync(It.IsAny<string>(), default))
      .ReturnsAsync(
        """
        ```json meta
        {
          "title": "Post Title",
          "date": "2021-01-01"
        }
        ```
        """
      );

    var result = await _sut.GetPostAsync("slug");

    result.Should().BeNull();
  }

  [Test]
  public async Task GetPostAsync_WhenCalledAndPostExistsButContainsInvalidMetaData_ItShouldReturnNull()
  {
    _mockFileSystem
      .Setup(x => x.Directory.Exists(It.IsAny<string>()))
      .Returns(true);

    _mockFileSystem
      .Setup(x => x.Path.Combine(It.IsAny<string>(), It.IsAny<string>()))
      .Returns("slug/INDEX.md");

    _mockFileSystem
      .Setup(x => x.File.Exists(It.IsAny<string>()))
      .Returns(true);

    _mockFileSystem
      .Setup(x => x.File.ReadAllTextAsync(It.IsAny<string>(), default))
      .ReturnsAsync(
        """
        # Post Title

        ```json meta
        {
          "title": "Post Title",
          "isPublished": "true",
          "date": "2021-01-01"
        }
        ```
        """
      );

    var result = await _sut.GetPostAsync("slug");

    result.Should().BeNull();
  }

  [Test]
  public async Task GetPostAsync_WhenCalledAndPostExistsButIsNotPublished_ItShouldReturnNull()
  {
    _mockFileSystem
      .Setup(x => x.Directory.Exists(It.IsAny<string>()))
      .Returns(true);

    _mockFileSystem
      .Setup(x => x.Path.Combine(It.IsAny<string>(), It.IsAny<string>()))
      .Returns("slug/INDEX.md");

    _mockFileSystem
      .Setup(x => x.File.Exists(It.IsAny<string>()))
      .Returns(true);

    _mockFileSystem
      .Setup(x => x.File.ReadAllTextAsync(It.IsAny<string>(), default))
      .ReturnsAsync(
        """
        # Post Title

        ```json meta
        {
          "title": "Post Title",
          "lead": "Post lead",
          "isPublished": false,
          "publishedAt": "2021-01-01"
        }
        ```

        Post content
        """
      );

    var result = await _sut.GetPostAsync("slug");

    result.Should().BeNull();
  }

  [Test]
  public async Task GetPostAsync_WhenCalledAndPostExists_ItShouldReturnPostWithContent()
  {
    _mockFileSystem
      .Setup(x => x.Directory.Exists(It.IsAny<string>()))
      .Returns(true);

    _mockFileSystem
      .Setup(x => x.Path.Combine(It.IsAny<string>(), It.IsAny<string>()))
      .Returns("slug/INDEX.md");

    _mockFileSystem
      .Setup(x => x.File.Exists(It.IsAny<string>()))
      .Returns(true);

    _mockFileSystem
      .Setup(x => x.File.ReadAllTextAsync(It.IsAny<string>(), default))
      .ReturnsAsync(
        """
        # Post Title

        ```json meta
        {
          "title": "Post Title",
          "lead": "Post lead",
          "isPublished": true,
          "publishedAt": "2021-01-01",
          "slug": "slug"
        }
        ```

        Post content
        """
      );

    var result = await _sut.GetPostAsync("slug");

    result.Should().NotBeNull();
    result.Should().BeEquivalentTo(new PostWithContent
    {
      Title = "Post Title",
      Lead = "Post lead",
      IsPublished = true,
      PublishedAt = new DateTime(2021, 1, 1),
      Slug = "slug",
      Content = "<h1 id=\"post-title\">Post Title</h1>\n<p>Post content</p>\n",
    });
  }
}