import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { AuthService } from '../../../core/auth/auth.service';
import { ErrorNotificationService } from '../../../core/notifications/error-notification.service';
import { RegisterPageComponent } from './register-page.component';

describe('RegisterPageComponent', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('should create', () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { queryParamMap: convertToParamMap({ returnUrl: '/invite/token' }) },
          },
        },
        { provide: AuthService, useValue: { register: vi.fn(() => of({})) } },
        { provide: ErrorNotificationService, useValue: { show: vi.fn() } },
      ],
    });

    const router = TestBed.inject(Router);
    const navigateByUrl = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
    const component = TestBed.runInInjectionContext(() => new RegisterPageComponent());
    const testableComponent = component as unknown as {
      form: {
        setValue(value: { email: string; username: string; password: string }): void;
      };
      submit(): void;
    };

    expect(component).toBeTruthy();
    testableComponent.form.setValue({
      email: 'alice@example.com',
      username: 'alice',
      password: 'password123',
    });
    testableComponent.submit();
    expect(navigateByUrl).toHaveBeenCalledWith('/invite/token');
  });
});
