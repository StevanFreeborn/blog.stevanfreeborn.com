```json meta
{
  "title": "Programming and Browser Automation: A Productivity Superpower",
  "lead": "Ever wish you could handle routine tasks without all the hassle? Using programming and browser automation, you can do just that. In this post, I'll share with you how I used these skills to automate renewing a daily subscription to the New York Times through my local library.",
  "isPublished": true,
  "publishedAt": "2024-08-07",
  "openGraphImage": "posts/programming-browser-automation-productivity-superpower/og-image.png",
}
```

## Glad I Learned That

I'm a big believer in this idea of [Skill Stacking](https://dariusforoux.com/skill-stacking/). It's sort of like the idea of compound interest, but for skills. The more skills you have, the more they can compound on each other and make you more valuable. And not just to others, but to yourself as well.

In the last couple years one of the skills I've been working on developing is programming. Mostly in the context of web development because it is extremely relevant to my work as a Quality Engineer. But I've also found that it's a skill that provides a ton of value in other areas of my life as well - like improving my productivity by having the ability to automate routine tasks.

## Why Am I Doing This?

I've been a long time listener of the [New York Times](https://www.nytimes.com/) podcast, [The Daily](https://www.nytimes.com/column/the-daily). It's a great way to stay informed on current events and I like that each episode spends a good amount of time diving deep into a single topic. It is what led me to pick up a subscription to the New York Times a few different times over the last couple years. And a few months ago I found out that my local library actually offers a free digital subscription to members.

Libraries really are such an underrated and underutilized resource, aren't they? But that is a topic for another day.

The only downside to this free subscription is it is basically like a day pass. You have to renew it every day by logging in to the library's website, clicking a link, and then logging in to your New York Times account. It is actually a pretty simple process with an uncomplicated user experience but after several weeks of doing this on an ad-hoc schedule I started to think why haven't I automated this yet?

It's a perfect candidate. It is the same sequence of steps every day. It's all accessible through a browser. And I know to program and automate a browser.

## TypeScript + Playwright + GitHub Actions = Automated

There is probably a few different ways to go about automating this, but I choose to stick with a set of tools I'm pretty familiar with at this point and are all free. I mean what is the point of automating something which is supposed to save you money if you have to pay to automate it?

I like TypeScript because it feels like a great balance between the flexibility of JavaScript and the type safety of a language like C#. I like Playwright because it is modern, actively maintained, and has a great API that takes care of a lot of the fragility that comes with automating a browser. I like GitHub Actions because my code is already on GitHub, it is free for public repositories, and has a really easy way of scheduling workflows using cron syntax.

## The Deets

I don't want to get too deep into the code here because if you are interested you can find it all in this [repo](https://github.com/StevanFreeborn/nytimes-sub-renewal), but I do want to share a bit of the approach I took.

### Start with Pseudo Code

Playwright has a great tool for recording a sequence of steps in a browser and then translating that into code. This is probably a great way to start if you aren't super familiar with the API. But I prefer to start by basically stepping through the process manually and writing out the steps in some pseudo code. So my first draft of the script looked something like this:

```typescript
// Navigate to the library's website
// Click the new york times link
// Login to the library's website
// Click the login link on the New York Times page
// Login to the New York Times website
// Check for text indicating successful renewal
```

### Make it Concrete

Then I went back and filled in the actual code for each step using the Playwright API. I also used some logging to help identify each step since the script will primarily be running in a CI/CD environment in headless mode. So the next draft looked something like this:

```typescript
import { env } from './env';
import { chromium } from 'playwright';

const browser = await chromium.launch({ headless: env.CI, slowMo: 3000 });
const context = await browser.newContext();
const initialPage = await context.newPage();

console.log('Navigating to library website');
await initialPage.goto('https://www.olathelibrary.org/online-resources/online-entertainment#enewspapers', { timeout: 300_000 });

console.log('Clicking on New York Times');
const libLoginPagePromise = context.waitForEvent('page');
await initialPage.getByRole('link', { name: /new york times/i }).click();
const loginPage = await libLoginPagePromise;

console.log('Logging in to library');
await loginPage.getByLabel('Username or Barcode:').pressSequentially(env.LIB_USERNAME, { delay: 150 });
await loginPage.getByLabel('PIN/Password :').pressSequentially(env.LIB_PASSWORD, { delay: 150 });
await loginPage.getByRole('button', { name: /log in/i }).click();
await loginPage.waitForURL(/nytimes/i);

console.log('Logging in to NYT');
await loginPage.getByTestId('login-lnk').click();
await loginPage.waitForURL(/myaccount\.nytimes/i);
await loginPage.getByLabel('Email Address').pressSequentially(env.NY_USERNAME, { delay: 150 });
await loginPage.getByTestId('submit-email').click();
await loginPage.getByLabel('Password', { exact: true }).pressSequentially(env.NY_PASSWORD, { delay: 150 });
await loginPage.getByTestId('login-button').click();
await loginPage.waitForLoadState('networkidle');

const confirmationText = loginPage.getByText(/set a calendar reminder to renew/i);
const confirmationTextVisible = await confirmationText.isVisible();

if (confirmationTextVisible) {
  console.log('Successfully renewed NYT subscription');
} else {
  console.log('Failed to renew NYT subscription');
}
```

> [!NOTE]
> I'm using [`dotenv`](https://www.npmjs.com/package/dotenv) and [`zod`](https://www.npmjs.com/package/zod) to load and validate environment variables. I've really been enjoying this pattern. It makes it really easy to keep track of environment variables available and ensure they are all present and of the correct type at runtime.

### Get Around Bot Detection

This all worked great locally and when running it with headless mode turned off, but when I tried moving it to a GitHub Actions workflow it failed. Turns out that the library's website was a little more sophisticated than I expected and was using some sort of bot detection that was blocking Playwright when running in headless mode. This is actually a pretty common issue with browser automation when working with live production sites that are primarily designed for human interaction.

I did though find two great packages that helped me get around this issue. The first is [`playwright-extra`](https://www.npmjs.com/package/playwright-extra) which extends the base Playwright API with the ability to use plugins. The second is [`puppeteer-extra-plugin-stealth`](https://www.npmjs.com/package/puppeteer-extra-plugin-stealth) which is a plugin that works with `playwright-extra` to implement a number of anti-detection techniques to help avoid bot detection such as changing the user agent. Including these packages required almost no changes:

```typescript
import { env } from './env';
import { chromium } from 'playwright-extra';
import stealth from 'puppeteer-extra-plugin-stealth';

chromium.use(stealth());

const browser = await chromium.launch({ headless: env.CI, slowMo: 3000 });
const context = await browser.newContext();
const initialPage = await context.newPage();

console.log('Navigating to library website');
await initialPage.goto('https://www.olathelibrary.org/online-resources/online-entertainment#enewspapers', { timeout: 300_000 });

console.log('Clicking on New York Times');
const libLoginPagePromise = context.waitForEvent('page');
await initialPage.getByRole('link', { name: /new york times/i }).click();
const loginPage = await libLoginPagePromise;

console.log('Logging in to library');
await loginPage.getByLabel('Username or Barcode:').pressSequentially(env.LIB_USERNAME, { delay: 150 });
await loginPage.getByLabel('PIN/Password :').pressSequentially(env.LIB_PASSWORD, { delay: 150 });
await loginPage.getByRole('button', { name: /log in/i }).click();
await loginPage.waitForURL(/nytimes/i);

console.log('Logging in to NYT');
await loginPage.getByTestId('login-lnk').click();
await loginPage.waitForURL(/myaccount\.nytimes/i);
await loginPage.getByLabel('Email Address').pressSequentially(env.NY_USERNAME, { delay: 150 });
await loginPage.getByTestId('submit-email').click();
await loginPage.getByLabel('Password', { exact: true }).pressSequentially(env.NY_PASSWORD, { delay: 150 });
await loginPage.getByTestId('login-button').click();
await loginPage.waitForLoadState('networkidle');

const confirmationText = loginPage.getByText(/set a calendar reminder to renew/i);
const confirmationTextVisible = await confirmationText.isVisible();

if (confirmationTextVisible) {
  console.log('Successfully renewed NYT subscription');
} else {
  console.log('Failed to renew NYT subscription');
}
```

That did the trick and allowed me to run the script in a GitHub Actions workflow without any issue, but the interwebz are a wild place and there is definitely a chance that I could run into other issues while running the script so adding a little bit of resilience is probably a good idea.

### Add Some Resilience

This isn't at all anything fancy or ground breaking. I took just a simple approach of wrapping the part of the script that actually does the user interaction in a `try/catch` block and then wrapping that in a `while` loop with a max number of attempts.

```typescript
import { env } from './env';
import { chromium } from 'playwright-extra';
import stealth from 'puppeteer-extra-plugin-stealth';

chromium.use(stealth()); // use stealth plugin

const browser = await chromium.launch({ headless: env.CI, slowMo: 3000 });
const context = await browser.newContext();
const initialPage = await context.newPage();

const maxAttempts = 3;
let attempts = 0;

while (attempts < maxAttempts) {
  console.log(`Attempt ${attempts + 1} of ${maxAttempts}`);

  try {
    console.log('Navigating to library website');
    await initialPage.goto('https://www.olathelibrary.org/online-resources/online-entertainment#enewspapers', { timeout: 300_000 });
  
    console.log('Clicking on New York Times');
    const libLoginPagePromise = context.waitForEvent('page');
    await initialPage.getByRole('link', { name: /new york times/i }).click();
    const loginPage = await libLoginPagePromise;
  
    console.log('Logging in to library');
    await loginPage.getByLabel('Username or Barcode:').pressSequentially(env.LIB_USERNAME, { delay: 150 });
    await loginPage.getByLabel('PIN/Password :').pressSequentially(env.LIB_PASSWORD, { delay: 150 });
    await loginPage.getByRole('button', { name: /log in/i }).click();
    await loginPage.waitForURL(/nytimes/i);
  
    console.log('Logging in to NYT');
    await loginPage.getByTestId('login-lnk').click();
    await loginPage.waitForURL(/myaccount\.nytimes/i);
    await loginPage.getByLabel('Email Address').pressSequentially(env.NY_USERNAME, { delay: 150 });
    await loginPage.getByTestId('submit-email').click();
    await loginPage.getByLabel('Password', { exact: true }).pressSequentially(env.NY_PASSWORD, { delay: 150 });
    await loginPage.getByTestId('login-button').click();
    await loginPage.waitForLoadState('networkidle');
  
    const confirmationText = loginPage.getByText(/set a calendar reminder to renew/i);
    const confirmationTextVisible = await confirmationText.isVisible();
  
    if (confirmationTextVisible) {
      console.log('Successfully renewed NYT subscription');
    } else {
      console.log('Failed to renew NYT subscription');
    }

    break;
  }
  catch (e) {
    console.error('Error: ', e);
    attempts++;
  }
}
```

Like I said, nothing fancy and extremely blunt, but for this use case it is probably plenty. If it does end up failing I'll get an email from GitHub Actions, I can check the logs, and then if needed I can rerun the workflow manually.

### Schedule the Workflow

I didn't show you yet the Github Actions workflow, but it is straightforward at this point. I checkout the repo, setup node, install dependencies, and run the script. I do though want it to run every day at midnight CST. This is where I was able to take advantage of the scheduling feature in GitHub Actions which supports cron syntax. Here is what the final workflow looks like:

```yaml
name: Run
on:
  workflow_dispatch:
  schedule:
    - cron: '0 6 * * *' # 6am UTC every day (midnight CST)
env:
  LIB_USERNAME: ${{ secrets.LIB_USERNAME }}
  LIB_PASSWORD: ${{ secrets.LIB_PASSWORD }}
  NY_USERNAME: ${{ secrets.NY_USERNAME }}
  NY_PASSWORD: ${{ secrets.NY_PASSWORD }}
  CI: true
jobs:
  renew_subscription:
    name: Renew Subscription
    runs-on: ubuntu-latest
    container:
      image: mcr.microsoft.com/playwright:v1.45.3-jammy
    steps:
      - name: Checkout repository
        uses: actions/checkout@v4
      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: lts/*
      - name: Install dependencies
        run: npm install
      - name: Run script
        run: npm run start
```

> [!NOTE]
> Its important to note that as part of my workflow I'm not installing the Playwright dependencies since I'm using the Playwright docker image which already has everything I need. If you are using a different image or running on a different platform you might need to install the Playwright dependencies.

## Why I Didn't I Do This Sooner?

Yeah, I know. Now that it is out there working for me I feel silly for the many minutes I spent renewing the subscription manually over the last few months. But I think this is a great example of what a force multiplier having a skill like programming can be in your life particularly for your own productivity. And sure some might say it is a small and trivial task, but it is a task that I no longer have to think about or worry about. And that is a win in my book.
