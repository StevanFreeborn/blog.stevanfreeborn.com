```json meta
{
  "title": "Why LLMs Don't Make Decisions",
  "lead": "We need to stop confusing 'highly accurate mapping' with 'decision-making.' The model isn't choosing to call a tool; it is simply completing the pattern you engineered.",
  "isPublished": true,
  "publishedAt": "2026-02-04",
  "openGraphImage": "posts/why-llms-dont-make-decisions/og-image.png"
}
```

There is a narrative in the agentic AI space that treats large language models as reasoning engines capable of making autonomous decisions. We talk about agents "deciding" to call a tool, "assessing" a PRD, or "choosing" a path of execution.

But I think we should be careful not to mistake highly accurate pattern completion for actual decision-making.

When you look under the hood, the model isn't "deciding" anything. It is simply completing a pattern. The only reason that pattern looks like a decision is because we, as engineers, have spent immense effort providing the exact right input context and system prompts to ensure the most statistically likely output aligns with what we want.

## The domino analogy

Think of it this way: If I set up a long row of dominoes to fall in a specific path, and the last domino falls and hits a bell, did that last domino "decide" to ring the bell?

Of course not. It just followed the physics of the environment I built.

LLMs are the same. The "decision" to call `get_weather(city="London")` is just the inevitable result of the constraints and context we've fed into the prediction engine. It is the physics of probability, not the agency of choice.

## The functional reality

If we strip away the magic, an LLM is effectively a pure, stateless function. Let's call it `PredictNext()`.

```csharp
ProbabilityDistribution PredictNext(int[] contextTokens, float[] fixedWeights)
{
    // insert matrix multiplication operations
    // more complex than I can explain here
    return probabilities;
}
```

1. You give it a static sequence of integers. Which remember, are just tokens representing words or pieces of words.
1. It pushes those integers through a fixed graph of matrix operations defined by its weights.
1. It returns a probability score for every word in its vocabulary.

There is no "state" inside that function where "pondering" happens. There is no loop where it weighs the pros and cons. It is a single, deterministic forward pass of linear algebra.

The "choice" only happens outside the model, when a sampler (based on temperature, top-k, etc.) picks the next token from that probability distribution. The model didn't "decide" to output a JSON bracket `{`; it just assigned it a 99.9% probability because you provided the words `return JSON` in the system prompt.

> [!NOTE]
> I know this is a simplification. There are more complex architectures and mechanisms (like attention) at play, but the core idea remains: LLMs are fundamentally pattern predictors, not decision makers. I found the book [_Build A Large Language Model From Scratch_](https://amzn.to/4fqvn0D) by [Sebastian Raschka](https://sebastianraschka.com/) to be a great resource for getting a better mental model of how these systems work.

## Where the logic actually lives

Calling it a "decision" gives the model credit for logic that actually lives in the **prompt engineering** and **agentic loop** we build around it.

When we build a reliable agent, we aren't creating a synthetic employee with judgment. We are creating a probabilistic slot machine, and then we are carefully trying to rig it so that it pays out the exact token we need 99% of the time. Or whatever success rate we deem acceptable for our use case.

It is still just next-token prediction. We have just gotten very, very good at engineering the context so that the "next token" is more often than not a useful one and not hallucinated poetry.

## Why I think it matters

Sure, some will say this is semantics. but I think it matters for how we build reliable software and systems.

If you believe the model is "deciding," you start trusting it to handle more and more ambiguity. You assume it has a mental model of your system and goals. You begin to rely on it for its nonexistent judgment.

But if you acknowledge that it is just completing a pattern, your engineering approach remains focused on controlling inputs and outputs. You design more robust prompts, better validation layers, and clearer context windows. You stop asking it to "think" and instead start focusing on:

1. Constraining the context so that the search space is reduced and the "right" token is more often the only probable one.
1. Rigging the inputs so the pattern that needs to be completed is the one that leads to the desired output.
1. Verifying the output because you know it didn't "choose" based on a belief it was correct, but rather it was the best statistical guess given the context.

## Conclusion

The model is a tool. It's a very powerful, non-deterministic, and probabilistic tool. But it is not a decision maker. That role still belongs to us, the engineers and designers who build the systems around it and line up the dominos so they fall more often than not right where we want them to.
