```json meta
{
  "title": "How Building A Discord Bot Made Me A Better Developer",
  "lead": "Some practical lessons I picked up while wrestling with WebSockets, concurrency, and Discord's Gateway API",
  "isPublished": true,
  "publishedAt": "2025-06-17",
  "openGraphImage": "posts/how-building-a-discord-bot-made-me-a-better-developer/og-image.png",
}
```

I wanted to build a Discord bot for my server. How hard could it be, right? Connect to Discord's API, listen for messages, respond when needed. Should be a weekend project.

Spoiler alert: it wasn't.

What I thought would be straightforward turned into a crash course in building resilient network clients. By the time I had a working `DiscordGatewayClient` that could stay connected reliably, I'd learned a bunch of things that made me a better developer.

Here's what building this bot taught me—maybe some of it will be useful for your projects too.

## Abstractions Are Worth The Extra Work (Even When They Feel Unnecessary)
