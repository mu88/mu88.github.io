---
layout: post
title: Dependency injection and legacy code
description: How to improve the testability of legacy code by slightly introducing dependency injection.
comments: true
---

During the last couple of months, I was doing a major refactoring of the dependency injection infrastructure on the product I build with my colleagues. The application relies heavily on the [service locator pattern](https://en.wikipedia.org/wiki/Service_locator_pattern). To improve the testability, a refactoring pattern evolved that some other people might find useful.

Let's start with an example showing the initial situation:

<script src="https://gist.github.com/mu88/184d4112bac50776ca20a0dfabd378c5.js?file=ServiceLocator.cs"></script>

The component to refactor is `CarFactory`. As you can see, a global static `ServiceLocator` is used to obtain the engine and chassis instances building up the car to construct. Writing a unit test for this class can be cumbersome because you have to consider the global service locator. Furthermore, the `ServiceLocator` obscures the usage of further dependencies like `IEngine` and `IChassis`.

The pure idea of dependency injection would teach us to refactor the code to something like this:

<script src="https://gist.github.com/mu88/184d4112bac50776ca20a0dfabd378c5.js?file=PureDependencyInjection.cs"></script>

Now we're requesting the necessary dependencies via constructor injection. For unit testing, this is a perfect situation, because now we can inject mocks that mimic the required behavior and everything works fine.

But since we're not using the service locator anymore, somebody has to provide the necessary dependencies within the production code.  
Sure, we could use a composition root and a dependency injection container. But depending on the circumstances (size of application, amount of time, etc.), this can become a very hard piece of work or even almost impossible.  
Instead of using constructor injection, we could set up an integration test with a differently configured service locator. But whenever possible, I tend to favour unit over integration tests because they are usually faster and have a narrower scope.

So basically, there are two seemingly competing demands:
* Don't change the public API in order to keep the production code as untouched as possible.
* Increase the testability.

And this is how I tended to consolidate the two demands:

<script src="https://gist.github.com/mu88/184d4112bac50776ca20a0dfabd378c5.js?file=Final.cs"></script>

As you can see, the approach is pretty close to the former one using constructor injection. The difference lies in the two constructors: we still have the constructor specifying all the necessary dependencies, but it is declared `private`.  
The `public` constructor still defines no parameters. However, it is calling the private constructor and resolves the necessary dependencies using the `ServiceLocator`. This way, nothing changes in terms of the component's public API and behavior.

But then what is the added value in terms of unit testing? Unlike the C# compiler, .NET allows the use of private constructors via reflection ([see here](https://docs.microsoft.com/en-us/dotnet/api/system.activator.createinstance?view=netframework-4.8#System_Activator_CreateInstance_System_Type_System_Boolean_)). This enables us to call the private constructor from an unit test.  
Doing so manually for each and every unit test would be a pain. Fortunately, there are packages like [AutoMocker for Moq](https://github.com/moq/Moq.AutoMocker) that take away the pain. Using that package, our test looks like this:

<script src="https://gist.github.com/mu88/184d4112bac50776ca20a0dfabd378c5.js?file=UnitTest.cs"></script>

Using this refactoring technique enabled me to write unit test for a whole bunch components of our application. But it is important to keep one thing in mind: a private constructor is not marked as `private` just for fun. There are reasons why the component's creator chose it that way. Furthermore, we're bypassing the compiler via reflection - usually not the best idea :wink:  
So this technique is more like medicine: use it only in small doses or preferably not at all. Whenever possible, go for dependency injection all the way.

Happy Coding :nerd_face: and thank you for reading!