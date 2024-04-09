namespace Blog.Posts;

record Post
{
    public string Title { get; init; } = string.Empty;
    public string Lead { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public DateTime PublishedAt { get; init; }
    public string Slug { get; init; } = string.Empty;
}
