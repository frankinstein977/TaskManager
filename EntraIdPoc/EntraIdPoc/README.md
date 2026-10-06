# 🔐 Entra ID POC — .NET 8 + PostgreSQL + React

A complete proof-of-concept demonstrating Microsoft Entra ID (Azure AD) authentication integrated with a .NET 8 Web API, PostgreSQL database, and React frontend. Features auto-provisioning, role-based access control, and an admin panel.

## Architecture Overview

```
┌─────────────┐     ┌──────────────────┐     ┌─────────────────┐     ┌─────────────┐
│   React     │────▶│  .NET 8 Web API  │────▶│   PostgreSQL    │     │  Microsoft  │
│  Frontend   │     │  (JWT Validation │     │  (Users, App    │◄────│  Entra ID   │
│  (MSAL.js)  │◄────│  + DB Bridge)    │     │   Data, Audit)  │     │  (Identity) │
└─────────────┘     └──────────────────┘     └─────────────────┘     └─────────────┘
```

**Key Design Principle:** Entra ID handles *authentication* (who you are). Your PostgreSQL database handles *authorization* (what you can do) and *application data*. They are linked by the immutable `entra_oid` claim.

## Features

- ✅ **Microsoft Entra ID OAuth 2.0 / OIDC** authentication
- ✅ **JWT token validation** against Microsoft JWKS endpoint
- ✅ **Auto-provisioning** — new users automatically created in PostgreSQL on first login
- ✅ **Role-based access control** — Admin, Editor, User, Guest roles
- ✅ **Guest user support** — external users get read-only access by default
- ✅ **Admin panel** — manage users, roles, activation status
- ✅ **Audit logging** — track all security events
- ✅ **React frontend** with MSAL.js integration
- ✅ **Swagger UI** with JWT authentication support
- ✅ **Ready for Azure App Service** deployment

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/)
- [PostgreSQL 14+](https://www.postgresql.org/download/)
- [Azure account](https://azure.microsoft.com/free/) (for Entra ID app registration)

## Step-by-Step Setup

### 1. Register Application in Microsoft Entra ID

1. Go to [Azure Portal](https://portal.azure.com) → **Microsoft Entra ID** → **App registrations** → **New registration**
2. **Name:** `EntraIdPoc-Local`
3. **Supported account types:** 
   - Single tenant: `Accounts in this organizational directory only`
   - Multi-tenant: `Accounts in any organizational directory` (for external guests)
4. **Redirect URI:** 
   - Platform: **Single-page application (SPA)**
   - URI: `http://localhost:5173`
5. Click **Register**

### 2. Configure App Registration

**Authentication tab:**
- Add another redirect URI: `http://localhost:5173/auth/callback`
- Enable **Access tokens** and **ID tokens** for implicit flow (for SPA)
- Enable **Refresh tokens**

**App roles tab (add these):**
| Display name | Value | Description | Allowed member types |
|-------------|-------|-------------|---------------------|
| App Admin | App.Admin | Full system access | Users/Groups |
| App Editor | App.Editor | Can create/edit content | Users/Groups |
| App User | App.User | Standard user access | Users/Groups |

**API permissions tab:**
- Add permission → **Microsoft Graph** → **Delegated permissions**
- Select: `openid`, `profile`, `User.Read`, `email`
- Click **Grant admin consent** (required for app roles to appear in tokens)

**Expose an API tab:**
- Add scope: `access_as_user` (for API protection)
- Add client application: your frontend's client ID

**Certificates & secrets tab:**
- Create a new **Client secret** (copy the value immediately!)

### 3. Note Down Credentials

From the **Overview** page, copy:
- **Application (client) ID** → `ClientId` in config
- **Directory (tenant) ID** → `TenantId` in config
- **Client secret** (from step above) → `ClientSecret` in config

### 4. Configure Backend

```bash
# Navigate to API project
cd EntraIdPoc.Api

# Set user secrets (recommended for local dev)
dotnet user-secrets init
dotnet user-secrets set "AzureAd:ClientId" "YOUR_CLIENT_ID"
dotnet user-secrets set "AzureAd:TenantId" "YOUR_TENANT_ID"
dotnet user-secrets set "AzureAd:ClientSecret" "YOUR_CLIENT_SECRET"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=entraid_poc;Username=postgres;Password=YOUR_PASSWORD"
```

Or edit `appsettings.Development.json` directly.

### 5. Set Up PostgreSQL

```bash
# Create database (using psql or pgAdmin)
CREATE DATABASE entraid_poc;

# Or run migrations (will auto-create if configured)
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 6. Run Backend

```bash
cd EntraIdPoc.Api
dotnet run
```
- API runs at: `https://localhost:7001`
- Swagger UI: `https://localhost:7001/swagger`

### 7. Configure & Run Frontend

```bash
cd EntraIdPoc.Frontend

# Copy environment file
cp .env.example .env

# Edit .env with your credentials
VITE_ENTRA_CLIENT_ID=YOUR_CLIENT_ID
VITE_ENTRA_TENANT_ID=YOUR_TENANT_ID

# Install dependencies
npm install

# Run dev server
npm run dev
```
- Frontend runs at: `http://localhost:5173`

## Authentication Flow

```
1. User clicks "Sign in with Microsoft" (React + MSAL.js)
         ↓
2. Redirected to login.microsoftonline.com (Entra ID)
         ↓
3. User authenticates (password, MFA, etc.)
         ↓
4. Redirect back to app with authorization code
         ↓
5. MSAL exchanges code for ID token + Access token
         ↓
6. Frontend calls API: Authorization: Bearer <token>
         ↓
7. .NET API validates JWT against Microsoft JWKS
         ↓
8. UserResolutionMiddleware extracts 'oid' claim
         ↓
9. Query: SELECT * FROM users WHERE entra_oid = '...'
         ↓
10. If found → update last_login, return user
    If not found → INSERT new user (auto-provision)
         ↓
11. Attach CurrentUserContext to request
         ↓
12. Authorization policies check app_role claim
         ↓
13. Controller executes with DB user context
```

## API Endpoints

### Authentication
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/auth/login` | Public | Initiate Microsoft login |
| GET | `/api/auth/logout` | Public | Sign out |
| GET | `/api/auth/me` | Bearer | Get current user profile |

### Projects
| Method | Endpoint | Policy | Description |
|--------|----------|--------|-------------|
| GET | `/api/projects` | Any user | Get my projects (or all if admin) |
| GET | `/api/projects/{id}` | Any user | Get single project |
| POST | `/api/projects` | NotGuest | Create project |
| PUT | `/api/projects/{id}` | NotGuest | Update project |
| DELETE | `/api/projects/{id}` | NotGuest | Delete project |

### Admin (AdminOnly policy)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/admin/users` | List all users |
| PUT | `/api/admin/users/{id}/role` | Change user role |
| PUT | `/api/admin/users/{id}/active` | Activate/deactivate user |
| GET | `/api/admin/dashboard` | Get system statistics |

## Database Schema

```sql
-- users: Bridge between Entra ID and your app
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entra_oid VARCHAR(36) UNIQUE NOT NULL,  -- Microsoft immutable ID
    entra_tid VARCHAR(36),
    email VARCHAR(255),
    display_name VARCHAR(255),
    app_role VARCHAR(50) DEFAULT 'user',      -- admin, editor, user, guest
    is_guest BOOLEAN DEFAULT FALSE,
    is_active BOOLEAN DEFAULT TRUE,
    department VARCHAR(100),
    preferences JSONB DEFAULT '{}',
    last_login_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT NOW()
);

-- projects: Example business entity
CREATE TABLE projects (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    title VARCHAR(255) NOT NULL,
    description TEXT,
    status VARCHAR(50) DEFAULT 'active',
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- audit_logs: Security trail
CREATE TABLE audit_logs (
    id SERIAL PRIMARY KEY,
    user_id UUID REFERENCES users(id),
    action VARCHAR(100) NOT NULL,
    details JSONB,
    ip_address INET,
    created_at TIMESTAMP DEFAULT NOW()
);
```

## Role-Based Access Matrix

| Role | Projects | Admin Panel | Notes |
|------|----------|-------------|-------|
| **Admin** | Full CRUD on all | Full access | Can manage users, roles, activation |
| **Editor** | Full CRUD on own | None | Can create/edit/delete own projects |
| **User** | Full CRUD on own | None | Standard employee access |
| **Guest** | Read-only | None | External users, invited to tenant |

## Deploying to Azure App Service

### 1. Create App Service
```bash
az webapp create   --resource-group myResourceGroup   --plan myAppServicePlan   --name entraid-poc-api   --runtime "DOTNET|8.0"
```

### 2. Update App Registration
- Add redirect URI: `https://entraid-poc-api.azurewebsites.net/signin-oidc`
- Add CORS origin: `https://your-frontend.azurestaticapps.net`

### 3. Configure Connection String
```bash
az webapp config connection-string set   --name entraid-poc-api   --resource-group myResourceGroup   --settings DefaultConnection="Host=...;Database=...;Username=...;Password=..."
```

### 4. Deploy
```bash
dotnet publish -c Release
# Deploy publish folder via Azure CLI, GitHub Actions, or VS Publish
```

### 5. Update Frontend `.env`
```
VITE_API_BASE_URL=https://entraid-poc-api.azurewebsites.net
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| `invalid_client` | Verify ClientId and ClientSecret match Azure app registration |
| `invalid_grant` | Check redirect URI exactly matches (including trailing slash) |
| Token missing `roles` claim | Ensure admin consent granted for API permissions |
| CORS errors | Add frontend origin to `Cors:AllowedOrigins` in appsettings |
| Database connection fails | Verify PostgreSQL is running and connection string is correct |
| User not auto-provisioning | Check that `oid` claim exists in token (use jwt.ms to inspect) |

## License

MIT — Free for personal and commercial use.
