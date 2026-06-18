// Contratos de autenticación (espejo de los DTOs del backend): peticiones de login/registro
// y la respuesta con el par de tokens (access corto + refresh) y los metadatos de vigencia.

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  fullName?: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  refreshExpiresIn: number;
  tokenType: string;
  email: string;
}
