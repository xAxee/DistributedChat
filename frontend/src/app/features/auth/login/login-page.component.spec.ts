import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { AuthService } from '../../../core/auth/auth.service';
import { ErrorNotificationService } from '../../../core/notifications/error-notification.service';
import { LoginPageComponent } from './login-page.component';

describe('LoginPageComponent', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('should create', () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: {} },
        { provide: ErrorNotificationService, useValue: {} },
      ],
    });

    const component = TestBed.runInInjectionContext(() => new LoginPageComponent());

    expect(component).toBeTruthy();
  });
});
