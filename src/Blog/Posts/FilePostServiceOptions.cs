namespace Blog.Posts;

class FilePostServiceOptions
{
  public string PostsDirectory { get; set; } = string.Empty;
}

class FilePostServiceOptionsSetup(IConfiguration configuration) : IConfigureOptions<FilePostServiceOptions>
{
  private const string SectionName = nameof(FilePostServiceOptions);
  private readonly IConfiguration _configuration = configuration;

  public void Configure(FilePostServiceOptions options)
  {
    _configuration.GetSection(SectionName).Bind(options);
  }
}