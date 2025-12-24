import { useState } from 'react';
import { Users, Globe, CreditCard, LogOut, Search, ChevronRight } from 'lucide-react';

type Tab = 'sites' | 'users' | 'payments';

// Available sites for configuration
const availableSites = [
  { key: 'pickleball.community', name: 'Pickleball Community', status: 'active' },
  { key: 'pickleball.college', name: 'Pickleball College', status: 'active' },
  { key: 'pickleball.date', name: 'Pickleball Date', status: 'active' },
  { key: 'pickleball.jobs', name: 'Pickleball Jobs', status: 'active' },
];

export function AdminDashboardPage() {
  const [activeTab, setActiveTab] = useState<Tab>('sites');
  const [userSearch, setUserSearch] = useState('');

  const handleLogout = () => {
    localStorage.removeItem('auth_token');
    window.location.href = '/login';
  };

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-white shadow-sm border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-4 py-4 flex items-center justify-between">
          <div>
            <h1 className="text-xl font-bold text-gray-900">System Admin</h1>
            <p className="text-sm text-gray-500">Funtime Pickleball Management</p>
          </div>
          <button
            onClick={handleLogout}
            className="inline-flex items-center gap-2 px-4 py-2 text-gray-600 hover:text-gray-900 hover:bg-gray-100 rounded-lg transition-colors"
          >
            <LogOut className="w-4 h-4" />
            Sign out
          </button>
        </div>
      </header>

      <div className="max-w-7xl mx-auto px-4 py-8">
        {/* Tabs */}
        <div className="flex gap-2 mb-8">
          <button
            onClick={() => setActiveTab('sites')}
            className={`flex items-center gap-2 px-4 py-2 rounded-lg font-medium transition-colors ${
              activeTab === 'sites'
                ? 'bg-primary-500 text-white'
                : 'bg-white text-gray-600 hover:bg-gray-100'
            }`}
          >
            <Globe className="w-4 h-4" />
            Sites
          </button>
          <button
            onClick={() => setActiveTab('users')}
            className={`flex items-center gap-2 px-4 py-2 rounded-lg font-medium transition-colors ${
              activeTab === 'users'
                ? 'bg-primary-500 text-white'
                : 'bg-white text-gray-600 hover:bg-gray-100'
            }`}
          >
            <Users className="w-4 h-4" />
            Users
          </button>
          <button
            onClick={() => setActiveTab('payments')}
            className={`flex items-center gap-2 px-4 py-2 rounded-lg font-medium transition-colors ${
              activeTab === 'payments'
                ? 'bg-primary-500 text-white'
                : 'bg-white text-gray-600 hover:bg-gray-100'
            }`}
          >
            <CreditCard className="w-4 h-4" />
            Payments
          </button>
        </div>

        {/* Sites Tab */}
        {activeTab === 'sites' && (
          <div className="bg-white rounded-xl shadow-sm border border-gray-200">
            <div className="p-6 border-b border-gray-200">
              <h2 className="text-lg font-semibold text-gray-900">Available Sites</h2>
              <p className="text-sm text-gray-500 mt-1">Configure and manage Funtime Pickleball sites</p>
            </div>
            <div className="divide-y divide-gray-200">
              {availableSites.map((site) => (
                <div key={site.key} className="p-4 flex items-center justify-between hover:bg-gray-50">
                  <div>
                    <h3 className="font-medium text-gray-900">{site.name}</h3>
                    <p className="text-sm text-gray-500">{site.key}</p>
                  </div>
                  <div className="flex items-center gap-4">
                    <span className={`px-2 py-1 text-xs font-medium rounded-full ${
                      site.status === 'active'
                        ? 'bg-green-100 text-green-700'
                        : 'bg-gray-100 text-gray-600'
                    }`}>
                      {site.status}
                    </span>
                    <button className="text-gray-400 hover:text-gray-600">
                      <ChevronRight className="w-5 h-5" />
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Users Tab */}
        {activeTab === 'users' && (
          <div className="bg-white rounded-xl shadow-sm border border-gray-200">
            <div className="p-6 border-b border-gray-200">
              <h2 className="text-lg font-semibold text-gray-900">User Management</h2>
              <p className="text-sm text-gray-500 mt-1">Search and manage user accounts</p>
              <div className="mt-4 relative">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-gray-400" />
                <input
                  type="text"
                  placeholder="Search by email or phone..."
                  value={userSearch}
                  onChange={(e) => setUserSearch(e.target.value)}
                  className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                />
              </div>
            </div>
            <div className="p-8 text-center text-gray-500">
              <Users className="w-12 h-12 mx-auto mb-4 text-gray-300" />
              <p>Enter a search term to find users</p>
              <p className="text-sm mt-1">Search by email, phone number, or user ID</p>
            </div>
          </div>
        )}

        {/* Payments Tab */}
        {activeTab === 'payments' && (
          <div className="bg-white rounded-xl shadow-sm border border-gray-200">
            <div className="p-6 border-b border-gray-200">
              <h2 className="text-lg font-semibold text-gray-900">Payment History</h2>
              <p className="text-sm text-gray-500 mt-1">View subscription and payment records</p>
            </div>
            <div className="p-8 text-center text-gray-500">
              <CreditCard className="w-12 h-12 mx-auto mb-4 text-gray-300" />
              <p>Payment tracking coming soon</p>
              <p className="text-sm mt-1">Subscription management will be available in a future update</p>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
