// API Client — kết nối tới ASP.NET Core backend tại http://localhost:5265

const API_BASE = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5265";

// ================================================================
// Types — khớp với DTO trong BookingApiController.cs
// ================================================================

export interface ApiRoomType {
  id: number;
  code: string;
  name: string;
  standardCapacity: number;
  maxCapacity: number;
  basePricePerNight: number;
  extraGuestFeePerNight: number;
  extraBedFeePerNight: number;
  priceFirstHour: number;
  priceExtraHour: number;
  priceOvernight: number;
  amenities: string | null;
  description: string | null;
  totalRooms: number;
}

export interface ApiAvailabilityResponse {
  checkIn: string;
  checkOut: string;
  nights: number;
  groups: ApiAvailabilityGroup[];
}

export interface ApiAvailabilityGroup {
  roomTypeId: number;
  roomTypeCode: string;
  roomTypeName: string;
  pricePerNight: number;
  priceFirstHour: number;
  priceExtraHour: number;
  priceOvernight: number;
  standardCapacity: number;
  maxCapacity: number;
  availableCount: number;
  nights: number;
  totalPriceEstimate: number;
  rooms: ApiAvailableRoom[];
}

export interface ApiAvailableRoom {
  roomId: number;
  roomNumber: string;
  floor: number;
  needsCleaning: boolean;
}

export interface CreateReservationPayload {
  checkIn: string;
  checkOut: string;
  roomTypeId: number;
  adults: number;
  children: number;
  specialRequests?: string;
  guest: {
    fullName: string;
    phoneNumber: string;
    email?: string;
    idNumber?: string;
  };
}

export interface CreateReservationResult {
  reservationId: number;
  reservationCode: string;
  guestName: string;
  checkIn: string;
  checkOut: string;
  roomNumber: string;
  roomTypeName: string;
  nights: number;
  estimatedTotal: number;
  status: string;
  message: string;
}

export interface ReservationLookup {
  reservationId: number;
  code: string;
  status: string;
  guestName: string;
  phoneNumber: string;
  checkIn: string;
  checkOut: string;
  nights: number;
  estimatedTotal: number;
  specialRequests?: string;
  rooms: {
    roomNumber: string;
    roomTypeName: string;
    adults: number;
    children: number;
    pricePerNight: number;
  }[];
}

// ================================================================
// API Functions
// ================================================================

async function fetchApi<T>(path: string, options?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      ...(options?.headers ?? {}),
    },
  });

  if (!res.ok) {
    let errorMsg = `API Error ${res.status}`;
    try {
      const errData = await res.json();
      errorMsg = errData.error ?? errData.title ?? errorMsg;
    } catch {
      // ignore JSON parse error
    }
    throw new Error(errorMsg);
  }

  return res.json() as Promise<T>;
}

/** GET /api/v1/booking/room-types — Lấy danh sách loại phòng */
export async function getRoomTypes(): Promise<ApiRoomType[]> {
  return fetchApi<ApiRoomType[]>("/api/v1/booking/room-types");
}

/** GET /api/v1/booking/availability — Kiểm tra phòng trống */
export async function getAvailability(
  checkIn: string,
  checkOut: string,
  guests?: number
): Promise<ApiAvailabilityResponse> {
  const params = new URLSearchParams({
    checkIn,
    checkOut,
    ...(guests ? { guests: String(guests) } : {}),
  });
  return fetchApi<ApiAvailabilityResponse>(`/api/v1/booking/availability?${params}`);
}

/** POST /api/v1/booking/reservations — Tạo đơn đặt phòng */
export async function createReservation(
  payload: CreateReservationPayload
): Promise<CreateReservationResult> {
  return fetchApi<CreateReservationResult>("/api/v1/booking/reservations", {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

/** GET /api/v1/booking/reservations/{code} — Tra cứu đơn đặt phòng */
export async function lookupReservation(
  code: string,
  phone: string
): Promise<ReservationLookup> {
  return fetchApi<ReservationLookup>(
    `/api/v1/booking/reservations/${encodeURIComponent(code)}?phone=${encodeURIComponent(phone)}`
  );
}
