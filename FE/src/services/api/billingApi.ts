export const billingApi = {
  getFolio: (stayId: number) => fetch(`/api/billing/folios/${stayId}`),
  addCharge: (stayId: number, data: any) => fetch(`/api/billing/folios/${stayId}/add-charge`, { method: 'POST', body: JSON.stringify(data) }),
  processPayment: (data: any) => fetch('/api/billing/payments', { method: 'POST', body: JSON.stringify(data) }),
  generateInvoice: (stayId: number) => fetch(`/api/billing/invoices/generate/${stayId}`, { method: 'POST' }),
  getRevenueReport: (params?: any) => fetch('/api/billing/reports/revenue?' + new URLSearchParams(params)),
  getOccupancyReport: (params?: any) => fetch('/api/billing/reports/occupancy?' + new URLSearchParams(params)),
};
