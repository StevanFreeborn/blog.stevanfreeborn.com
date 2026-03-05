```json meta
{
  "title": "Can LLMs Improve Their Own Prompts?",
  "lead": "I built an experiment to automate prompt engineering by letting an LLM critique and refine its own instructions. Here is a deep dive into the architecture, the costs, and whether meta-prompting actually works in practice.",
  "isPublished": true,
  "publishedAt": "2026-03-05",
  "openGraphImage": "posts/can-llms-improve-their-own-prompts/og-image.png",
}
```

A while back I wrote about [building an app for generative AI evaluations](https://blog.stevanfreeborn.com/building-an-app-for-generative-ai-evaluations) and how I'd been noodling on the idea that prompt engineering is really just another form of iterative software development. You form a hypothesis, you test it, you measure, you adjust. The difference is your artifact is text and your test harness is an LLM.

That got me thinking: what if we closed the loop entirely? What if instead of a human reviewing the error logs and rewriting the prompt, we handed that job to the model itself?

That question became [The Nag](https://github.com/StevanFreeborn/the-nag).

> [!NOTE]
> The source code is available [here](https://github.com/StevanFreeborn/the-nag). It's an experiment, not a polished library. Treat it accordingly.

## What I was trying to answer

The core question was deceptively simple:

*Can an LLM effectively critique and improve its own prompts?*

But once I started pulling on that thread a few more questions came with it:

- Does a training/validation split help detect overfitting to specific test cases?
- Does prompt quality consistently improve over iterations, or does it plateau or even regress?
- Can this pattern generalize across different domains and result types?

I had seen a lot of writing about "meta-prompting" as a concept but not a lot of concrete, runnable examples of what it actually looks like in practice at the code level. So I decided to build one.

## How it works

The system is organized around a concept I called a **scenario** — a self-contained unit that bundles together everything needed for one optimization run. Think of it as the equivalent of a test suite, but instead of testing your code, it trains and evaluates a prompt.

Each scenario defines:

- An `InitialPrompt` — intentionally bad to make the improvement signal obvious
- `TrainingCases` — test cases that drive the refinement loop
- `ValidationCases` — held-out cases that measure whether improvements generalize
- A `Judge` — a domain-specific evaluator that scores AI output against known ground truth
- A `MetaPrompt` factory — the function that assembles the refinement prompt from the current prompt and accumulated error logs
- A `TargetScore` and `MaxIterations` to bound the process

The `Optimizer` then runs the loop:

```txt
For each iteration:

Training Cases ──▶  Prompt + Context ──▶ AI ──▶ Judge ──▶ Score
                                               │
Validation Cases ──▶ Prompt + Context ──▶ AI ──▶ Judge ──▶ Score
                                               │
              Combined Error Logs ──▶ Meta-Prompt ──▶ Refined Prompt
```

Training scores drive the refinement. Validation scores are a read-only signal. They tell you whether the improved prompt is actually getting better at the general task or just better at the specific training cases. Refinding the prompt content so that it is overly specific to the training cases is a real risk and the validation split is the only way to detect it.

The two models are intentionally different. `gemini-2.5-flash` handles the structured task responses because it is fast, cheap, and constrained to a JSON schema so the output is always parseable. `gemini-2.5-pro` handles the meta-prompting because it is better suited to the heavier reasoning work of analyzing what went wrong and rewriting the prompt. Using a more capable model for the refinement step felt right given that task is arguably the harder of the two.

## The example: compliance control mapping

To make this concrete I built an example around a domain I've heard can be rather time-consuming for humans to do and could be a huge value-add if you had an LLM that is able to reliably assist you with it: mapping organizational policy documents to compliance frameworks. In this case the specific task was mapping an Access Control Policy to the controls in ISO 27001:2022.

The starting prompt was intentionally terrible:

```txt
"yo heres a policy. I need to know if it helps me become ISO 27001:2022 compliant."
```

The training case was an Access Control Policy with 10 known controls and their expected compliance status (Compliant, Partial, or Gap). The validation case was a completely separate Encryption Policy with 6 different controls — unseen during the refinement process — but evaluated using the same criteria.

The judge scored each control evaluation on a 10-point rubric:

- **7 points** for getting the compliance status right
- **3 points** for providing a meaningful verbatim quote from the policy
- **0 points** if the control was skipped entirely

Target score was 95%. Maximum iterations was 5.

Watching the prompt evolve across those iterations was genuinely interesting. The initial prompt produced responses that were vague, missed controls entirely, and never cited policy text. By the second or third iteration the refined prompt was explicitly instructing the model to enumerate every control, classify each one using specific criteria, and anchor every assessment to a direct quote from the source document. The model was essentially teaching itself to be more rigorous.

## What I actually learned

**The loop works, but the judge is really important.**

The quality of the refinement is only as good as the quality of the error signals. If your judge is fuzzy or inconsistent, the meta-prompt has nothing useful to work with. I spent more time thinking about the scoring rubric than I did about anything else in the system. I am still not sure I got it right, but the feedback from the judge is really the most meaningful signal in the whole loop. If you are building something like this, spend a lot of time on the judge.

**Training/validation split is worth the overhead.**

I was a little skeptical going in about whether a validation split was really necessary for something this small. It is. Even with a single training case and a handful of iterations I could see the validation score diverge from the training score in interesting ways. It gave me a signal I wouldn't have had otherwise about whether the prompt was becoming more general or just more specific to the training data. I found that 9 times out of 10 without the validation the model would end up with a prompt that was basically just a more verbose version of "here's the training case, do that but better."

**The model is not actually reasoning or learning.**

I've written before about [why LLMs don't make decisions](https://blog.stevanfreeborn.com/why-llms-dont-make-decisions). Building The Nag reinforced that view pretty hard. The meta-prompt step feels like the model is "reasoning" about its own failures, but really it is pattern-completing a prompt rewrite based on the error logs you've provided. The quality of that rewrite is almost entirely determined by how well you structure the error context and the meta-prompt itself. It is still just very good text completion. This is why the feedback from the judge is so critical. If you give the model clear, consistent signals about what went wrong and what right looks like, it can produce a much better refinement. If your error logs are noisy or your meta-prompt is vague, the model has no chance to produce anything useful.

**Structured output is best.**

Constraining the task model's output to a JSON schema via `ResponseSchema` in the Gemini API was one of the best decisions I made. It eliminated an entire category of problems around parsing and meant the judge could be a pure evaluation function rather than also having to handle malformed responses. If you are building anything like this you should definitely lean into whatever capabilities your model and framework have for structured output. It makes the whole system more robust and easier to reason about.

**You need observable feedback to learn.**

After every run the system writes a full session report to disk showing every iteration, every prompt, every score, and every per-test-case result including the raw AI response. I almost cut this feature to keep things simple and I'm glad I didn't. Going back through those reports and reading how the prompt evolved was where most of my actual learning came from. The numbers are useful but the text tells the story. Not to mention these session reports make for great artifacts to share with others who are interested in the process or with your favorite LLM to ideate about how you might improve the system further.

## The cost problem

Here is the part I didn't fully appreciate going in: even my small amount of experimentation — a single scenario, a handful of test cases, a few runs — ran up about $5 in API costs.

That sounds trivial. And for one experiment, it is.

But here is the problem. The conclusions you can draw from running one scenario with one training case and five iterations a handful of times are pretty limited. To actually learn something meaningful from this kind of system you'd want to:

- Run across many different scenarios and domains
- Use significantly more training and validation cases per scenario
- Compare a large number of iterations against a smaller number to understand where diminishing returns set in
- Experiment with token or dollar budgets as stopping criteria instead of a fixed iteration count
- Test whether the approach degrades gracefully when the judge is noisier or the ground truth is less clean

Each one of those axes multiplies your cost and your time. And inference is slow. A single run of even a modest scenario can take several minutes when you're chaining multiple API calls per iteration. Scale that up to the kind of systematic experimentation that would let you say something with any real confidence and you are talking about a lot of wall-clock time and a non-trivial amount of money.

For a team or a company this is probably fine. You budget for it the same way you budget for compute or testing infrastructure. But for an individual developer doing this on their own time, it is a real constraint. The alternative — self-hosting models so you're running on your own compute — solves the per-token cost problem but introduces a different one. Good hardware capable of running the models you'd actually want to use is expensive to acquire and maintain. You're trading one kind of cost for another.

What this means practically is that the interesting follow-up experiments are easy to identify and hard to actually run. I know what I'd want to test. I just can't afford — in time or money — to test it rigorously enough to trust the results. And running experiments that aren't rigorous enough to trust feels like a waste of the time and money you did spend.

It's a bit of a catch-22 if you are someone who is interested in this type of research but doesn't have the resources of a company behind them. I guess I need to start saving up some of my Christmas and birthday money for some decent hardware to host my own models and hope that the inference cost curve continues to move in the right direction.

## Where this could go

The Nag is an experiment, not a product, but the pattern it demonstrates feels broadly applicable. Any time you have:

- A repeatable task you want an LLM to perform
- A way to score the output against known good answers
- The patience to write a reasonable meta-prompt

...you have the ingredients for this loop. Compliance mapping was a convenient domain to test with but the same architecture would work for extraction tasks, classification tasks, summarization tasks, or anything else where you can define what "right" looks like. Spending time handcrafting a prompt for these kinds of tasks feels like a lot of unnecessary trial and error when you could just let the model do the work of refining itself. This pattern is a way to automate that process and get better results with less human effort.

The more interesting open question to me is whether this approach can scale. The example uses 10 training controls and 6 validation controls. What happens at 100 controls? What happens when the ground truth itself is ambiguous? What happens when you have no ground truth at all and have to use an LLM as the judge?

Those are harder problems and probably worth a follow-up experiment at some point. Whether I actually get there depends a lot on whether the cost and time equation ever gets more favorable for an individual developer.

## Wrapping up

If you are interested in prompt engineering, LLM evaluations, or just like watching AI systems improve themselves in a small and very bounded way, I think there is something worth looking at in [The Nag](https://github.com/StevanFreeborn/the-nag). The code is straightforward C# and the concepts are not specific to any particular model or framework.

The main thing I would leave you with is this: the value is not in the automation of the prompt improvement itself. The value is in building the scaffolding that forces you to define what good looks like before you start. The loop is almost secondary. The discipline of writing a judge and assembling real test cases is where the actual work is. And that work is worth doing whether you automate the refinement or not.

Just go in with your eyes open about what it costs to do it well.

