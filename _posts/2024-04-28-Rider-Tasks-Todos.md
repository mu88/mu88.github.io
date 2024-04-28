---
layout: post
title: Tasks and to-dos in JetBrains Rider
description: Organize and divide tasks into smaller pieces of work
comment_issue_id: 25
---

As a developer in an ideal world, resolving a task would be to solely work on it and finish all sub-tasks step by step. However, for most of us, this is a rather unrealistic scenario. Instead, we're confronted with lots of context switches, _Can you please quickly fix that?_ and _Oh damn, where did I stop?_ 🤷🏻‍♂️  
In this post, I want to show you how the onboard tools of JetBrains Rider help me to better handle everyday complexity.

## Tasks

When I see other developers switching from Visual Studio to Rider, I notice most of them solely focusing on Rider's first-class refactoring tools and performance. Very few of them use the issue tracker integration and tasks.

In Rider, a task is a set of the following aspects:
- branch
- opened editor tabs
- changes
- breakpoints
- bookmarks

There are _local tasks_ and _tracker tasks_. While a local task is not related to any issue, a tracker task corresponds to an issue coming from an issue tracker (e. g. Jira, GitHub, YouTrack). This way, you can even update an issue's state (_In Progress_, _Waiting_, _Closed_, etc.) when switching between tasks.

Let's see how we can configure Rider to load my issues from Jira.

### Configure Jira as the issue tracker

Hit `Shift` twice, enter _configure server_, and open the related settings:

![]({{ site.baseurl }}/public/post_assets/2024-04-28-Rider-Tasks-Todos/Image_1.png)

Here for example you see the config of a Jira server:

![]({{ site.baseurl }}/public/post_assets/2024-04-28-Rider-Tasks-Todos/Image_2.png)

Just enter a URL and your credentials and click _Test_ - that's all. As you can see, the pattern searches for all my issues which are not yet resolved.

In Rider, there is always at least the _Default_ task. If not yet done, I recommend checking whether the associated branch and changelist of the default task are explicitly set - that makes switching back and forth between an issue and the default task more convenient:

![]({{ site.baseurl }}/public/post_assets/2024-04-28-Rider-Tasks-Todos/Image_3.png)

![]({{ site.baseurl }}/public/post_assets/2024-04-28-Rider-Tasks-Todos/Image_4.png)

### Switching between tasks

Now that everything is configured, we can start using it.

Let's assume our Product Owner added a new Jira issue to the sprint. At first, I grab the issue and create a dedicated task in Rider. For opening a task, you can either click through the menu, a dedicated toolbar entry, or via the hotkey `Alt + Shift + T`:

![]({{ site.baseurl }}/public/post_assets/2024-04-28-Rider-Tasks-Todos/Image_5.png)

After clicking _Open Task..._, my unresolved Jira issues are loaded and I can pick the new issue:

![]({{ site.baseurl }}/public/post_assets/2024-04-28-Rider-Tasks-Todos/Image_6.png)

Rider automatically creates a dedicated and nicely named changelist and feature branch. It also sets the issue state to _In Progress_ so that my fellow teammates are aware that I'm working on this issue.  
**Hint:** via _Clear current context_ you can control whether all previously opened tabs will be closed or kept open after creating the task.

Now I start developing, make a few changes, create some commits, etc. But after a while, a colleague tells me that there's a severe bug that needs to be addressed immediately 🤯 our PO has already created a related bug issue. While in the middle of a big refactoring with 20 or so changed files which are not yet ready to commit, this is rather unfavorable, but production always wins - let's see how Rider can help me.

First of all, let's create another task for the bug:

![]({{ site.baseurl }}/public/post_assets/2024-04-28-Rider-Tasks-Todos/Image_7.png)

By opening this new task, Rider automatically shelves the related changes to the previous task silently, i. e. I don't have to deal with manually stashing/reverting files.

Now I can thoroughly fix the bug, file a PR, and push the fix to production. Finally, I close the bug task from within Rider, either from the menu or via `Alt + Shift + W`:

![]({{ site.baseurl }}/public/post_assets/2024-04-28-Rider-Tasks-Todos/Image_8.png)

![]({{ site.baseurl }}/public/post_assets/2024-04-28-Rider-Tasks-Todos/Image_9.png)

Now I can switch back and open my previous task, which will also automatically unshelve my previous unfinished changes so that I can continue working.

## To-do comments

Another nice productivity feature in Rider (and this time R# as well) is to-do comments. I mostly use them to remind myself to finish/refactor something before filing a PR and thereby focus on the most relevant things for the moment and fix related things later on.

### Configure to-do pattern

In Rider, there is the _TODO_ window where you can see all to-dos that were found based on preconfigured regular expressions. Those expressions can be configured here:

![]({{ site.baseurl }}/public/post_assets/2024-04-28-Rider-Tasks-Todos/Image_10.png)

As you can see, I've configured several RegEx patterns:

![]({{ site.baseurl }}/public/post_assets/2024-04-28-Rider-Tasks-Todos/Image_11.png)

Here's the corresponding RegEx:

{% highlight regex %}

(?<=\W|^)(?<TAG>TODO_\w{3}-\d+)(\W|$)(.*)

{% endhighlight %}

Furthermore, I've created the following _live template_ (see my previous blog post [Leveraging the power of ReSharper Templates]({{ site.baseurl }}/2021/03/19/ReSharper-Templates.html)):

{% highlight csharp %}

// TODO_$ticketType$-$ticketNumber$: $END$ 

{% endhighlight %}

![]({{ site.baseurl }}/public/post_assets/2024-04-28-Rider-Tasks-Todos/Image_12.png)

**Hint:** You might be wondering why there is an underscore between `TODO` and `$ticketType`: the reason is the bug [Custom TODO patterns containing default key words (TODO, BUG) are not resolved](https://youtrack.jetbrains.com/issue/RIDER-64607/Custom-TODO-patterns-containing-default-key-words-TODO-BUG-are-not-resolved) - please vote for 👍🏻

Finally, let's also enable issue navigation under _File \| Settings \| Version Control \| Issue Navigation_ like this:

![]({{ site.baseurl }}/public/post_assets/2024-04-28-Rider-Tasks-Todos/Image_13.png)

| Issue             | Link                           |
|-------------------|--------------------------------|
| `[A-Z]+\-\d+`     | https://jira.company.com/$0    |

This adds some nice navigation features both to the commit dialog and the code editor.

### Working with to-dos

With the aforementioned settings, I can simply type `jira` everywhere in the codebase and Rider will propose the matching live template, ending up with a comment like this:

![]({{ site.baseurl }}/public/post_assets/2024-04-28-Rider-Tasks-Todos/Image_14.png)

With a single click, I can navigate to the related Jira issue in the browser. And in the TODO window, I now see the newly created to-do popping up:

![]({{ site.baseurl }}/public/post_assets/2024-04-28-Rider-Tasks-Todos/Image_15.png)

This way, I can easily track my open points even on the code level, filter to-dos, and navigate to the underlying issue for more information.

## Closing

As we've seen, Rider ships with a bunch of productivity tools that support me during my daily developer business. I hope those tools offer you value, too.

Thx for reading and take care 👋🏻