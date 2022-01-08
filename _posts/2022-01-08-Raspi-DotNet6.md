---
layout: post
title: Running ASP.NET Core 6 within Docker on Raspberry Pi 4
published: true
comment_issue_id: 13
---

Yesterday I've upgraded all of my personal projects to .NET 6 and I had some issues running them within Docker on my Raspberry Pi 4.

On my Windows 10 dev machine the upgrade worked smoothly. After a couple of minutes, I could run and debug each dockerized application.

As I pulled the new Docker images onto my Raspi running _Debian Buster_ and tried to start the containers as before, nothing happened. Each container exited immediately without any error message. `docker logs <<container ID>>` returned nothing as well.

After filing a [GitHub issue](https://github.com/dotnet/aspnetcore/issues/39372), [mthalman](https://github.com/mthalman) gave me golden hint: the `libseccomp` package had to be updated. For this, the following steps were necessary:
1. Add additional source repo for `apt-get`:<br>
```shell
sudo nano /etc/apt/sources.list
deb http://raspbian.raspberrypi.org/raspbian/ testing main
```
2. Install the package update:<br>
```shell
sudo apt-get install libseccomp2/testing
```

After a final reboot I could successfully run my Docker containers based on .NET 6.

Maybe someone of you also has the same problem and this will help you.

Thanks for reading and take care!
