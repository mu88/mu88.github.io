---
layout: post
title: Structural Search and Replace
description: Create and maintain small refactorings with another neat ReSharper feature
comment_issue_id: 22
---

Last week, I implemented the following little extension method to make the conversion between `DateTime` and `DateOnly` more concise:

{% highlight csharp %}
using System;

namespace My.Extensions;

public static class DateTimeExtensions
{
    // ReSharper disable once replace.dateonly.fromdatetime.by.todateonly
    public static DateOnly ToDateOnly(this DateTime dateTime) => DateOnly.FromDateTime(dateTime);
}
{% endhighlight %}

Especially in automated tests, I love using the [Fluent Assertions for dates and times](https://fluentassertions.com/datetimespans/)  because it makes the following more readable:

{% highlight csharp %}
// old
var dateOnly = DateOnly.FromDateTime(12.April(1953));

// new
var dateOnly = 12.April(1953).ToDateOnly();
{% endhighlight %}

Aside this little extension method and the corresponding changes, I added a _ReSharper Search Pattern_ to the pull request for automatically suggesting to use the extension method:

![]({{ site.baseurl }}/public/post_assets/2023-07-31-Structural-Search-Replace/Image_1.png)

Since my fellow reviewing colleague was not aware of this feature, I decided to dedicate it a blog post.

## Structural Search and Replace
This is another member out of the collection _tiny but powerful ReSharper features many people are not aware of_. As usual, the [official JetBrains docs](https://www.jetbrains.com/help/resharper/Navigation_and_Search__Structural_Search_and_Replace.html) are pretty extensive, but put in my own very few words: this feature allows you to search your code not only textual but syntactical for certain patterns (e. g. with exactly one argument of a given type).

Not only can you search for those patterns, you can define replacement patterns and a custom severity - in my case it's highlighted as a _Suggestion_.

And last but not least, these refactorings are saved within the typical ReSharper `*.DotSettings` file, making it possible to keep your personal collection of refactorings or share them with you fellow colleagues via `YourSolution.sln.DotSettings`.  
It is a very lightweight approach, especially for such relatively small refactorings, and is therefore superior to a Roslyn Analyser because I don't like creating another assembly just for such a one-liner.

## How to configure
At the moment of writing, there is no way to configure this in Rider ([please upvote this JetBrains issue](https://youtrack.jetbrains.com/issue/RIDER-11489/Implement-UI-for-Structural-Search-and-Replace-SSR)) - so it’s one of those rare times when I open Visual Studio, load the Solution that I want the refactoring to apply to and navigate to _ReSharper | Options | Code Inspection | Custom Patterns_.

For example, my previously shown refactoring looks like this:

![]({{ site.baseurl }}/public/post_assets/2023-07-31-Structural-Search-Replace/Image_2.png)

Note that by specifying a _Suppression key_, you can even instruct ReSharper to ignore a particular finding.

As you might have noticed, I've added the following comment to the extension method:  
`// ReSharper disable once replace.dateonly.fromdatetime.by.todateonly`  
This way ReSharper doesn't suggest to use my extension method here as well, because otherwise it would end with the following recursive path:

![]({{ site.baseurl }}/public/post_assets/2023-07-31-Structural-Search-Replace/Image_3.png)

After saving the pattern to the corresponding layer, we're ready to go 💪🏻 no need for Visual Studio anymore, so it can be closed and after opening the Solution again in Rider, the new refactoring hint becomes available. Finally, don't forget to create a pull request to share it with your colleagues.

## Closing
I hope I could show you another little helper for your tool belt that you can use the next time when a dedicated Roslyn analyzer feels just too much.

Thank you for reading and take care!