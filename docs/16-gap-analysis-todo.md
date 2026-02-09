---
title: Gap Analysis — Remediation Tracker
---

# Gap Analysis — Remediation Tracker

This document lists **gaps** in the design and documentation in **todo format** for tracking remediation. Each gap has a unique **identifier** (e.g. **GAP-001**) so it can be referenced in chat or in other docs. Mark items as completed by changing `[ ]` to `[x]` and optionally adding a "Resolved" note.

**How to use:** Reference a gap by id (e.g. "fix GAP-003") or link to this doc. When a gap is remediated, check the box and add a short "Resolved: …" line.

**Categories for new gaps:** Definitions & terms | Data model & schema | Identifiers | MCP surface | Web/Mobile & auth | Implementation plan | Security & compliance | Testing & deployment | Cross-cutting. Use **High / Medium / Low** severity as needed.

**Note:** **Work items and tasks are the same entity**; a **task** is a **work item** with **level = Task**.

---

## Open gaps

### Medium — Implementation plan

## Resolved gaps

- [x] **GAP-001** — **GPS.SimpleMVC package reference:** Exact NuGet package ID/version confirmed. *Remediate: confirm package name/version and update 19-todo-library-implementation.md and solution references.* Resolved: set to `GPS.SimpleMVC` `2.0.0`.
- [x] **GAP-003** — **Migration strategy:** Aspire plan leaves the EF Core migration strategy undecided (startup migrate vs. separate step). *Remediate: choose the migration approach and update 20-aspire-implementation.md and related docs.* Resolved: use startup migrations via `context.Database.Migrate()`.
- [x] **GAP-002** — **Aspire hosting API names:** App Host plan uses placeholder method names for Postgres/database/reference/Dockerfile APIs. *Remediate: verify current Aspire.Hosting APIs and update 20-aspire-implementation.md with exact method names and package IDs.* Resolved: use `Aspire.Hosting.AppHost`/`Aspire.Hosting.PostgreSQL` 13.1.0 and `AddPostgres`, `AddDatabase`, `WithReference`, `AddDockerfile`.

To add a gap: use the next **GAP-XXX** id, follow the format below, add a row to the summary table, and place the item under the appropriate severity (High / Medium / Low) and category.

**Format:** `- [ ] **GAP-XXX** — **Title:** Description. *Remediate: …*`

**Resolved example:** `- [x] **GAP-001** — **Title:** … *Remediate: …* Resolved: added to 03 and 08.`

---

## Summary by identifier

| ID | Category | One-line summary |
|----|----------|-------------------|
| GAP-001 | Implementation plan | GPS.SimpleMVC package ID/version confirmed (resolved) |
| GAP-003 | Implementation plan | EF Core migration strategy set to startup migrate (resolved) |
| GAP-002 | Implementation plan | Aspire.Hosting API method names confirmed (resolved) |

*Total: 0 open gaps. Use this tracker for future additions.*
