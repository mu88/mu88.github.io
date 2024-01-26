---
layout: post
title: Using .NET SDK Container Building Tools
description: Throw away your boilerplate Dockerfiles and use the SDK tools 💪🏻
comment_issue_id: 23
---

With every new release of .NET at the end of the year comes a bunch of new language and framework features, but usually some handy SDK features as well. One of them are the _.NET SDK Container Building Tools_.

If you've never heard of them before: when it comes to containerizing a .NET app, most IDEs support creating `Dockerfile`s from an existing .NET project.  
In Rider, for example, adding Docker support is as simple as this which puts all the necessary things together:  

![]({{ site.baseurl }}/public/post_assets/2024-01-21-SDK-container-building-tools/Image_1.png)

While this is nice and good, with the rise of smaller and smaller projects and therefore even simpler `Dockerfile`s, having a dedicated `Dockerfile` with a lot of boilerplate code became more and more an unnecessary overhead.  
Thankfully, the .NET team at Microsoft recently added support for building container images to the SDK 🥳

## Build container image using `dotnet publish`
The default tool to publish an app is `dotnet publish`. Therefore, the container building support has been added to this tool as well. Let's see how this works.

First of all, this is our `Dockerfile` to migrate:

{% highlight dockerfile %}
ARG BASE_IMAGE=mcr.microsoft.com/dotnet/aspnet:8.0
FROM ${BASE_IMAGE} AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MyProject/MyProject.csproj", "MyProject/"]
RUN dotnet restore "MyProject/MyProject.csproj"
COPY . .
WORKDIR "/src/MyProject"
RUN dotnet build "MyProject.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MyProject.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyProject.dll"]
{% endhighlight %}

As you can see, it is a regular multi-stage build for an ASP.NET Core app.

The project file `MyProject.csproj` is even simpler:

{% highlight xml %}
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <DockerDefaultTargetOS>Linux</DockerDefaultTargetOS>
  </PropertyGroup>
  <ItemGroup>
    <Content Update="appsettings.json">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </Content>
  </ItemGroup>
</Project>
{% endhighlight %}

Now all we have to do is to ensure that Docker is running and call `dotnet publish --os linux --arch x64 /t:PublishContainer -c Release`. This will do the following:
- Build and publish the app in `Release` mode.
- Build a Docker image for Linux, targetting the `x64` platform, and add it to the local Docker registry.

And that's it, now we can throw away the `Dockerfile` 🚮

## Publishing the image to Docker Hub
As mentioned before, the created image will be added to the local Docker registry. But `dotnet publish` can also push the image to a container registry like Docker Hub.

For this, the MSBuild properties `ContainerRegistry` and `ContainerRepository` must be specified, either via command line or within `MyProject.csproj`. My default is to add `ContainerRepository` to the project file (to ensure a properly named image even in the local registry) and only add `ContainerRegistry` via command line when necessary (because when testing locally, I don't always want to push the newly created image to Docker Hub), so it looks like this:

{% highlight xml %}
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <DockerDefaultTargetOS>Linux</DockerDefaultTargetOS>
    <ContainerRepository>mu88/myproject</ContainerRepository>
  </PropertyGroup>
  <ItemGroup>
    <Content Update="appsettings.json">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </Content>
  </ItemGroup>
</Project>
{% endhighlight %}

Now when calling `dotnet publish --os linux --arch x64 /t:PublishContainer -c Release -p:ContainerRegistry=registry.hub.docker.com`, the .NET SDK will try to push the image to Docker Hub and... fail 💥  
The reason is simple: as Docker Hub requires authentication to upload images, `docker login` must be called before. After doing so, the command runs nicely.

## Further configuration
As you can read in [the official Microsoft docs](https://learn.microsoft.com/en-us/dotnet/core/docker/publish-as-container), there are several other options to configure the process of image creation. I want to highlight `ContainerImageTag(s)` and `ContainerFamily`.

Using the property `ContainerImageTag(s)`, several Docker image tags can be specified. So for example, we can add the following to `MyProject.csproj`:

{% highlight xml %}
<PropertyGroup>
    <ContainerImageTag>dev</ContainerImageTag>
</PropertyGroup>
{% endhighlight %}

This way, all locally built images will always receive the tag `dev`. When pushing the images to Docker Hub, the tags can be overridden dynamically via MSBuild: `dotnet publish --os linux --arch x64 /t:PublishContainer -c Release -p:ContainerRegistry=registry.hub.docker.com '-p:ContainerImageTags="1.0.0;latest"'`

This will lead to a nicely tagged image using `1.0.0` and `latest` on Docker Hub.

Unfortunately, it is not yet supported to build multi-platform images (upvote [this GitHub issue](https://github.com/dotnet/sdk-container-builds/issues/87) 🙏🏻). So while it is possible to build a single image which supports e. g. both `arm64` and `x64` which the native Docker pipeline, this cannot be achieved yet with the .NET SDK Container Building tools. Instead, the `dotnet publish` tool has to be called twice, once per platform, but also with different tags:

{% highlight shell %}
dotnet publish --os linux --arch arm64 /t:PublishContainer -c Release -p:ContainerRegistry=registry.hub.docker.com '-p:ContainerImageTags="1.0.0-arm64;latest-arm64"'

dotnet publish --os linux --arch x64 /t:PublishContainer -c Release -p:ContainerRegistry=registry.hub.docker.com '-p:ContainerImageTags="1.0.0-x64;latest-x64"'
{% endhighlight %}

The property `ContainerFamily` is extremely handy to easily change the base image used to build and publish the app. For example, by adding `p:ContainerFamily=jammy-chiseled` one can easily build an image using the new chiseled base images ([see official Microsoft blog post for more information](https://devblogs.microsoft.com/dotnet/announcing-dotnet-chiseled-containers/)). We will look at this in a later blog post.

## Closing
Now that we saw the new tools in action, the big question is: _why should we use them, what's the problem using a dedicated `Dockerfile`?_ And this question is absolutely valid. I personally see the following benefits:
1. Less files, less clutter, less maintenance → while it's only a marginal benefit, I'm always happy if I can remove unneeded things, especially when it comes to boilerplate code.
2. More coherent project structure → before, it was possible to have different versions of .NET specified in `*.csproj` and `Dockerfile`, either by intention or accident. I don’t like it when information that belongs together is spread over several files.

The second point becomes even more handy when updating to a new major version of .NET: until now, my dependency update tool Renovate Bot automatically filed a PR with the latest version of .NET for the `Dockerfile`, but without modifying the `*.csproj`, leading to a mix of different versions. Which the new approach using the SDK Container Building Tools, this "problems" simply vanishes.

Of course, there are situations when the process of building a Docker image has to be tweaked and then the `Dockerfile` remains the way to go. But it's really nice to see the SDK improving with year by year, not only C# and the .NET runtime.

I hope you enjoyed reading the first post in 2024 - take care 👋🏻