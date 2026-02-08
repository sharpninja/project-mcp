---
title: MCP Surface
---

# MCP Surface

## Resources (read-only)

Expose current project state so the client can load context without calling tools.

| URI | Description |
|-----|-------------|
| `project://current/spec` | Project metadata, tech stack, doc list |
| `project://current/tasks` | Full task list |
| `project://current/plan` | Milestones, releases |

If the MCP SDK does not support query params on resource URIs, use separate resource names for filtered views (e.g. `project://current/tasks/milestone/{id}`) only if needed; v1 can keep a single tasks resource and filter via tools.

## Tools (actions)

Naming: consistent `*_create`, `*_update`, `*_list` (and `*_delete` where applicable).

### Project

- **project_get_info** — Return current project metadata (name, description, status, tech stack, docs). Return empty/not-initialized if no project exists.
- **project_update** — Create or update project: name, description, status, techStack (object), docs (array of { name, path, type, description? }).

### Tasks

- **task_create** — title (required), description, status, priority, assignee, labels, milestoneId, releaseId.
- **task_update** — id (required), plus any fields to change.
- **task_list** — optional filters: status, milestoneId, assignee.
- **task_delete** — id (required).

### Planning

- **milestone_create** / **milestone_update** / **milestone_list**
- **release_create** / **release_update** / **release_list**

### Docs

- **doc_register** — Add or update a doc entry (name, path, type, optional description).
- **doc_list** — List registered docs.
- **doc_read** — Read file contents by path (relative to project root). Optional; useful so the AI can load doc content via MCP.

All tools return structured results (e.g. JSON) in the tool response content. Errors return a consistent shape (e.g. `{ error: string }`) with `isError: true` where applicable.
