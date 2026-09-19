export const housekeepingApi = {
  getRoomsStatus: () => fetch('/api/housekeeping/rooms/status'),
  updateRoomStatus: (roomId: number, data: any) => fetch(`/api/housekeeping/rooms/${roomId}/status`, { method: 'PUT', body: JSON.stringify(data) }),
  createServiceRequest: (data: any) => fetch('/api/housekeeping/requests', { method: 'POST', body: JSON.stringify(data) }),
  getPendingRequests: () => fetch('/api/housekeeping/requests/pending'),
  completeRequest: (id: number) => fetch(`/api/housekeeping/requests/${id}/complete`, { method: 'PUT' }),
  recordMinibarUsage: (roomId: number, data: any) => fetch(`/api/housekeeping/rooms/${roomId}/minibar-usage`, { method: 'POST', body: JSON.stringify(data) }),
};
