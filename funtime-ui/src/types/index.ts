// User types
export interface User {
  id: number;
  email?: string;
  phoneNumber?: string;
  isEmailVerified: boolean;
  isPhoneVerified: boolean;
  createdAt: string;
  lastLoginAt?: string;
}

export interface UserProfile {
  id: number;
  userId: number;
  firstName?: string;
  lastName?: string;
  displayName?: string;
  avatarUrl?: string;
  city?: string;
  state?: string;
  country?: string;
  skillLevel?: number;
  bio?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface UserSite {
  id: number;
  siteKey: string;
  joinedAt: string;
  isActive: boolean;
  role: string;
}

export interface UserFull extends User {
  profile?: UserProfile;
  sites: UserSite[];
  siteKeys: string[];
}

// Auth types
export interface AuthResponse {
  success: boolean;
  token?: string;
  message?: string;
  user?: User;
}

export interface ValidateTokenResponse {
  valid: boolean;
  userId?: number;
  email?: string;
  phoneNumber?: string;
  sites?: string[];
  message?: string;
}

// Payment types
export interface PaymentMethod {
  id: number;
  stripePaymentMethodId: string;
  type: string;
  cardBrand?: string;
  cardLast4?: string;
  cardExpMonth?: number;
  cardExpYear?: number;
  isDefault: boolean;
  createdAt: string;
}

export interface Payment {
  id: number;
  stripePaymentId: string;
  amountCents: number;
  amountDollars: number;
  currency: string;
  status: string;
  description?: string;
  siteKey?: string;
  createdAt: string;
}

export interface Subscription {
  id: number;
  stripeSubscriptionId: string;
  stripePriceId?: string;
  status: string;
  planName?: string;
  siteKey?: string;
  amountCents?: number;
  amountDollars?: number;
  currency: string;
  interval?: string;
  currentPeriodStart?: string;
  currentPeriodEnd?: string;
  canceledAt?: string;
  cancelAt?: string;
  createdAt: string;
}

// API Response types
export interface ApiResponse {
  success: boolean;
  message?: string;
}

export interface SetupIntentResponse {
  clientSecret: string;
}

export interface PaymentIntentResponse {
  clientSecret: string;
  paymentIntentId: string;
  amountCents: number;
  status: string;
}

// Site keys
export type SiteKey = 'community' | 'college' | 'date' | 'jobs';
