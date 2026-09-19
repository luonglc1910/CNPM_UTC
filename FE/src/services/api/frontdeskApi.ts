export const frontdeskApi = {
  checkIn: (data: any) => fetch('/api/frontdesk/checkin', { method: 'POST', body: JSON.stringify(data) }),
  walkInCheckIn: (data: any) => fetch('/api/frontdesk/checkin/walk-in', { method: 'POST', body: JSON.stringify(data) }),
  checkOut: (stayId: number) => fetch(`/api/frontdesk/checkout/${stayId}`, { method: 'POST' }),
  getActiveStays: () => fetch('/api/frontdesk/stays/active'),
  changeRoom: (stayId: number, data: any) => fetch(`/api/frontdesk/stays/${stayId}/change-room`, { method: 'PUT', body: JSON.stringify(data) }),
  addGuest: (stayId: number, data: any) => fetch(`/api/frontdesk/stays/${stayId}/add-guest`, { method: 'POST', body: JSON.stringify(data) }),
};
