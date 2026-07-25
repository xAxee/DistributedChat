import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import { APP_CONFIG } from '../config/app-config';
import { ApplicationStatusResponse } from '../models/status.models';

export type HealthCheck = 'live' | 'ready';

@Injectable({ providedIn: 'root' })
export class StatusApiService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = inject(APP_CONFIG).apiBaseUrl;
  private readonly serviceBaseUrl = this.apiBaseUrl.endsWith('/api')
    ? this.apiBaseUrl.slice(0, -4)
    : this.apiBaseUrl;

  getStatus(): Observable<ApplicationStatusResponse> {
    return this.http.get<ApplicationStatusResponse>(`${this.apiBaseUrl}/status`);
  }

  checkHealth(check: HealthCheck): Observable<boolean> {
    return this.http
      .get(`${this.serviceBaseUrl}/health/${check}`, { responseType: 'text' })
      .pipe(map(() => true));
  }
}
