```json meta
{
  "title": "Automate Ionos to Cloudflare DNS Migration with a .NET Script",
  "lead": "Stuck manually copying DNS records from Ionos to Cloudflare? See how a simple .NET 10 single-file app can generate a standard zone file and save you hours of tedious, error-prone work.",
  "isPublished": true,
  "publishedAt": "2025-10-09",
  "openGraphImage": "posts/ionos-to-cloudflare-dns-migration-dotnet/og-image.png",
}
```

I needed to move my DNS records from Ionos to Cloudflare. It's something I've put off for the better part of 3 years or so. Should've been simple, right? Import a zone file, point the nameservers, done.

Except Ionos doesn't make it easy to export a proper DNS zone file. And I had dozens of records. A records, CNAME records, MX records, TXT records for email verification, the works. The thought of manually copying each one over made me want to just...not migrate at all.

But I needed this move. Cloudflare's tooling is better, their interface makes more sense to me, and I wanted to consolidate where I manage my DNS configurations for all my domains. So I was stuck: how do I get my DNS records out of Ionos in a format Cloudflare (or any standards-compliant DNS provider) will accept?

## I'll Just Write a Script

If there is a reason to learn to code, this type of chore is it. If something is going to be tedious and error-prone, and you can't stop thinking to yourself "there has to be a better way" then you know you have a good candidate for automation.

Fortunately, Ionos has an API. Not the most intuitive API, but it's there. And I'd been wanting to try .NET 10's new single file app support anyway. This felt like the perfect use case. A focused utility that does exactly one thing. In the past I'd have probably reached for TypeScript for something like this, but not anymore. 😁

Single file apps in .NET 10 let you skip all the ceremony of a project, but still use all the power of .NET. I didn't need a lot of boilerplate or scaffolding for something I'd probably only use once. I just needed to hit an API, transform some data, and write it to a file.

## Building It

I'll be honest, my first attempt was messier than I'd like to admit. I started by just trying to dump the API response to a file and manually format it. That lasted about five minutes before I realized I'd need to handle all the different record types properly, deal with FQDN notation, handle disabled records, and about a dozen other edge cases I hadn't thought about.

So I stepped back and broke it down into the pieces I actually needed:

**1. An API Client**

Authentication with Ionos requires a "Public Prefix" and a "Secret" that get combined into an API key. Nothing fancy, but it took me a minute to figure out their authentication scheme. Once I had that working, I could retrieve zone information for any domain.

**2. A Zone File Formatter**

This is where things got interesting. DNS zone files follow [RFC 1035](https://datatracker.ietf.org/doc/html/rfc1035), which means there are rules. Lots of rules. CNAME records need trailing dots to indicate FQDNs. MX records need priority values. TXT records need to be quoted. Records should be grouped by type for readability. Oh, and you need to convert absolute record names to zone-relative notation.

I built this piece incrementally, testing with my actual DNS records. Each time I thought I was done, I'd discover another edge case. SRV records format priorities differently than MX records. PTR records need the same FQDN treatment as CNAMEs. Disabled records should be skipped with a comment explaining why.

**3. Configuration Management**

I used `Microsoft.Extensions.Configuration` because even in a script, I didn't want to hardcode my API credentials. The tool reads from `appsettings.json` for the credentials and takes the domain and output path as command-line arguments.

Here's what running it looks like:

```bash
dotnet index.cs --domain stevanfreeborn.com --output stevanfreeborn.zone
```

That's it. No `dotnet new`. No project files. No build step to think about. You just run the script.

## What I Got Right

After working through the edge cases, the tool generates a clean, RFC 1035-compliant zone file:

```text
; Zone file for stevanfreeborn.com
; Generated on 2025-01-09 14:30:00 UTC
; Original Zone ID: abc123

$ORIGIN stevanfreeborn.com.

; A Records
@                    3600     IN A        192.0.2.1
www                  3600     IN A        192.0.2.2

; CNAME Records
mail                 3600     IN CNAME    stevanfreeborn.com.

; MX Records
@                    3600     IN MX       10 mail.stevanfreeborn.com.
```

The file imported into Cloudflare without a hiccup. Well, aside from the fact I had too many records to fit within the limit for a free plan (200) so I had to do a bit of purging manually, but this was much easier to do in a text file than through a GUI.

I did add some validation checks for safety. The tool won't overwrite existing files. It validates that you've provided both a domain and an output path. It gives clear error messages if something goes wrong.

## Important Considerations

### NS and SOA Records

The generated zone file includes the NS (nameserver) and SOA (Start of Authority) records from Ionos. When you import to a new provider, they'll typically want to use their own values for these. Cloudflare overwrote them automatically, but not every provider will. Check your provider's documentation before importing.

### Disabled Records

If you have disabled records in your zone (records that exist but aren't active), the script skips them and adds a comment noting why. This seemed like the most logical way to handle them, especially if you are using this tool for migration purposes. Probably don't need to keep disabled records around in the new provider.

### API Rate Limits

If you're planning to export multiple domains in quick succession, be aware of Ionos's API rate limits. For a single domain migration like mine, it wasn't an issue. But it's something to keep in mind.

## What I Learned

Here's what I've learned from this experience:

**Single file apps are perfect for focused utilities.** When you have a clear, bounded problem to solve, single file apps give you all the power of .NET without the overhead of project management. You can still use NuGet packages, dependency injection, modern C# features—all of it. But you skip the ceremony.

**Building too a standard is ideal.** By generating an RFC 1035-compliant zone file, the output works with any DNS provider that supports standard imports. I didn't build a Cloudflare-specific tool; I built a tool that outputs a standard format. That's way more useful and could come in handy for myself or others in the future. If you're going to spend some time writing some code, you might as well try to aim for some reusability.

**Simple and clear validation helps you iterate.** File overwrite protection, configuration validation, clear error messages all helped me iterate quickly through my implementation. In utilities like this, it's much easier to fail early and don't worry about recovering when you end up in a state you didn't expect or plan to handle.

**API-first beats scraping, always.** I could have tried to scrape the Ionos web interface or manually copy records. Using their API made this solution robust and repeatable. It was worth the extra time to figure out their API...even though they didn't make it clear that the `User-Agent` header is required for requests to succeed. (Seriously, I spent an hour debugging that one.)

## Why This Matters

What could have been hours of tedious, error-prone manual work turned into a single command. More importantly, the next time I need to migrate DNS records or when someone else faces the same problem—the solution already exists.

.NET 10's single file app support represents something important: you don't need to choose between the power of a full framework and the simplicity of a script. You can have both. For one-off utilities, migration tools, and focused scripts, that's exactly what you are looking for. Kudos to the .NET team for recognizing that while .NET has always excelled at scalling up, it had room for improvement in simpler scenarios like this.

The code is [open source on GitHub](https://github.com/StevanFreeborn/ionos-dns-zone-file-generator) if you need something similar or just want to see how it's built. And if you're facing a similar migration, hopefully this saves you from the copy-paste hell I was trying to avoid.

