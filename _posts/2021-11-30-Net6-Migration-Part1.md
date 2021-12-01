---
layout: post
title: Migrating from .NET Core 3.1 to .NET 6 - Part 1
comment_issue_id: 11
---

Together with my colleagues at [Swiss Post](https://developer.apis.post.ch), I'm working on a .NET Solution with about 20 projects based on .NET Core 3.1. It has a pretty common architecture:
- Data Access Layer based on Entity Framework Core 3.1
- Business Logic based on .NET Core 3.1
- Web API based on ASP.NET Core 3.1

The front-facing client is written in Angular and for our tests we're using xUnit and the fabulous [Fluent Assertions](https://github.com/fluentassertions/fluentassertions) library. Give them a star if you appreciate their work as much as I do!

I've spent the last days migrating our code base to .NET 6 and I want to share some experiences I made and hurdles I've cleared.

# Where and how to start?
.NET 6 allows to reference projects targeting older .NET (Core) version, e. g. .NET Core 3.1, but not the other way round.\
Therefore I started the migration from the outside (ASP.NET Core) to the inside (EF Core). That approach ensured that I had a compiling Solution at all times. But I have to admit that the unit and integration tests were not usable during the transition due to runtime assembly conflicts.

The steps for each and every project were pretty much the same:
- Upgrade the _Target Framework Moniker_ within `*.csproj` to `net6.0`
- Upgrade all NuGet packages
- Remove unnecessary NuGet packages (always mind the Pathfinder rule, right?)
- Fix build warnings related to breaking API changes, obsolete members, etc.

Doing this for all of our 20 projects took me about one day. On the one side that feels pretty quick for such a major upgrade. But when considering __which__ obstacles I've encountered, I'd say I could have been twice as fast 😉

And if you're asking yourself: _did this guy even consider using the [.NET Upgrade Assistant](https://dotnet.microsoft.com/platform/upgrade-assistant)?_ Yes, I did! Before starting with all the manual work, I tried to migrate our Solution with that neat tool.\
But I probably used it in the wrong way, because I always ended up with either a partially migrated project (e. g. missing/invalid NuGet packages) or no changes at all (just sticking to .NET Core 3.1).

# Working with ASP.NET Core 6
Migrating to ASP.NET Core's new version worked almost fluently. I had some build warnings related to some now nullable properties (e. g. `HttpResponseMessage.Headers.Location` or `HttpContext.User.Identity`), but fixing that was just a matter of seconds.

There are so many heated discussions whether to use the new [Minimal APIs](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis) or not. I just can say that I didn't introduce them: not because I don't like them, but because we have a well working set of controllers which I didn't want to refactor.

# Working with EF Core 6
Upgrading EF Core was more... well, I'd say challenging 😎 compared to ASP.NET Core.

## Use entities instead of IDs
We had some sort of data seeding on `DbContext` creation to provide certain test data to our tests. Unfortunately the data seeder made heavily use of object IDs when referring to other objects. Just consider the following, rather simplified example:
```c#
public class Author
{
    public int Id { get; set; }
    // some stuff
}

public class Book
{
    public Author Author { get; set; }

    public int AuthorId { get; set; }
    // more stuff
}
```

Within the data seeder, there was code which looked similar to:
```c#
var author = new Author { Id = 1 };
var book = new Book { AuthorId = 1 };
```

This works if the `author` entity also gets persisted with an ID of 1. I didn't analyze it in depth, but that was not longer the case after the upgrade to EF Core 6.\
So when running the tests, most of them failed due to some sort of foreign key violation. The fix was pretty simple:
```c#
var author = new Author();
var book = new Book { Author = author };
```

But finding all the relevant spots took me several hours of boring "fix and try".

So here comes an advice to my future self: do not rely on object IDs or even manage them on your own when using EF Core! They are an implementation detail and should be considered evil.\
Actually I'm following that approach in my private projects by declaring all ID-related properties as `private` members.

## Nullability
Microsoft took the chance and embraced [Nullable Reference Types](https://docs.microsoft.com/en-us/dotnet/csharp/nullable-references) (imho one of THE BEST C# features EVER) with EF Core as well.

Therefore some common APIs like `DbContext.FindAsync<T>()` are now returning `ValueTask<T?>`, requiring the caller to do proper null checking. In many cases that was valuable feedback, because it was indeed putting spotlight onto potential null references.

## SQLite for in-memory testing
Some of our test run against an in-memory SQLite database. On test session start, we were calling `DbContext.Database.Migrate()`. Our main database in production is SQL Server, so all EF Core Migrations were tailored to that particular DBMS.\
But after upgrading, migrating the database within the tests failed with an exception saying that `varchar(max)` is not supported. Well, that's right: `varchar(max)` is indeed a SQL Server feature and not supported in SQLite.

Fortunately I came across [this GitHub issue](https://github.com/dotnet/efcore/issues/7030) which showed me the trick: calling `DbContext.Database.EnsureCreated()` instead of `DbContext.Database.Migrate()` helps. Otherwise I'd have to create dedicated EF Core Migrations for SQLite.\
But honestly I didn't check how this did work in the past.

# Closing
That's it for today! In the [next part]({{ site.url }}/2021/12/01/Net6-Migration-Part2.html), I will share some broader experiences, e. g. on new language features and IDEs.

Take care and thanks for reading 👋🏻
