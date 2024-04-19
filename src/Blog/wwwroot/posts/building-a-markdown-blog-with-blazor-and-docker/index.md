```json meta
{
  "title": "Building a Markdown Blog with Blazor and Docker",
  "lead": "Learn how I built a Markdown blog with Blazor and Docker. I'll cover the architecture, implementation, and deployment along with some of the interesting features and challenges I encountered.",
  "isPublished": true,
  "publishedAt": "2024-04-18",
  "openGraphImage": "posts/building-a-markdown-blog-with-blazor-and-docker/og-image.png",
}
```

## That Is Awesome! How Can I Do That?

A couple weeks ago I was listening to [this episode](https://www.devtools.fm/episode/92) on [devtoolsFM](https://www.devtools.fm/) with [Dan Abramov](https://twitter.com/dan_abramov2) and after some clicking around in the show notes I ended up at Dan's blog called [overreacted](https://overreacted.io/). I immediately fell in love with its simplicity and the first thing that came to my mind was:

> I want to build a blog like this!

I've had a blog since early 2011. I used it heavily to write about health and fitness. It started on [this weebly site](http://intellectualfitness.weebly.com/) then migrated to a wordpress site then to a Squarespace site and most recently back to just a free wordpress hosted site [here](https://stevanfreeborn.wordpress.com/).

But since that last migration it's been on my list to rebuild the blog myself. Mostly because I've just had the itch of late to write more, but also because it feels like another great opportunity to get reps building something for the web. I also don't like the advertisements that show up on the free wordpress site.

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

I also found [this in-depth series](https://chrissainty.com/series/building-a-blogging-app-with-blazor/) from [Chris Sainty](https://twitter.com/chrissainty) that covers building a blogging app with Blazor. I did not read all the posts in the series because I wasn't really looking to build an entire CRUD app and it is a bit dated now that .NET 8 is out, but from a quick scan of the posts it looks like a great resource for anyone looking to build a complete blogging app with Blazor.

## What Does This Look Like?

After reading and watching I felt like I had a pretty good idea of how I wanted this to work and look. I started by creating a new Blazor project using the dotnet CLI:

```bash
dotnet new blazor -ai --empty -o Blog
```

I used the `--empty` flag because I wanted to build the site from scratch and not use the default template. [Bootstrap](https://getbootstrap.com/) is great, but I always feel like any practice I can get writing CSS is a good thing. I also opted-in to the all-interactive version of the template using the `-ai` flag because I thought it might be nice to have some server-supported interactivity if needed.

Once the initial scaffolding was done I also set up a test project for the blog. I know [a Quality Engineer](https://www.linkedin.com/in/stevan-freeborn/) who writes tests - pretty wild right? But in my experience building projects I've found that you always regret not setting up testing from the beginning. I've also been trying to practice more of a Test-Driven Development workflow so I wanted to make sure I had a test project setup right away.

Based on the research I did it seems like the go-to library for unit testing Blazor components is [bUnit](https://bunit.dev/) and it is pretty agnostic about what test runner you use. I did though know I wanted to use [Playwright](https://playwright.dev/) for integration testing and they only currently support [NUnit](https://nunit.org/) and [MSTest](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-mstest-runner-intro) out of the box so I setup my test project using NUnit. Added the bUnit and Playwright packages and got to work.

```bash
dotnet new nunit -o Blog.Tests
cd Blog.Tests
dotnet add package bUnit
dotnet add package Playwright
dotnet add package Microsoft.Playwright.NUnit
dotnet add package FluentAssertions
dotnet add package Moq
dotnet add Microsoft.AspNetCore.Mvc.Testing
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

I think if you have spent any time testing components in Vue or React with something like the [Testing Library](https://testing-library.com/) you'll find bUnit pretty easy to pick up. It has a lot of the same concepts although I think it would be pretty sweet to see the API evolve to be more like in the future instead of relying just on css selectors.

And while I'll admit I'm a sucker for a file-system based router I really like the simplicity of Blazor's approach to that same problem. You create a component, you give it a `@page` directive, and specify the route template. Done.

```razor
@page "/"

<div>
  <h1>Blog</h1>
  <p>Welcome to my blog!</p>
</div>
```

The core functionality though for the blog really comes down to the `Feed` component and the `Post` page which both depend on the `IPostService` to get all the posts or get a single post with its complete content.

For now the concrete implementation of the `IPostService` is a service that reads markdown files from the file system, but it could always be swapped out for a service that reads from a database or an API. I've kind of been playing around with the idea of using [Onspring](https://onspring.com/) as a CMS of sorts and just pull the content from there.

And while programming against the interface is great for swapping out implementations it also makes unit testing components that depend on these sorts of services a breeze using bUnit's `TestContext` class and `Mock` classes.

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

I really like using markdown to author blog posts. It makes it super easy to store the metadata for the post right with the content in a structured way using a simple JSON code block at the top of the file with a unique argument included like `meta` to distinguish it from the rest of the content.

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

There are also a few sprinkles of client-side interactivity that I added to the post page, but I'll talk about those in more detail a little later. For now let's move on to the deployment setup.

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

The `Dockerfile` I created in reality is actually a multi-stage build so that I can build and run the site in a development environment and also prepare it for a production environment. But for the sake of brevity I'll just show the production stage of the build.

The initial stage of the build is about building and restoring the project's dependencies. For this it is important to use the full .NET SDK image. I refer to this stage as the `setup-stage`. You'll probably notice that I'm copying the `*.csproj` file first and running `dotnet restore` before copying the rest of the files. This is a common pattern to take advantage of Docker's layer caching mechanism.

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS setup-stage
WORKDIR /app
COPY *.csproj ./
RUN dotnet restore
COPY . .
```

The second stage of the build is about building the project and publishing it to a directory. For this stage it is important to use the .NET runtime image. I refer to this stage as the `build-stage`. You'll notice that I'm specifying the `--configuration Release` flag when running `dotnet publish`. This is to ensure that the project is built in release mode and that the output is optimized for production.

```dockerfile
FROM setup-stage as build-stage
RUN dotnet publish -c Release -o dist
```

The final stage of the build is about running the site. For this stage I'm just using the `aspnet` image so that the overall container size is much smaller as at this point I don't need the complete .NET SDK. I refer to this stage as the `run-stage`. You'll notice that I'm copying the published output from the `build-stage` and then using the `ENTRYPOINT` directive to specify the command to run when the container starts.

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS production-stage
WORKDIR /app
COPY --from=build-stage /app/dist ./
ENTRYPOINT ["dotnet", "Blog.dll"]
```

Once I have the instructions for building the blog's image I can use my github action to actually build and push the image to Docker Hub.

### The GitHub Action

The GitHub Action I created is pretty simple. It is triggered on a push to the `main` branch which should really only happen after a pull request has passed all the checks and been approved.

I start the job by checking out the repository, logging into docker hub, building the image, tagging the image, and then pushing the image to Docker Hub. I'm not doing anything complex for versioning. With a project like this it just seemed like overkill to keep the image version in sync with the project version. So at this point I'm not doing any type of versioning on the actual blog project, but simply tagging the image with the date and time that it was built.

```yaml
build:
  name: Build and push Docker image
  runs-on: ubuntu-latest
  outputs:
    version: ${{ steps.version.outputs.version }}
  steps:
    - name: Checkout repository
      uses: actions/checkout@v4
      with:
        fetch-depth: 0
    - name: Login to Docker Hub
      uses: docker/login-action@v3
      with:
        username: ${{ secrets.DOCKERHUB_USERNAME }}
        password: ${{ secrets.DOCKERHUB_TOKEN }}
    - name: Create version tag
      id: version
      run: echo "version=$(date +%Y.%m.%d.%H%M%S)" >> $GITHUB_OUTPUT
    - name: Build and push server image
      working-directory: src/Blog
      run: |
        TAG=${{ secrets.DOCKERHUB_USERNAME }}/blog.stevanfreeborn.com:${{ steps.version.outputs.version }}
        docker build -t $TAG .
        docker push $TAG
```

Now my new image is built and pushed to Docker Hub so all I have to do is deploy it.

### The Deployment Script

As I mentioned in the beginning I wanted to make sure I had the ability to deploy new versions of the blog that contains my new posts without it impacting the user. I thought the green-blue deployment strategy would be a good fit in this case.

From a landscape perspective the docker image is going to run on a single server behind an Nginx reverse proxy. Which means in order to make sure I can deploy new versions of the blog without impacting the user I need to be able to spin up a new container, redirect traffic to the new container, and then tear down the old container.

I implemented this using a powershell script that is checked into the repository. I use another github action to ssh into the server, copy the script to the server, and then execute it. The script takes the version of the image as an argument which is why I needed to output the version from the build action.

```yaml
deploy:
  name: Deploy to server
  runs-on: ubuntu-latest
  needs: build
  steps:
    - name: Checkout repository
      uses: actions/checkout@v4
      with:
        fetch-depth: 0
    - name: Copy files to server
      uses: appleboy/scp-action@v0.1.7
      with:
        host: ${{ secrets.SSH_HOST }}
        username: ${{ secrets.SSH_USERNAME }}
        key: ${{ secrets.SSH_KEY }}
        source: "scripts/deploy.ps1"
        target: blog.stevanfreeborn.com
        strip_components: 1
        rm: true
    - name: Run deploy script
      uses: appleboy/ssh-action@master
      with:
        host: ${{ secrets.SSH_HOST }}
        username: ${{ secrets.SSH_USERNAME }}
        key: ${{ secrets.SSH_KEY }}
        script: |
          chmod +x blog.stevanfreeborn.com/deploy.ps1
          sudo pwsh ./blog.stevanfreeborn.com/deploy.ps1 ${{ needs.build.outputs.version }}
```

When the script runs it will do some preliminary checks to make sure things like docker is installed and that it can find the Nginx configuration file that it will need to modify. If those gut checks pass it will see if a blue container is running and if it isn't it will just set up the initial blue container and redirect traffic to it. I'm omitting some checks just for brevity.

```powershell
if ($null -eq $blueContainerId) 
{
  Write-Host "Blue container is not running. Starting blue container."

  $blueContainerHostPort = StartContainer -containerColor "blue" -dockerTag $dockerTag

  Write-Host "Blue container is running."
  
  UpdateNginxConfig -filePath $NGINX_CONFIG_PATH -portNumber $blueContainerHostPort

  Write-Host "Nginx configuration updated to point to blue container on port $blueContainerHostPort."

  nginx -t

  nginx -s reload

  Write-Host "Nginx reloaded. Successfully deployed version $version."
  exit 0
}
```

If there is, it will stop the container and remove it. Then it will pull the new image, start the new container, and then redirect traffic to the new container.

```powershell
else
{
  Write-Host "Blue container is running. Starting green container."

  $greenContainerHostPort = StartContainer -containerColor "green" -dockerTag $dockerTag

  Write-Host "Green container is running."

  UpdateNginxConfig -filePath $NGINX_CONFIG_PATH -portNumber $greenContainerHostPort

  Write-Host "Nginx configuration updated to point to green container on port $greenContainerHostPort."

  nginx -t

  nginx -s reload

  Write-Host "Nginx reloaded. Successfully deployed version $version."

  Write-Host "Stopping and removing blue container."
  
  docker stop $blueContainerId

  docker rm $blueContainerId

  Write-Host "Blue container stopped and removed."

  Write-Host "Switching green container to blue container."

  docker rename "blog.stevanfreeborn.com.green" "blog.stevanfreeborn.com.blue"

  Write-Host "Successfully deployed version $version."

  exit 0
}
```

You might be wondering how it is I'm updating the Nginx configuration file since in the above it's abstracted away into a function. It's actually super simple. Since nginx is just proxying traffic to the containers once I have the port for the container from docker I just read in the configuration file, match the `proxy_pass` directive, replace it with the updated port, and then write the file back out.

```powershell
function UpdateNginxConfig 
{
  param (
    [string]$filePath,
    [string]$portNumber
  )

  $nginxConfig = Get-Content $filePath
  $pattern = "proxy_pass http://localhost:\d+;"
  $replacement = "proxy_pass http://localhost:$portNumber;"

  $modifiedContent = @()

  foreach ($line in $nginxConfig) {
    if ($line -match $pattern) {
        $line = $line -replace $pattern, $replacement
    }

    $modifiedContent += $line
  }

  Set-Content -Path $filePath -Value $modifiedContent
}
```

I had initially thought that it was going to be a much more complex job than it was.

Once I got this all setup the deployments worked...or they did until I noticed that the end user experience wasn't at all what I wanted. I noticed that if I was on a blog post page and a new version of the blog was being stood up the user would see a loading spinner and then after a certain amount of time the page would require a reload. Something like this:

![loading ui](posts/building-a-markdown-blog-with-blazor-and-docker/loading-ui.gif)

Turns out this is the default behavior when you are working with Blazor in interactive server mode. The UI I was seeing was the default UI that Blazor displays when there is an issue with the `SignalR` connection that is supporting that interactivity.

And because I was standing up a new instance of the app the connections were being rejected when successfully reconnected because the new instance of my app had no idea about the connections that the old instance had already established, requiring the page to be reloaded.

There is actually quite a lot of customization you can do to this UI. You can completely override it with your own custom UI and you can even override how the client side Blazor javascript handles disconnections and reconnections. But after looking at what I was really getting out of the server-side interactivity I decided it just wasn't worth all of the effort to implement. I could just as easily get the same client-side functionality I was looking for with some JavaScript running on the client after the page is loaded. So I actually decided to fall back to the new static SSR mode that Blazor now supports as of .NET 8.

Doing this meant I no longer had to worry about those persistent SignalR connections and the concern that it would alert any users that a new version of the blog was being stood up. With that change in place I was able to deploy new versions of the blog without any impact to the user experience.

## Interesting Features and Challenges

Okay so we've covered most of the nuts and bolts of the blog. I've talked about the architecture, the implementation, and the deployment. But I wanted to take a moment to highlight a couple of the interesting features I added to the blog post page and some of the oddities it required. I also don't want to forget to talk about how I set up the integration tests for the site using Playwright and the `WebApplicationFactory` class that comes with the `Microsoft.AspNetCore.Mvc.Testing` package.

### Syntax Highlighting, Code Block Copy Button, and Heading Links

You may have noticed already that the blog post supports these three things:

- Syntax highlighting
- A copy button on code blocks
- Heading links

All of which are not a part of what the server renders and sends to the client. They are all added to the post once it is loaded on the client using JavaScript. Remember I mentioned that since I was going to be using the static SSR mode of Blazor I'd need to rely on JavaScript to get these types of client-side enhancements done. However, it turns out that it requires a little bit of finesse to be able to have collocated JavaScript - `Post.razor.js` - be loaded and executed on just a single page-basis in Blazor when you are using the static SSR mode.

Lucky for me I found this great piece of documentation on Microsoft's Learn site that covers just how to do this. It was literally perfect timing cause it looks like the page went up just this month. Although I'll admit it feels like kind of a bit of work to implement. Seems kind of like something Blazor itself should just have out of the box. You can find the complete article [here](https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/static-server-rendering?view=aspnetcore-8.0).

The short of it though is that you basically create your own `PageScript` Blazor component that you can pass the path to your collocated JavaScript file.

```razor
@namespace Blog.Components.Utils

<page-script src="@Src"></page-script>

@code 
{
  [Parameter]
  [EditorRequired]
  public string Src { get; set; } = default!;
}
```

The `PageScript` component then uses a custom [web component](https://developer.mozilla.org/en-US/docs/Web/Web_Components) to manage loading, executing, and removing the script from the page. This custom web component essentially defines a set of methods that it expects to be exported from your JavaScript file that it can call at certain points in its lifecycle. And the custom-web component is defined in a separate JavaScript file that depends on another Blazor feature called [JavaScript Initializers](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/startup?view=aspnetcore-8.0#javascript-initializers). Which is basically another module that implements and exports specific methods that Blazor can call during its life cycle.

```javascript
const pageScriptInfoBySrc = new Map();

function registerPageScriptElement(src) {
  if (!src) {
    throw new Error('Must provide a non-empty value for the "src" attribute.');
  }

  let pageScriptInfo = pageScriptInfoBySrc.get(src);

  if (pageScriptInfo) {
    pageScriptInfo.referenceCount++;
    return;
  }

  pageScriptInfo = { referenceCount: 1, module: null };
  pageScriptInfoBySrc.set(src, pageScriptInfo);
  initializePageScriptModule(src, pageScriptInfo);
}

function unregisterPageScriptElement(src) {
  if (!src) {
    return;
  }

  const pageScriptInfo = pageScriptInfoBySrc.get(src);
  
  if (!pageScriptInfo) {
    return;
  }

  pageScriptInfo.referenceCount--;
}

async function initializePageScriptModule(src, pageScriptInfo) {
  if (src.startsWith("./")) {
    src = new URL(src.substr(2), document.baseURI).toString();
  }

  const module = await import(src);

  if (pageScriptInfo.referenceCount <= 0) {
    return;
  }

  pageScriptInfo.module = module;
  module.onLoad?.();
  module.onUpdate?.();
}

function onEnhancedLoad() {
  for (const [src, { module, referenceCount }] of pageScriptInfoBySrc) {
    if (referenceCount <= 0) {
      module?.onDispose?.();
      pageScriptInfoBySrc.delete(src);
    }
  }

  for (const { module } of pageScriptInfoBySrc.values()) {
    module?.onUpdate?.();
  }
}

export function afterWebStarted(blazor) {
  customElements.define(
    "page-script",
    class extends HTMLElement {
      static observedAttributes = ["src"];

      attributeChangedCallback(name, oldValue, newValue) {
        if (name !== "src") {
          return;
        }

        this.src = newValue;
        unregisterPageScriptElement(oldValue);
        registerPageScriptElement(newValue);
      }

      disconnectedCallback() {
        unregisterPageScriptElement(this.src);
      }
    }
  );

  blazor.addEventListener("enhancedload", onEnhancedLoad);
}
```

I know like I said it seems like a lot of work to just be able to use a collocated JavaScript file on a single page, but once you get it in place it works great. I could definitely see this pattern being something that Blazor adopts itself. At this point though all I had to do was create the `Post.razor.js` file and then add the `PageScript` component to the `Post` page. And lucky for me this script could actually be pretty straight forward by leveraging the following existing libraries to do a lot of the heavy lifting:

- [Prism.js](https://prismjs.com/)
- [clipboard.js](https://clipboardjs.com/)
- [anchor.js](https://www.bryanbraun.com/anchorjs/)

I load these libraries in the app's main entry point - `App.razor` - before the client-side Blazor code is started. This way I can ensure that they are available when the `Post.razor.js` script is loaded and run. Then in my `Post.razor.js` file I just need to wire things up.

```javascript
// this is the method that the custom web component
// will call when the script is loaded or an
// enhanced navigation occurs
export function onUpdate() {
  init();
}

function init(){
  addAnchors();
  addClipboard();
  Prism.highlightAll();
}

function addAnchors() {
  const selectors = [
    '.markdown-body h2',
    '.markdown-body h3',
    '.markdown-body h4',
    '.markdown-body h5',
    '.markdown-body h6'
  ];

  anchors.add(selectors.join(','));
}

function addClipboard() {
  const codeBlocks = document.querySelectorAll('pre');
  
  codeBlocks.forEach((block) => {
    const button = document.createElement('button');
    button.className = 'copy-button';
    button.type = 'button';
    button.innerHTML = `
      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 448 512" fill="currentColor">
        <path d="M384 336H192c-8.8 0-16-7.2-16-16V64c0-8.8 7.2-16 16-16l140.1 0L400 115.9V320c0 8.8-7.2 16-16 16zM192 384H384c35.3 0 64-28.7 64-64V115.9c0-12.7-5.1-24.9-14.1-33.9L366.1 14.1c-9-9-21.2-14.1-33.9-14.1H192c-35.3 0-64 28.7-64 64V320c0 35.3 28.7 64 64 64zM64 128c-35.3 0-64 28.7-64 64V448c0 35.3 28.7 64 64 64H256c35.3 0 64-28.7 64-64V416H272v32c0 8.8-7.2 16-16 16H64c-8.8 0-16-7.2-16-16V192c0-8.8 7.2-16 16-16H96V128H64z"/>
      </svg>
    `
    button.onclick = () => {
      button.style.color = 'green';
      setTimeout(() => {
        button.style.color = '';
      }, 1000);
    };

    const buttonText = document.createElement('span');
    buttonText.className = 'sr-only';
    buttonText.textContent = 'Copy';

    button.appendChild(buttonText);
    block.appendChild(button);
  });

  var clipboard = new ClipboardJS('.copy-button', {
    target: (trigger) => {
      return trigger.previousElementSibling;
    }
  });

  clipboard.on('success', function(e) {
    e.clearSelection();
  });
}
```

### Integration Testing

I mentioned earlier that I knew going into this that I wanted to be able to use Playwright for integration testing. But specifically I wanted to see if it would be possible to do this integration testing using the `WebApplicationFactory` class that comes with the `Microsoft.AspNetCore.Mvc.Testing` package. I love using this type of setup for integration testing in API projects, but it seems like it could be used for this as well. I'd just never seen it done before. Turns out there is a reason for that. It is not really supported out of the box at the moment.

However, as I said I always do, I went to the internet to see if some people smarter than me had the same itch and had already scratched it. And wouldn't you know it they had. I found [this article](https://danieldonbavand.com/2022/06/13/using-playwright-with-the-webapplicationfactory-to-test-a-blazor-application/) by Daniel Donbavand that covers how to override some of the default behavior of the `WebApplicationFactory` class to get Playwright to work with it.

The main crux of the issue is that by default the `WebApplicationFactory` class creates a host which isn't actually listening on an actual port. Which is a problem for Playwright because it needs to be able to actually navigate to an actual version of the site being served over the network.

This means to make this work we need to essentially override how the `WebApplicationFactory` class creates the host so that it is actually listening on a port. This is done by creating a custom `WebApplicationFactory` class that overrides the `CreateHost` method and then using that custom class in the test project.

```csharp
protected override IHost CreateHost(IHostBuilder builder)
{
  var testHost = builder
    .ConfigureWebHost(webHostBuilder =>
    {
      webHostBuilder.ConfigureLogging(config => config.ClearProviders());
      webHostBuilder.ConfigureTestServices(services =>
      {
        var postsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "TestPosts");
        var postServiceOptions = new FilePostServiceOptions() { PostsDirectory = postsDirectory };
        services.AddSingleton(Options.Create(postServiceOptions));
      });
    })
    .Build();

  builder.ConfigureWebHost(
    webHostBuilder => webHostBuilder.UseKestrel(
      o => o.Listen(IPAddress.Loopback, 0)
    )
  );

  _host = builder.Build();
  _host.Start();

  var server = _host.Services.GetRequiredService<IServer>();
  var addresses = server.Features.GetRequiredFeature<IServerAddressesFeature>();

  ClientOptions.BaseAddress = addresses.Addresses
    .Select(x => new Uri(x))
    .Last();

  testHost.Start();
  return testHost;
}
```

The most important part here being that I'm actually configuring the host to listen on an actual port using the `UseKestrel` method. Then once I've got the host started I can get the actual address it is listening on and set that as the base address for the custom `WebApplicationFactory`'s client options.

I can then add a public field to my custom `WebApplicationFactory` class that exposes this address so I can have access to it in my tests.

```csharp
private void EnsureServer()
{
  if (_host is null)
  {
    using var _ = CreateDefaultClient();
  }
}

public string ServerAddress
{
  get
  {
    EnsureServer();
    return ClientOptions.BaseAddress.ToString();
  }
}
```

The `EnsureServer` method is just to make sure that whenever we do access the `ServerAddress` property that the host is actually started and that therefore we are going to actually get the correct address.

Now I can use this custom `WebApplicationFactory` class in my integration tests to set the `BaseURL` option for the Playwright browser context that will be creating the `Page` each of my tests will use. I implemented this in a separate base `TestFixture` class that all of my integration tests inherit from.

```csharp
[TestFixture]
public class BlogTest : PageTest
{
  private readonly BlogHostFactory<Program> _factory = new();

  public override BrowserNewContextOptions ContextOptions()
  {
    var options = base.ContextOptions();
    options.BaseURL = _factory.ServerAddress;
    return options;
  }
}
```

I'll admit this definitely took a day or so to get my mind around and actually implement, but once I got there it was so worth it. The integration tests are speedy, reliable, and easy to write. And I have the added benefit of being able to override any of the configuration of my test host as my tests require. For example, because I'm injecting the location of my posts directory using the `IOptions` pattern I can easily override that in my tests to point to a different directory of a static set of posts I can test against.

## Conclusion

I've been really happy with the way the blog has turned out. I've been able to write and publish new posts without any impact to the user experience. I've been able to test the site with both unit and integration tests. And I've been able to deploy new versions of the site without much headache.

I'm sure there are still some rough edges to work out and some features I'd like to add, but I'm really happy with the way things have turned out so far. I'm looking forward to writing more posts and sharing them with you all. I hope you've enjoyed this post and I hope you'll come back for more.

And thanks again to Dan Abramov for the inspiration.
