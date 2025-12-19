import { useState, useCallback, useEffect } from 'react';
import type { PaymentMethod, Payment, Subscription } from '../types';
import { getFuntimeClient } from '../api/client';

export interface UsePaymentsReturn {
  paymentMethods: PaymentMethod[];
  payments: Payment[];
  subscriptions: Subscription[];
  isLoading: boolean;
  error: string | null;
  createSetupIntent: () => Promise<string>;
  attachPaymentMethod: (stripePaymentMethodId: string, setAsDefault?: boolean) => Promise<PaymentMethod>;
  deletePaymentMethod: (stripePaymentMethodId: string) => Promise<void>;
  setDefaultPaymentMethod: (stripePaymentMethodId: string) => Promise<void>;
  createPayment: (amountCents: number, description?: string, siteKey?: string) => Promise<{ clientSecret: string; paymentIntentId: string }>;
  createSubscription: (stripePriceId: string, siteKey?: string) => Promise<Subscription>;
  cancelSubscription: (subscriptionId: number, cancelAtPeriodEnd?: boolean) => Promise<Subscription>;
  refresh: () => Promise<void>;
}

export function usePayments(): UsePaymentsReturn {
  const [paymentMethods, setPaymentMethods] = useState<PaymentMethod[]>([]);
  const [payments, setPayments] = useState<Payment[]>([]);
  const [subscriptions, setSubscriptions] = useState<Subscription[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const refresh = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const client = getFuntimeClient();
      const [methods, paymentHistory, subs] = await Promise.all([
        client.getPaymentMethods(),
        client.getPaymentHistory(),
        client.getSubscriptions(),
      ]);
      setPaymentMethods(methods);
      setPayments(paymentHistory);
      setSubscriptions(subs);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load payment data');
    } finally {
      setIsLoading(false);
    }
  }, []);

  const createSetupIntent = useCallback(async (): Promise<string> => {
    const client = getFuntimeClient();
    const response = await client.createSetupIntent();
    return response.clientSecret;
  }, []);

  const attachPaymentMethod = useCallback(async (stripePaymentMethodId: string, setAsDefault = false): Promise<PaymentMethod> => {
    const client = getFuntimeClient();
    const method = await client.attachPaymentMethod(stripePaymentMethodId, setAsDefault);
    setPaymentMethods(prev => {
      if (setAsDefault) {
        return [...prev.map(m => ({ ...m, isDefault: false })), method];
      }
      return [...prev, method];
    });
    return method;
  }, []);

  const deletePaymentMethod = useCallback(async (stripePaymentMethodId: string): Promise<void> => {
    const client = getFuntimeClient();
    await client.deletePaymentMethod(stripePaymentMethodId);
    setPaymentMethods(prev => prev.filter(m => m.stripePaymentMethodId !== stripePaymentMethodId));
  }, []);

  const setDefaultPaymentMethod = useCallback(async (stripePaymentMethodId: string): Promise<void> => {
    const client = getFuntimeClient();
    await client.setDefaultPaymentMethod(stripePaymentMethodId);
    setPaymentMethods(prev => prev.map(m => ({
      ...m,
      isDefault: m.stripePaymentMethodId === stripePaymentMethodId,
    })));
  }, []);

  const createPayment = useCallback(async (amountCents: number, description?: string, siteKey?: string) => {
    const client = getFuntimeClient();
    const response = await client.createPayment(amountCents, description, siteKey);
    return { clientSecret: response.clientSecret, paymentIntentId: response.paymentIntentId };
  }, []);

  const createSubscription = useCallback(async (stripePriceId: string, siteKey?: string): Promise<Subscription> => {
    const client = getFuntimeClient();
    const sub = await client.createSubscription(stripePriceId, siteKey);
    setSubscriptions(prev => [...prev, sub]);
    return sub;
  }, []);

  const cancelSubscription = useCallback(async (subscriptionId: number, cancelAtPeriodEnd = true): Promise<Subscription> => {
    const client = getFuntimeClient();
    const sub = await client.cancelSubscription(subscriptionId, cancelAtPeriodEnd);
    setSubscriptions(prev => prev.map(s => s.id === subscriptionId ? sub : s));
    return sub;
  }, []);

  useEffect(() => {
    refresh();
  }, [refresh]);

  return {
    paymentMethods,
    payments,
    subscriptions,
    isLoading,
    error,
    createSetupIntent,
    attachPaymentMethod,
    deletePaymentMethod,
    setDefaultPaymentMethod,
    createPayment,
    createSubscription,
    cancelSubscription,
    refresh,
  };
}
