import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { PasswordModule } from 'primeng/password';
import { finalize } from 'rxjs';

import { AuthService } from '../../../core/auth/auth.service';
import { ErrorNotificationService } from '../../../core/notifications/error-notification.service';

@Component({
  selector: 'app-register-page',
  imports: [PasswordModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register-page.component.html',
  styleUrl: './register-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterPageComponent {
  private readonly authService = inject(AuthService);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly errorNotifications = inject(ErrorNotificationService);

  protected readonly loading = signal(false);

  protected readonly form = this.formBuilder.group({
    email: ['', [Validators.required, Validators.email]],
    username: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(30)]],
    password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(128)]],
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();

      return;
    }

    this.loading.set(true);

    this.authService
      .register(this.form.getRawValue())
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: () => void this.router.navigateByUrl(this.returnUrl()),
        error: (error: unknown) =>
          this.errorNotifications.show(error, 'Could not create the account.'),
      });
  }

  protected emailError(): string {
    const control = this.form.controls.email;
    if (control.hasError('required')) {
      return 'Email is required.';
    }

    return control.hasError('email') ? 'Enter a valid email address.' : '';
  }

  protected usernameError(): string {
    const control = this.form.controls.username;
    if (control.hasError('required')) {
      return 'Username is required.';
    }

    if (control.hasError('minlength')) {
      return 'Username must be at least 3 characters.';
    }

    return control.hasError('maxlength') ? 'Username can be at most 30 characters.' : '';
  }

  protected passwordError(): string {
    const control = this.form.controls.password;
    if (control.hasError('required')) {
      return 'Password is required.';
    }

    if (control.hasError('minlength')) {
      return 'Password must be at least 8 characters.';
    }

    return control.hasError('maxlength') ? 'Password can be at most 128 characters.' : '';
  }

  protected loginQueryParams(): { returnUrl: string } {
    return { returnUrl: this.returnUrl() };
  }

  private returnUrl(): string {
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');

    return returnUrl?.startsWith('/') ? returnUrl : '/rooms';
  }
}
