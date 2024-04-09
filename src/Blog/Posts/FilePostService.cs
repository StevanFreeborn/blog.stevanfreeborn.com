namespace Blog.Posts;

class FilePostService(
  IOptions<FilePostServiceOptions> options,
  IFileSystem fileSystem
) : IPostService
{
  private readonly FilePostServiceOptions _options = options.Value;

  private readonly IFileSystem _fileSystem = fileSystem;

  public Task<List<Post>> GetPostsAsync()
  {
    if (_fileSystem.Directory.Exists(_options.PostsDirectory) is false)
    {
      return Task.FromResult(new List<Post>());
    }

    var subDirectories = _fileSystem.Directory.GetDirectories(_options.PostsDirectory);
    var directoryNames = subDirectories.Select(s => _fileSystem.Path.GetFileName(s)).ToList();
    var posts = directoryNames.Select(s => new Post() { Title = s }).ToList();
    return Task.FromResult(posts);
  }
}