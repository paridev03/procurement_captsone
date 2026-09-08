import apiClient from './apiClient';

// Thin wrapper over the PurchaseRequests API — one function per endpoint, no business
// logic here (that all lives server-side; the frontend never re-derives it).
export const requestService = {
  getWorklist: () => apiClient.get('/purchase-requests').then((r) => r.data),
  getDashboard: () => apiClient.get('/purchase-requests/dashboard').then((r) => r.data),
  getById: (id) => apiClient.get(`/purchase-requests/${id}`).then((r) => r.data),
  create: (dto) => apiClient.post('/purchase-requests', dto).then((r) => r.data),
  update: (id, dto) => apiClient.put(`/purchase-requests/${id}`, dto).then((r) => r.data),
  // Procurement Admin's itemized flow — same header/state machine as create/update above,
  // with a catalog-backed line-item list instead of one inline quantity/unit cost.
  createItemized: (dto) => apiClient.post('/purchase-requests/itemized', dto).then((r) => r.data),
  updateItemized: (id, dto) => apiClient.put(`/purchase-requests/${id}/itemized`, dto).then((r) => r.data),
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
  requestVendorQuote: (id) =>
    apiClient.post(`/purchase-requests/${id}/vendor-quote`).then((r) => r.data),
  triggerPayment: (id) =>
    apiClient.post(`/purchase-requests/${id}/trigger-payment`).then((r) => r.data),
  checkBudget: (id) =>
    apiClient.get(`/purchase-requests/${id}/budget-check`).then((r) => r.data),
};

export const vendorService = {
  getActive: () => apiClient.get('/vendors').then((r) => r.data),
};

export const itemService = {
  getActive: () => apiClient.get('/items').then((r) => r.data),
};

// Payment Page — manual counterpart to requestService.triggerPayment above, driven by the
// Procurement Admin's own entered details instead of the simulated gateway.
export const paymentService = {
  saveDraft: (id, dto) => apiClient.post(`/purchase-requests/${id}/payment/draft`, dto).then((r) => r.data),
  markAsPaid: (id, dto) => apiClient.post(`/purchase-requests/${id}/payment/mark-paid`, dto).then((r) => r.data),
};

// Invoice Page — generated from the procurement + payment, never re-entered by hand.
export const invoiceService = {
  generate: (id, dto) => apiClient.post(`/purchase-requests/${id}/invoice/generate`, dto).then((r) => r.data),
  get: (id) => apiClient.get(`/purchase-requests/${id}/invoice`).then((r) => r.data),
};
