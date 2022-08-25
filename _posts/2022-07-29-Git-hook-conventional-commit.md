---
layout: post
title: Enforcing Conventional Commits with a Git hook and Rider
published: true
comment_issue_id: 15
---

In my new team, we're using [Conventional Commits](https://www.conventionalcommits.org), a specification for Git commit messages. Due to the used conventions it is possible to generate e. g. release notes directly from the Git history.

[//]: # (I've adapted this approach for one of my private projects. [Here]&#40;https://github.com/mu88/Project28/commits/main&#41; you can see the Git commit history and [here]&#40;https://github.com/mu88/Project28/releases/tag/1.0&#41; the automatically generated release notes.)

Unfortunately, I sometimes mess up with the different types of commit or simply forget it, which, as a consequence, breaks the release note generation. That's why I've set up a Git hook which triggers a PowerShell script on every `git commit` and checks the commit message.

The PowerShell script looks like this:

{% highlight powershell linenos %}
# The commit message has to be loaded from a Git-internal file
$commitMessage = Get-Content $args[0]

# Attention: contains a filter for Jira!
$conventionalCommitRegex = "(?-i)^(build|ci|docs|feat|fix|perf|refactor|style|test|chore|revert|BREAKING CHANGE)!?(\([\w\-]+\))?:\s(\w{3}-\d{4})?\s?[a-z]{1}.*"

# Without Jira filter:
# $conventionalCommitRegex = "(?-i)^(build|ci|docs|feat|fix|perf|refactor|style|test|chore|revert|BREAKING CHANGE)!?(\([\w\-]+\))?:\s[a-z]{1}.*"

$nuKeeperRegex = "(?-i)^:package: [A-Z].*"

if ($commitMessage -match $conventionalCommitRegex) {
    exit 0
}

if ($commitMessage -match $nuKeeperRegex) {
    exit 0
}

Write-Host "The commit message does not match the Conventional Commit rules"

exit 1
{% endhighlight %}

Git's current commit message is stored in the file `.git/COMMIT_EDITMSG` which gets read on the first line of the script. Then a pretty long RegEx defines all the different Conventional Commit varieties. This RegEx is executed against the current commit message in line 12. If it matches, `exit 0;` indicates success to the Git hook.

To enable this Git hook for a repository, the file `.git/commit-msg` has to be created with the following content:

{% highlight shell %}
#!/bin/sh
pwsh -noprofile C:/source/GitHub/config/hooks/Git_EnsureConventionalCommitMessage.ps1 $1
{% endhighlight %}

It is a default Shell script which runs `pwsh` (the PowerShell executable) and the formerly explained PowerShell script. The `-noprofile` flag skips loading the current user's PowerShell profile which is unwanted in my case (as I have several things like [Oh My Posh](https://ohmyposh.dev/) configured). Finally, the `$1` argument is provided by Git and contains the path of the Git commit message file (remember: `.git/COMMIT_EDITMSG`).

And that's all. Now when changing a file and trying to commit it via `git commit -a -m "some arbitrary commit message"`, the command fails with `The commit message does not match the Conventional Commit rules`.
But when using `git commit -a -m "feat: my fancy new feature"`, everything works fine as before.

This is great so far. But the setting has the disadvantage that the Git hook has to be configured for each and every repository. But with Git, there is a solution for everything 👍🏻 Look at my customized `.gitconfig`:

{% highlight conf %}
[includeIf "gitdir:C:/source/GitHub/"]
    path = .gitconfig-github
{% endhighlight %}

Whenever I'm inside the directory `C:\source\GitHub` (where all my cloned GitHub repos live), the additional Git config file `.gitconfig-github` with the following additional content gets loaded:

{% highlight conf %}
[core]
    hooksPath  = C:/source/GitHub/config/hooks
{% endhighlight %}

It overrides the path where Git looks for the hooks to execute. So instead of using the default `.git/hooks`, this path points to `C:\source\GitHub\config\hooks` on my dev machine and inside this folder there is the Git hook `commit-msg` and the PowerShell script `Git_EnsureConventionalCommitMessage.ps1`.
Now whenever I clone a new repository into `C:\source\GitHub`, the Git hook will be applied automatically 🤓

Pretty nice so far! The last piece for me is the Rider plugin [Conventional Commit](https://plugins.jetbrains.com/plugin/13389-conventional-commit). It supports when creating a commit with all the varieties that Conventional Commits offer.

To sum up, the Git hook avoids any invalid commit message after the fact and the Rider plugin supports me in writing correct commit messages before the fact.

Thanks for reading 👋🏻
