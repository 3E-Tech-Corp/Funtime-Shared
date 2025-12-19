# Funtime Pickleball - Shared Platform

## Architecture Overview

This repo contains shared infrastructure for all pickleball.* sites:
- Funtime.Identity.Api (.NET Core) - auth + payments
- funtime-ui (React) - shared components published as @funtime/ui

## Database: FuntimeIdentity (MS SQL Server)
- Users table with INT UserId (identity)
- UserProfiles (shared fields: name, avatar, phone, city, skill level)
- UserSites (tracks which sites each user has joined)
- PaymentCustomers, PaymentMethods, Payments (Stripe integration)
- Subscriptions

## Sites using this (separate repos)
- pickleball.community
- pickleball.college
- pickleball.date
- pickleball.jobs

Each site has its own database but references UserId from this shared DB.

## Key Decisions
- Integer UserIds for backward compatibility
- JWT auth with sites[] claim
- Stripe for payments
- funtime-ui published as npm package
