```json meta
{
  "title": "Why I Love Control Flow in C#",
  "lead": "Team 'Never Throw' vs. Team 'Always Throw'. In C#, you don't have to pick a side. Here is the hybrid architecture that uses the best of both worlds.",
  "isPublished": true,
  "publishedAt": "2026-01-30",
  "openGraphImage": "posts/why-i-love-control-flow-in-csharp/og-image.png"
}
```

If you have spent any time working with C#, you have likely encountered the debate between using exceptions for control flow versus returning result types that indicate success or failure. Each approach has its own set of advantages and disadvantages, and developers often find themselves aligning with one camp or the other. One side argues that exceptions should be reserved for truly exceptional circumstances, while the other side contends that exceptions are a powerful tool for handling errors and simplifying code.

I think the conversation was largely inspired by languages like Rust and Go which favor treating errors as values and forcing developers to handle the result of operations that can fail. However, in the C# community I think this is often framed as a false dichotomy. I instead choose to embrace a hybrid approach that leverages the strengths of both strategies. And C# is unique because it allows us to mix the paradigms comfortably. We have the robust `try/catch` mechanisms, but we also have the pattern matching and functional features needed to make result types work well.

## A hybrid architecture

It is often most helpful to think about these types of things in a concrete example. Let's say I'm building a web API in sort of your classic n-tier architecture. I have a controller layer that handles HTTP requests, a service layer that contains business logic, and a data access layer that interacts with the database. In this mixed-approach, we can think of the approach to error handling shifting from implicit to explicit as we move up the layers.

1. **Data Access Layer**: In the data access layer, we can use exceptions to handle unexpected issues like database connection failures or query timeouts. These are truly exceptional situations that we don't expect to happen during normal operation. For example, if a database query fails due to a connection issue, we can throw an exception that bubbles up to the service layer.

2. **Service Layer**: In the service layer, we can use result types to represent the outcome of business operations. For example, if we have a method that processes an order, we can return a `Result<Order>` type that indicates whether the operation was successful or if there were validation errors. This allows us to handle expected failures in a more controlled manner without relying on exceptions and giving the caller more context about what went wrong.

3. **Controller Layer**: In the controller layer, we can use the well-defined result types from the service layer to determine how to respond to HTTP requests. If the service returns a successful result, we can return a 200 OK response with the data. If it returns a failure result, we can return an appropriate HTTP status code (like 400 Bad Request) along with error details.

4. **Global**: Oftentimes we are calling code that we don't control or have knowledge of whether it throws or not, we can also implement global exception handling middleware in our web API. This middleware can catch any unhandled exceptions that bubble up from lower layers and convert them into standardized error responses. This ensures that even if an unexpected exception occurs, our API can still respond gracefully.

Let's step through an example of this in practice using the example of fetching a user.

### Querying the database

When I am writing the infrastructure code that queries the database, I don't want to deal with the noise of using results and more importantly I don't really necessarily know the context in which this code might be used. So I will just throw exceptions for any unexpected issues.

```csharp
public async Task<User?> GetUserAsync(int userId)
{
    // NOTE: EF Core might throw exceptions for connection issues, etc.
    var user = await _dbContext.Users.FindAsync(userId);

    // NOTE: Domain logic might throw exceptions for business rules
    if (user?.IsBanned)
    {
        // This is an exceptional state for this specific logic
        throw new UserBannedException(userId);
    }

    return user;
}
```

### Dealing with business logic

In the service layer, I want to be explicit about the possible outcomes of my operations. So I will use a result type to indicate success or failure.

```csharp
public async Task<Result<User>> GetUserAsync(int userId)
{
    try
    {
        var user = await _userService.GetUserAsync(userId);

        if (user is null)
        {
            // Logic flow errors become explicit failures
            return Result.Failure<User>(new UserNotFoundException(userId));
        }

        return Result.Success(user);
    }
    catch (UserBannedException ex)
    {
        // Expected exceptions are caught and converted
        return Result.Failure<User>(ex);
    }
}
```

### Handling HTTP requests

Finally, in the controller layer, I can use pattern matching to handle the result from the service layer and return appropriate HTTP responses. This allows the consumer to dictate the proper mapping of responses to the user based on the result received. This is where some of the flexibility of C# really shines.

```csharp
public static async Task<IResult> GetUserHandler(int userId, IUserService _service)
{
    var result = await _service.GetUserAsync(userId);

    return result.Match(
        user => Ok(user),
        error => error switch
        {
            UserNotFoundException => Results.NotFound(),
            UserBannedException => Results.Forbid(),
            // Unknown errors fall through
            _ => Results.InternalServerError()
        }
    );
}
```

### Catch 'em all

In most cases I'm not a fan of an app just crashing due to an unhandled exception. So I like to implement global exception handling middleware that can catch any unhandled exceptions and convert them into standardized error responses. Again, this is where C# allows us to have our cake and eat it too.

```csharp
public async ValueTask<bool> TryHandleAsync(
    HttpContext httpContext,
    Exception exception,
    CancellationToken cancellationToken
)
{
    _logger.LogError(exception, "Exception occurred: {Message}", exception.Message);

    httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

    await httpContext.Response.WriteAsync("An unexpected error occurred.", cancellationToken);

    return true;
}
```

## Conclusion

This is why I love C#. It gives me the freedom to choose the right tool for the layer I'm working in.

- Use exceptions when you are deep in the call stack and "failure" means stopping execution immediately.
- Use result types where business logic is centralized and you need to force the consumer to handle failure paths.
- Use global handlers to prevent crashes and log the things you didn't see coming.

You don't have to pick a side. You can have the best of both worlds.
