export type Listing = {
  id: string;
  title: string;
  description: string;
  location: string;
  nightlyPriceAmount: number;
  maxGuests: number;
  bedroomCount: number;
  bathroomCount: number;
  heroImageUrl: string;
  amenities: string;
  createdAt: string;
};

export type SearchListingsResponse = {
  items: Listing[];
  page: number;
  pageSize: number;
  totalCount: number;
  hasNextPage: boolean;
};

export type ListingAvailabilityResponse = {
  listingId: string;
  startDate: string;
  endDate: string;
  available: boolean;
};

export type MyListing = {
  id: string;
  title: string;
  description: string;
  location: string;
  nightlyPriceAmount: number;
  maxGuests: number;
  bedroomCount: number;
  bathroomCount: number;
  heroImageUrl: string;
  amenities: string;
  status: string;
  createdAt: string;
};

export type MyListingsResponse = {
  items: MyListing[];
  page: number;
  pageSize: number;
  totalCount: number;
  hasNextPage: boolean;
};

export type MyReservation = {
  id: string;
  listingId: string;
  listingTitle: string;
  startDate: string;
  endDate: string;
  status: string;
  paymentStatus: string;
  createdAt: string;
  updatedAt: string;
};

export type MyReservationsResponse = {
  items: MyReservation[];
  page: number;
  pageSize: number;
  totalCount: number;
  hasNextPage: boolean;
};

export type CreateListingResponse = {
  listingId: string;
  status: string;
};

export type CreateReservationResponse = {
  reservationId: string;
  status: string;
  paymentStatus: string;
};

export type CancelReservationResponse = {
  reservationId: string;
  status: string;
  paymentStatus: string;
};

export type ConfirmReservationPaymentResponse = {
  reservationId: string;
  status: string;
  paymentStatus: string;
};

export type ApiError = {
  status: number;
  title: string;
  detail?: string;
};

export type ApiResult<T> =
  | { ok: true; data: T }
  | { ok: false; error: ApiError };

const defaultApiBaseUrl = "http://localhost:5245";

export function normalizeApiBaseUrl(value: string | undefined): string {
  const trimmed = value?.trim();

  if (!trimmed) {
    return defaultApiBaseUrl;
  }

  return trimmed.replace(/\/+$/, "");
}

export function buildApiUrl(path: string, query?: Record<string, string | number | undefined>): string {
  const url = new URL(path, `${normalizeApiBaseUrl(process.env.BETTERBOOKING_API_BASE_URL)}/`);

  for (const [key, value] of Object.entries(query ?? {})) {
    if (value !== undefined && `${value}`.length > 0) {
      url.searchParams.set(key, `${value}`);
    }
  }

  return url.toString();
}

export async function getJson<T>(
  path: string,
  query?: Record<string, string | number | undefined>,
  accessToken?: string
): Promise<ApiResult<T>> {
  let response: Response;
  const headers: Record<string, string> = {
    Accept: "application/json"
  };

  if (accessToken) {
    headers.Authorization = `Bearer ${accessToken}`;
  }

  try {
    response = await fetch(buildApiUrl(path, query), {
      cache: "no-store",
      headers
    });
  } catch {
    return {
      ok: false,
      error: {
        status: 0,
        title: "API unavailable",
        detail: "The frontend could not reach the BetterBooking API."
      }
    };
  }

  if (response.ok) {
    return { ok: true, data: (await response.json()) as T };
  }

  return { ok: false, error: await readError(response) };
}

export async function postJson<T>(path: string, body: unknown, accessToken: string): Promise<ApiResult<T>> {
  let response: Response;

  try {
    response = await fetch(buildApiUrl(path), {
      method: "POST",
      cache: "no-store",
      headers: {
        Accept: "application/json",
        Authorization: `Bearer ${accessToken}`,
        "Content-Type": "application/json"
      },
      body: JSON.stringify(body)
    });
  } catch {
    return {
      ok: false,
      error: {
        status: 0,
        title: "API unavailable",
        detail: "The frontend could not reach the BetterBooking API."
      }
    };
  }

  if (response.ok) {
    return { ok: true, data: (await response.json()) as T };
  }

  return { ok: false, error: await readError(response) };
}

export async function postWithoutBody<T>(path: string, accessToken: string): Promise<ApiResult<T>> {
  let response: Response;

  try {
    response = await fetch(buildApiUrl(path), {
      method: "POST",
      cache: "no-store",
      headers: {
        Accept: "application/json",
        Authorization: `Bearer ${accessToken}`
      }
    });
  } catch {
    return {
      ok: false,
      error: {
        status: 0,
        title: "API unavailable",
        detail: "The frontend could not reach the BetterBooking API."
      }
    };
  }

  if (response.ok) {
    return { ok: true, data: (await response.json()) as T };
  }

  return { ok: false, error: await readError(response) };
}

export function searchListings(query: { q?: string; page?: number; pageSize?: number }) {
  return getJson<SearchListingsResponse>("/api/v1/listings", query);
}

export function getListing(listingId: string) {
  return getJson<Listing>(`/api/v1/listings/${listingId}`);
}

export function checkListingAvailability(listingId: string, startDate: string, endDate: string) {
  return getJson<ListingAvailabilityResponse>(`/api/v1/listings/${listingId}/availability`, {
    startDate,
    endDate
  });
}

export function getMyListings(query: { page?: number; pageSize?: number }, accessToken?: string) {
  return getJson<MyListingsResponse>("/api/v1/me/listings", query, accessToken);
}

export function getMyReservations(query: { page?: number; pageSize?: number }, accessToken?: string) {
  return getJson<MyReservationsResponse>("/api/v1/me/reservations", query, accessToken);
}

export type CreateListingRequest = {
  title: string;
  description: string;
  location: string;
  nightlyPriceAmount: number;
  maxGuests: number;
  bedroomCount: number;
  bathroomCount: number;
  heroImageUrl?: string;
  amenities?: string;
};

export function createListing(request: CreateListingRequest, accessToken: string) {
  return postJson<CreateListingResponse>("/api/v1/listings", request, accessToken);
}

export function publishListing(listingId: string, accessToken: string) {
  return postWithoutBody<CreateListingResponse>(`/api/v1/listings/${listingId}/publish`, accessToken);
}

export function unpublishListing(listingId: string, accessToken: string) {
  return postWithoutBody<CreateListingResponse>(`/api/v1/listings/${listingId}/unpublish`, accessToken);
}

export function archiveListing(listingId: string, accessToken: string) {
  return postWithoutBody<CreateListingResponse>(`/api/v1/listings/${listingId}/archive`, accessToken);
}

export function createReservation(
  request: { listingId: string; startDate: string; endDate: string },
  accessToken: string
) {
  return postJson<CreateReservationResponse>("/api/v1/reservations", request, accessToken);
}

export function cancelReservation(reservationId: string, accessToken: string) {
  return postWithoutBody<CancelReservationResponse>(`/api/v1/reservations/${reservationId}/cancel`, accessToken);
}

export function confirmReservationPayment(reservationId: string, accessToken: string) {
  return postWithoutBody<ConfirmReservationPaymentResponse>(`/api/v1/reservations/${reservationId}/payment/confirm`, accessToken);
}

async function readError(response: Response): Promise<ApiError> {
  if (response.headers.get("content-type")?.includes("application/json")) {
    const problem = (await response.json()) as Partial<ApiError>;

    return {
      status: response.status,
      title: problem.title ?? response.statusText,
      detail: problem.detail
    };
  }

  return {
    status: response.status,
    title: response.statusText || "Request failed"
  };
}
