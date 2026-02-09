# Project MCP (Design Phase)

Design and documentation for a **Software Project Management MCP server** and related applications: MCP server (stdio + REST), **TODO engine library**, **Blazor web app**, and **Avalonia mobile app**, backed by PostgreSQL. **Design documentation only** — no implementation yet.

The design is **methodology-agnostic** (no prescribed agile/waterfall). It covers enterprises, projects, work items (tasks), milestones, releases, requirements, issues, standards, keywords, and related entities, with a clear identifier scheme and MCP tool/resource surface.

**Note:** **Work items and tasks are the same entity**; a **task** is a **work item** with **level = Task**.

---

## Documentation

**Start here:** [Agent context](docs/agent-context.md) — Workspace overview, conventions, and how to build and serve the docs locally (for contributors and AI agents).

**Published site:** The docs are built with **Jekyll** and published to **GitHub Pages**. Enable in **Settings → Pages → Source: GitHub Actions**. Live site: **https://sharpninja.github.io/project-mcp/**.

**Canonical index:** The full list of design docs is in [docs/index.md](docs/index.md) (rendered as [index](https://sharpninja.github.io/project-mcp/index.html) on the site).

### Core design (00–16)

| Doc | Description |
|-----|-------------|
| [00 — Definitions](docs/00-definitions.md) | Enterprise, project, requirement, work item, task, milestone, and other canonical terms |
| [01 — Overview](docs/01-overview.md) | Goal, scope, non-goals |
| [02 — Architecture](docs/02-architecture.md) | Client, MCP server, storage, web app, mobile app |
| [03 — Data Model](docs/03-data-model.md) | Entities, PostgreSQL tables, relationships, change tracking |
| [04 — MCP Surface](docs/04-mcp-surface.md) | Context key, tools, resources (API surface) |
| [05 — Tech and Implementation](docs/05-tech-and-implementation.md) | Tech choices, implementation order |
| [06 — Tech Requirements](docs/06-tech-requirements.md) | Runtime, protocol, storage, security, deployment |
| [07 — Deployment](docs/07-deployment.md) | Docker, Aspire orchestration, App Host |
| [08 — Identifiers](docs/08-identifiers.md) | GUID, display slug, hierarchy rules |
| [09 — Gaps Analysis](docs/09-gaps-analysis.md) | Historical gap analysis |
| [10 — MCP Endpoint Diagrams](docs/10-mcp-endpoint-diagrams.md) | Activity and sequence diagrams |
| [11 — Implementation Plan](docs/11-implementation-plan.md) | Phased implementation (Phase 0–12) |
| [12 — Testing Plan](docs/12-testing-plan.md) | Unit, integration, E2E, manual testing |
| [13 — Deployment Plan](docs/13-deployment-plan.md) | Local, Docker, Aspire, CI/CD, rollback; NuGet publish (ProjectMCP.Todo) |
| [14 — Project Web App](docs/14-project-web-app.md) | Blazor web app: tree, search, reports, Gantt, issues; GitHub OAuth2 |
| [15 — Mobile App](docs/15-mobile-app.md) | Avalonia phone app: OAuth2, task queue |
| [16 — Gap Analysis (TODO)](docs/16-gap-analysis-todo.md) | Remediation tracker (GAP-001+) |
| [17 — Guardrails](docs/17-guardrails.md) | Conventions and constraints for agents and edits |

### Implementation and testing plans (18–24)

| Doc | Description |
|-----|-------------|
| [18 — REST Service Implementation](docs/18-rest-implementation.md) | MCP REST layer: routes, middleware, headers, errors |
| [19 — TODO Library Implementation](docs/19-todo-library-implementation.md) | TODO engine: DI, GPS.SimpleMvc, IView, entities, repositories, slugs |
| [20 — Aspire Orchestration Implementation](docs/20-aspire-implementation.md) | App Host: Postgres, MCP server, config injection, Docker |
| [21 — TODO Engine Testing Plan](docs/21-todo-engine-testing-plan.md) | TODO library: unit, integration, slug, scope, audit |
| [22 — Blazor Web App Implementation](docs/22-blazor-webapp-implementation.md) | Blazor web app: auth, scope, tree, search, reports, Gantt, issues, Docker |
| [23 — Blazor Web App Testing Plan](docs/23-blazor-webapp-testing-plan.md) | Blazor: unit, integration, E2E, manual (auth, scope, SUDO) |
| [24 — Blazor Web App Testing Plan](docs/24-blazor-webapp-testing-plan.md) | Blazor testing (unit, integration, E2E, manual; security, a11y) |

---

## Building the docs locally

From the `docs/` directory:

```bash
cd docs
bundle install
bundle exec jekyll serve
```

The site is served at **http://localhost:4000/project-mcp/** (see [agent context](docs/agent-context.md) for baseurl and CI).

---

## License and contributing

See repository settings for license. When contributing, follow the conventions in [17 — Guardrails](docs/17-guardrails.md) and the [docs index](docs/index.md).
