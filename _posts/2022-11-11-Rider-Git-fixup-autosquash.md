---
layout: post
title: Fixup commits in Rider
description: Maintaining a cleaner Git history made easy
comment_issue_id: 18
---

Sometimes you're using tools for years and then, out of a sudden, you discover a long-existing but yet new feature. This is what happened to me this week when I stumbled upon `fixup` commits, another wonderful Git feature.

But let's start from the beginning. I created a sample Git repository with three commits. This is what `git log --oneline` outputs:
{% highlight shell %}
08c7b38 (HEAD -> main) feat: implement second feature
30c1931 feat: implement first feature
194e8b4 initialize repository
{% endhighlight %}

In Rider it looks like this:
![]({{ site.baseurl }}/public/post_assets/2022-11-11-Rider-Git-fixup-autosquash/Image_1.png)

E. g. due to code review feedback, some changes need to be made to the commit `30c1931 feat: implement first feature` - nothing special, but how do we do this? Quite often I solved this task with a new dedicated commit like `refactor: fix typo`. I never liked it because it pollutes the Git history, but well, there are so many things that aren't perfect, right?

This is where `fixup` commits can help 💪🏻 let's see what Rider offers when right-clicking on the commit in the _Git_ window that we need to fix:
![]({{ site.baseurl }}/public/post_assets/2022-11-11-Rider-Git-fixup-autosquash/Image_2.png)

When executing the _Fixup..._ command, we can commit our fixed typo as usual:
![]({{ site.baseurl }}/public/post_assets/2022-11-11-Rider-Git-fixup-autosquash/Image_3.png)

Now let's see how the Git history looks like by executing `git log --oneline`:
{% highlight shell %}
462e46d (HEAD -> main) fixup! feat: implement first feature
08c7b38 feat: implement second feature
30c1931 feat: implement first feature
194e8b4 initialize repository
{% endhighlight %}

Or once again in Rider:
![]({{ site.baseurl }}/public/post_assets/2022-11-11-Rider-Git-fixup-autosquash/Image_4.png)

As we can see, a new commit `462e46d fixup! feat: implement first feature` has been added, using the commit message from the commit that needs to be fixed with the prefix `fixup! `.  
This prefix is a Git convention and you can find more about it in the [official Git docs](https://git-scm.com/docs/git-commit#Documentation/git-commit.txt---fixupamendrewordltcommitgt). In combination with another Git feature called `autosquash` ([official Git docs](https://git-scm.com/docs/git-rebase#Documentation/git-rebase.txt---autosquash)), Git will integrate `fixup` commits into the correct commit that needs to be fixed, providing a clean and cohesive history.

On the command-line, you'd do it via `git rebase -i --autosquash 30c1931`, where `30c1931` is the Git commit hash of the first commit we want to keep.  
In Rider all we have to do is right-clicking on this commit and selecting _Interactively Rebase from Here..._:
![]({{ site.baseurl }}/public/post_assets/2022-11-11-Rider-Git-fixup-autosquash/Image_5.png)

Which brings up the following dialog, showing nicely where the `fixup` commit will be integrated into:
![]({{ site.baseurl }}/public/post_assets/2022-11-11-Rider-Git-fixup-autosquash/Image_6.png)

As we'd expect, `462e46d fixup! feat: implement first feature` will be integrated into `30c1931 feat: implement first feature`. That's great! So lets hit _Start Rebasing_ and see how the Git history looks like afterwards:
![]({{ site.baseurl }}/public/post_assets/2022-11-11-Rider-Git-fixup-autosquash/Image_7.png)

Or if you prefer on the command-line via `git log --oneline`:
{% highlight shell %}
08c7b38 (HEAD -> main) feat: implement second feature
30c1931 feat: implement first feature
194e8b4 initialize repository
{% endhighlight %}

Now we have a nice and clean Git history without any ugly `fix typo` commit - I love it 🤓 I gave it a try in our code review workflow to integrate the reviewer's feedback as `fixup` commits and squash everything at the very last step of the PR.

Thanks for reading and happy rebasing!