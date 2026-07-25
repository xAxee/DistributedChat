export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  code?: string;
  errors?: Record<string, readonly string[] | string>;
  traceId?: string;
  correlationId?: string;
}

export interface CursorPagedResponse<T> {
  items: readonly T[];
  nextCursor: string | null;
  hasMore: boolean;
}
