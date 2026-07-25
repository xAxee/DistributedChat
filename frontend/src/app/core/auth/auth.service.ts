import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, catchError, map, of, tap } from 'rxjs';

import { APP_CONFIG } from '../config/app-config';
import { AuthResponse, CurrentUser, LoginRequest, RegisterRequest } from '../models/auth.models';
import { AuthTokenService } from './auth-token.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly tokenService = inject(AuthTokenService);
  private readonly currentUserSubject = new BehaviorSubject<CurrentUser | null>(null);
  private readonly apiBaseUrl = inject(APP_CONFIG).apiBaseUrl;

  readonly currentUser$ = this.currentUserSubject.asObservable();
  readonly isAuthenticated$ = this.currentUser$.pipe(map((user) => user !== null));

  get currentUserSnapshot(): CurrentUser | null {
    return this.currentUserSubject.value;
  }

  loadCurrentUser(): Observable<CurrentUser | null> {
    if (!this.tokenService.getToken()) {
      this.currentUserSubject.next(null);

      return of(null);
    }

    return this.http.get<CurrentUser>(`${this.apiBaseUrl}/users/me`).pipe(
      tap((user) => this.currentUserSubject.next(user)),
      catchError(() => {
        this.logout();

        return of(null);
      }),
    );
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.apiBaseUrl}/auth/login`, request)
      .pipe(tap((response) => this.applyAuthResponse(response)));
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.apiBaseUrl}/auth/register`, request)
      .pipe(tap((response) => this.applyAuthResponse(response)));
  }

  logout(): void {
    this.tokenService.clearToken();
    this.currentUserSubject.next(null);
  }

  expireSession(): void {
    this.logout();
    void this.router.navigate(['/login']);
  }

  private applyAuthResponse(response: AuthResponse): void {
    this.tokenService.setToken(response.accessToken);
    this.currentUserSubject.next(response.user);
  }
}
