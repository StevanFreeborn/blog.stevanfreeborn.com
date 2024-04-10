namespace Blog.Posts;

interface IPostService
{
  Task<List<Post>> GetPostsAsync();
  Task<PostWithContent?> GetPostAsync(string slug);
}