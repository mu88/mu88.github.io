---
layout: post
title: Localizing texts in a server-side Blazor app
description: How to use i18n (internationalization) and l10n (localization) with Blazor Server
comment_issue_id: 3
---

Like so many of us, I'm playing around with the different varieties of Blazor. Recently, I've ported an application based on Angular 2 and Electron to Blazor Server and [Electron.NET](https://github.com/ElectronNET). Thereby, I came across the topic of translating the app.

Technically, my purpose was not just translating the app into a specific language. I wanted to do it properly. Maybe you've stumbled across the terms `i18n` and `l10n`. Those cryptic acronyms stand for *internationalization* and *localization*. The first one simply means: *I modify my code in a way so that it is capable of handling different languages.* The second one means: *Now lets introduce the translations for the different languages.* So `i18n` can be considered as the base of `l10n` and it allows you to add new languages without changing the code consuming it.  
Do you wonder where these strange names come from? It is pretty simple: take VS Code or Notepad++, paste the words `internationalization` and `localization` into it and count the number of characters between `i` and `n` resp. `l` and `n`: `18` and `10`.

But how to apply `i18n` and `l10n` to Blazor Server? At the time of porting my app (a couple of month ago), there were almost no information about that topic. So I had to find my own solution.

Since Blazor Server is an ASP.NET Core application, I've written an injectable component called `CustomTranslator`:

{% highlight csharp %}
public class CustomTranslator : ICustomTranslator
{
    public CustomTranslator(IStringLocalizer<CustomTranslator> localizer)
    {
        Localizer = localizer;
    }

    public string GetTranslation(string text)
    {
        return Localizer[text];
    }

    private IStringLocalizer<CustomTranslator> Localizer { get;  }
}

public interface ICustomTranslator
{
    string GetTranslation(string text);
}
{% endhighlight %}

As you can see, the interface `ICustomTranslator` accepts a string and returns the translation. The implementation `CustomTranslator` does a lookup in a property called `Localizer`. This component is a built-in functionality of ASP.NET Core ([see here](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/localization?view=aspnetcore-3.1)) and it comes from the Dependency Injection container.

To use the custom translation service, it has to be registered within `Startup.cs`:

{% highlight csharp %}
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddLocalization(options => options.ResourcesPath = "Resources");
        services.AddSingleton<ICustomTranslator, CustomTranslator>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        var supportedCultures = new[]
                                {
                                    new CultureInfo("en"),
                                    new CultureInfo("de"),
                                };
        app.UseRequestLocalization(new RequestLocalizationOptions
                                   {
                                       DefaultRequestCulture = new RequestCulture("de"),
                                       SupportedCultures = supportedCultures,
                                       SupportedUICultures = supportedCultures
                                   });
    }
}
{% endhighlight %}

Let's go through this step by step. Within `ConfigureServices()`, basic localization is enabled and we're telling ASP.NET Core to look for all translations in the resource folder `Resources`. Next, the custom translation component is registered within the Dependency Injection container as a singleton.  
The array `supportedCultures` within `Configure()` defines all the languages the application will support. The following line configures the application to support the requested languages and defines that the default language of my application is German (`de`).

Now we can go to any Razor page and the Dependency Injection container will provide the custom translation component:

{% highlight csharp %}
@inject ICustomTranslator Translator
{% endhighlight %}

By using the following code, the translator can be consumed:

{% highlight csharp %}
<p>@Translator.GetTranslation("SomeString")</p>
{% endhighlight %}

Of course, we could also consume the translation component in any other class provided by the Dependency Injection container:

{% highlight csharp %}
public class MyBusinessLogicService
{
    public MyBusinessLogicService(ICustomTranslator translator)
    {
        CustomTranslator = translator;
    }

    private ICustomTranslator CustomTranslator { get; }

    private void MyMethod()
    {
        var translation = CustomTranslator.GetTranslation("SomeString");
    }
}
{% endhighlight %}

And that's all! Well, almost :wink: The code would work, but it wouldn't do anything because there are no translations yet. By creating the two resource files `Resources\CustomTranslator.en.resx` (English) and `Resources\CustomTranslator.de.resx` (German) and adding the key `SomeString` with an appropriate translation, the mission is completed. When running the app, the UI will be localized in German.

If I'd like to add support for French, all I had to do would be:
* Create a file `Resources\CustomTranslator.fr.resx` containing all the translations.
* Add `new CultureInfo("fr")` to the array `supportedCultures` in `Startup.Configure()`.

As you've hopefully seen, doing `i18n` and `l10n` with Blazor Server is not difficult at all. For people being familiar with ASP.NET Core, it should be super straight-forward.  
For me, the biggest challenge was to understand that the folder name `Resources` and file names `CustomTranslator.<<language>>.resx` have to match an exact pattern. Otherwise, they won't be recognized.

Thank you for reading!