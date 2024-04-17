```json meta
{
  "title": "Building a Markdown Blog with Blazor and Docker",
  "lead": "Learn how I built a Markdown blog with Blazor and Docker. I'll cover the architecture, implementation, and deployment along with some of the interesting features and challenges I encountered.",
  "isPublished": true,
  "publishedAt": "2024-04-17",
  "openGraphImage": "posts/building-a-markdown-blog-with-blazor-and-docker/og-image.png",
}
```

## That Is Awesome! How Can I Do That?

A couple weeks ago I was listening to [this episode](https://www.devtools.fm/episode/92) on [devtoolsFM](https://www.devtools.fm/) with [Dan Abramov](https://twitter.com/dan_abramov2) and after some clicking around in the show notes I ended up at Dan's blog called [overreacted](https://overreacted.io/). I immediately fell in love with its simplicity and the first thing that came to my mind was:

> I want to build a blog like this!

I've had a blog since early 2011. I used it heavily to write about health and fitness. It started on [this weebly site](http://intellectualfitness.weebly.com/) then migrated to a wordpress site then to a Squarespace site and most recently back to just a free wordpress hosted site [here](https://stevanfreeborn.wordpress.com/).

But since that last migration it's been on my list to rebuild the blog myself. Mostly because I've just has the itch of late to write more, but also because it feels like another great opportunity to get reps building something for the web. I also don't like the advertisements that show up on the free wordpress site.

## I Wonder If I Can Do That With Blazor?

Dan's blog is exactly the kind of blog that I've wanted to build for myself. It's simple and easy to read. It displays a feed of posts on the home page and each post is written in markdown, read from a file, parsed to HTML, and displayed on the page.

True to the spirit of Dan's content it is built with React using [Next.js](https://nextjs.org/) and deployed to [Vercel](https://vercel.com/). Which is a great stack for building a blog like this. I particularly like the [`<AutoRefresh />`](https://github.com/gaearon/overreacted.io/blob/main/app/AutoRefresh.js) component and [`watcher.js`](https://github.com/gaearon/overreacted.io/blob/main/watcher.js) that he added to automatically refresh the page when posts are being edited.

And while Vercel and Next.js are great I've been wanting to get more experience building sites with [Blazor](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor) and I thought this would be a great opportunity to do that.

From a hosting and deployment perspective I wanted to be able to self-host the site and publish new posts simply by adding a markdown file to a directory and merging the changes to the site's repository. Sort of like a DIY Git-based workflow similar to what [Vercel](https://vercel.com/) provides. Which gave me the opportunity to learn more about [Docker](https://www.docker.com/) and how to use it to build and deploy the site using a green-blue deployment strategy.

## What Would This Look Like?

I'm a big believer in standing on the shoulders of giants. I don't understand the need to reinvent the wheel if you can build on the learnings of others. So I did what I do just about anytime I want to learn or build something new - I asked the internet.

![asking the internet](posts/building-a-markdown-blog-with-blazor-and-docker/ask-internet.gif)

Specifically I asked the interwebz about building a markdown blog with Blazor. And wouldn't you know it I found some really great resources that helped me get started.

[This video](https://youtu.be/B2TWGlE8noU?si=932lYmkT5Y2Yi8-Y) from [codepey](https://www.youtube.com/@codepey) was really helpful for laying out the overall architecture of the blog and pointing me towards a great library called [Markdig](https://github.com/xoofx/markdig) that I could use to convert markdown to HTML.

I also found [this in-depth series](https://chrissainty.com/series/building-a-blogging-app-with-blazor/) from [Chris Sainty](https://twitter.com/chrissainty) that covers building a blogging app with Blazor. I did not read all the posts in the series because I wasm't really looking to build an entire CRUD app and it is a bit dated now that .NET 8 is out, but from a quick scan of the posts it looks like a great resource for anyone looking to build a complete blogging app with Blazor.

## What Does This Look Like?

After reading and watching I felt like I had a pretty good idea of how I wanted this to work and look. I started by creating a new Blazor project using the dotnet CLI:

```bash
dotnet new blazor -ai --empty -o Blog
```

I used the `--empty` flag because I wanted to build the site from scratch and not use the default template. [Bootstrap](https://getbootstrap.com/) is great, but I always feel like any practice I can get writing CSS is a good thing. I also opted-in to the all-interactive version of the template using the `-ai` flag because I thought it might be nice to have some server-supported interactivity if needed.

Once the initial scaffolding was done I also setup a test project for the blog. I know [a Quality Engineer](https://www.linkedin.com/in/stevan-freeborn/) who writes tests - pretty wild right? But in my experience building projects I've found that you always regret not setting up testing from the beginning. I've also been trying to practice more of a Test-Driven Development workflow so I wanted to make sure I had a test project setup right away.

Based on the research I did it seems like the go-to library for unit testing blazor components is [bUnit](https://bunit.dev/) and it is pretty agnostic about what test runner you use. I did though know I wanted to use [Playwright](https://playwright.dev/) for integration testing and they only currently support [NUnit](https://nunit.org/) and [MSTest](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-mstest-runner-intro) out of the box so I setup my test project using NUnit. Added the bUnit and Playwright packages and got to work.

```bash
dotnet new nunit -o Blog.Tests
cd Blog.Tests
dotnet add package bUnit
dotnet add package Playwright
dotnet add package Microsoft.Playwright.NUnit
dotnet add package FluentAssertions
dotnet add package Moq
dotnet build
pwsh bin/Debug/netX/playwright.ps1 install --with-deps
```

### The Meat and Potatoes

I don't want to spend too much time on the layout and styling of the blog. If you took a gander already at [Dan's blog](https://overreacted.io/) you'll see that it is pretty simple and I stuck pretty much to the same layout. There is a header, a main content area, and I added a footer with some boilerplate links.

I will though say that Blazor's component model is really nice and easy to pick up if you have worked in any other component-based framework. bUnit also keeps it really straight forward to unit test components by using `.razor` or `.cs` files. The former is currently my preference because it makes writing markup in the tests a breeze. Feels much more like writing a spec file for a component in Vue or React.

Here is for example my `Header` component:

```razor
@namespace Blog.Components.Layout

<header class="container">
  <div class="title-container">
    <NavLink href="/">
      <h1>journal</h1>
    </NavLink>
  </div>
  <div class="author-container">
    <span>by</span>
    <a href="https://stevanfreeborn.com" target="_blank">
      <img src="https://github.com/StevanFreeborn.png" alt="Stevan Freeborn" />
      <span class="sr-only">Stevan Freeborn</span>
    </a>
  </div>
</header>
```

> [!NOTE]
> If you are wondering were the styles are coming from it is good to know that Blazor's component model supports scoped css by using a collocated css file that matches the name of the component so in this case there is a `Header.razor.css` file that contains the styles for the classes referenced by the header component.

And here is a test for that component in `Header.spec.razor`:

```razor
@inherits bUnit.TestContext

@code
{
  [Test]
  public void Header_WhenRendered_ItShouldContainSiteTitle()
  {
    var cut = Render(@<Header />);

    var heading = cut.Find("h1");

    heading.MarkupMatches(@<h1>journal</h1>);
  }
}
```

I think if you have spent anytime testing components in Vue or React with something like the [Testing Library](https://testing-library.com/) you'll find bUnit pretty easy to pick up. It has a lot of the same concepts although I think it would be pretty sweet to see the API evolve to be more like in the future instead of relying just on css selectors.

And while I'll admit I'm a sucker for a file-system based router I really like the simplicity of Blazor's approach to that same problem. You create a component, you give it a `@page` directive, and specify the route template. Done.

```razor
@page "/"

<div>
  <h1>Blog</h1>
  <p>Welcome to my blog!</p>
</div>
```

The core functionality though for the blog really comes down to the `Feed` component and the `Post` page which both depend on the `IPostService` to get the all the posts or get a single post with it's complete content.

For now the concrete implementation of the `IPostService` is a service that reads markdown files from the file system, but it could always be swapped out for a service that reads from a database or an API. I've kind of been playing around with the idea of using [Onspring](https://onspring.com/) as a CMS of sorts and just pull the content from there.

And while programming against the interface is great for swapping out implementations it also makes unit testing components that depend on these sorts of service a breeze using bUnit's `TestContext` class and `Mock` classes.

Here is an example of testing the `Feed` component with the `IPostService` mocked:

```razor
@inherits bUnit.TestContext

@code 
{
  private Mock<IPostService> _postServiceMock = new();

  [Test]
  public void Feed_WhenRendedAndNoPosts_ItShouldRenderNoPostsMessage()
  {
    _postServiceMock
      .Setup(x => x.GetPostsAsync())
      .ReturnsAsync(new List<Post>());

    var ctx = new TestContext();

    ctx.Services.AddSingleton(_postServiceMock.Object);

    var cut = ctx.Render(@<Feed />);

    cut.Find("p").MarkupMatches("<p>Looks like writers block. Check back later.</p>");
  }
}
```

I really like using markdown to author the blog posts. It makes it super easy to store the metadata for the post right with the content in a structured way using a simple JSON code block at the top of the file with a unique argument included like `meta` to distinguish it from the rest of the content.

~~~markdown
```json meta
{
  "title": "Building a Markdown Blog with Blazor and Docker",
  "lead": "Learn how I built a Markdown blog with Blazor and Docker. I'll cover the architecture, implementation, and deployment along with some of the interesting features and challenges I encountered.",
  "isPublished": true,
  "publishedAt": "2024-04-17",
  "openGraphImage": "posts/building-a-markdown-blog-with-blazor-and-docker/og-image.png",
}
```
~~~

I can then use the `Markdig` library to parse the markdown, locate the code block that contains the metadata, bind that JSON object to a `Post` object, remove the metadata from the markdown content, and then convert the markdown content to HTML. This makes adding and managing metadata to posts really easy and I don't have to worry about dealing with frontmatter. It is just simple key value pairs and a .NET POCO.

```csharp
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

var metaContent = postMetadata?.Lines.ToString();

var post = JsonSerializer.Deserialize<Post>(metaContent, JsonSerializerOptions);
```

The rest of the blog is pretty uneventful. There is some basic styling using CSS and `@media` queries to make the site support light and dark themes. I took much of the markdown CSS from [this repository](https://github.com/sindresorhus/github-markdown-css) and modified as needed to get things just right. There are probably still some rough edges there, but I'll work those out as I write my posts.

There is also a few sprinkles of client-side interactivity that I added to the post page, but I'll talk about those in more detail a little later. For now let's move on to the deployment setup.

## Getting New Content Out Without People Noticing

Over the last couple months I've experimented quite a bit with Docker and I've really come to appreciate the simplicity and power of the tool especially when you start having to manage applications that have a lot of dependent external services. Docker makes it really easy to quickly spin up and tear down the app and all of its dependencies for development locally, for end-to-end testing, and for deployment.

In this case though I really wanted to utilize Docker for the deployment of the blog so that I could easily spin up new instances of the site on my one server and then redirect traffic to the new instance before tearing down the old instance. Basically a very rudimentary green-blue deployment strategy.

There were really three main pieces to the puzzle:

- A Dockerfile to build the site
- A [GitHub Action](https://github.com/features/actions) workflow to build and push the image to [Docker Hub](https://hub.docker.com/)
- A deployment script to...
  - Pull the new image
  - Start the new container
  - Redirect traffic to the new container
  - Tear down the old container

All of which I can kick off with a merge to the `main` branch of the repository.

### The Dockerfile

### The GitHub Action

### The Deployment Script

## Interesting Features and Challenges

- Noteworthy features implemented in the blog
- Challenges faced during development and how they were overcome

## Conclusion

- Summary of key takeaways
- Invitation for feedback and questions from readers
