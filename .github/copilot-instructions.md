# ProjectMCP Copilot Instructions

## Build/serve docs (Jekyll)
- From `docs\`:
  - `bundle install`
  - `bundle exec jekyll serve` (local site at `http://localhost:4000/project-mcp/`)
  - `bundle exec jekyll build`
- Docker (if Jekyll is installed via Docker), from `docs\`:
  - `docker run --rm -p 4000:4000 -v ${PWD}:/srv/jekyll jekyll/jekyll jekyll serve --watch --baseurl /project-mcp`
- Requires Ruby 3.2 + Bundler.

## High-level architecture (design)
- This is a **design/documentation-only** repo; implementation is planned but not present yet.
- Planned system: MCP server exposing **tools** (actions) and **resources** (read-only state) over **stdio + REST**, backed by **PostgreSQL**.
- A reusable **TODO engine library** encapsulates domain logic and is shared by the MCP server and other hosts.
- Optional clients: **Blazor web app** and **Avalonia mobile app** using the same API and OAuth2 (GitHub).

## Key conventions
- **Work items and tasks are the same entity**; a **task** is a **work item** with `level = Task`.
- **Methodology-agnostic** wording only (no agile/waterfall prescriptions).
- **Doc numbering:** two-digit prefixes (00–16); new docs use the next number and must be added to `docs\index.md` with title, `.html` link, and description.
- **Cross-references:** use built-site links with `.html` (e.g., `03-data-model.html`), not `.md`.
- **Gap tracking:** `docs\16-gap-analysis-todo.md` uses `GAP-XXX` ids and the checkbox format; mark resolved with `[x]` and optional “Resolved: …”.
- **Single source of truth:** update canonical docs first (Definitions 00, Data Model 03, MCP Surface 04, Identifiers 08) and then update references.
- **No hardcoded secrets:** document config as env/config keys only.
