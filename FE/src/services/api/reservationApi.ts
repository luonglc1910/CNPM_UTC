export const reservationApi = {
  checkAvailability: (params: any) => fetch('/api/reservations/availability?' + new URLSearchParams(params)),
  bookRoom: (data: any) => fetch('/api/reservations/book', { method: 'POST', body: JSON.stringify(data) }),
  getReservation: (id: number) => fetch(`/api/reservations/${id}`),
  updateReservation: (id: number, data: any) => fetch(`/api/reservations/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  cancelReservation: (id: number) => fetch(`/api/reservations/${id}/cancel`, { method: 'PUT' }),
  getUpcomingReservations: () => fetch('/api/reservations/upcoming'),
};
