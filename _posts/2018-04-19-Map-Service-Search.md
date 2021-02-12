---
layout: post
title: Map Service Search
description: Using FME, ArcGIS & AngularJS to index and search Map Services.
comment_issue_id: 8
---

Almost a year ago, I gave a talk at the [FME User Conference in Vancouver](https://fmeuc.com/) with the title *"Hey dude, where is my Workspace?"* This talk was focussed on the question how to keep track of all the FME Workspaces flying around all over the enterprise. In my case, this challenge was tremendously simplified by a tool which allowed to search through the metadata of Workspaces hosted on several instances of [FME Server](https://www.safe.com/fme/fme-server/).  
Since that time, I was curious whether it would be possible to adapt the approach and apply it to our [ArcGIS Server](https://www.esri.com/en-us/arcgis/products/arcgis-enterprise/overview) farm. With the growing demand for *Map Services* (HTTP services serving map content) and therefore growing infrastructure, it gets harder to stay on top of things and answer questions like:
* Which Map Services use a specific dataset?
* Which ArcGIS Server instance hosts the Map Service named *XYZ*?
* etc.

## The idea gets born
For quite some time I know that you can see all the [used workspaces of a Map Service in *ArcGIS Server Manager*](http://enterprise.arcgis.com/en/server/latest/publish-services/windows/reviewing-service-workspaces-in-manager.htm). But until a couple of weeks ago, I've never asked myself the question: **WHERE ARE THEY COMING FROM?**  
With the help of Chrome's Developer Tools, I discovered that *ArcGIS Server Manager* uses the [ArcGIS Server Administrator API](https://developers.arcgis.com/rest/services-reference/rest-api-admin.htm) for this, more precisely it calls `https://<<Name of ArcGIS Server>>/arcgis/admin/services/<<Name of Map Service>>.MapServer/iteminfo/manifest/manifest.json`. This JSON object contains all the desired information.

## From theory to practice
Based on this discovery, I've built a web crawler which determines all Map Services of an ArcGIS Server, retrieves their corresponding `manifest.json` and extracts the desired information. I'm a big fan of the FME platform, so my hammer for this nail was a FME Workspace. Since it is all about a couple of HTTP requests, the Workspace is quite simple. By doing this, the process provides the following information:
* Name of the Map Service
* Environment of the ArcGIS Server (e. g. *Staging* or *Production*)
* Type of the ArcGIS Server (e. g. *Intranet* or *Internet*)
* All used datasources with the following details:
  * Type of the datasource  (e. g. *SDE* or *File Geodatabase*)
  * Name of the datasource (e. g. name of the Oracle or SQL Server instance)
  * Name of the dataset (name of the Feature Class, table, file, etc.)
  * Authentication mode (e. g. *Operating System Authentication* or *Database Authentication*)
  * Username used to access the datasource (only in case of an Enterprise Geodatabase)

Now I had my information, but where to store them? Due to the experiences with [Elasticsearch](https://www.elastic.co/products/elasticsearch), I decided to use this product as my back end to store the data as a *search index*. Working with Elasticsearch means that you have to communicate with their REST API to create and modify the search index - just another [HttpCaller](https://www.safe.com/transformers/http-caller/)-Transformer in the FME Workspace.

The last step was to build a small, lightweight client to access the collected information. For the sake of truth I've to admit that my comfort zone is the back end - I like to design, develop, test and stress REST APIs and stuff like that, but when it comes to client development, I always feel like a child on its first day of school. So I did a lot of research, checked out all the new frameworks, but finally I ended up with a combination of [AngularJS](https://angularjs.org/) and [Bootstrap](http://getbootstrap.com/).  
The client is pretty simple: it communicates with Elasticsearch through the REST API. In the bootstrap phase, all the distinct values for type and environment of ArcGIS Server are read through [Elasticsearch Aggregrations](https://www.elastic.co/guide/en/elasticsearch/reference/current/search-aggregations.html). This enables to filter for specific types and environments with two drop-down lists.  
After doing a search, the user gets presented all Map Services matching the search criteria. In the detail view of a search result, all datasources are listed and the onces matching the search term are highlighted.

## Time for a demo
And that's all! For those being curious to see and use the app, I've created a small demo: [https://mu88.github.io/MapServiceSearch](https://mu88.github.io/MapServiceSearch/index.html)  
Start exploring the sample data by entering the search term 'tree'!

The source code as well as the FME Workspace to create the Elasticsearch index are available on [GitHub](https://github.com/mu88/MapServiceSearch). Feel free to use it, I'd appreciate if this little tool would also help others facing the same challenges.

Thanks for reading!