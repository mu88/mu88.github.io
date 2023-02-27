---
layout: post
title: Source Browsing in Rider and R#
description: Quickly share a code reference from within Rider or R# with a colleague
comment_issue_id: 20
---

Have you ever spotted something in your codebase that either feels odd or that you don't understand and want to share it quickly with your colleague? Of course, you could call him/her right away and share your screen, but sometimes this doesn't feel appropriate, and you'd prefer doing it asynchronously (e. g. via Teams or Slack): _Hey buddy, could you please have a look at this particular piece of code?_

Both Rider and R# (for Visual Studio) allow you creating a URL for the source file so that you can send your colleague e. g. a link to Bitbucket for opening the code in the browser. So let's see how this can be configured.

## Configure Source Browsing
The relevant feature is called [Source Browsing](https://www.jetbrains.com/help/resharper/Options_Source_Browsing.html) and can be used in both R# and Rider. However, R# is necessary for configuration because the corresponding dialog doesn't yet exist in Rider. Please vote [here](https://youtrack.jetbrains.com/issue/RIDER-69445) if you'd like to see it as well ♥

Open the R# settings in Visual Studio and navigate to _Environment → Search & Navigation → Source Browsing_. Click _Add_ and enter the following values:
- _Title_ → `Bitbucket link to $PATH_PROJECT$:$LINE$ (branch '$GIT_BRANCH_NE$')`
- _URI pattern_ → `https://my.bitbucket.instance.com/projects/myProject/repos/myRepo/browse/$GIT_PATH$?at=refs%2Fheads%2F$GIT_BRANCH$#$LINE$`

As you can see in the dialog, there are a couple of macros/placeholders available. The previous template uses:

- `$LINE$` → the current line within the source file.
- `$GIT_PATH$` → the path of the current file in the Git file system, relative to the Git repo's root directory.
- `$PATH_PROJECT$` → the path of the current file, relative to the project's root directory.
- `$GIT_BRANCH_NE$` → the Git branch name, where `NE` stands for _not escaped_, i. e. the value for the Git branch `feature/my-current-feature` remains `feature/my-current-feature`.
- `$GIT_BRANCH$` → the Git branch name with escaped URL-safe characters, i. e. the value for the Git branch `feature/my-current-feature` becomes `feature$2Fmy-current-feature`.

After saving the template, it becomes active in both Rider and R#:  
![]({{ site.baseurl }}/public/post_assets/2023-02-27-Source-browsing-in-Rider/Image_1.png)

Executing the action copies the following URL into the clipboard:  
`https://my.bitbucket.instance.com/projects/myProject/repos/myRepo/src/BusinessCore/Extensions/JsonConvertExtensions.cs?at=refs%2Fheads%2Fmaster#8`

Another tiny but very helpful JetBrains feature - learning never stops!
