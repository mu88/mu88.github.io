---
layout: post
title: Is .NET Core cool enough to cool a Raspberry Pi? - Part 2
description: Building a fan controller for a Raspberry Pi using Blazor Server
---

In the [last post]({{ site.url }}/2020/04/24/Raspi-Fan-Controller_p1), I mainly described how to set up the software. In this part, I will focus on the hardware part and bringing everything together.


## Bring it all together

During the development, I could easily test my app by using the *Inverse of Control pattern*  and utilizing Dependency Injection to inject a fake temperature provider and fan controller. At a certain point, I was conscious enough to test it on the Raspi.

At first, I was convinced to deploy the app via Docker. But after some time, I was not sure whether a `sudo` command  executed from within a Docker container will be forwarded to the OS (remember the temperature measurement). So I decided to ship it as a [self-contained executable](https://docs.microsoft.com/en-us/dotnet/core/deploying/#publish-self-contained). This can be done as following:
```
dotnet publish -r linux-arm -c Release /p:PublishSingleFile=true
```

The following command copies the build results to the Raspi:
```
scp -r \bin\Release\netcoreapp3.1\linux-arm\publish pi@raspberry:/tmp/RaspiFanController/
```

On the Raspi, we have to allow the app to be executed:
```
chmod 777 /tmp/RaspiFanController/RaspiFanController
```

And finally, start the app using `sudo`. This is important because otherwise, reading the temperature doesn't work.
```
sudo /tmp/RaspiFanController/RaspiFanController
```

There were some firewall/reverse proxy issues in my case, but that's out of this post. In the end, I could successfully access the app and it was showing the current temperature.


## Soldering

This was definitely the hardest part for me in this project. But several other blog posts like the following gave me the necessary information which components I had to buy and how to connect them:

* [Automatically Control Your Raspberry Pi Fan (and Temperature) with Python](https://howchoo.com/g/ote2mjkzzta/control-raspberry-pi-fan-temperature-python)
* [Temperature Controlled Fan for Raspberry Pi 4](https://fizzy.cc/raspberry-pi-fan/)

Finally, I bought:

* Breadboard
* Red LED
* Transistor BC 337
* Resistor 680 Ω for the Transistor
* Resistor 1 kΩ for the LED
* Jumper wires

Because I was afraid to somehow destroy the fan, I made a first test with my controller software and the LED:

![]({{ site.url }}/public/post_assets/200424_Raspi_Fan_Controller/Image2.jpg)

After a successful test, I switched over and used the fan:

![]({{ site.url }}/public/post_assets/200424_Raspi_Fan_Controller/Image3.jpg)

And it was working! So I had no more excuses to solder everything and do the final assembly:

![]({{ site.url }}/public/post_assets/200424_Raspi_Fan_Controller/Image4.jpg)


## Register the app as a service

Now that everything was working fine, I wanted to register my little app as a service. This will ensure that the controller automatically gets started after a reboot.

For this, I had to create a *service unit configuration file*  on the Raspi:
```
 sudo nano /etc/systemd/system/RaspiFanController.service
```

It has the following content:

<script src="https://gist.github.com/mu88/080e248107d3722fa47411b17f6ce3da.js?file=RaspiFanController.service "></script>

With the following commands, the service will be created:
```
sudo cp RaspiFanController.service /etc/systemd/system/RaspiFanController.service
sudo systemctl daemon-reload
sudo systemctl start RaspiFanController
```

Now the app will start on every reboot.


## Summary

Due to modern developments in the .NET ecosystem, I was able to write a controller for a Linux device like a Raspberry Pi. I could leverage all the new features my favorite platform provides:

* Cross-platform
* Worker Services
* ASP.NET Core Blazor Server

In theory, this app could be easily ported to any other OS like Windows 10 IoT Core or device like Arduino - if the necessary parts like temperature retrieval are available.

For me, this was another great experience of how the modern development world can look like: serve every platform with the tools of your choice.

Thank you for reading!