import { useState } from 'react';
import { Link } from 'react-router-dom';
import { authApi } from '../utils/api';
import { redirectWithToken, getSiteDisplayName, getSiteKey, getReturnTo } from '../utils/redirect';

type AuthMode = 'email' | 'phone';

export function RegisterPage() {
  const [mode, setMode] = useState<AuthMode>('email');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Email registration state
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');

  // Phone registration state
  const [phoneNumber, setPhoneNumber] = useState('');
  const [otpCode, setOtpCode] = useState('');
  const [otpSent, setOtpSent] = useState(false);

  const siteKey = getSiteKey();
  const siteName = getSiteDisplayName(siteKey);
  const returnTo = getReturnTo();

  const handleEmailRegister = async (e: React.FormEvent) => {
    e.preventDefault();

    if (password !== confirmPassword) {
      setError('Passwords do not match');
      return;
    }

    if (password.length < 8) {
      setError('Password must be at least 8 characters');
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      const response = await authApi.register(email, password);
      if (response.success && response.token) {
        redirectWithToken(response.token);
      } else {
        setError(response.message || 'Registration failed');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Registration failed');
    } finally {
      setIsLoading(false);
    }
  };

  const handleSendOtp = async () => {
    setIsLoading(true);
    setError(null);

    try {
      const response = await authApi.sendOtp(phoneNumber);
      if (response.success) {
        setOtpSent(true);
      } else {
        setError(response.message || 'Failed to send code');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to send code');
    } finally {
      setIsLoading(false);
    }
  };

  const handlePhoneRegister = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setError(null);

    try {
      const response = await authApi.verifyOtp(phoneNumber, otpCode);
      if (response.success && response.token) {
        redirectWithToken(response.token);
      } else {
        setError(response.message || 'Invalid code');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Verification failed');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-100 px-4">
      <div className="max-w-md w-full">
        {/* Logo and Site Info */}
        <div className="text-center mb-8">
          <div className="inline-flex items-center justify-center w-16 h-16 bg-green-600 rounded-full mb-4">
            <span className="text-white text-2xl font-bold">F</span>
          </div>
          <h1 className="text-2xl font-bold text-gray-900">Create your account</h1>
          <p className="text-sm text-gray-500 mt-1">
            Join {siteName}
            {returnTo && <span> to continue to {returnTo}</span>}
          </p>
        </div>

        {/* Auth Card */}
        <div className="bg-white rounded-lg shadow-md p-6">
          {/* Mode Toggle */}
          <div className="flex border-b border-gray-200 mb-6">
            <button
              onClick={() => setMode('email')}
              className={`flex-1 py-2 text-sm font-medium border-b-2 transition-colors ${
                mode === 'email'
                  ? 'border-green-600 text-green-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700'
              }`}
            >
              Email
            </button>
            <button
              onClick={() => setMode('phone')}
              className={`flex-1 py-2 text-sm font-medium border-b-2 transition-colors ${
                mode === 'phone'
                  ? 'border-green-600 text-green-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700'
              }`}
            >
              Phone
            </button>
          </div>

          {/* Error Message */}
          {error && (
            <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
              {error}
            </div>
          )}

          {/* Email Registration Form */}
          {mode === 'email' && (
            <form onSubmit={handleEmailRegister} className="space-y-4">
              <div>
                <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-1">
                  Email
                </label>
                <input
                  type="email"
                  id="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  required
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-green-500 focus:border-green-500"
                  placeholder="you@example.com"
                />
              </div>

              <div>
                <label htmlFor="password" className="block text-sm font-medium text-gray-700 mb-1">
                  Password
                </label>
                <input
                  type="password"
                  id="password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
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
                {isLoading ? 'Creating account...' : 'Create Account'}
              </button>
            </form>
          )}

          {/* Phone Registration Form */}
          {mode === 'phone' && (
            <form onSubmit={handlePhoneRegister} className="space-y-4">
              <div>
                <label htmlFor="phone" className="block text-sm font-medium text-gray-700 mb-1">
                  Phone Number
                </label>
                <div className="flex gap-2">
                  <input
                    type="tel"
                    id="phone"
                    value={phoneNumber}
                    onChange={(e) => setPhoneNumber(e.target.value)}
                    required
                    disabled={otpSent}
                    className="flex-1 px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-green-500 focus:border-green-500 disabled:bg-gray-100"
                    placeholder="+1 (555) 123-4567"
                  />
                  {!otpSent && (
                    <button
                      type="button"
                      onClick={handleSendOtp}
                      disabled={isLoading || !phoneNumber}
                      className="px-4 py-2 bg-gray-100 text-gray-700 font-medium rounded-lg hover:bg-gray-200 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                    >
                      Send Code
                    </button>
                  )}
                </div>
              </div>

              {otpSent && (
                <>
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
                      onClick={() => {
                        setOtpSent(false);
                        setOtpCode('');
                      }}
                      className="text-gray-500 hover:text-gray-700"
                    >
                      Change number
                    </button>
                    <button
                      type="button"
                      onClick={handleSendOtp}
                      disabled={isLoading}
                      className="text-green-600 hover:text-green-700"
                    >
                      Resend code
                    </button>
                  </div>

                  <button
                    type="submit"
                    disabled={isLoading || otpCode.length < 6}
                    className="w-full py-2 px-4 bg-green-600 text-white font-medium rounded-lg hover:bg-green-700 focus:ring-2 focus:ring-green-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                  >
                    {isLoading ? 'Verifying...' : 'Create Account'}
                  </button>
                </>
              )}
            </form>
          )}

          {/* Terms */}
          <p className="mt-4 text-xs text-gray-500 text-center">
            By creating an account, you agree to our{' '}
            <a href="#" className="text-green-600 hover:underline">Terms of Service</a>
            {' '}and{' '}
            <a href="#" className="text-green-600 hover:underline">Privacy Policy</a>
          </p>
        </div>

        {/* Login Link */}
        <p className="mt-6 text-center text-sm text-gray-600">
          Already have an account?{' '}
          <Link
            to={`/login${window.location.search}`}
            className="text-green-600 hover:text-green-700 font-medium"
          >
            Sign in
          </Link>
        </p>
      </div>
    </div>
  );
}
