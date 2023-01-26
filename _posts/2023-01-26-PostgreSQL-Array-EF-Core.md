---
layout: post
title: PostgreSQL Arrays and EF Core
description: Working with .NET collections in EF Core POCOs and mapping them to arrays on a database-level
comment_issue_id: 19
---

We recently faced the challenge in our team of storing a collection of primitive values (in our case `string`) within PostgreSQL. I'd like to share some interesting insights and learnings, so let's go!

## Imitating arrays by string concatenation
As we're using EF Core, our first naive approach was to use a [field-only property](https://learn.microsoft.com/en-us/ef/core/modeling/backing-field?tabs=data-annotations#field-only-properties) which looked like this:

{% highlight csharp %}
// POCO
public class Book
{
    private string _concatenatedAuthorNames;

    public string Key { get; set; }

    public string[] GetAuthorNames() =>
        _concatenatedAuthorNames.Split(";", StringSplitOptions.RemoveEmptyEntries);

    public void SetAuthorNames(string[] authorNames) =>
        _concatenatedAuthorNames = string.Join(";", authorNames);
}

// EF Core Configuration
public class BookContext : DbContext
{
    public DbSet<Book> Books { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>()
            .Property("_concatenatedAuthorNames");
    }
}
{% endhighlight %}

That worked pretty well, and we built our business logic around this pattern. But to answer questions like _which books are written by this particular author_, all `Book` entities had to be loaded from the database into memory - _dark performance problem clouds darken the friendly Swiss Post sky_. So this search was moved to the database like this:

{% highlight csharp %}
// Business Logic
public async Task<List<string>> FindAllBooksOfAuthorAsync(string authorName)
{
    await using BookContext context = await ContextFactory.CreateDbContextAsync();

    return await context.Books
            .Where(book =>
                      EF.Functions.Like(EF.Property<string>(book, "_concatenatedAuthorNames"), $"%{authorName}%"))
            .ToListAsync();
}
{% endhighlight %}

Which had some downsides as well:
- A text search with wildcards (`%`) is necessary.
- The field `_concatenatedAuthorNames` must be known but it is `private`.
- What if there are authors with double names like _Shakespeare_ and _Shakespeare-Doyle_?

## Using arrays on the database
Some days later, a colleague nudged me to give the [PostgreSQL Array Type Mapping](https://www.npgsql.org/efcore/mapping/array.html) a try - a feature I was simply unaware of and which lets you store and search arrays on the database-level.
There are basically two ways for using the PostgreSQL data type `text[]` in .NET POCOs:
1. `string[]`
2. `List<string>`

We strove for using the latter and here's what our code looked like:

{% highlight csharp %}
// POCO
public class Book
{
    public string Key { get; set; }

    public List<string> AuthorNames { get; set; }
}

// EF Core Configuration
public class BookContext : DbContext
{
    public DbSet<Book> Books { get; set; }
}

// Business Logic
public async Task<List<string>> FindAllBooksOfAuthorAsync(string authorName)
{
    await using BookContext context = await ContextFactory.CreateDbContextAsync();

    return await context.Books
            .Where(book => book.AuthorNames.Contains(authorName))
            .ToListAsync();
}
{% endhighlight %}

Super nice from the .NET perspective because...
- the business logic doesn't need to know about any EF Core specific details like `_concatenatedAuthorNames`,
- the private field `_concatenatedAuthorNames` is gone at all,
- no need to manually override the model builder in `OnModelCreating()`,
- well-adopted types like `List<T>` or LINQ APIs like `.Contains()` can be used, feeling very natural.

## Testing with SQLite
All our system integration tests (using a containerized PostgreSQL database) passed after switching the approach - but the unit and integration test suite literally exploded 🤯💣 What happened?

First of all let's have a look how EF Core translates the `BookContext`:

{% highlight csharp %}
[DbContext(typeof(BookContext))]
partial class BookContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "7.0.2")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

        modelBuilder.Entity("Book", b =>
            {
                b.Property<string>("Key")
                    .HasMaxLength(50)
                    .HasColumnType("character varying(50)");

                b.Property<List<string>>("AuthorNames")
                    .IsRequired()
                    .HasColumnType("text[]");

                b.HasKey("Key");

                b.ToTable("Book");
            });
#pragma warning restore 612, 618
    }
}
{% endhighlight %}

We see that `AuthorNames` is mapped to the database type `text[]` which exists for PostgreSQL but not for SQLite. Since our unit and integration tests are using an in-memory SQLite database, this has to be adapted.

To not pollute the `BookContext` with testing concerns, a dedicated `TestBookContext` is used:

{% highlight csharp %}
public class TestBookContext : BookContext
{
    public TestBookContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ValueComparer<List<string>> listComparer = new(
            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode(StringComparison.OrdinalIgnoreCase))),
            c => c.ToList());

        ValueConverter<List<string>, string> listConverter = new(
            strings => string.Join(",", strings),
            s => s.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList());

        modelBuilder
            .Entity<Book>()
            .Property(book => book.AuthorNames)
            .HasColumnType("text")
            .HasConversion(listConverter, listComparer);
    }
}
{% endhighlight %}

Please notice three important aspects:
1. `listComparer` → the EF Core Change Tracker will use this comparer to identify changed entities. Without this comparer, EF Core would only detect a completely changed collection (e. g. `book.AuthorNames = new List<string> { "New author" };`) but not changes to the collection's content (e. g. `book.AuthorNames.Add("New author");`).
2. `listConverter` → as SQLite has no equivalent array datatype, we pick up the string concatenation approach from this post's beginning and treat the .NET collection (`List<string>`) as a plain string on the database-level.
3. Datatype of column `AuthorNames` is now `text` instead of `text[]` and uses the custom `listConverter` and `listComparer`.

Now all unit and integration tests were green 🥳

## Summary
The colleague who nudged me to use this feature found the perfect summary: _thank you for going down the rabbit hole with me._ I couldn't agree more! 😅 It was quite a journey to get it all up and running, but in the end I appreciate this solution due to its simplicity and performance.
