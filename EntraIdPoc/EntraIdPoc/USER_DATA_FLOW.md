# Question 3: How User Details Flow from Azure to Your Database

## The Complete Data Flow (Step-by-Step)

### Step 1: User Authenticates with Microsoft
```
User opens app → clicks "Sign in with Microsoft"
         ↓
Browser redirects to:
https://login.microsoftonline.com/{tenant}/oauth2/v2.0/authorize
         ↓
Microsoft shows login page (password, MFA, etc.)
```

### Step 2: Microsoft Issues a JWT Token
After successful authentication, Microsoft creates a **JSON Web Token (JWT)** containing **claims** about the user:

```json
{
  "oid": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",     ← Immutable user ID (KEY!)
  "tid": "11111111-2222-3333-4444-555555555555",     ← Tenant ID
  "preferred_username": "john.doe@company.com",        ← Email
  "name": "John Doe",                                   ← Display name
  "given_name": "John",
  "surname": "Doe",
  "roles": ["App.Admin"],                               ← App roles (if configured)
  "idtyp": "user",                                      ← "guest" for external users
  "iat": 1719999999,                                    ← Issued at
  "exp": 1720003599                                     ← Expires at
}
```

**Critical claim: `oid` (Object ID)**
- This is an immutable UUID assigned by Microsoft when the user is created in Entra ID
- It never changes, even if the user changes their email or name
- This is what you store in your database as `entra_oid`

### Step 3: Token Travels to Your API
```
Frontend (React) receives token from Microsoft
         ↓
Sends API request:
GET /api/auth/me
Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIs...
         ↓
Your .NET API receives the request
```

### Step 4: API Validates the Token
```csharp
// In Program.cs: Microsoft.Identity.Web handles this automatically
builder.Services.AddAuthentication()
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
```

What happens under the hood:
1. Extract `kid` (key ID) from JWT header
2. Fetch Microsoft's public key from `https://login.microsoftonline.com/{tenant}/discovery/v2.0/keys`
3. Verify token signature using RSA public key
4. Check `iss` (issuer) matches your tenant
5. Check `aud` (audience) matches your Client ID
6. Check `exp` (expiry) hasn't passed

### Step 5: Extract Claims and Resolve User
```csharp
// UserResolutionMiddleware.cs
public async Task InvokeAsync(HttpContext context, CurrentUserContext currentUser, ...)
{
    // Extract claims from validated token
    var oid = context.User.FindFirst("oid")?.Value;        // "a1b2c3d4-..."
    var email = context.User.FindFirst("preferred_username")?.Value;
    var name = context.User.FindFirst("name")?.Value;
    var roles = context.User.FindAll("roles").Select(c => c.Value);

    // Call service to bridge Entra → DB
    var dbUser = await userService.ResolveUserAsync(context.User);

    // Attach to request context for controllers
    currentUser.DbId = dbUser.Id;           // Your DB UUID
    currentUser.EntraOid = dbUser.EntraOid;
    currentUser.AppRole = dbUser.AppRole;   // "admin", "user", etc.
}
```

### Step 6: Auto-Provisioning (First Login)
```csharp
// UserResolutionService.cs
public async Task<User> ResolveUserAsync(ClaimsPrincipal entraUser, ...)
{
    // 1. Check if user exists in YOUR database
    var user = await _db.Users
        .AsNoTracking()
        .FirstOrDefaultAsync(u => u.EntraOid == oid);

    if (user != null)
    {
        // RETURNING USER: Update last login, sync any changed data
        await _db.Database.ExecuteSqlAsync($"""
            UPDATE users 
            SET last_login_at = NOW(),
                email = COALESCE({email}, email),
                display_name = COALESCE({displayName}, display_name),
                updated_at = NOW()
            WHERE id = {user.Id}
        """);
        return user;
    }

    // FIRST TIME USER: Create new record in YOUR database
    var newId = Guid.NewGuid();
    var initialRole = DeriveRoleFromEntra(entraUser, isGuest);

    await _db.Database.ExecuteSqlAsync($"""
        INSERT INTO users (id, entra_oid, entra_tid, email, display_name, 
                          app_role, is_guest, is_active, last_login_at, created_at)
        VALUES ({newId}, {oid}, {tid}, {email}, {displayName},
                {initialRole}, {isGuest}, {true}, {DateTime.UtcNow}, {DateTime.UtcNow})
    """);

    return await _db.Users.AsNoTracking().FirstAsync(u => u.Id == newId);
}
```

### Step 7: Data Lives in Your Database
After first login, the user exists in **both** systems:

| System | Data | Example |
|--------|------|---------|
| **Microsoft Entra ID** | Identity truth | Password, MFA, groups, org chart |
| **Your PostgreSQL DB** | Application truth | App role, preferences, projects, audit logs |

```sql
-- Your users table (bridged by entra_oid)
SELECT * FROM users WHERE entra_oid = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890';

-- Result:
-- id: 550e8400-e29b-41d4-a716-446655440000  (YOUR UUID)
-- entra_oid: a1b2c3d4-e5f6-7890-abcd-ef1234567890  (Microsoft's UUID)
-- email: john.doe@company.com
-- display_name: John Doe
-- app_role: admin
-- is_guest: false
-- department: Engineering
-- preferences: {"theme":"dark","notifications":true}
```

### Step 8: All Business Data Links to YOUR User ID
```sql
-- Projects reference users.id (NOT entra_oid!)
SELECT p.title, p.status, u.display_name as owner
FROM projects p
JOIN users u ON p.owner_id = u.id
WHERE u.entra_oid = 'a1b2c3d4-...';
```

**Why `users.id` and not `users.entra_oid`?**
- If you switch from Entra ID to Okta/Auth0 later, only the login flow changes
- Your app data stays intact — just update `entra_oid` column values
- `entra_oid` is only used during login to find the user

---

## What Data Comes from Where?

| Data Field | Source | Writable in Your App? | Why? |
|------------|--------|------------------------|------|
| `entra_oid` | Microsoft Entra | ❌ No | Immutable identifier |
| `email` | Microsoft Entra | ⚠️ Sync only | Source of truth is directory |
| `display_name` | Microsoft Entra | ⚠️ Sync only | HR system manages names |
| `app_role` | Derived from Entra | ✅ Yes | App-specific permission |
| `department` | Your DB | ✅ Yes | Not in Entra (or outdated) |
| `preferences` | Your DB | ✅ Yes | App-specific settings |
| `projects` | Your DB | ✅ Yes | Business data |
| `last_login_at` | Your DB | ✅ Auto | Track engagement |

---

## Visual Summary

```
┌─────────────────┐         ┌─────────────────┐         ┌─────────────────┐
│   Microsoft     │         │   Your .NET API │         │   PostgreSQL    │
│   Entra ID      │ ──────► │   (Middleware)  │ ──────► │   Database      │
│                 │  JWT    │                 │  SQL    │                 │
│  - Passwords    │ Token   │  - Validate JWT │         │  - users        │
│  - MFA          │         │  - Extract oid  │         │  - projects     │
│  - Directory    │         │  - Find/Create  │         │  - audit_logs   │
│  - Org chart    │         │    DB user      │         │                 │
└─────────────────┘         └─────────────────┘         └─────────────────┘
       │                             │                             │
       │    Identity Truth           │    Bridge Layer             │    App Truth
       │    (Who you are)            │    (Link by oid)            │    (What you do)
       │                             │                             │
       ▼                             ▼                             ▼
  john.doe@company              entra_oid =                users.id = 550e...
  Password + MFA                a1b2c3d4...               app_role = admin
                                                          department = Eng
                                                          preferences = {...}
```
