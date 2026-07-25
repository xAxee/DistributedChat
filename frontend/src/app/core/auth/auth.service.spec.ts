import { HttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { APP_CONFIG } from '../config/app-config';
import { AuthService } from './auth.service';
import { AuthTokenService } from './auth-token.service';

describe('AuthService', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('should create', () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: APP_CONFIG, useValue: { apiBaseUrl: '/api' } },
        { provide: HttpClient, useValue: {} },
        { provide: AuthTokenService, useValue: {} },
      ],
    });

    expect(TestBed.inject(AuthService)).toBeTruthy();
  });
});
