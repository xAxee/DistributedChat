import { TestBed } from '@angular/core/testing';
import { MessageService } from 'primeng/api';

import { ErrorNotificationService } from './error-notification.service';

describe('ErrorNotificationService', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('should create', () => {
    configure();

    expect(TestBed.inject(ErrorNotificationService)).toBeTruthy();
  });

  it('publishes the mapped summary and detail through MessageService', () => {
    const add = vi.fn();
    configure(add);

    TestBed.inject(ErrorNotificationService).show(
      new Error('HubException: RateLimit.SendMessage: Too many messages.'),
    );

    expect(add).toHaveBeenCalledWith({
      severity: 'error',
      summary: 'Slow down',
      detail: "You're sending messages too quickly. Please wait a moment before trying again.",
      life: 4500,
      closable: true,
    });
  });
});

function configure(add = vi.fn()): void {
  TestBed.configureTestingModule({
    providers: [{ provide: MessageService, useValue: { add } }],
  });
}
