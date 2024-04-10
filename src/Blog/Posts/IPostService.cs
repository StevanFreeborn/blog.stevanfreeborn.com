namespace Blog.Posts;

interface IPostService
{
  Task<List<Post>> GetPostsAsync();
}