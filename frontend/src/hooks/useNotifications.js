import { useCallback, useEffect, useState } from 'react';
import { notificationService } from '../api/notificationService';

const POLL_INTERVAL_MS = 25_000;

/// Owns notification state/polling only — NotificationBell owns rendering. Splitting these
/// means either can change for its own reason: swap polling for a push channel later and
/// no component needs touching; restyle the bell and no data logic needs touching.
export function useNotifications() {
  const [items, setItems] = useState([]);
  const [unreadCount, setUnreadCount] = useState(0);

  const refreshCount = useCallback(() => {
    notificationService.getUnreadCount().then(setUnreadCount).catch(() => {});
  }, []);

  const refreshList = useCallback(() => {
    notificationService.getAll().then(setItems).catch(() => {});
  }, []);

  useEffect(() => {
    refreshCount();
    const timer = setInterval(refreshCount, POLL_INTERVAL_MS);
    return () => clearInterval(timer);
  }, [refreshCount]);

  const markAsRead = useCallback(async (id) => {
    await notificationService.markAsRead(id);
    setItems((prev) => prev.map((n) => (n.id === id ? { ...n, isRead: true } : n)));
    setUnreadCount((c) => Math.max(0, c - 1));
  }, []);

  const markAllAsRead = useCallback(async () => {
    await notificationService.markAllAsRead();
    setItems((prev) => prev.map((n) => ({ ...n, isRead: true })));
    setUnreadCount(0);
  }, []);

  return { items, unreadCount, refreshList, markAsRead, markAllAsRead };
}
