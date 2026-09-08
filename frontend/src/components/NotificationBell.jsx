import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useNotifications } from '../hooks/useNotifications';

const EVENT_ICONS = {
  RequestSubmitted: '📝',
  ManagerApproved: '✅',
  ManagerRejected: '⛔',
  FinanceApproved: '💰',
  FinanceRejected: '⛔',
  VendorSelected: '📦',
  PaymentStarted: '⏳',
  PaymentCompleted: '✅',
  PaymentFailed: '⚠️',
  RequestCompleted: '🏁',
};

function timeAgo(iso) {
  const seconds = Math.floor((Date.now() - new Date(iso).getTime()) / 1000);
  if (seconds < 60) return 'just now';
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(hours / 24);
  return `${days}d ago`;
}

export default function NotificationBell() {
  const { items, unreadCount, refreshList, markAsRead, markAllAsRead } = useNotifications();
  const [open, setOpen] = useState(false);
  const ref = useRef(null);
  const navigate = useNavigate();

  useEffect(() => {
    if (open) refreshList();
  }, [open, refreshList]);

  useEffect(() => {
    function onDocClick(e) {
      if (ref.current && !ref.current.contains(e.target)) setOpen(false);
    }
    document.addEventListener('mousedown', onDocClick);
    return () => document.removeEventListener('mousedown', onDocClick);
  }, []);

  async function handleItemClick(n) {
    if (!n.isRead) await markAsRead(n.id);
    setOpen(false);
    if (n.purchaseRequestId) navigate(`/requests/${n.purchaseRequestId}`);
  }

  return (
    <div className="notif-bell" ref={ref}>
      <button
        type="button"
        className="notif-bell__trigger"
        onClick={() => setOpen((o) => !o)}
        aria-label={`Notifications${unreadCount > 0 ? ` (${unreadCount} unread)` : ''}`}
      >
        🔔
        {unreadCount > 0 && <span className="notif-bell__badge">{unreadCount > 9 ? '9+' : unreadCount}</span>}
      </button>

      {open && (
        <div className="notif-panel">
          <div className="notif-panel__header">
            <span>Notifications</span>
            {unreadCount > 0 && (
              <button type="button" className="notif-panel__mark-all" onClick={markAllAsRead}>
                Mark all as read
              </button>
            )}
          </div>
          <div className="notif-panel__list">
            {items.length === 0 ? (
              <div className="notif-panel__empty">You're all caught up.</div>
            ) : (
              items.map((n) => (
                <button
                  type="button"
                  key={n.id}
                  className={`notif-item${n.isRead ? '' : ' is-unread'}`}
                  onClick={() => handleItemClick(n)}
                >
                  <span className="notif-item__icon" aria-hidden="true">{EVENT_ICONS[n.event] || '🔔'}</span>
                  <span className="notif-item__body">
                    <span className="notif-item__message">{n.message}</span>
                    <span className="notif-item__meta">
                      {n.purchaseRequestNumber && <span>{n.purchaseRequestNumber} · </span>}
                      {timeAgo(n.createdAt)}
                    </span>
                  </span>
                  {!n.isRead && <span className="notif-item__dot" aria-hidden="true" />}
                </button>
              ))
            )}
          </div>
        </div>
      )}
    </div>
  );
}
