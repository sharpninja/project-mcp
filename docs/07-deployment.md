---
title: Deployment
---

# Deployment: Docker and Aspire Orchestration

Deployment is via **Docker containers** with **.NET Aspire** orchestration.

## Model

- **MCP server** — Run as a containerized .NET application.
- **PostgreSQL** — Run as a container (or Aspire-managed Postgres resource).
- **Orchestration** — .NET Aspire App Host project that composes and runs the containers, injects connection strings, and provides a single entry point (e.g. `dotnet run` for the App Host).

## Aspire App Host

- **App Host project** — .NET project that references the MCP server and uses Aspire.Hosting to define the distributed application.
- **Resources:**
  - **PostgreSQL** — Add via `AddPostgresContainer()` (or Aspire.Hosting.PostgreSQL) so the server has a database. Aspire can assign a unique host/port and pass the connection string to the MCP server.
  - **MCP server** — Add as a .NET project or container (`AddProject()` or `AddDockerfile()`) that receives the Postgres connection string as configuration (e.g. from Aspire’s configuration or service discovery).
- **Configuration** — Connection strings and env vars (e.g. `PROJECT_MCP_ROOT`, `DATABASE_URL`) are supplied by the App Host to the server container so no hardcoded secrets are needed.
- **Dashboard** (optional) — Use Aspire’s dashboard in development for logs, metrics, and resource listing.

## Docker

- **Dockerfile** — For the MCP server: multi-stage build (build on SDK image, run on runtime image). Publish the app and run the published executable. No HTTP server required if the host attaches to stdio; if the server is invoked as `docker run ... <mcp-server>`, the host (e.g. IDE) may run the container and attach stdin/stdout, or the server may expose a transport (e.g. HTTP) for container-friendly access; design choice to align with how the MCP client connects.
- **Base image** — Use official .NET runtime image (e.g. `mcr.microsoft.com/dotnet/aspnet:8.0` or `runtime:8.0` for console).
- **PostgreSQL** — Use official Postgres image when running as a container; Aspire can pull and run it.

## Runtime flow

1. Developer (or CI) runs the **App Host** (e.g. `dotnet run --project AppHost`).
2. Aspire starts the Postgres container and the MCP server (as container or process).
3. Aspire injects the Postgres connection string into the MCP server’s configuration.
4. MCP server connects to Postgres and is ready. If using stdio, the host that launches the App Host (or a sidecar) is responsible for attaching the MCP client to the server’s stdio or network endpoint.

## Requirements

- **Docker** — Required for running the Postgres container and (if used) the MCP server container. Dockerfile(s) in the repo.
- **Aspire** — Use Aspire.Hosting (and optional Aspire.Hosting.PostgreSQL or AddPostgresContainer) in the App Host. Target the same .NET version as the server (e.g. .NET 8).
- **Single solution** — App Host, MCP server project, and any shared projects in one solution; App Host references the server project and declares the container/resource topology.

## Out of scope for this doc

- Kubernetes or other orchestrators (Aspire + Docker only).
- Production hosting details (e.g. cloud-specific Aspire extensions) — design assumes Aspire + Docker as the deployment model; production hardening (secrets, scaling) can be added later.
