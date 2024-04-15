```json meta
{
  "title": "Building a Markdown Blog with Blazor and Docker",
  "lead": "Learn how I built a Markdown blog with Blazor and Docker. I'll cover the architecture, implementation, and deployment along with some of the interesting features and challenges I encountered.",
  "isPublished": true,
  "publishedAt": "2024-04-15",
  "openGraphImage": "posts/building-a-markdown-blog-with-blazor-and-docker/og-image.png",
}
```

## That Is Awesome! How Can I Do That?

A couple weeks ago I was listening to [this episode](https://www.devtools.fm/episode/92) on [devtoolsFM](https://www.devtools.fm/) with [Dan Abramov](https://twitter.com/dan_abramov2) and after some clicking around in the show notes I ended up at Dan's blog called [overreacted](https://overreacted.io/). And I immediately fell in love with its simplicity and the first thing that came to my mind was:

> I wanted to build a blog like that!

I've had a blog since early 2011. I used it heavily to write about health and fitness. It started on [this weebly site](http://intellectualfitness.weebly.com/) then migrated to a wordpress site then to a Squarespace site and most recently back to just a free wordpress hosted site [here](https://stevanfreeborn.wordpress.com/).

But since that last migration it's been on my list to rebuild the blog myself. Mostly because I've just has the itch of late to write more, but also because it feels like another great opportunity to get reps building something for the web.

## I Wonder If I Can Do That With Blazor?

Dan's blog is exactly the kind of blog that I've wanted to build for myself. It's simple and easy to read. It displays a feed of posts on the home page and each post is written in markdown, read from a file, parsed to HTML, and displayed on the page.

True to the spirit of Dan's content it is built with React using [Next.js](https://nextjs.org/) and deployed to [Vercel](https://vercel.com/). Which is a great stack for building a blog like this. I particularly like the [`<AutoRefresh />`](https://github.com/gaearon/overreacted.io/blob/main/app/AutoRefresh.js) component and [`watcher.js`](https://github.com/gaearon/overreacted.io/blob/main/watcher.js) that he added to automatically refresh the page when posts are being edited.

And while Vercel and Next.js are great I've been wanting to get more experience building sites with [Blazor](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor) and I thought this would be a great opportunity to do that.

From a hosting and deployment perspective I wanted to be able to self-host the site and publish new posts simply by adding a markdown file to a directory and merging the changes to the site's repository. Sort of like DIY git-based workflow similar to what [Vercel](https://vercel.com/) provides. Which gave me the opportunity to learn more about [Docker](https://www.docker.com/) and how to use it to build and deploy the site using a green-blue deployment strategy.

## What Would This Look Like?

I'm a big believer in standing on the shoulders of giants. I don't understand the need to reinvent the wheel if you can build on the learnings of others. So I did what I do just about anytime I want to learn something new - I asked the internet.

![asking the internet](posts/building-a-markdown-blog-with-blazor-and-docker/ask-internet.gif)

## What Does This Look Like?

I started by creating a new Blazor project using the dotnet CLI:

```bash
dotnet new blazor -ai --empty -o Blog
```

## Deployment

- Preparing the application for deployment
- Docker configuration and setup
- Deploying the application and testing

## Interesting Features and Challenges

- Noteworthy features implemented in the blog
- Challenges faced during development and how they were overcome

## Conclusion

- Summary of key takeaways
- Invitation for feedback and questions from readers
