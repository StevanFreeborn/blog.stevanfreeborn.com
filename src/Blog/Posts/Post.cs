namespace Blog.Posts;

record Post
{
  public string Title { get; init; } = string.Empty;
  public string Lead { get; init; } = string.Empty;
  public bool IsPublished { get; init; } = false;
  public DateTime PublishedAt { get; init; }
  public string Slug { get; init; } = string.Empty;
  public string OpenGraphImage { get; init; } = string.Empty;
}

record PostWithContent : Post
{
  public string Content { get; init; } = string.Empty;
}