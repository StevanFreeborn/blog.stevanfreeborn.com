namespace Blog.Posts;

class FilePostService(
  IOptions<FilePostServiceOptions> options,
  IFileSystem fileSystem
) : IPostService
{
  private static readonly JsonSerializerOptions JsonSerializerOptions = new()
  {
    PropertyNameCaseInsensitive = true,
    AllowTrailingCommas = true,
  };

  private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
    .UseAdvancedExtensions()
    .UsePrism()
    .Build();
  
  private const string MetaFence = "meta";
  private const string IndexFile = "INDEX.md";

  private readonly FilePostServiceOptions _options = options.Value;
  private readonly IFileSystem _fileSystem = fileSystem;

  private static (string? metadata, MarkdownDocument document) ParsePost(string postText)
  {
    var markDoc = Markdown.Parse(postText, MarkdownPipeline);
    var postMetadata = markDoc
      .Where(
        x =>
          x is FencedCodeBlock fencedCodeBlock &&
          fencedCodeBlock.Arguments is not null &&
          fencedCodeBlock.Arguments.Contains(MetaFence)
      )
      .Select(x => x as FencedCodeBlock)
      .FirstOrDefault();

    if (postMetadata is not null)
    {
      markDoc.Remove(postMetadata);
    }

    var metaContent = postMetadata?.Lines.ToString();

    return (metaContent, markDoc);
  }

  public async Task<List<Post>> GetPostsAsync()
  {
    var posts = new List<Post>();

    if (_fileSystem.Directory.Exists(_options.PostsDirectory) is false)
    {
      return posts;
    }

    var subDirectories = _fileSystem.Directory.GetDirectories(_options.PostsDirectory);

    if (subDirectories.Length is 0)
    {
      return posts;
    }

    foreach (var subDirectory in subDirectories)
    {
      try
      {
        var indexFilePath = _fileSystem.Path.Combine(subDirectory, IndexFile);
        
        if (_fileSystem.File.Exists(indexFilePath) is false)
        {
          continue;
        }
        
        var postText = await _fileSystem.File.ReadAllTextAsync(indexFilePath);

        var (metaContent, document) = ParsePost(postText);

        if (metaContent is null || document.Count is 0)
        {
          continue;
        }

        var post = JsonSerializer.Deserialize<Post>(metaContent, JsonSerializerOptions);

        if (post is null || post.IsPublished is false)
        {
          continue;
        }

        var finalPost = post with
        {
          Slug = _fileSystem.Path.GetFileName(subDirectory),
        };

        posts.Add(finalPost);
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.Message);
      }
    }

    return [.. posts.OrderByDescending(x => x.PublishedAt)];
  }
}