---
title: Overview
---

# Software Project Management MCP — Overview

## Goal

Build an MCP server that gives an AI assistant the ability to manage software projects end-to-end. The top-level hierarchy is **enterprise** (ownership of projects and resources); under it are **projects** and their work items. See [00 — Definitions](00-definitions.html).

- **Tasks/issues** — create, list, update, assign, prioritize
- **Planning** — milestones, releases
- **Documentation** — register docs (README, ADRs, specs), list, optionally read file contents
- **Project metadata** — name, description, status
- **Tech stack** — languages, frameworks, key dependencies

**Note:** **Work items and tasks are the same entity**; a **task** is a **work item** with **level = Task**.

**Storage:** PostgreSQL database. The design will allow adding GitHub, Jira, or other backends later (e.g. sync or alternative store).

## Scope

- **In scope:** Enterprise as top-level ownership; full project management (tasks, planning, docs, metadata, tech stack) under enterprises, with PostgreSQL as the primary store.
- **Out of scope for v1:** Integrations (GitHub, Jira); multi-project or multi-workspace switching; authentication.

## Methodology neutrality

**Do not assign any assumptions of methodology from outside agile or any other specific methodology.** The design is **methodology-agnostic**: it does not assume or impose concepts that belong to a particular process (e.g. agile, Scrum, Waterfall, Kanban). Terms such as work, task, milestone, requirement, and release are defined in this doc set without binding them to any external methodology. Organizations apply their own process on top of this model.

## How an agent (Copilot, Cursor, or other MCP client) uses this MCP

An AI agent such as GitHub Copilot or Cursor connects to the Project MCP server as the **MCP client**. The agent uses the server in two ways: **resources** (read-only context) and **tools** (actions). The agent’s role is to turn user intent into the right sequence of resource reads and tool calls, then summarize or act on the results.

### Loading context with resources

**Resources** are read-only URIs the agent can subscribe to or fetch to load project state into its context without performing writes.

- **`project://current/spec`** — Project metadata, tech stack, and doc list. The agent uses this to know the project’s name, description, status, and which docs (README, ADRs, specs) exist.
- **`project://current/tasks`** — Full task list. The agent uses this to answer “what tasks are there?”, “what’s in progress?”, or to reason about workload before creating or updating tasks.
- **`project://current/plan`** — Milestones and releases. The agent uses this to answer “what’s in this milestone?” or “when is the next release?” and to link tasks to milestones/releases.

The agent typically fetches or subscribes to these resources when the user’s question is about *current state* (e.g. “What’s the status of the project?”, “List open tasks”, “What milestones do we have?”). Loading resources gives the model structured, up-to-date context so it can answer or decide the next action without guessing.

### Taking action with tools

**Tools** are operations the agent calls to change or query data. The agent maps user intent to one or more tool calls and returns the tool results (or an error) to the user.

- **Project** — `project_get_info` to read metadata; `project_update` to create or change the project (name, description, status, tech stack, docs).
- **Tasks** — `task_create`, `task_update`, `task_list`, `task_delete` so the agent can add tasks, change status/assignee/priority, list/filter tasks, or remove them.
- **Planning** — `milestone_create` / `milestone_update` / `milestone_list`, `release_create` / `release_update` / `release_list` so the agent can manage milestones and releases and associate work with them.
- **Docs** — `doc_register` to add or update a doc entry; `doc_list` to list docs; `doc_read` to read file contents (e.g. README or an ADR) so the agent can use doc content in its reasoning or answers.

The agent chooses tools based on the user’s request (e.g. “Add a task to fix the login bug” → `task_create`; “Mark task X done” → `task_update`; “What does the spec say about auth?” → `doc_read` or `project://current/spec` plus `doc_read` if it needs the file body).

### Typical flows

1. **“What’s going on with the project?”** — Agent fetches `project://current/spec`, `project://current/tasks`, and optionally `project://current/plan`, then summarizes.
2. **“Add a task: Implement password reset”** — Agent calls `task_create` with title (and optional description, status, priority, assignee, milestone/release); may call `task_list` or resources afterward to confirm or show the new task.
3. **“What’s in the Beta milestone?”** — Agent fetches `project://current/plan` (or uses `milestone_list`), then `project://current/tasks` or `task_list` filtered by milestone, and lists tasks in that milestone.
4. **“Update the README”** — Agent might use `doc_read` to get current content, then (outside MCP) suggest or apply edits; it can use `doc_register` to register or update the doc entry if the path or metadata changes.
5. **“Get next work item and begin work”** — Agent calls `work_queue_get` to receive the next work item (returned as a prompt); it **subscribes to `work_item://{id}` for the work item and its child items**, then begins processing the prompt, creating or updating child work items/tasks as needed.
6. **“Get next five work items”** — Agent calls `work_queue_get` with **count = 5** and returns the first five work items or tasks in the queue.
7. **“Get next five tasks”** — Agent calls `work_queue_get` with **count = 5** to get the queued work items, then **walks each work item’s queue** (another `work_queue_get`) to retrieve task-level items (level = Task).
8. **“Mark work complete”** — Agent calls `work_item_update` with the work item **slug** and sets **state = Complete**; the completed item no longer appears in `work_queue_get`.
9. **“Mark task complete”** — Agent calls `work_item_update` with the task **slug** and sets **status = done** (task = work item with level = Task).
10. **“Read the architecture decision for auth”** — Agent uses `doc_list` to find the relevant ADR, then `doc_read` with that path to load the content and answer.

### Example: requirement → work breakdown

User prompt: **“Add requirement to implement a todo list and assign the work to Copilot.”**

**Expected tool flow (tasks are work items with level = Task):**

1. **requirement_create** — Create the requirement (title: “Implement todo list”, description as needed).
2. **work_item_create** — Create a **parent work item** (level = Work) assigned to **copilot**; use the requirement as the rationale.
3. **work_item_requirement_add** — Link the parent work item to the requirement.
4. **work_item_create** — Create a **planning work item** (level = Work) as a child of the parent (parentId = parent work item).
5. **work_item_create** — Create **planning tasks** (level = Task) under the planning work item (e.g. “Create implementation plan”, “Write plan markdown”).
6. **doc_register** — Register the plan file (e.g. `docs/plan.md`) after the agent writes it in the repo.
7. **work_item_create** — Create an **implementation work item** (level = Work) as another child of the parent, with child tasks (level = Task) for coding steps.
8. **work_item_create** — Create a **testing work item** (level = Work) as another child of the parent, with child tasks (level = Task) for unit/integration tests.

This yields a single requirement linked to a parent work item, with second-level work items for planning, implementation, and testing, each with task-level children.

### Summary

The server **issues a context key** during the initial handshake; the agent **must include it in all subsequent requests** (tool calls and resource reads). The agent **sets scope once** (via **scope_set** or during the handshake); the server **remembers that scope for the agent’s session** until the agent requests a different scope. The agent **uses resources to load context** and **uses tools to query or change state** within that scope. It does not need to know PostgreSQL or internal APIs; it only uses the MCP surface (resource URIs and tool names/parameters). The server is the single interface for project state and actions, so Copilot, Cursor, or any other MCP-compatible client can manage the project in a uniform way.

## Non-goals

- No code implementation in this phase — design documentation only.
