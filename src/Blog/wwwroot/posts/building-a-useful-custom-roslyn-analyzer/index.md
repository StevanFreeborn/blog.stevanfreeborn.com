```json meta
{
  "title": "Building a Roslyn Analyzer for the Flag Argument Hack",
  "lead": "A colleague needed to introduce async APIs into a legacy codebase. The Flag Argument Hack was working well until a PR review revealed a subtle bug. I got nerd sniped into building a Roslyn analyzer to catch it automatically.",
  "isPublished": true,
  "publishedAt": "2026-06-30",
  "openGraphImage": "posts/building-a-useful-custom-roslyn-analyzer/og-image.png"
}
```

A colleague at work recently inherited a task that depending on who you are sounds like a ton of fun or a whole lot of work: take an extremely legacy, all-synchronous codebase and start making asynchronous APIs available without breaking anything. The kind of codebase where the service layer was written before `async` and `await` existed, and touching one method means tracing calls through layers of business logic you don't really want to be forced to refactor. This is particularly challenging because of the viral nature of `async`/`await`. Once you introduce it, it propagates through the call stack. If you don't want to make everything async, you need a way to expose both synchronous and asynchronous APIs side by side, which itself is a challenge because you don't want to maintain two separate implementations of the same logic.

Luckily, though this challenge of moving from synchronous to asynchronous APIs is a common one that teams in the .NET ecosystem have faced for years and generous folks have taken the time to share their experiences and solutions. One helpful resource my colleague found was Stephen Cleary's 2015 MSDN Magazine article, "Brownfield Async Development," and particularly the approach Cleary calls the **Flag Argument Hack** which he credits to Stephen Toub. The idea is pretty straightforward: write a private `CoreAsync` method that takes a `bool sync` parameter. When `sync` is `true`, call the synchronous API. When it's `false`, `await` the async one. Then expose thin public wrappers that call `CoreAsync` with the appropriate flag.

```csharp
private async Task<string> GetCoreAsync(int id, bool sync)
{
  return sync
    ? _dataService.Get(id)
    : await _dataService.GetAsync(id);
}

public string Get(int id) => GetCoreAsync(id, sync: true).GetAwaiter().GetResult();

public Task<string> GetAsync(int id) => GetCoreAsync(id, sync: false);
```

Because the task is already completed when `sync` is `true`, calling `.GetAwaiter().GetResult()` on it can't deadlock. It just reads the value straight out. Clean, clever, and it avoids the code duplication that comes with maintaining separate sync and async implementations side by side. Granted boolean flags are often considered a code smell, but in this case the tradeoff is worth it. The alternative is to maintain two separate implementations of the same logic, which is a much bigger headache.

The use of the Flag Argument Hack has been working well for my colleague and their team. They were able to introduce async APIs into the legacy codebase without breaking anything, and the pattern was being applied consistently across the codebase. Code reviews were going smoothly, and progress was being made on a genuinely difficult migration.

Then, during a PR review, someone noticed a call where the sync flag had been dropped. Someone missed passing the parameter from the enclosing method. A tiny mistake really, one argument in one method call and it looks harmless in isolation. The parameter had a default value, so the compiler doesn't complain. But in production, that synchronous wrapper would hang forever.

I saw someone mention this issue in a PR review and then a subsequent reminder in a team chat. This got me thinking about how easy it is to forget to pass the sync flag through a call chain, and how easy it is to miss during code review. I wondered if there was a way to catch this kind of bug programmatically, before it ever reached a human reviewer. I knew that libraries like xUnit have their own sorts of analyzers that catch common mistakes or flag certain antipatterns. I wondered if I could build a Roslyn analyzer that would catch this specific issue. Not to mention, I've always been a bit curious about how all that diagnostic and code fix stuff works under the hood so I figured this would be a good opportunity to kill two birds with one stone: build a useful analyzer and learn more about Roslyn analyzers in the process.

## The Forwarding Problem

When your service layer exposes `GetCoreAsync(int id, bool sync)` and your business logic calls it inside its own `GetFrobCoreAsync(bool sync)`, you need to pass that flag through:

```csharp
return await _dataService.GetCoreAsync(17, sync);
```

It's easy to forget. Maybe you're in a hurry. Maybe the parameter has a default of `false` so the compiler stays quiet. Maybe someone reads the signature and thinks "we'll never actually call the sync path from here." Whatever the reason, when the sync flag gets dropped or hardcoded, the synchronous wrapper silently deadlocks at runtime.

This is the kind of bug that:

- Passes code review (it _looks_ fine)
- Passes compilation (default parameter values are valid)
- Fails at runtime in production

This is also the sort of issue that seems like the extensibility of Roslyn analyzers was made for. It is an architectural violation specific to our particular use of the Flag Argument Hack, and it is something that can be detected statically.

## SYNC001: The Analyzer

I built a Roslyn analyzer called [StevanFreeborn.AsyncSyncFlagAnalyzer](https://github.com/stevanfreeborn/StevanFreeborn.AsyncSyncFlagAnalyzer) that flags every `await` expression where the sync parameter is dropped or hardcoded in an optionally-asynchronous call chain.

The analyzer registers a `SyntaxNodeAction` on `AwaitExpression` nodes. When it finds one:

1. It walks up to the enclosing method and checks whether that method has a parameter matching a known sync name (default `sync`, configurable via `.editorconfig`).
2. It resolves the invoked method's symbol and checks whether _it_ also has a sync parameter.
3. It walks the invocation's arguments. If none of them reference the enclosing method's sync parameter by identifier, it reports **SYNC001**.

The code fix handles three cases:

| Before                          | After                          |
| ------------------------------- | ------------------------------ |
| `GetDataAsync(17)`              | `GetDataAsync(17, sync)`       |
| `GetDataAsync(17, false)`       | `GetDataAsync(17, sync)`       |
| `GetDataAsync(17, sync: false)` | `GetDataAsync(17, sync: sync)` |

It preserves named arguments, handles out-of-order parameters, and works when sync isn't the last parameter in the target method signature.

In all honesty, implementing the code fix was much more challenging than the analyzer itself. The analyzer is a simple check for a specific pattern, but the code fix has to understand the context of the invocation and how to rewrite it correctly. I had to dig into Roslyn's syntax APIs and learn how to manipulate syntax trees effectively. This was made a lot more difficult by the fact that it seems like Microsoft doesn't have a lot of helpful documentation on the code analysis APIs. I spent a fair amount of time reading through the source code of other analyzers and looking at how they did things. Particularly, the [analyzer](https://github.com/dotnet/sdk/blob/main/src/Microsoft.CodeAnalysis.NetAnalyzers/src/Microsoft.CodeAnalysis.NetAnalyzers/Microsoft.NetCore.Analyzers/Runtime/ForwardCancellationTokenToInvocations.Analyzer.cs) and [code fix](https://github.com/dotnet/sdk/blob/main/src/Microsoft.CodeAnalysis.NetAnalyzers/src/Microsoft.CodeAnalysis.NetAnalyzers/Microsoft.NetCore.Analyzers/Runtime/ForwardCancellationTokenToInvocations.Fixer.cs) that exists for making sure you forward cancellation tokens through async call chains as it was solving a similar problem to what I was trying to solve.

One part of the analyzer I'm proud of is that it is configurable. I think if you were building this analyzer purely only to be ever used in the context of your own codebase, you could likely do without this feature. But since I knew I wanted to share what I built with others in case they might also find it useful, I wanted to make sure it was flexible enough to be used in other codebases that might have different naming conventions for their sync parameter. So I made the sync parameter name configurable via `.editorconfig`. You can specify a comma-separated list of names that the analyzer will recognize as valid sync parameters. The default is just `sync`, but you can add others if your codebase uses different conventions.

```ini
[*.cs]
dotnet_diagnostic.SYNC001.additional_sync_names = runSynchronously, isSync
```

By default the severity is **Error**, which I chose intentionally as this really should always be caught because it is incorrect otherwise. But you can dial it down to warning if that's too aggressive. Configurability for the win.

## Conclusion

There are probably some different ways you could go about addressing this both from a programming and/or process perspective. You could require unit tests. You could add some sort of explicit PR review checklist for this particular item. But the thing about this sort of architectural pattern is that the call chains can become quite deep. Service layer → business logic → orchestration layer → UI. By the time you're five layers deep, relying on "just be careful" or "don't forget to" isn't really a great strategy. Not to mention doing some sort of one-time audit of all changes every cycle would get really laborious. But the flexibility and extensibility of the Roslyn APIs give us an alternative. A custom analyzer like this allows us to offload the responsibility to deterministic machines we can count on to be way better at catching this issue and deliver the feedback while you are writing the code or building the solution.
