---
layout: post
title: Leveraging the power of ReSharper Templates
comment_issue_id: 10
---

Among .NET developers, the [JetBrains tool ReSharper (or R#)](https://www.jetbrains.com/resharper/) is some kind of silver bullet when it comes to code analysis and refactoring. However, I've often seen developers not being familiar with [ReSharper Code Templates](https://www.jetbrains.com/resharper/features/code_templates.html). That's why I want to give a brief introduction into this topic.

Basically, Code Templates are little code snippets that can be used in different scopes of coding. JetBrains calls them *Live Templates*, *Surround Templates* and *File Templates*. Let's take a look at them!

<h2>Live Templates</h2>
Live Templates are snippets that can be inserted while coding in a file. This can be `if` statements or `for each` loops: just start typing `if`, hit `Enter` and ReSharper will come up with a little workflow guiding us through the different parts, e. g. the condition.
 
 Out of the box, there are 170+ templates that come with ReSharper. But we can define our own templates via the *Templates Explorer* (*Extensions → ReSharper → Tools → Templates Explorer...* in Visual Studio). For example, I have a small template named `xunitasync` that looks like this:

{% highlight csharp %}
[Xunit.Fact]
public async Task $TestName$()
{
    // Arrange
    var testee = new $TestType$();
    $END$

    // Act

    // Assert
    throw new System.NotImplementedException();
}
{% endhighlight %}
 
 When typing `xunitasync` within a class, this template will create an asynchronous xUnit test body for me. When looking closer to the template code, we notice the two strings `$TestName$` and `$END$`. These are Template Parameters and they are context-aware. In the most easiest way, they are just plain strings that ReSharper will ask us to enter when using the template. For example, the `$TestName$` is just the name of the test we want to use. `$END$` is the location where the caret will be placed after the template has been applied. In my case, I want to start writing the *Arrange* part of the test.

 By *context-aware*, I mean that such parameters can be more than just plain strings. For example, a parameter can be configured so that whenever it is applied, IntelliSense will pop-up and a type is requested. This happens in the example for the parameter `$TestType$`. JetBrains calls them *Template Macros* and there's a bunch of them ([see here](https://www.jetbrains.com/help/resharper/Template_Macros.html)).

I have plenty of those live templates, e. g. for creating To-do items or Get-Only C# Properties.

 <h2>Surround Templates</h2>
 The second interesting option are Surround Templates. They can be used to surround a selected piece of code with another piece of code. Let's look at the following template:

{% highlight csharp %}
var stopwatch = System.Diagnostics.Stopwatch.StartNew();

$SELECTION$

stopwatch.Stop();$END$
{% endhighlight %}
 
 We are already familiar with the `$END$` parameter. The `$SELECTION$` parameter represents the piece of code that is selected. Before this code, a [`Stopwatch`](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.stopwatch.startnew?view=net-5.0) instance `stopwatch` is created and started. After the selected code, `stopwatch` is stopped. I use this frequently as the most trivial form of performance measurement.

 With this template, the following code...

{% highlight csharp %}
var service = new MyCustomService();
await service.ExecuteLongRunningProcessAsync();
{% endhighlight %}
 
 ...becomes to...

{% highlight csharp %}
var stopwatch = System.Diagnostics.Stopwatch.StartNew();

var service = new MyCustomService();
await service.ExecuteLongRunningProcessAsync();

stopwatch.Stop();
{% endhighlight %}

 <h2>File Templates</h2>
  I tend to use them not as much as Live or Surround Templates, but File Templates are also very handy in some situations. For example, when working with MSTest, each test class has to be decorated with the `[TestClass]` attribute. I have a File Template that creates a MSTest file for me and it looks like this:

{% highlight csharp %}
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace $Namespace$
{
    [TestClass]
    public class $TestClassName$
    {
        $END$
    }
}
{% endhighlight %}

 Again, there are some parameters which are pretty obvious: `$Namespace$` stands for the Namespace and it will be automatically retrieved by a macro. So I never have to take care of this manually, it will be determined by the file location within the Solution.

<h2>Postfix Templates</h2>
 It is not an exaggeration to say that I like these the most. With Postfix Templates, I can write `new MyService().var` and the little suffix `.var` will create a variable. Or we can use `.foreach` to create a `for each` loop.

 Unfortunately, we cannot create Postfix Templates through the *Templates Explorer* yet. But I hope the JetBrains guys will make this possible in the future 😃.

 <h2>Summary</h2>
 This was a short introduction into the concept of ReSharper Templates. There is a ton more to say, but I hope this is enough to catch your curiosity. For me in personal, I can say that ReSharper templates have revolutionized the way that I code. I can keep my focus on **what** I want to write, not **how**. And the extensibility gives me a way to be more productive in small, but repetitious coding tasks.

 If you need more information, I'd recommend the excellent [documentation](https://www.jetbrains.com/resharper/features/code_templates.html). There is a list of all the available macros, GIFs to see the Templates in action and much more.

 Last but not least, I'd be curious whether you've created a custom template and for what.

 Thank you for reading and take care!