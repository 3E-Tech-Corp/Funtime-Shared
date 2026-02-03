# 🔐 Funtime-Shared — Analysis & TODO
**Last Updated:** 2026-02-03 | **Maintainer:** Synthia

## Current State
- **Repo:** 3E-Tech-Corp/Funtime-Shared
- **Live:** shared.funtimepb.com
- **Stack:** .NET 8 + React/TS + SQL Server + Stripe + SignalR
- **Status:** Stable, maintenance mode. Centralized auth for all pickleball sites.

## Purpose
Centralized identity service — login, registration, OTP, asset management, cross-site identity. All pickleball.* sites share authentication through this service.

## Sites Using This
- pickleball.community (active)
- pickleball.college (planned)
- pickleball.date (planned)
- pickleball.jobs (planned)
- pickleball.casino (planned)

## ✅ Working
- [x] Email/password authentication
- [x] Phone OTP via Twilio
- [x] OAuth providers
- [x] Account linking across auth types
- [x] JWT tokens with site claims
- [x] BCrypt password hashing ✅
- [x] Asset storage and serving
- [x] Stripe payments (subscriptions + one-time)
- [x] funtime-ui shared component library
- [x] SignalR push notifications

## 🟠 High
- [ ] **Merge security PR #6** — Review and merge pending security fixes
- [ ] **Secrets rotation** — Old secrets exposed in git history, need rotation
- [ ] **Rate limiting enforcement** — OTP: 5 attempts per 15 minutes (configurable, verify it's active)
- [ ] **Password cost factor audit** — BCrypt is used but verify the work factor is adequate (≥12)

## 🟡 Medium
- [ ] **funtime-ui package update** — Shared component library may need version bumps
- [ ] **Multi-site admin dashboard** — Admin view of users across all pickleball.* sites
- [ ] **Account deactivation flow** — Users should be able to delete their accounts (GDPR/privacy)
- [ ] **Refresh token rotation** — Current JWT flow may not have proper refresh token handling

## 🟢 Low
- [ ] **OAuth provider expansion** — Add WeChat login (important for Chinese user base)
- [ ] **2FA / TOTP** — Optional two-factor authentication for high-value accounts
- [ ] **API rate limiting** — General rate limiting beyond just OTP
