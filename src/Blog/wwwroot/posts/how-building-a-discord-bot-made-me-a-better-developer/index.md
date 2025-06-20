```json meta
{
  "title": "How Building A Discord Bot Made Me A Better Developer",
  "lead": "Some practical lessons I picked up while wrestling with WebSockets, concurrency, and Discord's Gateway API",
  "isPublished": true,
  "publishedAt": "2025-06-17",
  "openGraphImage": "posts/how-building-a-discord-bot-made-me-a-better-developer/og-image.png"
}
```

I wanted to build a Discord bot for my server. How hard could it be, right? Connect to Discord's API, listen for messages, respond when needed. Should be a weekend project.

Spoiler alert: it wasn't.

What I thought would be straightforward turned into a crash course in building resilient network clients. By the time I had a working `DiscordGatewayClient` that could stay connected reliably, I'd learned a bunch of things that made me a better developer.

Here's what building this bot taught me. I am hoping maybe some of it will be useful for your projects too.

## Abstractions Are Worth The Extra Work

My first instinct was to just use .NET's ClientWebSocket directly. Why add extra layers when I could just call the API?

Then I tried to write tests and immediately regretted that decision.

How do you test reconnection logic without actually connecting to Discord? How do you simulate network failures? How do you test your error handling without hitting rate limits?

So I bit the bullet and created some interfaces:

```csharp
internal interface IWebSocket : IDisposable
{
  WebSocketState State { get; }
  Task ConnectAsync(Uri uri, CancellationToken cancellationToken);
  Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken);
  Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken);
  Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken);
}

internal interface IWebSocketFactory
{
  IWebSocket Create();
}
```

Yeah, it felt like overkill at first. But once I had these abstractions, testing became so much easier. I could inject mock WebSocket implementations, simulate connection failures, and test edge cases without depending on Discord's servers.

The lesson here isn't groundbreaking: abstractions make testing easier. But it reinforced the fact that while they feel like extra work upfront future me always appreciates the effort.

## Async Doesn't Mean Thread-Safe

I've done a lot async programming. I've used Task.Run and handled CancellationToken parameters before. But building a WebSocket client that needs to handle multiple concurrent operations simultaneously taught me a few things.

The client needs to:

- Send heartbeats on a timer
- Continuously receive messages
- Handle reconnections when things go wrong
- Keep track of connection state
- All at the same time, without stepping on its own feet.

My first attempts had race conditions everywhere. Heartbeat timers would conflict with reconnection logic. State would get corrupted when multiple threads tried to update it simultaneously.

So I had to get serious about thread safety which meant using locks, but in the asynchronous context that meant using a `SemaphoreSlim` to control access to shared state. I ended up wrapping the `SemaphoreSlim` in a an `AsyncLock` class to make it easier to use. You can watch me discuss the specifics of that in [this video](https://youtu.be/E4OLKVlRxyI), but the value in it was that I could then mutate shared state safetly like this:

```csharp
private readonly AsyncLock _lock = new();

private async Task SetSequenceAsync(int? sequence, CancellationToken cancellationToken)
{
  using var _ = await _lock.LockAsync(cancellationToken);
  _lastSequence = sequence;
}
```

It does lead to a bit of boilerplate, but it was worth it to ensure that my WebSocket client could handle multiple operations concurrently without crashing or losing messages. Plus it means I don't have to spend time debugging and troubleshooting race conditions later.

I also learned that you can get pretty fancy with `CancellationToken`s like so:

```csharp
private async Task StartReceiveMessagesAsync(CancellationToken cancellationToken)
{
  var newReceiveCts = new CancellationTokenSource();
  var newReceiveLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(
    cancellationToken, newReceiveCts.Token
  );
    
  await SetReceiveCtsAsync(newReceiveCts, cancellationToken);
  await SetLinkedReceiveCtsAsync(newReceiveLinkedCts, cancellationToken);
    
  _ = Task.Run(async () => {
    // Long-running message loop
  }, newReceiveLinkedCts.Token);
}
```

This dual-token approach lets me handle two types of cancellation: external (when the app shuts down) and internal (when you need to restart a specific task during reconnection).

## Error Handling Is Where The Magic Happens


