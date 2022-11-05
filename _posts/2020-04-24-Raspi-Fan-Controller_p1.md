---
layout: post
title: Is .NET Core cool enough to cool a Raspberry Pi? - Part 1
description: Building a fan controller for a Raspberry Pi using Blazor Server
comment_issue_id: 2
---

A couple of weeks ago, I bought a new toy: a [Raspberry Pi 4 Model B](https://www.raspberrypi.org/products/raspberry-pi-4-model-b/). I wanted to set up my own DNS server, but mainly, I wanted to get in contact with this new platform. Since I had already read other blog posts saying that the Raspi gets quite warm under normal conditions, I ordered a case with a built-in fan as well.  
The shipment arrived and I assembled everything curiously. First impression: wow, the ramp-up time to assemble and install is super fast! Second impression: the built-in fan is a bit loud... and my spouse thought so as well :wink: So the Raspi had to live its first days in the kitchen.

In the next days, I found several blog posts describing how to build a small electric circuit and a bit of software to control the fan. The hardware part was new to me anyway. But for the software, the blog authors were mostly using Python. Since my heart beats for .NET and C#, I was intrigued by the idea of using my favorite technologies. And I found the [.NET Core IoT Libraries](https://github.com/dotnet/iot) - a NuGet package provided by Microsoft to build applications for devices like the Raspi. This package was my missing piece in the puzzle - how to control the hardware. Now I was on fire and decided to build a fan controller based on Blazor Server and the found NuGet package.

All the code can be found in my GitHub repo [Raspi Fan Controller](https://github.com/mu88/RaspiFanController). Lets focus on the main parts:

* Temperature provider
* Temperature controller
* Fan controller
* Frontend


## Temperature provider

To control the temperature, we need to measure it, right? Fortunately, the Raspi's OS *Raspian* comes with a built-in command to retrieve its current temperature:

{% highlight shell %}
sudo vcgencmd measure_temp
{% endhighlight %}

It returns a text like `temp=39.0°C`. The class [`Logic\RaspiTemperatureProvider`](https://github.com/mu88/RaspiFanController/blob/master/RaspiFanController/Logic/RaspiTemperatureProvider.cs) does a little bit of RegEx to parse the current temperature and unit into a tuple `(39.0, "C")`.

After retrieving the current temperature, we can act on it.


## Temperature controller

The temperature controller [`Logic\RaspiTemperatureController`](https://github.com/mu88/RaspiFanController/blob/master/RaspiFanController/Logic/RaspiTemperatureController.cs) is nothing but a `while` loop. It regularly checks the current temperature and turns on the fan if a upper threshold is reached and turns it off if a lower threshold is reached.  
This loop is `async`: since the temperature controller will be started by a [.NET Core Worker Service](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-3.1&tabs=visual-studio), the `while`'s exit condition is the `CancellationToken` provided by the ASP.NET Core environment.  
In between, there is a sleep time between two loop runs via `Task.Delay()`.


## Fan controller

This is the place where we really access the hardware. The Raspi has so called *General Purpose Input/Output* (GPIO) pins - physical pins on its board that can be used for custom extension. The NuGet package [.NET Core IoT Library](https://github.com/dotnet/iot) abstracts and allows us to set these pins in a very easy way. Take a look into [`Logic\RaspiFanController`](https://github.com/mu88/RaspiFanController/blob/master/RaspiFanController/Logic/RaspiFanController.cs):

{% highlight csharp %}
var gpioController = new GpioController();
gpioController.OpenPin(17, PinMode.Output);
gpioController.Write(17, PinValue.High);
{% endhighlight %}

The turn on the fan, the GPIO pin 17 is set to high value. And that's it.


## Frontend

The user interface is a single Razor page providing the necessary information like current temperature and threshold. These information are read from the temperature controller.

<img src="{{ site.baseurl }}/public/post_assets/200424_Raspi_Fan_Controller/Image1.jpg" width="350" />


In the [next part]({{ site.baseurl }}/2020/04/24/Raspi-Fan-Controller_p2), I will describe how to bring everything together.