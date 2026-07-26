import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  HostListener,
  Input,
  Output,
  inject,
} from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { RoomDetails } from '../../../core/models/room.models';

@Component({
  selector: 'app-room-settings-popup',
  imports: [ReactiveFormsModule],
  templateUrl: './room-settings-popup.component.html',
  styleUrl: './room-settings-popup.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoomSettingsPopupComponent {
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private currentRoom!: RoomDetails;

  @Input({ required: true })
  set room(room: RoomDetails) {
    this.currentRoom = room;
    this.roomSettingsForm.controls.name.setValue(room.name);
  }

  get room(): RoomDetails {
    return this.currentRoom;
  }

  @Input() saving = false;
  @Input() inviteGenerating = false;
  @Input() inviteLink: string | null = null;

  @Output() readonly closed = new EventEmitter<void>();
  @Output() readonly roomNameSaved = new EventEmitter<string>();
  @Output() readonly roomPasswordSaved = new EventEmitter<string>();
  @Output() readonly inviteRequested = new EventEmitter<void>();
  @Output() readonly inviteCopyRequested = new EventEmitter<void>();
  @Output() readonly roomDeleteRequested = new EventEmitter<void>();

  protected readonly roomSettingsForm = this.formBuilder.group({
    name: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(50)]],
  });

  protected readonly passwordSettingsForm = this.formBuilder.group({
    password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100)]],
  });

  protected saveRoomName(): void {
    if (this.roomSettingsForm.invalid) {
      this.roomSettingsForm.markAllAsTouched();
      return;
    }

    this.roomNameSaved.emit(this.roomSettingsForm.getRawValue().name.trim());
  }

  protected saveRoomPassword(): void {
    if (this.passwordSettingsForm.invalid) {
      this.passwordSettingsForm.markAllAsTouched();
      return;
    }

    this.roomPasswordSaved.emit(this.passwordSettingsForm.getRawValue().password);
  }

  protected close(): void {
    if (!this.saving && !this.inviteGenerating) {
      this.closed.emit();
    }
  }

  @HostListener('document:keydown.escape')
  protected closeOnEscape(): void {
    this.close();
  }
}
