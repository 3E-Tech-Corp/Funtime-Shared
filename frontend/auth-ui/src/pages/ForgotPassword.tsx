import { useState } from 'react';
import { Link } from 'react-router-dom';
import { authApi } from '../utils/api';
import { getSiteDisplayName, getSiteKey } from '../utils/redirect';

type Step = 'phone' | 'code' | 'password' | 'success';

export function ForgotPasswordPage() {
  const [step, setStep] = useState<Step>('phone');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [phoneNumber, setPhoneNumber] = useState('');
  const [otpCode, setOtpCode] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');

  const siteKey = getSiteKey();
  const siteName = getSiteDisplayName(siteKey);

  const handleSendCode = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setError(null);

    try {
      const response = await authApi.requestPasswordReset(phoneNumber);
      if (response.success) {
        setStep('code');
      } else {
        setError(response.message || 'Failed to send code');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to send code');
    } finally {
      setIsLoading(false);
    }
  };

  const handleVerifyCode = async (e: React.FormEvent) => {
    e.preventDefault();
    // For now, just move to password step
    // In production, you might want to verify the code first
    setStep('password');
  };

  const handleResetPassword = async (e: React.FormEvent) => {
    e.preventDefault();

    if (newPassword !== confirmPassword) {
      setError('Passwords do not match');
      return;
    }

    if (newPassword.length < 8) {
      setError('Password must be at least 8 characters');
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      const response = await authApi.resetPassword(phoneNumber, otpCode, newPassword);
      if (response.success) {
        setStep('success');
      } else {
        setError(response.message || 'Failed to reset password');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to reset password');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-100 px-4">
      <div className="max-w-md w-full">
        {/* Logo */}
        <div className="text-center mb-8">
          <div className="inline-flex items-center justify-center w-16 h-16 bg-green-600 rounded-full mb-4">
            <span className="text-white text-2xl font-bold">F</span>
          </div>
          <h1 className="text-2xl font-bold text-gray-900">
            {step === 'success' ? 'Password Reset!' : 'Reset your password'}
          </h1>
          {step !== 'success' && (
            <p className="text-sm text-gray-500 mt-1">{siteName}</p>
          )}
        </div>

        {/* Card */}
        <div className="bg-white rounded-lg shadow-md p-6">
          {/* Error Message */}
          {error && (
            <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
              {error}
            </div>
          )}

          {/* Step 1: Enter Phone Number */}
          {step === 'phone' && (
            <form onSubmit={handleSendCode} className="space-y-4">
              <p className="text-sm text-gray-600 mb-4">
                Enter your phone number and we'll send you a verification code to reset your password.
              </p>

              <div>
                <label htmlFor="phone" className="block text-sm font-medium text-gray-700 mb-1">
                  Phone Number
                </label>
                <input
                  type="tel"
                  id="phone"
                  value={phoneNumber}
                  onChange={(e) => setPhoneNumber(e.target.value)}
                  required
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-green-500 focus:border-green-500"
                  placeholder="+1 (555) 123-4567"
                />
              </div>

              <button
                type="submit"
                disabled={isLoading || !phoneNumber}
                className="w-full py-2 px-4 bg-green-600 text-white font-medium rounded-lg hover:bg-green-700 focus:ring-2 focus:ring-green-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              >
                {isLoading ? 'Sending...' : 'Send Code'}
              </button>
            </form>
          )}

          {/* Step 2: Enter Verification Code */}
          {step === 'code' && (
            <form onSubmit={handleVerifyCode} className="space-y-4">
              <p className="text-sm text-gray-600 mb-4">
                We sent a code to <strong>{phoneNumber}</strong>. Enter it below.
              </p>

              <div>
                <label htmlFor="otp" className="block text-sm font-medium text-gray-700 mb-1">
                  Verification Code
                </label>
                <input
                  type="text"
                  id="otp"
                  value={otpCode}
                  onChange={(e) => setOtpCode(e.target.value)}
                  required
                  maxLength={6}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-green-500 focus:border-green-500 text-center text-lg tracking-widest"
                  placeholder="000000"
                />
              </div>

              <div className="flex items-center justify-between text-sm">
                <button
                  type="button"
                  onClick={() => setStep('phone')}
                  className="text-gray-500 hover:text-gray-700"
                >
                  Change number
                </button>
                <button
                  type="button"
                  onClick={handleSendCode}
                  disabled={isLoading}
                  className="text-green-600 hover:text-green-700"
                >
                  Resend code
                </button>
              </div>

              <button
                type="submit"
                disabled={otpCode.length < 6}
                className="w-full py-2 px-4 bg-green-600 text-white font-medium rounded-lg hover:bg-green-700 focus:ring-2 focus:ring-green-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              >
                Verify Code
              </button>
            </form>
          )}

          {/* Step 3: Enter New Password */}
          {step === 'password' && (
            <form onSubmit={handleResetPassword} className="space-y-4">
              <p className="text-sm text-gray-600 mb-4">
                Enter your new password below.
              </p>

              <div>
                <label htmlFor="newPassword" className="block text-sm font-medium text-gray-700 mb-1">
                  New Password
                </label>
                <input
                  type="password"
                  id="newPassword"
                  value={newPassword}
                  onChange={(e) => setNewPassword(e.target.value)}
                  required
                  minLength={8}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-green-500 focus:border-green-500"
                  placeholder="••••••••"
                />
                <p className="mt-1 text-xs text-gray-500">Must be at least 8 characters</p>
              </div>

              <div>
                <label htmlFor="confirmPassword" className="block text-sm font-medium text-gray-700 mb-1">
                  Confirm Password
                </label>
                <input
                  type="password"
                  id="confirmPassword"
                  value={confirmPassword}
                  onChange={(e) => setConfirmPassword(e.target.value)}
                  required
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-green-500 focus:border-green-500"
                  placeholder="••••••••"
                />
              </div>

              <button
                type="submit"
                disabled={isLoading}
                className="w-full py-2 px-4 bg-green-600 text-white font-medium rounded-lg hover:bg-green-700 focus:ring-2 focus:ring-green-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              >
                {isLoading ? 'Resetting...' : 'Reset Password'}
              </button>
            </form>
          )}

          {/* Step 4: Success */}
          {step === 'success' && (
            <div className="text-center">
              <div className="inline-flex items-center justify-center w-16 h-16 bg-green-100 rounded-full mb-4">
                <svg className="w-8 h-8 text-green-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                </svg>
              </div>
              <p className="text-gray-600 mb-6">
                Your password has been reset successfully. You can now sign in with your new password.
              </p>
              <Link
                to={`/login${window.location.search}`}
                className="inline-block w-full py-2 px-4 bg-green-600 text-white font-medium rounded-lg hover:bg-green-700 focus:ring-2 focus:ring-green-500 focus:ring-offset-2 transition-colors text-center"
              >
                Sign In
              </Link>
            </div>
          )}
        </div>

        {/* Back to Login Link */}
        {step !== 'success' && (
          <p className="mt-6 text-center text-sm text-gray-600">
            Remember your password?{' '}
            <Link
              to={`/login${window.location.search}`}
              className="text-green-600 hover:text-green-700 font-medium"
            >
              Sign in
            </Link>
          </p>
        )}
      </div>
    </div>
  );
}
