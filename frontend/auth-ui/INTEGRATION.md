# Auth-UI Integration Guide

Shared authentication UI for the Funtime Pickleball platform. This UI serves multiple sites (pickleball.community, pickleball.college, pickleball.date, pickleball.jobs) with site-specific branding.

## Quick Start for External Sites

### 1. Link to Auth-UI

```html
<!-- Login link -->
<a href="https://auth.example.com/login?site=community&redirect=https://pickleball.community/auth/callback">
  Login
</a>

<!-- Register link -->
<a href="https://auth.example.com/register?site=community&redirect=https://pickleball.community/auth/callback">
  Sign Up
</a>
```

### 2. Handle the Callback

After successful auth, users are redirected to your callback URL with a token:

```
https://pickleball.community/auth/callback?token=eyJhbG...
```

Your callback handler should:
1. Extract the token from URL
2. Store it (session/cookie/localStorage)
3. Use it for API requests

```javascript
// Example callback handler
const urlParams = new URLSearchParams(window.location.search);
const token = urlParams.get('token');

if (token) {
  // Store token
  localStorage.setItem('auth_token', token);

  // Redirect to dashboard or home
  window.location.href = '/dashboard';
}
```

### 3. Make Authenticated API Requests

```javascript
fetch('https://api.example.com/user/profile', {
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  }
});
```

---

## URL Parameters

| Parameter | Description | Example |
|-----------|-------------|---------|
| `site` | Site key for branding | `?site=community` |
| `redirect` | Callback URL after auth | `?redirect=https://pickleball.community/auth/callback` |
| `returnTo` | Path to return to after callback | `?returnTo=/dashboard` |

**Full Example:**
```
/login?site=community&redirect=https://pickleball.community/auth/callback&returnTo=/dashboard
```

---

## Available Routes

| Route | Purpose | Public |
|-------|---------|--------|
| `/login` | Email/Phone login (password or OTP) | Yes |
| `/register` | New account creation | Yes |
| `/forgot-password` | Password reset + quick account creation | Yes |
| `/sites` | Site selection after login | No |
| `/admin` | Admin dashboard (SU role only) | No |
| `/terms-of-service` | Terms of Service display | Yes |
| `/privacy-policy` | Privacy Policy display | Yes |

---

## Authentication Flows

### Email + Password
```
1. User enters email + password
2. POST /auth/login { email, password }
3. Response: { token, user }
4. Redirect to callback with token
```

### Phone + OTP
```
1. User enters phone number
2. POST /auth/otp/send { phoneNumber }
3. SMS code sent
4. User enters 6-digit code
5. POST /auth/otp/verify { phoneNumber, code }
6. Response: { token, user }
7. Redirect to callback with token
```

### Phone + Password
```
1. User enters phone + password
2. POST /auth/login/phone { phoneNumber, password }
3. Response: { token, user }
4. Redirect to callback with token
```

### Password Reset
```
1. User enters email or phone
2. POST /auth/password-reset/send { email } or { phoneNumber }
3. Code sent via email/SMS
4. POST /auth/password-reset/verify { email/phoneNumber, code }
5. Response includes: { accountExists: true/false }
6a. If account exists: POST /auth/password-reset/complete { ..., newPassword }
6b. If no account: POST /auth/password-reset/register { ..., password } (creates new account)
```

---

## Runtime Configuration

The UI uses `/config.js` for runtime configuration:

```javascript
// /config.js (served at web root)
window.__FUNTIME_CONFIG__ = {
  API_URL: "https://api.funtime.com",
  STRIPE_PUBLISHABLE_KEY: "pk_live_..."
};
```

**For development:** Use `.env` file with `VITE_API_URL` and `VITE_STRIPE_PUBLISHABLE_KEY`

---

## API Endpoints

### Public Auth Endpoints

```
GET  /auth/sites                    - List public sites
POST /auth/login                    - Email login
POST /auth/login/phone              - Phone login with password
POST /auth/register                 - Email registration
POST /auth/otp/send                 - Send OTP to phone
POST /auth/otp/verify               - Verify OTP code
POST /auth/password-reset/send      - Request password reset
POST /auth/password-reset/verify    - Verify reset code
POST /auth/password-reset/complete  - Complete password reset
POST /auth/password-reset/register  - Quick register (from reset flow)
```

### Settings Endpoints (Public)

```
GET /settings/logo                  - Get main logo
GET /settings/terms-of-service      - Get Terms of Service
GET /settings/privacy-policy        - Get Privacy Policy
```

### Admin Endpoints (Require Bearer Token)

```
GET    /admin/stats                 - Dashboard statistics
GET    /admin/sites                 - List all sites
POST   /admin/sites                 - Create site
PUT    /admin/sites/{key}           - Update site
POST   /admin/sites/{key}/logo      - Upload site logo
DELETE /admin/sites/{key}/logo      - Delete site logo
GET    /admin/users                 - Search users
GET    /admin/users/{id}            - User details
PUT    /admin/users/{id}            - Update user
GET    /admin/payments              - Payment history
POST   /admin/payments/charge       - Create payment intent
```

---

## Site Branding

### Site Key Matching

Site keys are **case-insensitive**. The UI looks up the site in the database and displays:
- Main logo (shared across all sites)
- Site logo overlay (bottom-right of main logo)
- Title: `Pickleball.{SiteName}` or `Funtime Pickleball` (fallback)

### Logo Configuration

Upload via Admin Dashboard (`/admin` > Settings tab):
1. **Main Logo** - Shared branding, appears on all auth pages
2. **Site Logo** - Per-site branding, overlays the main logo

---

## Allowed Redirect Domains

For security, only these domains can receive auth callbacks:

```
pickleball.community
pickleball.college
pickleball.date
pickleball.jobs
localhost
127.0.0.1
```

To add domains, update `frontend/auth-ui/src/utils/redirect.ts`.

---

## Token Format

The auth token is a JWT containing:
- User ID
- Email/Phone
- System Role (e.g., `SU` for super user)
- Expiration

**Storage:** `localStorage.auth_token`

**Header format:** `Authorization: Bearer {token}`

---

## Building & Deployment

### Development
```bash
cd frontend/auth-ui
npm install
npm run dev
```

### Production Build
```bash
npm run build
```

Output in `dist/` folder. Deploy to web server and configure `/config.js`.

### Environment Variables

| Variable | Purpose |
|----------|---------|
| `VITE_API_URL` | API base URL (dev fallback) |
| `VITE_STRIPE_PUBLISHABLE_KEY` | Stripe public key (dev fallback) |

---

## Integration Checklist

```
[ ] Register your site in Admin Dashboard (/admin > Sites)
[ ] Upload site logo for branding
[ ] Add login/register links with ?site={key}&redirect={callback}
[ ] Implement callback handler to extract token
[ ] Store token and use for authenticated requests
[ ] (Optional) Add Terms of Service and Privacy Policy content
```

---

## Admin Dashboard Features

Access at `/admin` (requires SU system role):

| Tab | Features |
|-----|----------|
| **Overview** | User stats, revenue, subscriptions |
| **Sites** | Create/edit sites, upload logos |
| **Users** | Search, view details, manage users |
| **Payments** | Payment history, manual charges |
| **Notifications** | Email profiles, templates, queue |
| **Settings** | Main logo, Terms of Service, Privacy Policy |

---

## Tech Stack

- **React 19** with TypeScript
- **Vite** for building
- **Tailwind CSS** for styling
- **React Router** for navigation
- **Stripe** for payments
- **Lucide** for icons
