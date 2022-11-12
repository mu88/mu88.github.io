---
layout: post
title: Using new C# features in .NET Framework 4.8 
description: Have you ever wished to use newer language features in the "old" .NET world? Here's a possible solution. 
comment_issue_id: 17
---

When taking a look at the [version history of C#](https://en.wikipedia.org/wiki/C_Sharp_(programming_language)#Versions), the language is picking up pace in the last couple of years: while 13 years passed between C# 1.0 and 6.0, there's now a new major version coming every year. I personally _love_ the following new features from the latest versions:
- Nullable reference types
- Global `using` declarations
- File-scoped namespaces
- Raw string literals

One half of my worktime at Swiss Post I'm working in the parcel sorting. There we're using the latest .NET stack and all these cool new features. But the other half I'm spending in project which is bound to .NET Framework 4.8 and where I want to use some of the mentioned features as well. Surprisingly this can be done very easily.

The C# compiler offers the property [`LangVersion`](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/language#langversion). The used default language version depends on the used target framework as you can see [here](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/configure-language-version#defaults). And for all versions of the .NET Framework the default C# language version points to  `7.3`.

Now the trick is pretty easy: open the `csproj` of the .NET Framework project and set the `LangVersion` property, e. g. to `<LangVersion>11</LangVersion>` for C# 11 which was releases just recently 💡

This way you can easily use newer C# features on older platforms like .NET Framework 4.8. In my case I created a shared library targeting .NET Standard 2.0 (which uses C# 7.3 by default) with C# 11 features - pretty cool 💪🏻

Thanks for reading!
