```json meta
{
  "title": "It Doesn't Work All the Time, But When It Does, It's Magic",
  "lead": "I've been playing with generative AI lately, looking at how you might build features with it. But I'm kind of struggling with seeing a roadmap to getting consistent quality out of this stuff for specific use cases. At times it feels like trying to herd cats. Granted I'm a relative noob, but here are some of my initial thoughts and questions",
  "isPublished": true,
  "publishedAt": "2024-05-04",
  "openGraphImage": "posts/navigating-generative-ai-implementation-complexity/og-image.png",
}
```

Over the last couple weeks I’ve been spending some of my free time working with generative AI.

What I’ve found most interesting is that seems like currently implementations exists on a very narrow spectrum of complexity. On the lower end you have something like “take user input, send it to a LLM provider, and give back the response to the user” with a fair warning to the user that results may vary. This to me is very much the product that OpenAI built initially with GPT-3. They even were fairly open about the fact that it was mainly an experiment and that they didn’t know what people would do with it.

But as soon as you want to do something more sophisticated or provide a more concrete guarantee of the quality of responses - which seems definitely the case if you want to put AI into your product in a meaningful way - you go immediately to needing to either pursue some sort of retrieval augmented generation approach (RAG) or use fine tuning.

The RAG approach feels kind of brittle and fine tuning puts you into a whole other world of considerations, both also present this question of “where do you get the data?”. Either way as the product owner you immediately gain a whole new set of challenges to overcome to if you are going to build something that you can confidently put in front of users to solve a problem over and over again and charge them for it.

I mean when you do get a response that was exactly what you were expecting it feels magical, but pushing that from “it works” to “it works every time for everyone” seems really challenging. And from a quality perspective I’m still a little stumped at how you’d best approach providing good assurance to something that is by nature non-deterministic?

For example it’s pretty accepted in data analytics use cases that if a user makes a request to get a report of data they should be able to have a high degree of confidence that the data they are looking at is accurate and can be used to make decisions nearly 100% of the time. If that same data were to be called up through an LLM and summarized and the LLM delivers an inaccurate summary is it acceptable to just ask the user to try a different prompt? Does then hit or miss quality of LLMs become sort of an accepted quirk?

I’m sure there is a ton of way smarter people than me working on this stuff, but after some initial experiments these are some of the things I’m thinking about and wondering how to tackle.

I know there is so much pressure on companies right now to be a part of the AI wave, but in my - knowing limited - experience it seems like using it in a meaningful way is a pretty big undertaking that requires a lot of considerations both technical and non-technical.
