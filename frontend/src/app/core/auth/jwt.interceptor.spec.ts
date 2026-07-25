import { HttpRequest, HttpResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { AuthService } from './auth.service';
import { AuthTokenService } from './auth-token.service';
import { jwtInterceptor } from './jwt.interceptor';

describe('jwtInterceptor', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('adds JWT bearer token to outgoing requests', () => {
    TestBed.configureTestingModule({
      providers: [
        { provide: AuthTokenService, useValue: { getToken: () => 'test-token' } },
        { provide: AuthService, useValue: { expireSession: vi.fn() } },
      ],
    });

    let authorizationHeader: string | null = null;
    TestBed.runInInjectionContext(() => {
      jwtInterceptor(new HttpRequest('GET', '/api/rooms'), (request) => {
        authorizationHeader = request.headers.get('Authorization');
        return of(new HttpResponse({ status: 200 }));
      }).subscribe();
    });

    expect(authorizationHeader).toBe('Bearer test-token');
  });
});
