# Clean Architecture Guide for Entra ID POC

## Question 2: Should Authentication/Authorization Live in the API?

### Short Answer: YES — but structured properly.

In modern distributed systems, **authentication** (proving identity) and **authorization** (checking permissions) are cross-cutting concerns that should be handled at the **API gateway/layer**, not buried in business logic. However, the implementation should follow Clean Architecture principles to keep it maintainable.

---

## Current POC Architecture (Simplified Clean Architecture)

```
┌─────────────────────────────────────────────────────────────────┐
│                        PRESENTATION LAYER                        │
│  React SPA (MSAL.js) → HTTP calls with Bearer token              │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                         API LAYER                                │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  Controllers (Auth, Projects, Admin)                     │   │
│  │  - Thin, no business logic                              │   │
│  │  - Delegate to services                                  │   │
│  └──────────────────────────────────────────────────────────┘   │
│                              │                                   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  Middleware Pipeline                                     │   │
│  │  ├── Authentication (JWT validation via Microsoft)       │   │
│  │  ├── UserResolution (Entra → DB bridge)                │   │
│  │  └── Authorization (Policy checks: AdminOnly, etc.)    │   │
│  └──────────────────────────────────────────────────────────┘   │
│                              │                                   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  Application Services                                    │   │
│  │  ├── UserResolutionService (auto-provisioning)           │   │
│  │  ├── ProjectService (business logic)                     │   │
│  │  └── AuditService (logging)                            │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      INFRASTRUCTURE LAYER                        │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  Data Access (EF Core + PostgreSQL)                      │   │
│  │  - AppDbContext                                          │   │
│  │  - LINQ queries + raw SQL where efficient                │   │
│  └──────────────────────────────────────────────────────────┘   │
│                              │                                   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  External Identity (Microsoft Entra ID)                    │   │
│  │  - JWT validation via JWKS endpoint                      │   │
│  │  - Token issuance (OAuth 2.0 / OIDC)                     │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

---

## Why This Structure Works

| Layer | Responsibility | Why It Belongs Here |
|-------|---------------|---------------------|
| **Middleware** | AuthN/AuthZ | Runs on every request before controllers. No business logic. |
| **Controllers** | HTTP concerns | Route mapping, input validation, HTTP status codes. |
| **Services** | Business logic | What users can do, how data transforms. No HTTP/DB details. |
| **DbContext** | Data access | EF Core handles SQL generation. Services don't write SQL. |

---

## Alternative: Extract to Separate Identity Service

For larger systems, you might want a dedicated **Identity Microservice**:

```
┌─────────────┐     ┌─────────────────────┐     ┌─────────────┐
│   React     │────▶│  API Gateway        │────▶│  Your API   │
│  Frontend   │     │  (YARP / Ocelot)    │     │  (Business) │
└─────────────┘     └─────────────────────┘     └─────────────┘
                           │                           │
                           ▼                           ▼
                    ┌─────────────┐             ┌─────────────┐
                    │  Identity   │             │  PostgreSQL │
                    │  Service    │             │  (App Data) │
                    │  (Entra ID) │             └─────────────┘
                    └─────────────┘
```

**When to use this:**
- Multiple APIs need the same auth logic
- You have non-.NET services (Python, Node) that need auth
- You need custom token issuance (refresh tokens, API keys)

**For this POC:** The current single-API approach is simpler and sufficient.

---

## Clean Architecture Folder Structure (If You Want to Go Further)

```
EntraIdPoc/
├── src/
│   ├── EntraIdPoc.Api/              # Presentation (Controllers, Middleware)
│   ├── EntraIdPoc.Application/     # Business logic (Services, DTOs, Interfaces)
│   ├── EntraIdPoc.Domain/           # Core entities (User, Project, pure C#)
│   ├── EntraIdPoc.Infrastructure/    # EF Core, External APIs, Email
│   └── EntraIdPoc.Identity/         # Entra ID integration (optional separate service)
├── tests/
│   ├── EntraIdPoc.UnitTests/
│   └── EntraIdPoc.IntegrationTests/
└── EntraIdPoc.Frontend/
```

**Current POC is a pragmatic middle ground** — not full Clean Architecture (which adds 3+ projects), but cleanly separated within one project. Good for POCs, easy to refactor later.
