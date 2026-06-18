// Envelope uniforme que devuelve la API (mismo contrato para éxito y error).
export interface ApiResponse<T> {
  ok: boolean;
  data: T | null;
  error: ApiError | null;
  requestId?: string;
  timestamp?: string;
}

export interface ApiError {
  code: string;
  message: string;
  details?: string[] | null;
}
