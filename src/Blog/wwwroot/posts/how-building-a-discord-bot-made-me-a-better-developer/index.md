```json meta
{
  "title": "How Building A Discord Bot Made Me A Better Developer",
  "lead": "Some practical lessons I picked up while wrestling with WebSockets, concurrency, and Discord's Gateway API",
  "isPublished": true,
  "publishedAt": "2025-06-20",
  "openGraphImage": "posts/how-building-a-discord-bot-made-me-a-better-developer/og-image.png"
}
```

I wanted to build a Discord bot for my server. How hard could it be, right? Connect to Discord's API, listen for messages, respond when needed. Should be a weekend project.

Spoiler alert: it wasn't.

What I thought would be straightforward turned into a crash course in building resilient network clients. By the time I had a working `DiscordGatewayClient` that could stay connected reliably, I'd learned a bunch of things that made me a better developer.

Here's what building this bot taught me. I am hoping maybe some of it will be useful for your projects too.

> [!NOTE]
> The complete source for the bot is available on [GitHub](https://github.com/StevanFreeborn/steves-bot) if you want to see how it all fits together. You can also find more details on Discord's [Gateway documentation](https://discord.com/developers/docs/events/gateway) if you want to understand the protocol better.

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

My initial implementation worked great when everything went perfectly. Then I deployed it and discovered that networks are unreliable, messages sometimes get corrupted, and connections drop at inconvenient times.

This forced me to think about what happens when things go wrong:

```csharp
try
{
  e = await JsonSerializer.DeserializeAsync<DiscordEvent>(
    memoryStream,
    _jsonSerializerOptions,
    _linkedReceiveMessageCts.Token
  );
}
catch (JsonException ex)
{
  _logger.LogError(ex, "Failed to deserialize message: {Message}", msgFromMemoryStream);
  _logger.LogDebug("Message from StringBuilder: {Message}", msgFromStringBuilder);
}

if (e is null)
{
  _logger.LogInformation("Received null event.");
  continue; // Keep processing other messages
}
```

Instead of crashing when Discord sends malformed JSON (which does happen occasionally), the client logs the issue and keeps running.

The reconnection logic also had to be pretty careful about cleanup:

```csharp
private async Task ReconnectAsync(CancellationToken cancellationToken)
{
  // Cancel background tasks first
  await CancelHeartbeatTaskAsync(cancellationToken);
  await CancelReceiveMessagesTaskAsync(cancellationToken);
    
  // Reset state
  await SetHeartbeatSentAsync(DateTimeOffset.MinValue, cancellationToken);
  await SetHeartbeatAcknowledgedAsync(DateTimeOffset.MinValue, cancellationToken);

  // Close connection properly
  var closeStatus = _canResume ? WebSocketCloseStatus.MandatoryExtension : WebSocketCloseStatus.NormalClosure;
  await CloseIfOpenAsync(closeStatus, "Reconnecting", cancellationToken);
    
  _webSocket?.Dispose();

  // Decide whether to resume or start fresh
  if (_canResume)
  {
    await SetWebSocketAsync(_webSocketFactory.Create(), cancellationToken);
    await ConnectWithResumeUrlAsync(cancellationToken);
    await StartReceiveMessagesAsync(cancellationToken);
    return;
  }

  await ConnectAsync(cancellationToken);
}
```

Every reconnection needs careful orchestration: cancel tasks, reset state, close connections, dispose resources, then decide whether to resume or restart.

Implementing features is fun, but most apps are really defined by how they handle errors and edge cases.

## Resource Management Matters Even More In Long-Running Apps

Discord bots typically run 24/7, which means resource leaks that might not matter in short-lived applications become real problems.

I had to be careful about disposing everything properly:

```csharp
public void Dispose()
{
  _heartbeatCts?.Dispose();
  _linkedHeartbeatCts?.Dispose();

  _receiveMessageCts?.Dispose();
  _linkedReceiveMessageCts?.Dispose();

  _webSocket?.Dispose();
  _lock.Dispose();
}
```

But disposal alone isn't enough. I also needed to make sure scoped resources were cleaned up promptly:

```csharp
if (_eventHandlers.TryGetValue(eventType, out var handler))
{
  try
  {
    await using var scope = _serviceScopeFactory.CreateAsyncScope();
    await handler(de, scope.ServiceProvider, cancellationToken);
  }
  catch (Exception ex)
  {
    _logger.LogError(ex, "Error handling event: {Event}", eventType);
  }
}
```

The await using ensures that any dependencies resolved for event handling get disposed immediately, not whenever the garbage collector gets around to it.

In long-running applications, every IDisposable object and every background task is a potential resource leak.

## What I'd Do Differently

Looking back, there are a few things I might approach differently:

The numerous `SetXAsync` methods for state management work but create a lot of boilerplate. A more sophisticated state management approach might reduce the repetition.

Some values like buffer sizes and timeouts are hardcoded when they probably should be configurable.

But overall, the patterns I learned here—abstractions for testability, careful synchronization, defensive error handling, and proper resource management—have been useful in other projects too.

## Conclusion

Building this Discord bot wasn't earth-shattering, but it was a good reminder that even seemingly simple projects can teach you things. Working with real-time connections, managing concurrent operations, and integrating with complex APIs forces you to think about problems you might not encounter in typical CRUD applications.

If you're looking for a side project that will stretch your skills a bit, I'd recommend building something that involves persistent connections or real-time communication. The problems you'll run into like handling failures gracefully, managing state across concurrent operations, and working with finicky protocols are pretty transferable to other kinds of systems.

Plus, you'll end up with a working Discord bot, which is pretty satisfying.
