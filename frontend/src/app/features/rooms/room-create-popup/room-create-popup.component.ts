import {
  ChangeDetectionStrategy,
  Component,
  HostListener,
  inject,
  input,
  output,
} from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { CreateRoomRequest } from '../../../core/models/room.models';

@Component({
  selector: 'app-room-create-popup',
  imports: [ReactiveFormsModule],
  templateUrl: './room-create-popup.component.html',
  styleUrl: './room-create-popup.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoomCreatePopupComponent {
  private readonly formBuilder = inject(NonNullableFormBuilder);

  readonly creating = input(false);
  readonly closed = output<void>();
  readonly createRequested = output<CreateRoomRequest>();

  protected readonly createRoomForm = this.formBuilder.group({
    name: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(50)]],
    visibility: ['public'],
    password: ['', [Validators.minLength(8), Validators.maxLength(100)]],
  });

  protected submit(): void {
    const values = this.createRoomForm.getRawValue();
    if (values.visibility === 'private' && !values.password.trim()) {
      this.createRoomForm.controls.password.setErrors({ required: true });
    }

    if (this.createRoomForm.invalid) {
      this.createRoomForm.markAllAsTouched();
      return;
    }

    this.createRequested.emit({
      name: values.name.trim(),
      isPrivate: values.visibility === 'private',
      password: values.visibility === 'private' ? values.password : null,
    });
  }

  protected close(): void {
    if (!this.creating()) {
      this.closed.emit();
    }
  }

  @HostListener('document:keydown.escape')
  protected closeOnEscape(): void {
    this.close();
  }

  protected roomNameError(): string {
    const control = this.createRoomForm.controls.name;
    if (control.hasError('required')) return 'Room name is required.';
    if (control.hasError('minlength')) return 'Room name must be at least 3 characters.';
    return control.hasError('maxlength') ? 'Room name can be at most 50 characters.' : '';
  }

  protected roomPasswordError(): string {
    const control = this.createRoomForm.controls.password;
    if (control.hasError('required')) return 'Password is required for a private room.';
    if (control.hasError('minlength')) return 'Password must be at least 8 characters.';
    return control.hasError('maxlength') ? 'Password can be at most 100 characters.' : '';
  }

  protected isPrivateRoomSelected(): boolean {
    return this.createRoomForm.controls.visibility.value === 'private';
  }
}
