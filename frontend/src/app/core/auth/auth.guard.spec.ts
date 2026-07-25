import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  Router,
  RouterStateSnapshot,
  UrlTree,
  provideRouter,
} from '@angular/router';
import { Observable, firstValueFrom, of } from 'rxjs';

import { AuthService } from './auth.service';
import { authGuard } from './auth.guard';

describe('authGuard', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('blocks unauthenticated users and redirects to login', async () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: { currentUser$: of(null) } },
      ],
    });

    const result = TestBed.runInInjectionContext(() =>
      authGuard({} as ActivatedRouteSnapshot, { url: '/rooms/room-id' } as RouterStateSnapshot),
    ) as Observable<boolean | UrlTree>;
    const guardResult = await firstValueFrom(result);

    expect(TestBed.inject(Router).serializeUrl(guardResult as UrlTree)).toBe(
      '/login?returnUrl=%2Frooms%2Froom-id',
    );
  });
});
