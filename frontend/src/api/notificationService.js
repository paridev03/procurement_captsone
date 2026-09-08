import apiClient from './apiClient';

export const notificationService = {
  getAll: () => apiClient.get('/notifications').then((r) => r.data),
  getUnreadCount: () => apiClient.get('/notifications/unread-count').then((r) => r.data.count),
  markAsRead: (id) => apiClient.post(`/notifications/${id}/read`).then((r) => r.data),
  markAllAsRead: () => apiClient.post('/notifications/read-all').then((r) => r.data),
};
