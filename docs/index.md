---
title: Documentation
---

# Project MCP — Documentation

Design and documentation for the Software Project Management MCP server.

## Design docs

| Doc | Description |
|-----|-------------|
| [Agent Context](agent-context.html) | Workspace overview, conventions, doc index, build/serve instructions |
| [00 — Definitions](00-definitions.html) | Canonical terms (Enterprise, Project, Requirement, etc.) |
| [01 — Overview](01-overview.html) | Goal, scope, and high-level concepts |
| [02 — Architecture](02-architecture.html) | Client, MCP server, storage |
| [03 — Data Model](03-data-model.html) | Entities, tables, PostgreSQL |
| [04 — MCP Surface](04-mcp-surface.html) | Tools and resources |
| [05 — Tech and Implementation](05-tech-and-implementation.html) | Implementation order |
| [06 — Tech Requirements](06-tech-requirements.html) | Technical requirements |
| [07 — Deployment](07-deployment.html) | Docker, Aspire, hosting |
| [08 — Identifiers](08-identifiers.html) | GUID, slug, index rules |
| [09 — Gaps Analysis](09-gaps-analysis.html) | Historical gap analysis with resolution status; current tracking in 16 |
| [10 — MCP Endpoint Diagrams](10-mcp-endpoint-diagrams.html) | Activity and sequence diagrams per endpoint |
| [11 — Implementation Plan](11-implementation-plan.html) | Phased implementation, tasks, deliverables |
| [12 — Testing Plan](12-testing-plan.html) | Unit, integration, E2E, manual testing |
| [13 — Deployment Plan](13-deployment-plan.html) | Local, Docker, Aspire, CI/CD, rollback |
| [14 — Project Web App](14-project-web-app.html) | Blazor (.NET 10) website: tree, search, reports, Gantt, issues; Docker |
| [15 — Mobile App](15-mobile-app.html) | Avalonia UI phone app: OAuth2, task queue for assigned work items |
| [16 — Gap Analysis (TODO)](16-gap-analysis-todo.html) | Remediation tracker (currently no open gaps) |
| [17 — Guardrails](17-guardrails.html) | Constraints and conventions for agents and edits; single reference for guardrails |
| [18 — REST Service Implementation](18-rest-implementation.html) | Excruciatingly detailed implementation plan for the MCP REST layer (routes, middleware, headers, errors) |
| [19 — TODO Library Implementation](19-todo-library-implementation.html) | Excruciatingly detailed implementation plan for the TODO engine library (DI, GPS.SimpleMvc, IView, entities, repositories, slugs) |
| [20 — Aspire Orchestration Implementation](20-aspire-implementation.html) | Excruciatingly detailed implementation plan for .NET Aspire App Host (Postgres, MCP server, config injection, Docker) |

**Note:** **Work items and tasks are the same entity**; a **task** is a **work item** with **level = Task**.
