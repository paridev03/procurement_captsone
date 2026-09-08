import apiClient from './apiClient';

// Thin wrapper over the PurchaseRequests API — one function per endpoint, no business
// logic here (that all lives server-side; the frontend never re-derives it).
export const requestService = {
  getWorklist: () => apiClient.get('/purchase-requests').then((r) => r.data),
  getById: (id) => apiClient.get(`/purchase-requests/${id}`).then((r) => r.data),
  create: (dto) => apiClient.post('/purchase-requests', dto).then((r) => r.data),
  update: (id, dto) => apiClient.put(`/purchase-requests/${id}`, dto).then((r) => r.data),
  submit: (id) => apiClient.post(`/purchase-requests/${id}/submit`).then((r) => r.data),
  cancel: (id) => apiClient.post(`/purchase-requests/${id}/cancel`).then((r) => r.data),
  managerDecision: (id, approve, comment) =>
    apiClient
      .post(`/purchase-requests/${id}/manager-decision?approve=${approve}`, { comment })
      .then((r) => r.data),
  financeDecision: (id, approve, comment) =>
    apiClient
      .post(`/purchase-requests/${id}/finance-decision?approve=${approve}`, { comment })
      .then((r) => r.data),
  selectVendor: (id, vendorId) =>
    apiClient.post(`/purchase-requests/${id}/select-vendor`, { vendorId }).then((r) => r.data),
  triggerPayment: (id) =>
    apiClient.post(`/purchase-requests/${id}/trigger-payment`).then((r) => r.data),
};

export const vendorService = {
  getActive: () => apiClient.get('/vendors').then((r) => r.data),
};
