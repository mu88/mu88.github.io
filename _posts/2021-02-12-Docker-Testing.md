---
layout: post
title: Utilizing Docker when testing performance enhancements
description: I'll show how I used Docker to test and measure the performance enhancements that I've implemented.
comment_issue_id: 9
---

With the beginning of 2021, I started my new job as a Full Stack Developer for [Swiss Post](https://www.post.ch/en/). My first task was to improve the performance of a long-running ASP.NET Core Web API endpoint. As usual, I was writing unit and integration tests to ensure that the new code is doing what it is supposed to.

When finished with coding, I wanted to measure how the enhancements apply to a larger, more realistic dataset. I decided to figure out how I can make use of Docker containers.

For local development, we're using [SQL Server LocalDB](https://docs.microsoft.com/de-de/sql/database-engine/configure-windows/sql-server-express-localdb). Since Microsoft shipped WSL 2, Linux Docker Containers are super fast on Windows 10 and I switched over to [SQL Server for Linux](https://hub.docker.com/_/microsoft-mssql-server). I created a container with the following command:

{% highlight shell %}
docker run --name sqlserver2017 -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=yourStrong(!)Password' -p 1433:1433 -d mcr.microsoft.com/mssql/server:2017-latest
{% endhighlight %}

Now I applied our database schema using an Entity Framework Core Migration. This resulted in a complete but empty database. I created a snapshot of my current data state so that I could always come back to my starting point. I did so by calling:

{% highlight shell %}
docker commit sqlserver2017 mu88/db:initialized
{% endhighlight %}

This command created a new Docker Image `mu88/db:initialized` by taking a snapshot from the container `sqlserver2017`.

Thankfully, my wise colleagues had already created some Web API endpoints to seed test data. So all I had to do was a little `HTTP POST` and wait a couple of minutes to generate my test data (lets say for 50 customers). After that I was ready to test my new code. Since the mentioned long-running process does some data mutation, I created another snapshot:

{% highlight shell %}
docker commit sqlserver2017 mu88/db:test_50_customers
{% endhighlight %}

Now I could trigger the long-running process and extracted some relevant metrics after it was finished. The numbers were not so clear, that's why I decided to do another test with 500 customers.

For this, I had to stop and remove the actually running container and go back to my starting point `mu88/db:initialized`:

{% highlight shell %}
docker stop sqlserver2017
docker rm sqlserver2017
docker run --name sqlserver2017 -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=yourStrong(!)Password' -p 1433:1433 -d mu88/db:initialized
{% endhighlight %}

Please note the very last line: I started a new container using my snapshot image. Now I did another `HTTP POST` to generate 500 customers and created another snapshot:

{% highlight shell %}
docker commit sqlserver2017 mu88/db:test_500_customers
{% endhighlight %}

After another performance test, the metrics looked good, but we immediately found another code spot that yelled for optimization. For the testing, I could rely on my snapshots by calling

{% highlight shell %}
docker stop sqlserver2017
docker rm sqlserver2017
docker run --name sqlserver2017 -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=yourStrong(!)Password' -p 1433:1433 -d mu88/db:test_50_customers
{% endhighlight %}

or

{% highlight shell %}
docker stop sqlserver2017
docker rm sqlserver2017
docker run --name sqlserver2017 -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=yourStrong(!)Password' -p 1433:1433 -d mu88/db:test_500_customers
{% endhighlight %}

With every round of "code & measure", that approach became more and more valuable because I could rely on a set of data snapshots.

For me this was a super interesting lesson of how container technologies can help when it comes to testing.

I hope this was interesting for you. Thanks for reading!