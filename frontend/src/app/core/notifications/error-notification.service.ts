import { Injectable, inject } from '@angular/core';
import { MessageService } from 'primeng/api';

import { mapApiError } from '../api/api-error.mapper';

@Injectable({ providedIn: 'root' })
export class ErrorNotificationService {
  private readonly messages = inject(MessageService);

  show(error: unknown, fallbackMessage?: string): void {
    this.messages.add({
      severity: 'error',
      ...mapApiError(error, fallbackMessage),
      life: 4500,
      closable: true,
    });
  }

  showMessage(detail: string): void {
    this.messages.add({
      severity: 'error',
      summary: 'Something went wrong',
      detail,
      life: 4500,
      closable: true,
    });
  }
}
