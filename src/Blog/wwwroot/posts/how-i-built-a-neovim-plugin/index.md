```json meta
{
  "title": "How I Built A Neovim Plugin",
  "lead": "I got tired of clunky terminal splits and constantly breaking my focus just to check test runs. Here is the story of how and why I built a native, async Neovim wrapper for the watchexec CLI.",
  "isPublished": true,
  "publishedAt": "2026-07-12",
  "openGraphImage": "posts/how-i-built-a-neovim-plugin/og-image.png"
}
```

<video src="https://share.stevanfreeborn.com/blog.stevanfreeborn.com/how-i-built-a-neovim-plugin/example.mp4" controls title="Plugin demonstration"></video>

If you spend some time writing code, it doesn't take long before you become pretty particular about your workflow. I think a lot of it comes down to minimizing as much friction and context management as possible. Basically, anything to keep you in that flow state for just a few minutes longer. For me, a really productive workflow looks something like: write a test, save the file, watch the tests run, see the result, and fix the code. The quicker and smoother that happens, the more I'm likely to get done in any given chunk of time. Recently, I've come to really enjoy using this handy little CLI (written in Rust, btw) called [watchexec](https://github.com/watchexec/watchexec). It is a fantastic, fast CLI tool for triggering commands when files change. It is really flexible and works within just about any development context.

But I've been struggling with a way to use it the way I want without having to break my focus and switch contexts outside of Neovim. I tried a few different things, but for the most part, I had just settled on running it in a separate terminal pane and then either having that pane open on a separate monitor if I was at my desk, or just flipping back and forth between tabs in Windows Terminal. It wasn't terrible, but then I wondered how difficult it might be to build my own wrapper for it so I could just keep all the context and my focus within Neovim. It is really similar to how I've migrated to using [lazygit](https://github.com/jesseduffield/lazygit) through a [plugin](https://github.com/kdheepak/lazygit.nvim) versus running it in a separate terminal pane.

So I decided to take the plunge and try to build my first Neovim plugin, and man, did I learn a lot. It is actually kind of insane how extensible and rich the Neovim API is. Here’s a look at why I wanted to tackle it, and what I figured out along the way.

> [!NOTE]
> The project is fully open-source, and I’d love for you to try it out, break it, or check out the code. If you want to add it to your setup via `lazy.nvim`, you can find everything right [here](https://github.com/StevanFreeborn/watchexec.nvim).

## Trying to fix my own workflow

To be fair, there are plenty of ways to run tasks in Neovim, but I kept running into minor frustrations with my setup. Using a basic blocking shell command like `:!` was not feasible because it freezes the whole editor until the command finishes. On the flip side, opening a standard native `:terminal` split often felt a bit clunky to manage, stole my cursor focus, and left me staring at raw, unparsed ANSI color escape codes that made some outputs harder to read.

I didn't want a massive, all-singing, all-dancing plugin. I just wanted to see if I could build a focused tool that checked a few specific boxes for my own UX. The watcher needed to run quietly in the background without making my editor stutter. I wanted to strip out those raw ANSI sequences and map test failures or successes to some native Neovim diagnostic highlights. If I closed the main log window to get screen space back, I still wanted some sort of non-intrusive indicator to let me know if things were currently passing or failing in the background. And I wanted the minimum amount of work for having to tell Neovim about where the `watchexec` binary lives, whether I was working on Linux, Windows, or in a WSL environment.

## Breaking down the architecture

When I first sat down to write this, I realized fairly quickly that a monolithic Lua file wasn't going to be a good idea. I decided to separate the logic into distinct modules to keep myself sane. I mapped out `init.lua` to act as the entryway for the plugin's setup and commands, while `config.lua` handled all the default user options and deep-merging configurations. The core of the plugin lives in `runner.lua`, which is where I spent a lot of time figuring out how to manage process lifecycles and background jobs. I isolated all the tedious math for drawing buffers, adjusting window sizes, and toggling visibility into `window.lua`, and then threw the state tracking for that minimal background status window into `indicator.lua`. I admit I am far from a Lua expert (in fact, this plugin is by far the most Lua I've ever written) but it was really helpful to look at other Neovim plugins and draw a lot of inspiration from them.

## The technical challenges

As most of us who write code know, it’s one thing to have an idea, but a whole other thing to actually implement it. The async experience I wanted meant I had to do a fair bit of learning and reading about the Neovim API. I still have some skepticism about whether I did it all correctly. But hey, it works on my machine. In order to make sure the background process didn’t lock up my cursor, I relied on `vim.fn.jobstart` to pipe `stdout` and `stderr` asynchronously. One tricky piece I had to figure out was cleanup: if a user manually forces a rerun, the plugin needs to cleanly kill the existing job before starting a new one. I also had to hook into the `VimLeavePre` autocommand so that if I abruptly closed my editor, I wouldn't leave a bunch of zombie processes running in the background. This was probably the thing I was most concerned with, so I tended to keep Task Manager open while I was developing and testing to make sure I caught whenever I produced an orphaned process.

Raw terminal text is also incredibly messy. I spent some quality time writing regex patterns (read: talking to Gemini about what I wanted) to strip out raw ANSI codes so the output looked clean. Once the text was sanitized, I set up basic string matchers to scan for words like `ERROR`, `FAIL`, or `PASSED` so I could attach them directly to Neovim’s native `DiagnosticError` and `DiagnosticOk` highlight groups.

Probably most important to me was the ability to hide the output window to focus on coding but still receive feedback. I ended up configuring the plugin to scan the live text stream for status changes, like `[Running]` or `[Command was successful]`. If the main log split is closed, it triggers a tiny, non-focusable floating window in the corner of the screen to give me a subtle heads-up on the status without getting in my way. I also wanted to make sure this worked well regardless of the OS, so I wrote a `find_binary()` helper that dynamically scans the user's path, appends `.exe` on Windows, checks Homebrew locations on macOS, and routes through `wsl.exe --exec` if it detects it's running inside a Windows host environment.

## Automated testing for a plugin

I'd be a pretty big hypocrite if I built a project like this and didn't test it given what I do for a living, but I hate manually testing things that can almost certainly be automated. Lucky for me, someone else obviously has somewhat similar feelings in the Neovim community because they've got this awesome, though unfortunately unmaintained, utility called [Plenary](https://github.com/nvim-lua/plenary.nvim) that makes it easy to build and run your [Busted](https://lunarmodules.github.io/busted/) unit tests. Because dealing with system binaries and floating windows involves a lot of live side-effects, I relied heavily on stubbing via `luassert.stub` to keep the tests predictable. I know, I know... some of you are going to have some strong feelings about that, but it's my project, so stub I will. This let me test more complex, async UX pieces, like making sure the log window automatically drops the oldest lines when it hits a `max_lines` limit, or ensuring layouts recalculate gracefully when a `VimResized` event occurs.

For local peace of mind, I put together a quick PowerShell script called `run_checks.ps1` to run formatting and linting. In CI, I set up an automated workflow to guarantee `StyLua` formatting stays consistent and the language server doesn't complain on new pull requests.

## Conclusion

In this "build a project in a day with AI" world, it was a lot of fun to sit down and dig into a new area of development in a language I know very little about. It was also a great reminder that oftentimes the difference between a frustrating tool and a satisfying one comes down to the tiniest details. From an end-user perspective, having the ability to address those details in the way that best suits you is incredibly delightful. At the end of the day, there’s no magic happening inside `watchexec.nvim`. It’s just the result of combining a great CLI tool with a bit of async orchestration and some layout management. But in my experience, it is additions like this that make your workflow your own. Plus, who doesn't like being able to change the way their editor works with a bit of code?

