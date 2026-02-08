---
title: Overview
---

# Software Project Management MCP — Overview

## Goal

Build an MCP server that gives an AI assistant the ability to manage software projects end-to-end. The top-level hierarchy is **enterprise** (ownership of projects and resources); under it are **projects** and their work items. See [00 — Definitions](00-definitions.md).

- **Tasks/issues** — create, list, update, assign, prioritize
- **Planning** — milestones, releases
- **Documentation** — register docs (README, ADRs, specs), list, optionally read file contents
- **Project metadata** — name, description, status
- **Tech stack** — languages, frameworks, key dependencies

**Storage:** PostgreSQL database. The design will allow adding GitHub, Jira, or other backends later (e.g. sync or alternative store).

## Scope

- **In scope:** Enterprise as top-level ownership; full project management (tasks, planning, docs, metadata, tech stack) under enterprises, with PostgreSQL as the primary store.
- **Out of scope for v1:** Integrations (GitHub, Jira); multi-project or multi-workspace switching; authentication.

## Methodology neutrality

**Do not assign any assumptions of methodology from outside agile or any other specific methodology.** The design is **methodology-agnostic**: it does not assume or impose concepts that belong to a particular process (e.g. agile, Scrum, Waterfall, Kanban). Terms such as work, task, milestone, requirement, and release are defined in this doc set without binding them to any external methodology. Organizations apply their own process on top of this model.

## Non-goals

- No code implementation in this phase — design documentation only.
