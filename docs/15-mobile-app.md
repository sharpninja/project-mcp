---
title: Mobile App
---

# Mobile App — Specification

This document defines a **mobile application** in **phone form factor** built with **Avalonia UI**. Users authenticate with **GitHub** as the OAuth2 provider (same as the [Project Web App](14-project-web-app.html)) and use the app to **manage work items assigned to them** through a **task queue** view. The app shares the same [data model](03-data-model.html) and PostgreSQL backend (via an API) as the MCP server and web app.

---

## 1. Purpose and scope

- **Purpose:** Give users a **phone-sized** UI to view and act on **work items and tasks assigned to them** — a personal **task queue** — without opening the web app or MCP.
- **Users:** Team members and resources who need to see their assigned work, reorder or update tasks, and move work items through states (e.g. Planning → Implementation → Complete) from their phone.
- **Scope:** Centered on **assigned work**: work items and tasks where the current user (or a team they belong to) is the assigned resource. **Work items and tasks are the same entity** (task = work item with level = Task). Full tree navigation, reports, and Gantt are out of scope; the mobile app is a focused “my work” experience.
- **Authentication:** Users sign in with **GitHub** as the OAuth2 provider (same as the web app). Visibility is **claims-based**: the user only sees work assigned to them (or to a team they are a member of), within enterprises/projects allowed by their token claims.

---

## 2. Technology stack

| Layer | Choice | Notes |
|-------|--------|--------|
| **UI framework** | Avalonia UI | Cross-platform .NET UI; targets phone form factor (e.g. single-window, responsive layout for narrow screens). |
| **Runtime** | .NET 10 | Same BCL as server; Avalonia supports mobile runtimes (e.g. Android, iOS via MAUI host or Avalonia mobile). |
| **Authentication** | GitHub OAuth2 (same as web app) | Use the same GitHub OAuth App (or a dedicated mobile client with PKCE). Claims (GitHub user + app-defined scope) drive which projects/enterprises (and thus which assignments) the user can see. |
| **Backend** | REST (or similar) API | Mobile app does not connect to PostgreSQL directly. It calls an **API** (e.g. the same ASP.NET Core API used by the web app, or a dedicated mobile API) that returns the user’s task queue and accepts updates. API validates the OAuth2 access token and applies claims-based filtering. |
| **Data** | Same data model | Work items, tasks, work queue (`work_queue_items`), assignments (`resource_id` on work, work_items, tasks); see [03 — Data Model](03-data-model.html) and [00 — Definitions](00-definitions.html) (Work item, Task, Work queue). |

---

## 3. Authentication

- **GitHub as OAuth2 provider:** The mobile app uses **GitHub** as the OAuth2 provider (same as the web app). Configuration uses GitHub OAuth App **client id**, **redirect URI**, and **scopes**; for native/mobile, use **PKCE** (no client secret). Register the app as a GitHub OAuth App with a callback URL for the mobile scheme (e.g. myapp://callback).
- **Flow:** User opens app → sign-in screen → redirect or in-app browser to **GitHub** → user signs in and authorizes → GitHub redirects back with authorization code → app exchanges code (with PKCE) for access token. App stores the token securely (e.g. secure storage / keychain); access token is sent with every API request in the `Authorization` header.
- **Claims:** The **same claims** model as the web app applies: GitHub user identity plus **app-defined** enterprise/project scope (from the API after validating the GitHub token). The API restricts which work items and tasks are returned (only those in allowed projects and assigned to the user or their teams). The current user’s **resource** is resolved by matching the **GitHub user id** (from the token or GitHub API) to a resource’s **oauth2_sub** in the [data model](03-data-model.html). The API uses that resource (and any teams the resource belongs to) to filter “assigned to me” for the task queue.
- **MCP agent identity:** MCP clients (Copilot/Cursor) must resolve their agent name to a **Resource** in the enterprise; otherwise requests return **Unauthorized. Agent not approved for Enterprise** (see [04 — MCP Surface](04-mcp-surface.html)).

---

## 4. Task queue UI

- **Concept:** The user sees a **task queue** of work items and tasks **assigned to them** (or to a team they belong to). The queue reflects the [work queue](03-data-model.html) ordering where applicable: within a work item, items are ordered by `work_queue_items.position`; tasks at the same position can be grouped (e.g. “these can be done in parallel”). **Assignment rule:** direct user assignment takes precedence over team assignment; if both exist, show the item once. The mobile app may show a **flat list** of “my next items” aggregated across work items, or a **grouped view** (e.g. by work item, then queue order).
- **Contents:** Each entry in the queue represents either:
  - A **task** assigned to the user (title, status, priority, optional due date, work item and project context), or
  - A **work** entity assigned to the user (if the data model exposes work-level assignments in the queue),
  - Optionally **work item** as a header with its child tasks listed in queue order below it.
- **Actions:** The user can:
  - **View** task/work item details (title, description, state, requirements link, project).
  - **Update status** (e.g. task: todo → in-progress → done; work item state: Planning → Implementation → … → Complete).
  - **Reorder** (if the API supports it) or mark complete and let the queue advance.
  - Optionally **add a note** or log time (if the API supports it in a later phase).
- **Filtering / grouping:** Optional filters: by project, by work item state, or “due soon.” Default view: queue order (or by priority/due date) so the user knows what to do next.
- **Offline / sync:** Out of scope for v1; all data is fetched from the API when online. Future: optional local cache and sync for offline queue view.

---

## 5. Screens and navigation (outline)

- **Sign-in:** GitHub OAuth2 sign-in (redirect or in-app browser); after success, store token and go to the task queue.
- **Task queue (home):** List or grouped list of work items/tasks assigned to the user, in queue order (or priority/due). Pull-to-refresh to sync with API. Tapping an item opens its detail.
- **Task / work item detail:** Show title, description, state, project (and optionally work item parent), requirements. Buttons or picker to change state (e.g. Start, Complete). Optional: link to “open in web app” for full context.
- **Settings / profile:** Optional: show current user (from token), logout (clear tokens and return to sign-in). Optionally resource switcher if the user can act as multiple resources (e.g. team lead).

---

## 6. API contract (summary)

- The mobile app **does not** talk to PostgreSQL or MCP directly. It calls an **HTTP API** that:
  - **Authenticates** each request with the OAuth2 **access token** (e.g. Bearer token).
  - **Resolves** the user’s identity and allowed scope from the token (same claims as web app); requires **scope_slug** on scoped requests.
  - **Validates enterprise scope on every request:** Any data requested or submitted must be within the enterprise(s) the user is associated with. The API rejects and **logs** any attempt to access or modify data of another enterprise; such attempts are **followed up manually**. See [06 — Tech Requirements](06-tech-requirements.html) (Security and safety).
  - **Returns** the user’s task queue: work items and tasks where `resource_id` equals the user’s resource (or a team they are in), restricted to allowed enterprises/projects, with ordering (e.g. by work item and `work_queue_items.position`, or by task due date/priority).
  - **Accepts** updates: e.g. PATCH task (status, optional fields), PATCH work item (state). All updates are validated and scoped (user can only update items assigned to them within their claimed scope). Creation of **enterprise** records is restricted to users with the **SUDO** role (allowlist from `PROJECT_MCP_SUDO_GITHUB_IDS`); the API enforces this for any client.
- This API may be the **same** as the one used by the Blazor web app (e.g. ASP.NET Core controllers or minimal API); the mobile app is just another client using the same endpoints and auth.

---

## 7. Form factor and accessibility

- **Phone form factor:** Layout and navigation are designed for a **phone** (narrow portrait, touch, single-handed use). Avalonia UI is used with responsive controls and phone-appropriate targets (e.g. list rows, large tap areas for state change). Tablet or desktop is not the primary target but may be supported by the same app if Avalonia is configured for multiple form factors.
- **Accessibility:** Follow platform guidance (e.g. contrast, font scaling, screen readers) where Avalonia supports it.

---

## 8. Deployment and distribution

- **Build:** Avalonia app is built as a native mobile app (e.g. Android APK/AAB, iOS IPA) or as a desktop/mobile package per Avalonia’s target options. Build and signing are outside this doc; the app must be configured with the **API base URL** and **GitHub OAuth2** client settings (client id, redirect URI, scopes) for the environment.
- **No direct DB or MCP:** The mobile app **only** communicates with the **API** and **GitHub** (for sign-in); no PostgreSQL or MCP connection from the device.
- **Same provider and API as web app:** GitHub as OAuth2 provider and the same API ensure one login and consistent claims-based visibility across web and mobile.

---

## 9. Summary

| Aspect | Detail |
|--------|--------|
| **UI** | Avalonia UI, phone form factor. |
| **Auth** | **GitHub** OAuth2 (same as web app); tokens and claims drive visibility. |
| **Function** | Manage work items assigned to the user via a **task queue** (view, update status, optional reorder). |
| **Data** | Work items, tasks, work queue; assigned to the user (or their team); scope from token claims. |
| **Backend** | REST API (same as or shared with web app); no direct DB or MCP access from the device. |

This specification defines the mobile app that allows users to authenticate with GitHub as the OAuth2 provider and manage their assigned work through a task queue on their phone, implemented with Avalonia UI.
