---
layout: post
title: Debugging a docker-compose file using JetBrains Rider
published: true
comment_issue_id: 14
---

I recently had my first working day in my new team at Swiss Post. As usual there are tons of new stuff to learn, discover and explore. It is super interesting due to its nature in the company's core domain (parcel management). But it comes with such an amount of new concepts and inputs that it is mind-blowing at the same time 🤯😅

It is my very first time that I'm getting the chance to work with tools like Docker, Kubernetes and Kafka in my everyday professional life. Of course, I've seen dozens of talks and read even more blog posts, but putting my own hands on these technologies is a completely different thing and therefore challenging and exciting at the same time.

We have a great sample project which gives new developers an easy way of getting used to these technologies. It consists of a .NET 6 microservice, working in conjunction with a locally hosted Kafka instance and a PostgreSQL database. These different parts are running as Docker containers and getting combined in a `docker-compose.yml` file.

When trying to debug this setting with JetBrains Rider, I had several problems, e. g. [this](https://stackoverflow.com/questions/67406041/debug-docker-with-rider-exited-with-code-244) and [that](https://stackoverflow.com/questions/69919664/publish-error-found-multiple-publish-output-files-with-the-same-relative-path). With the help of the fabulous JetBrains support, I finally managed it and want to share my findings with you.

# Set up the _Run Configuration_
Make sure that the following options are configured within your `docker-compose` _Run Configuration_:
- Set the only service to run the application you want to debug (the .NET microservice). All the other services (Kafka, PostgreSQL) will start automatically as the application depends on them.
- The containers must be started detached. Therefore, make sure that _Attach to: none_ is set.

So it should look like this:

[![Image]({{ site.baseurl }}/public/post_assets/2022-05-18-Rider-debug-docker-compose/Image1.png)]({{ site.baseurl }}/public/post_assets/2022-05-18-Rider-debug-docker-compose/Image1.png)

Save the configuration, set some breakpoints and hit _Debug_!

# Troubleshooting
If you’re luckier than me, this will work 😉 but if not, here are some tips that helped me a lot:
- Stop Rider
- Delete all `docker-compose.override*` files from `%LOCALAPPDATA%\JetBrains\Rider<<CurrentVersion>>\tmp`
- Try to debug again

If these steps do not help, try the following:
- Delete all data from Docker (`docker system prune -a --volumes`)
- Clean Rider's temp folder (`%LOCALAPPDATA%\JetBrains\Rider<<CurrentVersion>>\tmp`)
- Clean Rider's cache (_File_ → _Invalidate caches_ → enable _Clear file system cache and Local History_ → _Invalidate and Restart_)
- Recompile everything
- Try to debug again

Following these steps, I was finally able to debug my `docker-compose.yml` file in Rider 🥳 Thanks for reading and take care!
