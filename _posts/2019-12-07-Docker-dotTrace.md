---
layout: post
title: Profiling a .NET Core 3.0 Console App running in Docker for Windows with dotTrace
description: Profiling a .NET Core 3.0 Console App running in Docker for Windows with dotTrace
comment_issue_id: 4
---

Recently, I was asked to profile a .NET Console App running in Docker for Windows. I'm a big fan of the JetBrains tools for .NET: ReSharper, dotPeek, dotTrace - they are all part of my toolbelt. Since I've never profiled a Docker container with dotTrace, this post shall illustrate how to do this.

First of all, we need some code to profile. The complete code can be downloaded from [this GitHub repo](https://github.com/mu88/DockerDotTrace). But basically, it is nothing more than this:

{% highlight csharp %}
using System;
using System.Threading.Tasks;

namespace TestWithDocker
{
    internal class Program
    {
        private static async Task Main()
        {
            while (true)
            {
                await Task.Delay(1000);

                DoSomeWork();
            }
        }

        private static void DoSomeWork()
        {
            for (var i = 0; i < 100; i++)
            {
                Console.WriteLine(new Random().Next());
            }
        }
    }
}
{% endhighlight %}

As we can see, every second 100 random numbers will be generated and printed to the console.

Furthermore, a `Dockerfile` is needed and it looks like this:

{% highlight docker %}
FROM mcr.microsoft.com/windows/servercore:1903

COPY bin/Release/netcoreapp3.0/win-x64/* ./

ENTRYPOINT ["TestWithDocker.exe"]
{% endhighlight %}

We're pulling the base image `mcr.microsoft.com/windows/servercore:1903`, adding the compiled application and setting it as the `ENTRYPOINT`.

Before building the Docker image, the application has to be built using the `dotnet` global tool:
{% highlight shell %}
dotnet publish -c Release
{% endhighlight %}

Afterwards, we can build the Docker image:
{% highlight shell %}
docker build -t test-with-docker .
{% endhighlight %}

Before running and profiling the container, please make sure that you have dotTrace installed and if not happened yet, make your self comfortable with the how-to [Starting Remote Profiling Session](https://www.jetbrains.com/help/profiler/Starting_Remote_Profiling_Session.html). In summary, it says the following:

- Unzip `RemoteAgent.zip` to the environment to profile (in our case the Docker container).
- Start dotTrace and connect to the **Remote Agent URL**. By default, the Remote Agent uses port 9100.
- Attach to the application.

So lets do this step by step. At first, we will start the Docker container and map the container port 9100 to its local pendant:
{% highlight shell %}
docker run -d -p 9100:9100 --name test test-with-docker
{% endhighlight %}

To copy the unzipped Remote Agent, the following command has to be executed:
{% highlight shell %}
docker cp RemoteAgent/. test:/RemoteAgent
{% endhighlight %}

This copies all the content from the host's folder `RemoteAgent` to the container's folder `RemoteAgent`. In case this commands fails saying that you cannot copy content while a container is running: this seems to be a Windows/Hyper V limitation. We can work around this by stopping the container, copying the content and finally starting it again:
{% highlight shell %}
docker stop test
docker cp RemoteAgent/. test:/RemoteAgent
docker start test
{% endhighlight %}

Now the Remote Agent is there, but is has to be started:
{% highlight shell %}
docker exec -d test RemoteAgent/RemoteAgent.exe
{% endhighlight %}

Finally, we can connect to our application using dotTrace. As **Remote Agent URL**, we use `net.tcp://localhost:9100/RemoteAgent`. This accesses the local port 9100 of my machine which is mapped to port 9100 of the Docker container where the Remote Agent is up and running. Now we can attach dotTrace to `TestWithDocker.exe` and collect snapshots as usual.

![]({{ site.baseurl }}/public/post_assets/2019-12-07-Docker-dotTrace/image1.jpg)

As you can see in the following screenshot, everything works as usual when profiling an application and we find our method `DoSomeWork()`:

![]({{ site.baseurl }}/public/post_assets/2019-12-07-Docker-dotTrace/image2.jpg)