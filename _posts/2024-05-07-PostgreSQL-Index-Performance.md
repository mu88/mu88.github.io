---
layout: post
title: Poor query performance after PostgreSQL major upgrade
description: See how outdated statistics can blow up database query performance
comment_issue_id: 26
---

I want to share my learnings from a recent performance degradation in one of our apps with you. On April 24, the performance of one of our AWS RDS PostgreSQL databases became so bad that several EF Core queries timed out, leading to further problems.

**tl;dr: ALWAYS update PostgreSQL statistics after a major upgrade via `ANALYZE`.**

What happened: in March, I did a major upgrade of one of our AWS RDS PostgreSQL databases from `v14` to `v16` following a company-internal how-to guide. At the very bottom of this guide there is already the hint to update the statistics afterwards:

> When the upgrade is completed, run the following command on your database to optimize performance: `ANALYZE VERBOSE;`

Well, I somehow must have missed it 🤷🏻‍♂️

For several weeks, everything was running well and so I concluded that the upgrade was successful. But on April 24, shit hit the fan 💩 my fellow colleagues noticed that our app ran into timeouts when querying the database.  
So I analyzed the issue in-depth, but before explaining it, let's refresh some PostgreSQL insides.

## Diving into PostgreSQL insides

**Please bear with me that I'm not a database expert and therefore my rather abridged description might make some of you cry.** 😇

- _Dead tuples_ → database rows within a table that are obsolete due to deletion or an update. The associated physical space is not freed immediately, that's what `VACUUM` is for.
- _Live tuples_ → database rows within a table that are not dead, i. e. the data you receive when running a dumb `SELECT * FROM <<MyTable>>;`.
- `ANALYZE` command → gathers statistics from tables. The query planner then uses this data to determine the best execution plan for a query.
- `VACUUM` command → reclaims physical space by cleaning up dead tuples from tables so that subsequent `INSERT` or `UPDATE` statements can reuse this space.
- _Autovacuum daemon_ → periodically checks whether an `ANALYZE` or `VACUUM` command should be run to maintain a good query performance and free up space.

Since for the remainder of this post `VACUUM` won't be of importance, I will focus on `ANALYZE` from now on.

One missing piece is the question of when the autovacuum daemon is obliged to trigger `ANALYZE`. It does so by calculating a threshold like this:

{% highlight text %}

analyze threshold = analyze base threshold + analyze scale factor * number of tuples

{% endhighlight %}

- _analyze base threshold_ → config parameter, `50` by default
- _analyze scale factor_ → config parameter, `0.05` by default
- _number of tuples_ → taken from `pg_class.reltuples`

Then the analyze threshold is compared to the number of changed tuples (i. e. inserted, updated, or deleted rows) since the last `ANALYZE`.

## Detective work

The following table (with slightly rounded numbers) contains some of the aforementioned information at the time when the problem occurred:

| Table name | Inserted | Updated | Deleted | `reltuples` | Live | Dead | Last autoanalyze | Last analyze |
|------------|----------|---------|---------|----------------------|-----------|---------|------------------|---------------|
| `OurTable` | 20 | 860,000 | 0 | 40,000,000 | 2,000,000 | 150,000 | \<\<empty\>\> | \<\<empty\>\> |

As we can see, the autovacuum daemon never automatically analyzed `OurTable`, but why? Let's calculate some numbers:

{% highlight text %}

analyze threshold = 50 + 0.05 * 40,000,000 = 2,000,050
changed tuples = 20 + 860,000 + 0 = 860,020

{% endhighlight %}

From what we've learned, now the problem becomes clear: since `pg_class.reltuples`  is 20 times bigger than the number of live tuples, the analyze threshold is never reached by the number of changed tuples (`860,020 < 2,000,050`) and therefore the autovacuum daemon does not trigger `ANALYZE`.

Since the statistics diverged from reality, the query analyzer made an unpleasant choice regarding the execution plan: rather than using existing indices, a full table scan was used to query the requested data, taking way longer than accessing the data by the appropriate index.

## The solution

While it remains still unclear to me why `pg_class.reltuples` could differ from the live tuples by one order of magnitude and thereby distort the threshold, the solution was as simple as manually running `ANALYZE` for the affected table:

{% highlight sql %}

ANALYZE VERBOSE OurTable;

{% endhighlight %}

Now the previously shown table looks like this:

| Table name | Inserted | Updated | Deleted | `reltuples` | Live | Dead | Last autoanalyze | Last analyze |
|------------|----------|---------|---------|----------------------|-----------|------|------------------|-----------------|
| `OurTable` | 0 | 0 | 0 | 2,000,000 | 2,000,000 | 0 | \<\<empty\>\> | 2024-04-30 10:00 |

As you can see, the numbers of `pg_class.reltuples` and _Live_ are now the same.

## Testing

To make sure that the autovacuum daemon now triggers the `ANALYZE` command automatically again, I applied a bulk change to the table with 500,000 updates and now the table looks like this:

| Table name | Inserted | Updated | Deleted | `reltuples` | Live | Dead | Last autoanalyze | Last analyze |
|------------|----------|---------|---------|----------------------|-----------|---------|------------------|-----------------|
| `OurTable` | 0 | 500,000 | 0 | 2,000,000 | 2,000,000 | 500,000 | \<\<empty\>\> | 2024-04-30 10:00 |

After a couple of seconds, the autovacuum daemon jumped in and automatically ran `ANALYZE`:

| Table name | Inserted | Updated | Deleted | `reltuples` | Live | Dead | Last autoanalyze | Last analyze |
|------------|----------|---------|---------|----------------------|-----------|------|------------------|-----------------|
| `OurTable` | 0 | 0 | 0 | 2,000,000 | 2,000,000 | 0 | 2024-04-30 10:31 | 2024-04-30 10:00 |

Some further load tests within the app with changes between 500,000 and 2,000,000 `UPDATE`s/`INSERT`s also proved that.

## Closing

Incidents like this always leave me divided: on the one hand, it stresses me to handle and fix issues in production. But on the other hand, it's an extremely valuable opportunity to learn and become more senior.

What still annoys me is that I don't yet understand how `pg_class.reltuples` could become that outdated, so that's still an open question.

So once again my key learning 😅 never ever forget running `ANALYZE` manually after a major PostgreSQL upgrade.

Thx for reading and take care!