import { useState, useEffect } from 'react';
import { Loader2, ArrowLeft } from 'lucide-react';
import { settingsApi } from '../utils/api';

export function TermsOfServicePage() {
  const [content, setContent] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadContent();
  }, []);

  const loadContent = async () => {
    try {
      const response = await settingsApi.getTermsOfService();
      setContent(response.content || '');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load Terms of Service');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="max-w-4xl mx-auto px-4 py-8">
        <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-8">
          <div className="flex items-center gap-4 mb-6">
            <button
              onClick={() => window.close()}
              className="text-gray-500 hover:text-gray-700"
              title="Close"
            >
              <ArrowLeft className="w-5 h-5" />
            </button>
            <h1 className="text-2xl font-bold text-gray-900">Terms of Service</h1>
          </div>

          {isLoading ? (
            <div className="flex items-center justify-center py-12">
              <Loader2 className="w-8 h-8 animate-spin text-primary-500" />
            </div>
          ) : error ? (
            <div className="p-4 bg-red-50 border border-red-200 rounded-lg text-red-700">
              {error}
            </div>
          ) : content ? (
            <div
              className="prose prose-gray max-w-none"
              dangerouslySetInnerHTML={{ __html: content }}
            />
          ) : (
            <div className="text-gray-500 text-center py-8">
              No Terms of Service have been configured yet.
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
