---
layout: post
title: Leverage static lambdas in C# to avoid memory allocations
description: Optimize C# lambdas with static to reduce memory allocations and improve performance
comment_issue_id: 34
---

Today I will have a look at how using delegates in C# affects the app's memory footprint and how this can be optimized in certain scenarios, e.g. when using closures.

# Intro
Recently within one of my pull requests, I changed the LINQ statement `.Where(item => item.SomeProperty == "SomeFixedValue" && item.State == States.Ready)` to `.Where(static item => item.SomeProperty == "SomeFixedValue" && item.State == States.Ready)`. As you can see, all I did was add the `static` keyword to the declaration of the anonymous delegate/lambda. A colleague reviewing this PR approached me and told me that he'd never seen this before and was interested in why I did this. So I thought this might be a good thing for a dedicated blog post.

For the sake of brevity, I will use the term _lambda_ as a synonym for an anonymous function, anonymous delegate, expression lambda, and statement lambda. Of course, there are differences ([see the official Microsoft docs](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/lambda-expressions)), but they are not relevant to this article.

> 💡 In this article, the concept of _closures_ will be refered to. So let's quickly recap what a closure is. For this, take the following code `Enumerable.Range(0, 100_000).Sum(item => item * multiplier);`
> As you can see, an array with 100.000 elements is created. Afterwards, every item is multiplied by 2 and all data are summed up. In this case, the variable `multiplier` is a _closure_ of the lambda `item => item * multiplier`, as the compiler has to capture the value of `multiplier` and provide it to the lambda upon execution.

# Running some benchmarks
With the scene being set, let's dive into some code 🤓 I'm a "numbers guy", and I want to see measurement results, stuff like that. Therefore, I really love the tool [BenchmarkDotNet](https://benchmarkdotnet.org/) for doing micro benchmarking, as it takes care of a ton of aspects to provide meaningful, reproducible measurements.

First of all, this is our benchmarking code which we will go through line by line in a second:

{% highlight csharp linenos %}
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

BenchmarkRunner.Run<Benchmarks>();

[MemoryDiagnoser]
[ShortRunJob]
public class Benchmarks
{
    private static readonly long StaticMultiplier = 2;
    private int[] _items = [];

    [Params(100_000, 10_000_000)]
    public int NumberOfItems;

    [GlobalSetup]
    public void GlobalSetup() => _items = Enumerable.Range(0, NumberOfItems).ToArray();

    [Benchmark(Baseline = true)]
    public long LinqWithMultiplierAsClosure()
    {
        var multiplier = 2L;
        return _items.Sum(item => item * multiplier);
    }

    [Benchmark]
    public long LinqWithMultiplierAsConstant() => _items.Sum(item => item * 2L);

    [Benchmark]
    public long LinqStaticWithMultiplierAsConstant() => _items.Sum(static item => item * 2L);

    [Benchmark]
    public long LinqStaticWithMultiplierAsStaticField() => _items.Sum(static item => item * StaticMultiplier);
}
{% endhighlight %}

By executing `BenchmarkRunner.Run<Benchmarks>()`, BenchmarkDotNet will run all benchmarks being marked with the `[Benchmark]` attribute inside the class `Benchmarks`. So there are the following four benchmarks:
- `LinqWithMultiplierAsClosure` → this is our baseline to compare with, which contains a closure of the variable `multiplier`.
- `LinqWithMultiplierAsConstant` → instead of capturing the multiplier as a closure, directly embed it into the lambda as a local.
- `LinqStaticWithMultiplierAsConstant` → instead of having a dedicated variable `multiplier` for the value `2`, it is now moved into the lambda as a local. This avoids the closure capture.
- `LinqStaticWithMultiplierAsStaticField` → instead of having a dedicated variable `multiplier` for the value `2`, it is now moved into a static field. This avoids the closure capture.

Last but not least, the benchmark is parameterized via the public field `NumberOfItems`, i.e. all four benchmarking methods will be called twice: once with an array of 100.000 elements and another time with 10.000.000 elements. We do this to see whether the amount of input data plays a role in the allocation.

# Benchmarking results
Running the benchmark gives the following results:

| Method                                  |  NumberOfItems  | Allocated | Alloc Ratio |
|-----------------------------------------|-----------------|-----------|-------------|
| `LinqWithMultiplierAsClosure`           | 100.000         |     120 B |        1.00 |
| `LinqWithMultiplierAsConstant`          | 100.000         |      32 B |        0.27 |
| `LinqStaticWithMultiplierAsConstant`    | 100.000         |      32 B |        0.27 |
| `LinqStaticWithMultiplierAsStaticField` | 100.000         |      32 B |        0.27 |
|                                         |                 |           |             |
| `LinqWithMultiplierAsClosure`           | 10.000.000      |     132 B |        1.00 |
| `LinqWithMultiplierAsConstant`          | 10.000.000      |      44 B |        0.33 |
| `LinqStaticWithMultiplierAsConstant`    | 10.000.000      |      44 B |        0.33 |
| `LinqStaticWithMultiplierAsStaticField` | 10.000.000      |      44 B |        0.33 |

That's a lot of numbers 🤯 so let me explain:
- _Method_ → the method name of the current benchmark.
- _NumberOfItems_ → the number of elements within the array.
- _Allocated_ → the amount of memory allocated by the code under benchmark.

As we can see, both `static` cases perform better in terms of memory (32 bytes vs. 120 bytes). Interestingly though, there is no difference between the two `static` cases. To understand this better, we need to take a look at what's going on under the hood, i.e. what the compiler does. Of course, we could drive stick shift, directly jump into the IL code emitted by the compiler, and figure it out on a very low level. Although this is sometimes inevitable when analyzing certain issues, in this case checking the lowered C# code is sufficient.

> 💡 Lowering is the process when the compiler converts a high-level language feature into a lower-level language feature. For example, the C# compiler lowers a `foreach` loop into an old-fashioned `for` loop.

There are a bunch of tools out there for this, but as I'd like to stay in the inner dev loop, I prefer to use the [_IL Viewer_ tool within my JetBrains Rider IDE](https://www.jetbrains.com/help/rider/Viewing_Intermediate_Language.html) and set it to _Low-Level C#_.

## Static vs. non-static with closure
Here's the lowered C# code for `LinqWithMultiplierAsClosure` vs. `LinqStaticWithMultiplierAsConstant`:

{% highlight csharp linenos %}
public class Benchmarks
{
  public long LinqWithMultiplierAsClosure()
  {
    Benchmarks.<>c__DisplayClass4_0 cDisplayClass40 = new Benchmarks.<>c__DisplayClass4_0();
    cDisplayClass40.multiplier = 2L;
    return ((IEnumerable<int>) this._items).Sum<int>(new Func<int, long>((object) cDisplayClass40, __methodptr(<LinqWithMultiplierAsClosure>b__0)));
  }

  public long LinqStaticWithMultiplierAsConstant()
  {
    return ((IEnumerable<int>) this._items).Sum<int>(Benchmarks.<>c.<>9__5_0 ?? (Benchmarks.<>c.<>9__5_0 = new Func<int, long>((object) Benchmarks.<>c.<>9, __methodptr(<LinqStaticWithMultiplierAsConstant>b__5_0))));
  }

  [CompilerGenerated]
  private sealed class <>c
  {
    public static Func<int, long> <>9__5_0;

    internal long <LinqStaticWithMultiplierAsConstant>b__5_0(int item)
    {
      return (long) item * 2L;
    }
  }

  [CompilerGenerated]
  private sealed class <>c__DisplayClass4_0
  {
    public long multiplier;

    internal long <LinqWithMultiplierAsClosure>b__0(int item)
    {
      return (long) item * this.multiplier;
    }
  }
}
{% endhighlight %}

Of course, this is rather ugly to read - the compiler has to make sure that the lowered code does not interfere with any higher C# language feature that we could potentially write on our own. So while it's not valid for us to name a class `<>c`, this is perfectly fine for the compiler. So don't mix the angle brackets up with the concept of generics - it's really just names.

This time, let's start bottom up:
- The class `<>c__DisplayClass4_0` has a public instance field `multiplier` (line 29) which is used within the method `<LinqWithMultiplierAsClosure>b__0` (line 33), i.e. every instance of `<>c__DisplayClass4_0` could have its own `multiplier`. The method takes an `int` as input (one array element) and returns the calculation result (`long`).
- The class `<>c` has a public delegate field `<>9__5_0` (line 18) which takes an `int` as input (one array element), and returns a `long` (calculation result). Note that this is not an instance member but a static field, i.e. while there can be n instances of class `<>c`, there is only one instance of the member `<>c.<>9__5_0`.
- The class `<>c` also contains the method `<LinqStaticWithMultiplierAsConstant>b__5_0` (line 20). The method takes an `int` as input (one array element) and returns the calculation result (`long`).

Now comes the interesting part where the compiler uses the two generated classes `<>c` and `<>c__DisplayClass4_0` within the class `Benchmarks`:
- In `LinqStaticWithMultiplierAsConstant`, the compiler passes the public delegate field `<>9__5_0` from class `<>c` to LINQ's `Sum` method (line 12). If it `null`, a `new Func<int, long>` gets instantiated, pointing to the already existing member `<LinqStaticWithMultiplierAsConstant>b__5_0` from class `<>c`. Due to the null-coalescing operator `??`, only one instance will be created and reused.
- In `LinqWithMultiplierAsClosure`, the compiler creates a new instance of the `<>c__DisplayClass4_0` class and assigns the captured multiplier value to the field `multiplier` (lines 5 and 6). This is the actual closure capture. Now this instance gets passed to LINQ's `Sum` method together with the instance method `<LinqWithMultiplierAsClosure>b__0` of class `<>c__DisplayClass4_0`, encapsulated in a `new Func<int, long>` (line 7).

If you're literally a nitpicker, you will have noted the following:
- `LinqWithMultiplierAsClosure` → creates two instances
    - 1x `<>c__DisplayClass4_0`
    - 1x `Func<int, long>`
- `LinqStaticWithMultiplierAsConstant` → creates only one instance
    - 1x `Func<int, long>`

And this is where the difference of 88 bytes (120 bytes - 32 bytes resp. 132 - 44 bytes) comes from.

## Non-static with vs. without closure
The results also show that the same difference as before applies to `LinqWithMultiplierAsConstant`, too, i.e. the compiler is smart enough to detect that the lambda contains no closure and thereby avoids the corresponding allocation. For the sake of brevity, it will skip the corresponding lowered code here, as it looks exactly the same as `LinqStaticWithMultiplierAsConstant` before.

## Static vs. static
For the sake of completeness, here's the lowered C# code for `LinqWithMultiplierAsClosure` vs. `LinqStaticWithMultiplierAsConstant`:

{% highlight csharp linenos %}
public class Benchmarks
{
  private static readonly long StaticMultiplier;

  public long LinqStaticWithMultiplierAsConstant()
  {
    return ((IEnumerable<int>) this._items).Sum<int>(Benchmarks.<>c.<>9__5_0 ?? (Benchmarks.<>c.<>9__5_0 = new Func<int, long>((object) Benchmarks.<>c.<>9, __methodptr(<LinqStaticWithMultiplierAsConstant>b__5_0))));
  }

  public long LinqStaticWithMultiplierAsStaticField()
  {
    return ((IEnumerable<int>) this._items).Sum<int>(Benchmarks.<>c.<>9__6_0 ?? (Benchmarks.<>c.<>9__6_0 = new Func<int, long>((object) Benchmarks.<>c.<>9, __methodptr(<LinqStaticWithMultiplierAsStaticField>b__6_0))));
  }

  [CompilerGenerated]
  private sealed class <>c
  {
    public static Func<int, long> <>9__5_0;
    public static Func<int, long> <>9__6_0;

    internal long <LinqStaticWithMultiplierAsConstant>b__5_0(int item)
    {
      return (long) item * 2L;
    }

    internal long <LinqStaticWithMultiplierAsStaticField>b__6_0(int item)
    {
      return (long) item * Benchmarks.StaticMultiplier;
    }
  }
}
{% endhighlight %}

Due to what we've learned before, it becomes now quickly obvious why there is no benefit in terms of allocations. The compiler-generated class `<>c` now contains two public delegate fields (`<>9__5_0` and `<>9__6_0`, lines 18 and 19), and two methods for the calculation (`<LinqStaticWithMultiplierAsConstant>b__5_0` and `<LinqStaticWithMultiplierAsStaticField>b__6_0`, lines 21 to 29). But when using it inside the methods `LinqStaticWithMultiplierAsConstant` and `LinqStaticWithMultiplierAsStaticField` in class `Benchmarks`, there is no difference: we will always end up with one `new Func<int, long>` (lines 7 and 12), pointing to the corresponding calculation method. Therefore, we see no further performance gain.

# And now what?
You might be wondering and asking yourself the question _why the heck does this guy care about 88 bytes?_ And that's a great question!

So let's slightly modify our `Benchmarks` class:

{% highlight csharp %}
public class Benchmarks
{
    private int[] _items = [];
    
    [Params(100, 1_000)]
    public int NumberOfItems;

    [GlobalSetup]
    public void GlobalSetup() => _items = Enumerable.Range(0, NumberOfItems).ToArray();

    [Benchmark(Baseline = true)]
    public long LinqWithMultiplierAsClosure()
    {
        long sum = 0;
        for (int i = 0; i < _items.Length; i++)
        {
            sum += _items.Select(item => item * i).Sum();
        }
        return sum;
    }

    [Benchmark]
    public long LinqStaticWithMultiplierFromPassedState()
    {
        long sum = 0;
        for (int i = 0; i < _items.Length; i++)
        {
            sum += _items.Select(static (item, index) => item * index).Sum();
        }
        return sum;
    }
}
{% endhighlight %}

Here are the benchmark results:

| Method                                    | NumberOfItems | Allocated | Alloc Ratio |
|-------------------------------------------|---------------|-----------|-------------|
| `LinqWithMultiplierAsClosure`             | 100           |  10.96 KB |        1.00 |
| `LinqStaticWithMultiplierFromPassedState` | 100           |  10.16 KB |        0.93 |
|                                           |               |           |             |
| `LinqWithMultiplierAsClosure`             | 1.000         |  109.4 KB |        1.00 |
| `LinqStaticWithMultiplierFromPassedState` | 1.000         | 101.56 KB |        0.93 |

Now this little change begins to sum up: for 100 iterations, using the closure costs ≈800 bytes, but for 1.000 iterations already ≈8.000 bytes. So let's check the lowered C# code again:

{% highlight csharp linenos %}
public class Benchmarks
{
  public long LinqWithMultiplierAsClosure()
  {
    long sum = 0;
    Benchmarks2.<>c__DisplayClass3_0 cDisplayClass30 = new Benchmarks2.<>c__DisplayClass3_0();
    for (cDisplayClass30.i = 0; cDisplayClass30.i < this._items.Length; cDisplayClass30.i++)
      sum += (long) ((IEnumerable<int>) this._items).Select<int, int>(new Func<int, int>((object) cDisplayClass30, __methodptr(<LinqWithMultiplierAsClosure>b__0))).Sum();
    return sum;
  }

  public long LinqStaticWithMultiplierFromPassedState()
  {
    long sum = 0;
    for (int i = 0; i < this._items.Length; ++i)
      sum += (long) ((IEnumerable<int>) this._items).Select<int, int>(Benchmarks2.<>c.<>9__4_0 ?? (Benchmarks2.<>c.<>9__4_0 = new Func<int, int, int>((object) Benchmarks2.<>c.<>9, __methodptr(<LinqStaticWithMultiplierFromPassedState>b__4_0)))).Sum();
    return sum;
  }

  [CompilerGenerated]
  private sealed class <>c
  {
    public static readonly Benchmarks2.<>c <>9;
    public static Func<int, int, int> <>9__4_0;

    internal int <LinqStaticWithMultiplierFromPassedState>b__4_0(int item, int index)
    {
      return item * index;
    }
  }

  [CompilerGenerated]
  private sealed class <>c__DisplayClass3_0
  {
    public int i;

    internal int <LinqWithMultiplierAsClosure>b__0(int item)
    {
      return item * this.i;
    }
  }
}

{% endhighlight %}

Due to the changing captured closure, the compiler will have to capture 1.000 different multipliers within `Benchmarks.LinqWithMultiplierAsClosure`, i.e. 1.000 different instances of `Func<int, int>` (line 8). But for `LinqStaticWithMultiplierFromPassedState`, the passed lambda has the shape `Func<int, int, int>` (line 16), i.e. it accepts one parameter more - which is the index used as a multiplier, passed as state via the second parameter of `.Select(static (item, index) => ...)`.

Imagine we're in a hot path of an app, so you hopefully see my point by now: since it's so easy to capture a closure, one can easily end up with a lot of memory traffic, causing allocations (memory) and garbage collection (CPU).

# Conclusions
Now that we've analyzed the implications of using lambdas in terms of memory, let's focus on some conclusions.

## Awareness of implications
With the rise of the **L**anguage **IN**ntegrated **Q**uery (LINQ), lambdas are almost everywhere in modern C# code. For example when working with EF Core:

{% highlight csharp %}
var address = await GetAddressFromSomewhereElseAsync();
var personsAtThisAddress = dbContext.Persons.Where(person => person.StreetId == address.StreetId && person.HouseId == address.HouseId);
{% endhighlight %}

If you want to work with EF Core, you cannot avoid lambdas. But in general, that's not a problem - it all depends 😉 it's all about trading benefits with costs - if a tool like EF Core (which comes with the implication of making heavy use of lambdas) provides a high value and the corresponding code is not a super hot path, you're good to go. But you should be **aware** that there is a certain cost so that you can actually weigh it against the benefits.

Furthermore, let me also repeat the fact that just making a lambda statement `static` does not save you a single bit. It's more about the code's intent: by using a `static` lambda, you can express that you don't want this portion to capture any closures - it's a statement for the next reader of this code (maybe you). Furthermore, it protects you from accidentally capturing a closure in the midst of a bigger refactoring where such subtle changes can happen quickly. If a lambda is `static`, the compiler will throw an error that this is forbidden.

So should you avoid using lambdas at all? Not at all.  
Should you clutter all your lambdas with `static`, e.g. `.OrderBy(static person => person.Age)`? Definitely not, as we've seen in the _non-static with vs. without closure_ scenario.  
But you should be aware of the implications and might want to consider marking certain complex lambdas as `static`:

{% highlight csharp %}
public async Task<List<Building>> GetAllHeavyAndHighBuildingsFromTheFirstCenturyAsync()
{
    return await _dbContext
        .Buildings
        .Where(static building => 
            building.BuiltInYear >= 0
            && building.DestroyedInYear < 100
            && building.MassInElephants > 10_000
            && building.HeightInElephants > 30)
        .OrderBy(building => building.Key)
        .ToListAsync();
}
{% endhighlight %}

## Look for other APIs
Although I didn't explicitly mention it, but we already saw another alternative by using APIs avoiding closure allocations by passing additional state. Using `.Select(static (item, index) => ...)` is one such example, where capturing the index from the outer scope can be avoided by using another overload of `Select`. So if you're lucky, you might find other APIs allowing you to pass additional state.

Here's another example from some EF Core code:

{% highlight csharp %}
public async Task<int> RunInTransactionWithClosureAsync(CustomDbContext context, CancellationToken cancellationToken = default)
{
    IExecutionStrategy strategy = context.Database.CreateExecutionStrategy();
    return await strategy.ExecuteAsync(async () => await InnerRunInTransactionAsync(context, cancellationToken));
}

public async Task<int> RunInTransactionWithLessClosureAsync(CustomDbContext context, CancellationToken cancellationToken = default)
{
    IExecutionStrategy strategy = context.Database.CreateExecutionStrategy();
    return await strategy.ExecuteAsync(
        context,
        async (innerContext, innerToken) => await InnerRunInTransactionAsync(innerContext, innerToken),
        cancellationToken);
}
{% endhighlight %}

As you can see, `IExecutionStrategy.ExecuteAsync` provides an additional overload which accepts some state (the `CustomDbContext` in this case) and an external `CancellationToken`. By using this specific overload, the closure captures of `context` and `cancellationToken` can be mitigated. However, there will still be a closure allocation of `Func<Task<int>>` for the method `InnerRunInTransactionAsync`.

## Let your IDE help you
Modern IDEs come with a ton of features. But from my experience, only a small fraction of developers use them - so get to know your toolbelt! 🤓

For example, in JetBrains Rider/ReSharper, there is the inspection _Lambda expression/anonymous method can be made static_. It is disabled by default, and in my environments, I've configured this as a hint so that I get informed when there is an opportunity for making a lambda `static`. I started with having it as a warning, but that was too verbose for me, as it is also raised for simple statements like `.OrderBy(person => person.Age)`.

And again for JetBrains lovers, there is the fantastic [HeapAllocationViewer for Rider and ReSharper](https://github.com/controlflow/resharper-heapview) which statically checks your code and (among other things) informs you about closure allocations with a nice little hint, too.

# Summary
In this post, we've seen how capturing closures with anonymous functions/lambdas can lead to memory allocations. Furthermore, we analyzed the compiler-generated low-level C# code to see what's going on under the hood. In closing, we took a look at when to use `static` lambdas and how JetBrains tools can support you with that.

Thx for reading and take care!
