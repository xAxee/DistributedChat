import { HttpErrorResponse } from '@angular/common/http';

import { mapApiError } from './api-error.mapper';

describe('mapApiError', () => {
  it('maps HTTP 429 responses to a friendly rate-limit notification', () => {
    const error = new HttpErrorResponse({
      status: 429,
      error: { title: 'Too Many Requests', detail: 'Too many requests.' },
    });

    expect(mapApiError(error)).toEqual({
      summary: 'Slow down',
      detail: "You're doing that too often. Please wait a moment before trying again.",
    });
  });

  it('maps the SignalR send-message rate-limit marker without exposing technical details', () => {
    const error = new Error(
      "An unexpected error occurred invoking 'SendMessage'. " +
        'HubException: RateLimit.SendMessage: Too many messages.',
    );

    expect(mapApiError(error)).toEqual({
      summary: 'Slow down',
      detail: "You're sending messages too quickly. Please wait a moment before trying again.",
    });
  });

  it('keeps the default notification summary for other errors', () => {
    expect(mapApiError(new Error('Request failed.'))).toEqual({
      summary: 'Something went wrong',
      detail: 'Request failed.',
    });
  });
});
