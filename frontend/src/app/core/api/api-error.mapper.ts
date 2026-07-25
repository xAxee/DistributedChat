import { HttpErrorResponse } from '@angular/common/http';

import { ProblemDetails } from '../models/common.models';

export interface ApiErrorNotification {
  readonly summary: string;
  readonly detail: string;
}

const DEFAULT_ERROR_SUMMARY = 'Something went wrong';
const RATE_LIMIT_SUMMARY = 'Slow down';
const SEND_MESSAGE_RATE_LIMIT_CODE = 'RateLimit.SendMessage:';

export function mapApiError(
  error: unknown,
  fallbackMessage = 'The operation could not be completed.',
): ApiErrorNotification {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 0) {
      return defaultNotification(
        'Cannot connect to the server. Check your connection and try again.',
      );
    }

    if (error.status === 429) {
      return {
        summary: RATE_LIMIT_SUMMARY,
        detail: "You're doing that too often. Please wait a moment before trying again.",
      };
    }

    const problem = parseProblemDetails(error.error);
    const fieldError = problem?.errors ? firstFieldError(problem.errors) : null;
    if (fieldError) {
      return defaultNotification(fieldError);
    }

    if (problem?.detail) {
      return defaultNotification(problem.detail);
    }

    if (problem?.title) {
      return defaultNotification(problem.title);
    }

    if (error.message) {
      return defaultNotification(error.message);
    }
  }

  if (error instanceof Error && error.message) {
    if (error.message.includes(SEND_MESSAGE_RATE_LIMIT_CODE)) {
      return {
        summary: RATE_LIMIT_SUMMARY,
        detail: "You're sending messages too quickly. Please wait a moment before trying again.",
      };
    }

    return defaultNotification(error.message);
  }

  return defaultNotification(fallbackMessage);
}

function defaultNotification(detail: string): ApiErrorNotification {
  return { summary: DEFAULT_ERROR_SUMMARY, detail };
}

function parseProblemDetails(value: unknown): ProblemDetails | null {
  if (!value) {
    return null;
  }

  if (typeof value === 'string') {
    try {
      return JSON.parse(value) as ProblemDetails;
    } catch {
      return { detail: value };
    }
  }

  if (typeof value === 'object') {
    return value as ProblemDetails;
  }

  return null;
}

function firstFieldError(errors: Record<string, readonly string[] | string>): string | null {
  for (const value of Object.values(errors)) {
    if (Array.isArray(value) && value.length > 0) {
      return value[0];
    }

    if (typeof value === 'string' && value.trim().length > 0) {
      return value;
    }
  }

  return null;
}
