---
layout: post
title: Dive into your Android's device network traffic
description: Analyze the network traffic of your Android device with Fiddler
comment_issue_id: 6
---

Recently, I came across the challenge to analyze the network traffic of my Android smartphone. I wanted to know which HTTP requests a specific app executes if the user triggers an UI action. When analyzing any app on a Windows PC, my silver bullet is [Fiddler](https://www.telerik.com/fiddler). So I was curious how to do this with my smartphone.

The general approach is to make Fiddler the smartphone's network proxy. For this, both PC and smartphone have to be in the same network.

At first, we need an installation of Fiddler. This web debugging proxy has tons of options, but comes with a well-defined out-of-the-box setting. After starting the application, we immediately see the incoming and outgoing traffic from the browser or mail client.

![]({{ site.baseurl }}/public/post_assets/2019-06-17-Android-Fiddler/fiddler_1.jpg)

This works because Fiddler registers itself as a local proxy running on port `8888`. The port can be changed in the options: *Tools -> Options -> Connections -> Fiddler listens on port*

![]({{ site.baseurl }}/public/post_assets/2019-06-17-Android-Fiddler/fiddler_2.jpg)

While we're here, we enable the option *Allow remote computers to connect*. This will allow the Android phone to use Fiddler as its proxy.

![]({{ site.baseurl }}/public/post_assets/2019-06-17-Android-Fiddler/fiddler_3.jpg)

That's it about configuring Fiddler. Finally, we can set up the Android device to use the proxy. To do so, open the settings of the WiFi to use and expand the *Advanced options*.

![]({{ site.baseurl }}/public/post_assets/2019-06-17-Android-Fiddler/wifi_settings_1.jpg)

Now we have to enter the following values:
- *Proxy* = `Manual`
- *Proxy hostname* = IP address of your PC where Fiddler is running (e. g. `192.168.178.53`)
- *Proxy port* = Port on your PC on which Fiddler is listening (e.g. `8888`)

![]({{ site.baseurl }}/public/post_assets/2019-06-17-Android-Fiddler/wifi_settings_2.jpg)

To see whether it works we can simply check this by trying to open an arbitrary website in the browser. Now we should see all the network traffic within Fiddler. If doesn't work, check your firewall whether incoming traffic to Fiddler's port is allowed.