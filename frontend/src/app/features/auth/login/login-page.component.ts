import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { PasswordModule } from 'primeng/password';
import { finalize } from 'rxjs';

import { AuthService } from '../../../core/auth/auth.service';
import { ErrorNotificationService } from '../../../core/notifications/error-notification.service';

@Component({
  selector: 'app-login-page',
  imports: [PasswordModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login-page.component.html',
  styleUrl: './login-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginPageComponent {
  private readonly authService = inject(AuthService);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly errorNotifications = inject(ErrorNotificationService);

  protected readonly loading = signal(false);

  protected readonly form = this.formBuilder.group({
    login: ['', [Validators.required]],
    password: ['', [Validators.required]],
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();

      return;
    }

    this.loading.set(true);

    this.authService
      .login(this.form.getRawValue())
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: () => void this.router.navigateByUrl(this.returnUrl()),
        error: (error: unknown) => this.errorNotifications.show(error, 'Could not sign in.'),
      });
  }

  protected loginError(): string {
    return this.form.controls.login.hasError('required') ? 'Email or username is required.' : '';
  }

  protected passwordError(): string {
    return this.form.controls.password.hasError('required') ? 'Password is required.' : '';
  }

  private returnUrl(): string {
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');

    return returnUrl?.startsWith('/') ? returnUrl : '/rooms';
  }
}
