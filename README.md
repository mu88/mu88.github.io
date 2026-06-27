This blog uses [Jekyll](http://jekyllrb.com) and is based on the theme [Lanyon](https://github.com/poole/lanyon). I had to modify almost nothing, just fork and run. So all the glory goes to [mdo](https://github.com/mdo).

Open sourced under the [MIT license](LICENSE.md).

## Features

### GitHub Pages + slowUp Calendar Feed

A single GitHub Actions workflow builds the Jekyll site, generates an iCalendar feed (`slowUp.ics`) from [slowUp.ch](https://www.slowup.ch/national/de.html) events, and publishes both from GitHub Pages artifacts. The feed is automatically available at `https://mu88.github.io/slowUp.ics` every Monday at 2:00 UTC.

**Workflow:**
- Script: `scripts/generate-slowup-ics.cs`
- Workflow: `.github/workflows/pages.yml`
- Output: `_site/slowUp.ics` (not committed to git, published via GitHub Pages artifacts)

**Features of the feed:**
- Event title extracted from detail page (e.g., "slowUp Emmental-Oberaargau")
- Date and time in Europe/Zurich timezone (DST-aware)
- Events marked as free/busy time (TRANSP: TRANSPARENT)
- Detail page URL included in event description

The Pages source for this repository must be set to **GitHub Actions** so the built `_site` folder can be published without writing the generated feed back into the repository.