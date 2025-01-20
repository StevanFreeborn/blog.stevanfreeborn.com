```json meta
{
  "title": "How to Authenticate with Google in a Console App",
  "lead": "Learn how to authenticate with Google in a console application. This step-by-step guide covers setting up a project, generating credentials, and securely accessing Google APIs, making it easy to integrate your console app with Google services.",
  "isPublished": true,
  "publishedAt": "2025-01-19",
  "openGraphImage": "posts/how-to-authenticate-with-google-in-a-console-app/og-image.png",
}
```

I've taken a bit of a detour lately into learning about how to authenticate a console app with Google so that the app can access Google APIs. I figured others might want to build similar apps, so I decided to write up a small example of how you do it. Keep in mind that the following is up to date and relevant as of the time of writing, but as we all know things in the tech world are always subject to change.

>[!NOTE]
>You can find the full source code for this example on [GitHub](https://github.com/StevanFreeborn/google-auth-example).

## Prerequisites

Before we get started, you'll want to make sure you have the following:

1. A [Google account](https://accounts.google.com/signup)
2. The [.NET SDK](https://dotnet.microsoft.com/download) installed on your machine. I'm using .NET 9.0 in this example.

## Configuring the Google Cloud Console

The first thing you'll need to do is set up a project in the Google Cloud Console. This project will be used to authenticate your app and give it access to Google APIs. You can think of a project as a container for all the resources you'll use to authenticate and interact with Google APIs.

### Create a New Project

Start by navigating to the [Google Cloud Console](https://console.cloud.google.com/). If you don't already have a project, you'll be prompted to create one. Click the "Select a project" dropdown in the top navigation bar and then click "New Project". Give your project a name and click "Create". Once your project is created, you'll want to make sure it's selected in the dropdown.

Here is an example of creating a new project in the Google Cloud Console:

<video src="https://share.stevanfreeborn.com/blog.stevanfreeborn.com/how-to-authenticate-with-google-in-a-console-app/add-project-in-google-cloud-console.mp4" controls title="Create A Project"></video>

### Configure the OAuth Consent Screen

Next, you'll need to configure the OAuth consent screen. The OAuth consent screen is what users will see when they authenticate your app with Google. To configure the OAuth consent screen, click on "OAuth consent screen" in the left-hand navigation menu. You'll need to fill out some basic information about your app, such as the app name, user support email, and developer contact information. Once you've filled out the required fields, click "Save and Continue".


Here is an example of configuring the OAuth consent screen:

<video src="https://share.stevanfreeborn.com/blog.stevanfreeborn.com/how-to-authenticate-with-google-in-a-console-app/configure-oauth-consent-screen.mp4" controls title="Configure OAuth Consent Screen"></video>

>[!NOTE]
>If you're creating an app that you plan to distribute to external users, you'll need to make sure you have a terms of service URL and a privacy policy URL. In this example I'm using something as simple as wiki pages on the GitHub repo for the project. This is important because Google will require you to submit your app for verification before it can be used by external users and these URLs are required for the verification process.

### Create OAuth 2.0 Credentials

After you've configured the OAuth consent screen, Google will allow you to create OAuth 2.0 credentials. Click on "Credentials" in the left-hand navigation menu, then click "Create Credentials" and select "OAuth client ID". You'll be prompted to select the application type. This might be a little confusing, but we are going to select "Web application" even though we are building a console app. This is because the OAuth 2.0 flow we are going to use requires a redirect URI, which is a URL that Google will redirect the user to after they authenticate your app. However, since we are building a console app the redirect URI we provide will use `localhost` as the domain and a specific port number.

Here is an example of creating OAuth 2.0 credentials:

<video src="https://share.stevanfreeborn.com/blog.stevanfreeborn.com/how-to-authenticate-with-google-in-a-console-app/creating-oauth-client.mp4" controls title="Create OAuth 2.0 Credentials"></video>

## Setting Up the Console App

With the Google Cloud Console configured, we can now set up our console app. We'll start by creating a new console app using the dotnet CLI.

Here is how I typically setup a new console app:

```pwsh
mkdir google-auth-example
cd google-auth-example
mkdir src
dotnet new console -n GoogleAuthExample -o src
```

>[!NOTE]
>I'm using a dotnet console app in this example, but the overall flow here can be applied to any language or platform that supports OAuth 2.0.

### Create Console App Configuration

Remember the client ID and client secret we generated when we created the OAuth 2.0 credentials in the Google Cloud Console? We'll need to be able to access these values along with the redirect URI value in our console app. To do this, we will follow the dotnet convention of using an `appsettings.json` file to store these values, read them in at runtime, and use them to authenticate with Google.

Here is an example of what the `appsettings.json` file could look like:

```json
{
  "GoogleAuth": {
    "ClientId": "YOUR_CLIENT_ID",
    "ClientSecret": "YOUR_CLIENT_SECRET",
    "RedirectUri": "YOUR_REDIRECT_URI"
  }
}
```

You'll also need to update your `csproj` file to include the `appsettings.json` file in the output directory. This will ensure that the `appsettings.json` file is copied to the output directory when you build the app.

Here is an example of what the `csproj` file could look like:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <None Update="appsettings.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
</Project>
```

While you could most definitely use `ConfigurationBuilder` to read in the values from the `appsettings.json` file using the `Microsoft.Extensions.Configuration` package, I'm going to keep things simple and just read the values in directly from the file and deserialize them into a strongly typed object.

```csharp
static async Task<GoogleAuthOptions> GetAuthOptionsAsync()
{
  var settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
  var settingsText = await File.ReadAllTextAsync(settingsPath);
  var settings = JsonSerializer.Deserialize<JsonDocument>(settingsText);

  if (settings is null)
  {
    throw new ApplicationException("appsettings.json is missing or invalid");
  }

  var googleAuth = settings.RootElement.GetProperty("GoogleAuth");
  var clientId = googleAuth.GetProperty("ClientId").GetString();
  var clientSecret = googleAuth.GetProperty("ClientSecret").GetString();
  var redirectUri = googleAuth.GetProperty("RedirectUri").GetString();

  if (
    string.IsNullOrWhiteSpace(clientId) ||
    string.IsNullOrWhiteSpace(clientSecret) ||
    string.IsNullOrWhiteSpace(redirectUri)
  )
  {
    throw new ApplicationException("ClientId or ClientSecret is missing or invalid");
  }

  return new(clientId, clientSecret, redirectUri);
}


record GoogleAuthOptions(string ClientId, string ClientSecret, string RedirectUri);
```

### Build the Authentication URL

With client ID, client secret, and redirect URI loaded, we can build the URL that the user will use to authenticate with Google. This URL will include the client ID, redirect URI, and the scopes that the user is granting your app access to. Scopes are essentially permissions that the user is granting your app to access their Google account. You can find a list of available scopes in the [Google API documentation](https://developers.google.com/identity/protocols/oauth2/scopes). You'll also notice that I'm requesting an offline access type. This will allow us to request a refresh token, which can be used to get a new access token when the current one expires. This allows our app to access Google APIs without requiring the user to re-authenticate every time the access token expires.

```csharp
static string BuildAuthUrl(GoogleAuthOptions options)
{
  const string baseAuthUri = "https://accounts.google.com/o/oauth2/v2/auth";
  var authUriQueryParams = new Dictionary<string, string>
  {
    ["client_id"] = options.ClientId,
    ["redirect_uri"] = options.RedirectUri,
    ["response_type"] = "code",
    ["scope"] = "https://www.googleapis.com/auth/userinfo.profile", // add the scopes you need
    ["access_type"] = "offline" // request a refresh token
  };

  var authUri = $"{baseAuthUri}?{string.Join("&", authUriQueryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"))}";

  return authUri;
}
```

### Get the Authorization Code

All we've done is build the URL that the user will use to authenticate with Google. We still need to actually open a browser and have the user authenticate with Google. Once the user has authenticated with Google, they will be redirected to the redirect URI we provided when we created the OAuth 2.0 credentials. The redirect that is sent by Google to the redirect URI will include a query parameter called `code` that contains the authorization code. This authorization code is what we will use to get the access token that we need to access Google APIs.

```csharp
static string GetOAuthCodeAsync(string authUrl, GoogleAuthOptions options)
{
  using var listener = new HttpListener();
  listener.Prefixes.Add(options.RedirectUri);
  listener.Start();
  Console.WriteLine("Waiting for login response...");
  Process.Start(new ProcessStartInfo
  {
    FileName = authUrl,
    UseShellExecute = true
  });

  var listenerContext = listener.GetContext();
  var oauthCode = listenerContext.Request.QueryString["code"];

  try
  {
    if (string.IsNullOrEmpty(oauthCode))
    {
      throw new ApplicationException("OAuth code is missing or invalid");
    }

    const string responseHtml = @"
      <html>
        <body>
          <script>
            alert('Login successful! You can close this window now.');
          </script>
        </body>
      </html>";
    var buffer = Encoding.UTF8.GetBytes(responseHtml);
    listenerContext.Response.ContentLength64 = buffer.Length;
    listenerContext.Response.OutputStream.Write(buffer, 0, buffer.Length);
    listenerContext.Response.Close();

    return oauthCode;
  }
  finally
  {
    listener.Stop();
  }
}
```

>[!NOTE]
>There are a few things to note here. First, we are using an `HttpListener` to listen for the redirect from Google. This is a simple way to listen for the redirect in a dotnet console app. Second, we are opening the browser to the authentication URL using `Process.Start`. This will open the default browser on the user's machine.

### Get the Access Token

The next step in the flow is to get the access token. We'll use the authorization code we got from the login response to make a request to Google's token endpoint. This request will include the authorization code, client ID, client secret, and redirect URI. Google will respond with an access token that we can use to access Google APIs.

```csharp
static async Task<TokenResponse> GetAccessTokenAsync(string oauthCode, GoogleAuthOptions options)
{
  const string tokenUri = "https://oauth2.googleapis.com/token";
  var tokenRequestParams = new Dictionary<string, string>
  {
    ["code"] = oauthCode,
    ["client_id"] = options.ClientId,
    ["client_secret"] = options.ClientSecret,
    ["redirect_uri"] = options.RedirectUri,
    ["grant_type"] = "authorization_code"
  };

  var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenUri)
  {
    Content = new FormUrlEncodedContent(tokenRequestParams)
  };

  using var httpClient = new HttpClient();
  var tokenResponse = await httpClient.SendAsync(tokenRequest);
  var tokenResponseContent = await tokenResponse.Content.ReadAsStringAsync();

  if (tokenResponse.IsSuccessStatusCode is false)
  {
    throw new ApplicationException($"Failed to get access token.");
  }

  var token = JsonSerializer.Deserialize<TokenResponse>(tokenResponseContent);

  if (token is null)
  {
    throw new ApplicationException($"Failed to parse access token response.");
  }

  return token;
}

record TokenResponse(
  [property: JsonPropertyName("access_token")]
  string AccessToken,
  [property: JsonPropertyName("expires_in")]
  int ExpiresIn,
  [property: JsonPropertyName("token_type")]
  string TokenType,
  [property: JsonPropertyName("scope")]
  string Scope,
  [property: JsonPropertyName("refresh_token")]
  string RefreshToken
);
```

### Make a Request to a Google API

At this point we can considered the app authenticated and the user logged in. We can now use the access token to make requests to Google APIs. In this example, I'm going to make a request to the Google People API to get the user's profile information.

However, while we requested the proper scope to access the user's profile information, we still need to enable the People API in the Google Cloud Console. To do this, navigate to the [Google Cloud Console](https://console.cloud.google.com/), select your project, click on "APIs & Services" in the left-hand navigation menu, then click on "Library". Search for "People API" and click on it. Click the "Enable" button to enable the API.

Here is an example of enabling the People API in the Google Cloud Console:

<video src="https://share.stevanfreeborn.com/blog.stevanfreeborn.com/how-to-authenticate-with-google-in-a-console-app/enable-people-api.mp4" controls title="Enable People API"></video>

Now that the People API is enabled, we can make a request to the API to get the user's profile information.

```csharp
var name = await GetUsersNameAsync(token);

Console.WriteLine($"The logged user's name is {name}");

static async Task<string> GetUsersNameAsync(TokenResponse token)
{
  const string userInfoUri = "https://people.googleapis.com/v1/people/me?personFields=names";
  var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, userInfoUri);
  userInfoRequest.Headers.Add("Authorization", $"Bearer {token.AccessToken}");

  using var httpClient = new HttpClient();
  var userInfoResponse = await httpClient.SendAsync(userInfoRequest);
  var userInfoResponseContent = await userInfoResponse.Content.ReadAsStringAsync();

  if (userInfoResponse.IsSuccessStatusCode is false)
  {
    throw new ApplicationException($"Failed to get user info.");
  }

  if (string.IsNullOrEmpty(userInfoResponseContent))
  {
    throw new ApplicationException($"User info response is empty.");
  }

  var userInfo = JsonSerializer.Deserialize<PersonResponse>(userInfoResponseContent, new JsonSerializerOptions
  {
    PropertyNameCaseInsensitive = true
  });

  if (userInfo is null)
  {
    throw new ApplicationException($"Failed to parse user info response.");
  }
  
  return userInfo.Names[0].DisplayName;
}

record PersonResponse(string ResourceName, string ETag, List<Name> Names);

record Name(
  Metadata Metadata, 
  string DisplayName, 
  string FamilyName, 
  string GivenName, 
  string DisplayNameLastFirst, 
  string UnstructuredName
);

record Metadata(bool Primary, Source Source);

record Source(string Type, string Id);
```

## All Of It In Action

Now that we have all the pieces in place, we can put it all together and run the app. When you run the app, it will open a browser window and prompt you to authenticate with Google. After you authenticate, the app will use the access token to make a request to the Google People API and get your profile information.

Here is an example of the app in action:

<video src="https://share.stevanfreeborn.com/blog.stevanfreeborn.com/how-to-authenticate-with-google-in-a-console-app/all-in-action.mp4" controls title="App In Action"></video>

## Couple of Notes

When it comes to the information we used to configure the app you are probably wondering where or how you'd actually protect the client id and secret if you were going to distribute your app. This is a great question and while you could probably compile them into your app and obfuscate them I think that it isn't worth the effort or risk. Instead, I'd probably do one of two things.

If I wanted to avoid having to have my users configure anything I'd create an API that my app would call when authenticating that essentially acts as a proxy to Google's API, but allows me to keep the client ID and client secret protected on a server I control. If I didn't want to have to build, deploy, and host that API I'd probably just have the user provide the client ID and client secret when they run the app. This way they are responsible for protecting the values and I don't have to worry about it. If either of those options sound like something you'd like to see a more concrete example of let me know on [Bluesky](https://bsky.app/profile/stevanfreeborn.com).

You may also be thinking about what to do with the token response in terms of persisting it so that the access token can be reused until it expires and the refresh token can be used to get a new access token when needed. Another good question and one that I felt was a bit ouf of the scope of this example.

However, I'd probably store the token response on disk in a place I can be confident that it will be persisted between app runs. More than likely in the user's app data directory. I'd also probably encrypt the token response before writing it to disk using some machine specific information as the encryption key. This way the token response is protected from prying eyes and can be used to make requests to Google APIs without the user having to re-authenticate every time the app is run. If you'd like to see a more concrete example of how to do this let me know on [Bluesky](https://bsky.app/profile/stevanfreeborn.com).

## Conclusion

And that's it! You've successfully authenticated with Google in a console app. You've set up a project in the Google Cloud Console, created OAuth 2.0 credentials, and used those credentials to authenticate with Google and access the Google People API. You can now use this example as a starting point to integrate your console app with other Google APIs. I hope you found this example helpful and that it makes it easier for you to build console apps that interact with Google services. If you did enjoy it I'd love to [hear about it](https://bsky.app/profile/stevanfreeborn.com).

