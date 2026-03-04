import { useState, useEffect } from 'react';
import { 
  Mail, CheckCircle, XCircle, Clock, AlertTriangle, RefreshCw, 
  Eye, Send, Trash2, Filter, ChevronLeft, ChevronRight,
  Check, X, RotateCcw
} from 'lucide-react';
import { config } from '../utils/config';

interface NotificationQueueItem {
  id: number;
  siteKey: string;
  typeCode: string;
  userId?: number;
  recipientEmail?: string;
  recipientPhone?: string;
  recipientName?: string;
  channelCode: string;
  subject?: string;
  bodyHtml?: string;
  bodyText?: string;
  status: string;
  moderationStatus?: string;
  moderationNote?: string;
  priority: number;
  attempts: number;
  errorMessage?: string;
  createdAt: string;
  sentAt?: string;
}

interface QueueResponse {
  items: NotificationQueueItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

const STATUS_COLORS: Record<string, string> = {
  Pending: 'bg-yellow-100 text-yellow-800',
  Held: 'bg-orange-100 text-orange-800',
  Sending: 'bg-blue-100 text-blue-800',
  Sent: 'bg-green-100 text-green-800',
  Failed: 'bg-red-100 text-red-800',
  Cancelled: 'bg-gray-100 text-gray-800',
};

const STATUS_ICONS: Record<string, React.ReactNode> = {
  Pending: <Clock className="w-4 h-4" />,
  Held: <AlertTriangle className="w-4 h-4" />,
  Sending: <Send className="w-4 h-4" />,
  Sent: <CheckCircle className="w-4 h-4" />,
  Failed: <XCircle className="w-4 h-4" />,
  Cancelled: <Trash2 className="w-4 h-4" />,
};

export function NotificationPipelineTab() {
  const [items, setItems] = useState<NotificationQueueItem[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  
  // Filters
  const [statusFilter, setStatusFilter] = useState<string>('');
  const [siteFilter, setSiteFilter] = useState<string>('');
  
  // View modal
  const [viewingItem, setViewingItem] = useState<NotificationQueueItem | null>(null);
  
  // Moderation
  const [moderatingId, setModeratingId] = useState<number | null>(null);
  const [moderationNote, setModerationNote] = useState('');

  useEffect(() => {
    loadQueue();
  }, [page, statusFilter, siteFilter]);

  const loadQueue = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const params = new URLSearchParams({
        page: page.toString(),
        pageSize: pageSize.toString(),
      });
      if (statusFilter) params.append('status', statusFilter);
      if (siteFilter) params.append('siteKey', siteFilter);

      const response = await fetch(`${config.API_URL}/notifications/queue?${params}`, {
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`,
        },
      });
      
      if (!response.ok) throw new Error('Failed to load queue');
      
      const data: QueueResponse = await response.json();
      setItems(data.items);
      setTotalCount(data.totalCount);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load queue');
    } finally {
      setIsLoading(false);
    }
  };

  const handleApprove = async (id: number) => {
    try {
      const response = await fetch(`${config.API_URL}/notifications/${id}/approve`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ note: moderationNote }),
      });
      
      if (!response.ok) throw new Error('Failed to approve');
      
      setModeratingId(null);
      setModerationNote('');
      loadQueue();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to approve');
    }
  };

  const handleReject = async (id: number) => {
    try {
      const response = await fetch(`${config.API_URL}/notifications/${id}/reject`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ note: moderationNote }),
      });
      
      if (!response.ok) throw new Error('Failed to reject');
      
      setModeratingId(null);
      setModerationNote('');
      loadQueue();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to reject');
    }
  };

  const handleRetry = async (id: number) => {
    try {
      const response = await fetch(`${config.API_URL}/notifications/${id}/retry`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`,
        },
      });
      
      if (!response.ok) throw new Error('Failed to retry');
      
      loadQueue();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to retry');
    }
  };

  const totalPages = Math.ceil(totalCount / pageSize);

  const formatDate = (dateStr: string) => {
    const date = new Date(dateStr);
    return date.toLocaleString();
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-semibold text-gray-900">Notification Pipeline</h2>
          <p className="text-sm text-gray-500">Monitor and moderate outgoing notifications</p>
        </div>
        <button
          onClick={loadQueue}
          disabled={isLoading}
          className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50"
        >
          <RefreshCw className={`w-4 h-4 ${isLoading ? 'animate-spin' : ''}`} />
          Refresh
        </button>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
        {['Pending', 'Held', 'Sent', 'Failed', 'Cancelled'].map(status => {
          const count = items.filter(i => i.status === status).length;
          return (
            <button
              key={status}
              onClick={() => setStatusFilter(statusFilter === status ? '' : status)}
              className={`p-4 rounded-lg border-2 transition-all ${
                statusFilter === status 
                  ? 'border-blue-500 bg-blue-50' 
                  : 'border-gray-200 hover:border-gray-300'
              }`}
            >
              <div className="flex items-center gap-2">
                <span className={`p-1.5 rounded ${STATUS_COLORS[status]}`}>
                  {STATUS_ICONS[status]}
                </span>
                <span className="font-medium text-gray-900">{status}</span>
              </div>
              <p className="mt-1 text-2xl font-bold text-gray-900">
                {statusFilter === status ? totalCount : count}
              </p>
            </button>
          );
        })}
      </div>

      {/* Filters */}
      <div className="flex gap-4 items-center bg-gray-50 p-4 rounded-lg">
        <Filter className="w-5 h-5 text-gray-400" />
        <select
          value={statusFilter}
          onChange={(e) => { setStatusFilter(e.target.value); setPage(1); }}
          className="px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500"
        >
          <option value="">All Statuses</option>
          <option value="Pending">Pending</option>
          <option value="Held">Held (needs review)</option>
          <option value="Sent">Sent</option>
          <option value="Failed">Failed</option>
          <option value="Cancelled">Cancelled</option>
        </select>
        <select
          value={siteFilter}
          onChange={(e) => { setSiteFilter(e.target.value); setPage(1); }}
          className="px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500"
        >
          <option value="">All Sites</option>
          <option value="community">community</option>
          <option value="uca">uca</option>
          <option value="date">date</option>
        </select>
        {(statusFilter || siteFilter) && (
          <button
            onClick={() => { setStatusFilter(''); setSiteFilter(''); setPage(1); }}
            className="text-sm text-blue-600 hover:underline"
          >
            Clear filters
          </button>
        )}
      </div>

      {/* Error */}
      {error && (
        <div className="p-4 bg-red-50 border border-red-200 rounded-lg text-red-700">
          {error}
        </div>
      )}

      {/* Queue Table */}
      <div className="bg-white rounded-lg border border-gray-200 overflow-hidden">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Status</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Type</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Recipient</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Subject</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Channel</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Created</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200">
            {isLoading ? (
              <tr>
                <td colSpan={7} className="px-4 py-8 text-center text-gray-500">
                  <RefreshCw className="w-6 h-6 animate-spin mx-auto mb-2" />
                  Loading...
                </td>
              </tr>
            ) : items.length === 0 ? (
              <tr>
                <td colSpan={7} className="px-4 py-8 text-center text-gray-500">
                  <Mail className="w-8 h-8 mx-auto mb-2 text-gray-300" />
                  No notifications found
                </td>
              </tr>
            ) : (
              items.map(item => (
                <tr key={item.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3">
                    <span className={`inline-flex items-center gap-1 px-2 py-1 rounded text-xs font-medium ${STATUS_COLORS[item.status]}`}>
                      {STATUS_ICONS[item.status]}
                      {item.status}
                    </span>
                  </td>
                  <td className="px-4 py-3">
                    <div className="text-sm font-medium text-gray-900">{item.typeCode}</div>
                    <div className="text-xs text-gray-500">{item.siteKey}</div>
                  </td>
                  <td className="px-4 py-3">
                    <div className="text-sm text-gray-900">{item.recipientName || '-'}</div>
                    <div className="text-xs text-gray-500">{item.recipientEmail || item.recipientPhone}</div>
                  </td>
                  <td className="px-4 py-3">
                    <div className="text-sm text-gray-900 max-w-xs truncate">{item.subject || '-'}</div>
                  </td>
                  <td className="px-4 py-3">
                    <span className="text-xs font-medium text-gray-600 bg-gray-100 px-2 py-1 rounded">
                      {item.channelCode}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-sm text-gray-500">
                    {formatDate(item.createdAt)}
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-2">
                      <button
                        onClick={() => setViewingItem(item)}
                        className="p-1.5 text-gray-500 hover:text-blue-600 hover:bg-blue-50 rounded"
                        title="View details"
                      >
                        <Eye className="w-4 h-4" />
                      </button>
                      {item.status === 'Held' && (
                        <>
                          <button
                            onClick={() => { setModeratingId(item.id); }}
                            className="p-1.5 text-green-600 hover:bg-green-50 rounded"
                            title="Approve"
                          >
                            <Check className="w-4 h-4" />
                          </button>
                          <button
                            onClick={() => handleReject(item.id)}
                            className="p-1.5 text-red-600 hover:bg-red-50 rounded"
                            title="Reject"
                          >
                            <X className="w-4 h-4" />
                          </button>
                        </>
                      )}
                      {item.status === 'Failed' && (
                        <button
                          onClick={() => handleRetry(item.id)}
                          className="p-1.5 text-orange-600 hover:bg-orange-50 rounded"
                          title="Retry"
                        >
                          <RotateCcw className="w-4 h-4" />
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>

        {/* Pagination */}
        {totalPages > 1 && (
          <div className="flex items-center justify-between px-4 py-3 border-t border-gray-200 bg-gray-50">
            <div className="text-sm text-gray-500">
              Showing {(page - 1) * pageSize + 1} to {Math.min(page * pageSize, totalCount)} of {totalCount}
            </div>
            <div className="flex items-center gap-2">
              <button
                onClick={() => setPage(p => Math.max(1, p - 1))}
                disabled={page === 1}
                className="p-2 text-gray-500 hover:bg-gray-200 rounded disabled:opacity-50"
              >
                <ChevronLeft className="w-4 h-4" />
              </button>
              <span className="text-sm text-gray-700">
                Page {page} of {totalPages}
              </span>
              <button
                onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                disabled={page === totalPages}
                className="p-2 text-gray-500 hover:bg-gray-200 rounded disabled:opacity-50"
              >
                <ChevronRight className="w-4 h-4" />
              </button>
            </div>
          </div>
        )}
      </div>

      {/* View Modal */}
      {viewingItem && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-xl shadow-xl max-w-2xl w-full mx-4 max-h-[80vh] overflow-hidden">
            <div className="flex items-center justify-between px-6 py-4 border-b">
              <h3 className="text-lg font-semibold">Notification Details</h3>
              <button onClick={() => setViewingItem(null)} className="p-2 hover:bg-gray-100 rounded">
                <X className="w-5 h-5" />
              </button>
            </div>
            <div className="p-6 overflow-y-auto max-h-[60vh] space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="text-xs font-medium text-gray-500">Status</label>
                  <div className={`mt-1 inline-flex items-center gap-1 px-2 py-1 rounded text-sm font-medium ${STATUS_COLORS[viewingItem.status]}`}>
                    {STATUS_ICONS[viewingItem.status]} {viewingItem.status}
                  </div>
                </div>
                <div>
                  <label className="text-xs font-medium text-gray-500">Type</label>
                  <p className="mt-1 text-sm">{viewingItem.typeCode}</p>
                </div>
                <div>
                  <label className="text-xs font-medium text-gray-500">Site</label>
                  <p className="mt-1 text-sm">{viewingItem.siteKey}</p>
                </div>
                <div>
                  <label className="text-xs font-medium text-gray-500">Channel</label>
                  <p className="mt-1 text-sm">{viewingItem.channelCode}</p>
                </div>
                <div>
                  <label className="text-xs font-medium text-gray-500">Recipient</label>
                  <p className="mt-1 text-sm">
                    {viewingItem.recipientName && <span className="font-medium">{viewingItem.recipientName}<br /></span>}
                    {viewingItem.recipientEmail || viewingItem.recipientPhone}
                  </p>
                </div>
                <div>
                  <label className="text-xs font-medium text-gray-500">Created</label>
                  <p className="mt-1 text-sm">{formatDate(viewingItem.createdAt)}</p>
                </div>
                {viewingItem.sentAt && (
                  <div>
                    <label className="text-xs font-medium text-gray-500">Sent</label>
                    <p className="mt-1 text-sm">{formatDate(viewingItem.sentAt)}</p>
                  </div>
                )}
                {viewingItem.attempts > 0 && (
                  <div>
                    <label className="text-xs font-medium text-gray-500">Attempts</label>
                    <p className="mt-1 text-sm">{viewingItem.attempts}</p>
                  </div>
                )}
              </div>
              
              {viewingItem.subject && (
                <div>
                  <label className="text-xs font-medium text-gray-500">Subject</label>
                  <p className="mt-1 text-sm font-medium">{viewingItem.subject}</p>
                </div>
              )}
              
              {viewingItem.bodyHtml && (
                <div>
                  <label className="text-xs font-medium text-gray-500">Body (HTML)</label>
                  <div 
                    className="mt-1 p-4 bg-gray-50 rounded-lg border text-sm prose prose-sm max-w-none"
                    dangerouslySetInnerHTML={{ __html: viewingItem.bodyHtml }}
                  />
                </div>
              )}
              
              {viewingItem.bodyText && !viewingItem.bodyHtml && (
                <div>
                  <label className="text-xs font-medium text-gray-500">Body (Text)</label>
                  <pre className="mt-1 p-4 bg-gray-50 rounded-lg border text-sm whitespace-pre-wrap">
                    {viewingItem.bodyText}
                  </pre>
                </div>
              )}
              
              {viewingItem.errorMessage && (
                <div>
                  <label className="text-xs font-medium text-gray-500">Error</label>
                  <p className="mt-1 p-3 bg-red-50 text-red-700 rounded text-sm">{viewingItem.errorMessage}</p>
                </div>
              )}
              
              {viewingItem.moderationNote && (
                <div>
                  <label className="text-xs font-medium text-gray-500">Moderation Note</label>
                  <p className="mt-1 p-3 bg-yellow-50 text-yellow-800 rounded text-sm">{viewingItem.moderationNote}</p>
                </div>
              )}
            </div>
            <div className="flex justify-end gap-3 px-6 py-4 border-t bg-gray-50">
              {viewingItem.status === 'Held' && (
                <>
                  <button
                    onClick={() => { handleReject(viewingItem.id); setViewingItem(null); }}
                    className="px-4 py-2 text-red-600 border border-red-300 rounded-lg hover:bg-red-50"
                  >
                    Reject
                  </button>
                  <button
                    onClick={() => { handleApprove(viewingItem.id); setViewingItem(null); }}
                    className="px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700"
                  >
                    Approve & Send
                  </button>
                </>
              )}
              {viewingItem.status === 'Failed' && (
                <button
                  onClick={() => { handleRetry(viewingItem.id); setViewingItem(null); }}
                  className="px-4 py-2 bg-orange-600 text-white rounded-lg hover:bg-orange-700"
                >
                  Retry
                </button>
              )}
              <button
                onClick={() => setViewingItem(null)}
                className="px-4 py-2 bg-gray-100 text-gray-700 rounded-lg hover:bg-gray-200"
              >
                Close
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Approval Modal */}
      {moderatingId && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-xl shadow-xl max-w-md w-full mx-4">
            <div className="px-6 py-4 border-b">
              <h3 className="text-lg font-semibold">Approve Notification</h3>
            </div>
            <div className="p-6">
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Note (optional)
              </label>
              <textarea
                value={moderationNote}
                onChange={(e) => setModerationNote(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500"
                rows={3}
                placeholder="Add a note about this approval..."
              />
            </div>
            <div className="flex justify-end gap-3 px-6 py-4 border-t bg-gray-50">
              <button
                onClick={() => { setModeratingId(null); setModerationNote(''); }}
                className="px-4 py-2 bg-gray-100 text-gray-700 rounded-lg hover:bg-gray-200"
              >
                Cancel
              </button>
              <button
                onClick={() => handleApprove(moderatingId)}
                className="px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700"
              >
                Approve & Send
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
