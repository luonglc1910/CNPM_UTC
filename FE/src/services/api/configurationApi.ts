export const configurationApi = {
  getRoomTypes: () => fetch('/api/configurations/room-types'),
  createRoomType: (data: any) => fetch('/api/configurations/room-types', { method: 'POST', body: JSON.stringify(data) }),
  getRooms: () => fetch('/api/configurations/rooms'),
  createRoom: (data: any) => fetch('/api/configurations/rooms', { method: 'POST', body: JSON.stringify(data) }),
  updateRoomStatus: (id: number, data: any) => fetch(`/api/configurations/rooms/${id}/status`, { method: 'PUT', body: JSON.stringify(data) }),
  getServices: () => fetch('/api/configurations/services'),
  createService: (data: any) => fetch('/api/configurations/services', { method: 'POST', body: JSON.stringify(data) }),
  getEmployees: () => fetch('/api/configurations/employees'),
  createEmployee: (data: any) => fetch('/api/configurations/employees', { method: 'POST', body: JSON.stringify(data) }),
};
