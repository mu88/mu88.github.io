---
layout: post
title: Build and debug Qooxdoo with Visual Studio Code
---

Because of my job, I got in touch with the JavaScript framework [Qooxdoo](https://www.qooxdoo.org/). I've never heard about it before, since the web development solar system seems to rotate around Angular, React and all the other big frameworks.

The process of building is pretty straightforward: you simply have to run a Python script, usually called `generate.py`. That does all the ceremony of bundling, etc. and gives you the final application.

When it comes to debugging of the application, you can put on your usual "web developer tool belt": open the HTML in your favorite browser and use it's *Developer Tools*.

For me as a Visual Studio loving C# developer, it is still odd to have three different components to build and debug my app:
* IDE to develop
* Console to build
* Browser tools to debug

Of course, these are primarily prejudices. I have read too many articles not to know that it is much easier today. So I wanted to combine all the three steps into on environment, which is Visual Studio Code.

I'll pass the first step, since it is not worth mentioning the I can write JavaScript code in VS Code :grin: Let's focus on the second step, which is building the application. According to the [Microsoft documentation](https://code.visualstudio.com/docs/editor/tasks), I've set up a `.vscode\tasks.json` with the following content:
```json
{
    "version": "2.0.0",
    "tasks": [
        {
            "label": "Build HelloWorld",
            "type": "shell",
            "command": "${workspaceFolder}/generate.py",
            "group": {
                "kind": "build",
                "isDefault": true
            }
        }
    ]
}
```
It creates a default build task called *Build HelloWorld* (which is my sample application) that simply calls the Python generator script.

Lastly, there is the step of debugging my built application right from VS Code. Again, the [Microsoft documentation](https://code.visualstudio.com/Docs/editor/debugging) was very helpful. To debug an application, I had to set up the file `.vscode\launch.json` in the following way:
```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "type": "chrome",
            "request": "launch",
            "name": "Launch HelloWorld",
            "file": "${workspaceFolder}/source/index.html"
        }
    ]
}
```
This creates the launch profile *Launch HelloWorld* which launches Google Chrome with my application. Furthermore, the IDE gets attached to the browser and I can set breakpoints in VS Code.

For me, this is a pretty convenient setting which reduces a C# developers anxiety to work with JavaScript code :wink: If you want a jump start, you can use my sample application which is available on [GitHub](https://github.com/mu88/QooxdooHelloWorld).