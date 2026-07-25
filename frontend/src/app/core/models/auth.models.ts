export interface CurrentUser {
  id: string;
  username: string;
  email: string;
  createdAt: string;
}

export interface AuthResponse {
  accessToken: string;
  expiresAt: string;
  user: CurrentUser;
}

export interface LoginRequest {
  login: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  username: string;
  password: string;
}
