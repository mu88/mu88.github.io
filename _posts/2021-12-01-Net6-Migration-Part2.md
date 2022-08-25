---
layout: post
title: Migrating from .NET Core 3.1 to .NET 6 - Part 2
comment_issue_id: 11
---

In the [last part]({{ site.url }}/2021/11/30/Net6-Migration-Part1.html), I described some issues during the migration of the EF Core and ASP.NET Core part of our .NET Solution at [Swiss Post](https://developer.apis.post.ch).\
This time I will focus on broader things like IDEs and new language/runtime features.

# Working with .NET 6
C# 10 and .NET 6 introduced some pretty interesting new features and syntactic sugar. I didn't introduce all of them to our codebase 😉 but some I find so helpful that I couldn't resist.

## Global Using Declarations and Implicit Usings
We all mentally ignore the first twenty or so lines of each C# file because of all the `using` declarations. This is where Global Using Declarations and Implicit Usings can shine. You can learn more about this in [this great blog post from Microsoft](https://devblogs.microsoft.com/dotnet/welcome-to-csharp-10/#global-and-implicit-usings).

I've introduced the following file `Directory.Build.props` at the root of our repo:
{% highlight xml %}
<Project>
  <PropertyGroup>
    <ImplicitUsings>true</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="..\GlobalUsings\GlobalUsings.cs">
      <Link>GlobalUsings.cs</Link>
    </Compile>
  </ItemGroup>

  <ItemGroup Condition="$(MSBuildProjectName.EndsWith('.Test'))">
    <Compile Include="..\GlobalUsings\GlobalUsingsForTests.cs">
      <Link>GlobalUsingsForTests.cs</Link>
    </Compile>
  </ItemGroup>

  <ItemGroup Condition="$(MSBuildProjectName.StartsWith('Api.'))">
    <Compile Include="..\GlobalUsings\GlobalUsingsForWebApi.cs">
      <Link>GlobalUsingsForWebApi.cs</Link>
    </Compile>
  </ItemGroup>
</Project>
{% endhighlight %}

This file gets automatically picked up by the compiler and links the mentioned C# classes into projects matching the naming pattern.

The `GlobalUsings\GlobalUsingsFor*.cs` files look like this:
{% highlight csharp %}
// GlobalUsings.cs
global using System.Collections.ObjectModel;
{% endhighlight %}
{% highlight csharp %}
// GlobalUsingsForTests.cs
global using FluentAssertions;
global using Xunit;
global using Xunit.Sdk;
{% endhighlight %}
{% highlight csharp %}
// GlobalUsingsForWebApi.cs
global using Microsoft.AspNetCore.Authorization;
global using Microsoft.AspNetCore.Mvc;
global using System.Net;
global using System.Net.Mime;
{% endhighlight %}

So the next time I create a test assembly named `Yoda.Test` (which matches our naming convention), it automatically references xUnit and Fluent Assertions.

Finally I did a Solution-wide cleanup of all `using` declarations and could remove so much noise.

## File-scoped Namespaces
All the years we C# developers wasted two or four spaces of horizontal indentation although approximately 99% of all C# files contain only a single class. With C# 10, we can rewrite such a file to:
{% highlight csharp %}
namespace MyNamespace;

public class MyClass
{
    // more stuff
}
{% endhighlight %}

Previously it looked like this:
{% highlight csharp %}
namespace MyNamespace
{
    public class MyClass
    {
        // more stuff
    }
}
{% endhighlight %}

[Rider](https://www.jetbrains.com/rider/), my favorite IDE, provides a refactoring to apply this pattern to the whole Solution.

## `System.Text.Json` instead of `Newtonsoft.Json`
Last but not least I worked on a very interesting feature. Over all the years, .NET Framework didn't have a competitive JSON serializer. `Newtonsoft.Json` (aka _Json.NET_) did an amazing job and was so feature-rich and performant that even Microsoft added it as a reference to previous versions of ASP.NET Core.\
But in the last releases, both the `Newtonsoft.Json` core developer and Microsoft worked together and created the new JSON serializer `System.Text.Json` which is part of .NET's Base Class Libraries.

You can do pretty crazy stuff with the new serializer ([learn more at Microsoft](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-overview)), like serialization metadata creation at compile-time. But as a first step, I simply tried to use `System.Text.Json` as a drop-in replacement for `Newtonsoft.Json`.\
An in fact that was pretty easy. I've just uninstalled all NuGet references, replaced all `JsonConvert.DeserializeObject<T>` calls with `JsonSerializer.Deserialize<T>` and it worked in 90% of all cases 💪🏻\
The other 10% are spots where the new serializer is more strict than Json.NET. Let me give you some examples.

### Casing of JSON properties
Imagine the following class to be serialized:
{% highlight csharp %}
public class MyDto
{
    Dictionary<string, int> Data { get; set; }
}
{% endhighlight %}

With Json.NET, all keys of `Data` were camelcase in the resulting JSON. Now with `System.Text.Json`, the keys keep their casing the way there are added to `Data`. Take a look at the following example:
{% highlight csharp %}
var dto = new Dto { Data = new Dictionary<string, string> { {"Bla", "blub"} } };
// Json.NET           →  {"data": {"bla": "blub"}}
// System.Text.Json   →  {"Data": {"Bla": "blub"}}
{% endhighlight %}

This actually broke some of our client code, but it can be controlled with...
{% highlight csharp %}
var options = new JsonSerializerOptions();
options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
options.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
{% endhighlight %}

### Public property accessors
The following class could be deserialized using Json.NET:
{% highlight csharp %}
public class MyDto<T>
{
    public List<T> Data;
}
{% endhighlight %}

With `System.Text.Json`:
{% highlight csharp %}
public class MyDto<T>
{
    public List<T> Data { get; set; }
}
{% endhighlight %}

### Public constructor
The following class could be deserialized using Json.NET:
{% highlight csharp %}
public class MyDto
{
    public MyDto(string name) => Name = name;

    public string Name { get; set; }

    public int Age { get; set; }
}
{% endhighlight %}

With `System.Text.Json`:
{% highlight csharp %}
public class MyDto
{
    [JsonConstructor]
    public MyDto()
    {
    }

    public MyDto(string name) => Name = name;

    public string Name { get; set; }

    public int Age { get; set; }
}
{% endhighlight %}

# And what about tools and IDEs?
All of our Visual Studio developers had to switch to Visual Studio 2022 since this is the only version supporting .NET 6.

For the JetBrains freaks (including me 🤓) it was a bit of a pain because the JetBrains .NET products (ReSharper and Rider) are not yet ready for .NET 6. They provide preview/EAP versions which are pretty stable and allow us to work with our code base. But due to the preview nature, all of our beloved plugins are not ready yet. Hopefully JetBrains will release the products soon so that all the great plugins out there can follow.

The same is true for other tools we're using, e. g. TeamCity or Sonar. I had to tweak them a bit because they don't yet support new language features, e. g. Global Using Declarations.

# Performance
I didn't do any performance comparisons between .NET Core 3.1 and .NET 6 yet. Maybe I'll find some time after christmas when it will be a bit more quiet.

# Closing
Especially the second part become rather long, but I hope you appreciated it anyway and you could get some helpful insights!

I enjoyed upgrading our code base as I really love the feeling of having something brought to the latest state. I'm curios which of the cool new features we will adapt in the future.

And I can't stress enough how important a comprehensive and multi-tiered test suite is. Especially for this upgrade task it was so valuable because I discovered 99% of all issues without even starting the web API or client.

Thanks again for reading, take care and I'd really appreciate your feedback 👋🏻
