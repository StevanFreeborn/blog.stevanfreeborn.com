```json meta
{
  "title": "A Trick for Designing Friendly APIs in Go",
  "lead": "Stop using magic booleans and confusing config structs. Learn why the Functional Options Pattern is the gold standard for writing clean, extensible APIs in Go.",
  "isPublished": true,
  "publishedAt": "2026-01-16",
  "openGraphImage": "posts/functional-options-pattern-in-go/og-image.png"
}
```

There is nothing worse than opening a codebase, finding a constructor, and seeing a list of magic numbers and boolean flags.

You know the ones:

```go
server := NewServer(true, 30, false)
```

What is `true`? What is `30`? Is `false` good or bad? You have to go read the function definition just to understand the call site. It’s a bad developer experience, and quite frankly, it's just ugly code.

## The Optional Configuration Problem

This is the core problem we are trying to solve. Go doesn't support method overloading like C# or Java. You can't just define `NewServer()` and `NewServer(int port)` and `NewServer(int port, bool tls)` side-by-side.

So when you write a constructor like `NewServer`, you’re stuck with that signature. If you have optional parameters—like a timeout or a verbose flag—you end up forcing the user to pass `0` or `nil` or `true` just to satisfy the compiler.

And it gets worse when requirements change.

Let's say six months later, you need to add TLS support. If you just add a parameter to the function, you have now broken every single place in your application where `NewServer` was called. You have to go refactor the entire world just to add one optional setting.

## The "Okay" Alternative: Config Structs

Now, the common response I hear is: *"Just use a config struct."*

```go
type Config struct {
    Port int
    TLS  bool
}

func NewServer(cfg Config) *Server { ... }
```

And honestly? This is fine. It's certainly better than magic numbers. But it has a flaw: Ambiguity around zero values.

If I pass `Port: 0` in that config... do I mean *"Please bind to a random available port"? Or do I mean "I forgot to set this, please use the default port 80"*?

Distinguishing between "default" and "intentionally zero" is annoying with structs in Go. You end up dealing with pointers to integers (`*int`) just to check for `nil`, which makes the API clumsy to use.

## The Solution: Functional Options

The Go community eventually settled on a better pattern. It looks a little scary at first because it uses closures, but once you get it, you’ll never go back.

It essentially allows you to create a variadic constructor that accepts any number of functions to modify the internal state.

### Step 1: The Option Type

First, we define our `Server` struct, but we keep the fields unexported so they can't be messed with directly. Then, we define a type—let's call it `Option`.

An `Option` is just a function that takes a pointer to your `Server` and modifies it. It returns nothing.

```go
type Server struct {
    host string
    port int
    tls  bool
}

type Option func(*Server)
```

### Step 2: The Closures

Next, we create *"Constructor"* functions that return that `Option` type.

This is where the magic happens. Look at `WithPort` below. It takes an integer, and it returns a closure that *captures* that integer. When that inner function eventually runs, it will apply that specific port to the server.


```go
func WithHost(host string) Option {
    return func(s *Server) {
        s.host = host
    }
}

func WithPort(port int) Option {
    return func(s *Server) {
        s.port = port
    }
}
```

### Step 3: The Variadic Constructor

Finally, we build the actual constructor. Instead of taking a fixed list of arguments, we take a variadic slice of our `Option` type (`...Option`).

Inside the constructor, we do three things:

1. Initialize the server with sensible defaults (e.g., Host is localhost, Port is 80).
1. Loop through the provided options and execute them. This overwrites the defaults with whatever the user provided.
1. Return the server.

```go
func NewServer(opts ...Option) *Server {
    // 1. Set Defaults
    svr := &Server{
        host: "localhost",
        port: 80,
    }

    // 2. Apply Options
    for _, opt := range opts {
        opt(svr)
    }

    // 3. Return
    return svr
}
```

## The Result: Clean, Readable APIs

The result is a beautiful API usage. For my C# friends out there, this feels a lot like a building a fluent interface. It reads like a sentence.


```go
srv := NewServer(
    WithHost("127.0.0.1"),
    WithPort(8080),
)
```

I don't need to know what the default timeout is. I don't need to pass `nil` for things I don't care about. I just declare what I want to change.

### Extensibility is the Killer Feature

But the real reason this pattern is the gold standard is extensibility.

Remember that TLS requirement? With the functional options pattern, I can add a `WithTLS` option function later on:

```go
func WithTLS(cert string, key string) Option {
    return func(s *Server) {
        // ... config logic
    }
}
```

And guess what? I didn't break a single existing line of code.

```go
// Old code still works
srv1 := NewServer(WithPort(8080))

// New code uses the new feature
srv2 := NewServer(WithPort(8080), WithTLS("cert.pem", "key.pem"))
```

Backward compatibility is preserved, and your consumers don't hate you.

## Conclusion

To recap, why should you use this pattern?

Defaults are easy: You define them once in the constructor, and the user never has to think about them.

It's self-documenting: No more true, false, 30. The code explains itself.

It's future-proof: You can add new options forever without breaking your consumers.

If you're building a library or a package in Go, treat your users nicely. Use the functional options pattern.

It’s a little more code to write upfront, but the developer experience it provides is worth every keystroke.
