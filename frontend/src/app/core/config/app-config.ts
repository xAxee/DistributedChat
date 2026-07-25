import { InjectionToken } from '@angular/core';

export interface AppConfig {
  readonly applicationName: string;
  readonly apiBaseUrl: string;
  readonly signalRHubUrl: string;
}

export const APP_CONFIG = new InjectionToken<AppConfig>('APP_CONFIG');

export async function loadAppConfig(
  configUrl = 'appconfig.json',
  fetchConfig: typeof fetch = fetch,
): Promise<AppConfig> {
  const response = await fetchConfig(configUrl, { cache: 'no-store' });
  if (!response.ok) {
    throw new Error(`Could not load application configuration (${response.status}).`);
  }

  return parseAppConfig(await response.json());
}

function parseAppConfig(value: unknown): AppConfig {
  if (!isRecord(value)) {
    throw new Error('Application configuration must be a JSON object.');
  }

  return {
    applicationName: readRequiredString(value, 'applicationName'),
    apiBaseUrl: normalizeUrl(readRequiredString(value, 'apiBaseUrl')),
    signalRHubUrl: normalizeUrl(readRequiredString(value, 'signalRHubUrl')),
  };
}

function readRequiredString(config: Record<string, unknown>, property: keyof AppConfig): string {
  const value = config[property];
  if (typeof value !== 'string' || value.trim().length === 0) {
    throw new Error(`Application configuration property "${property}" must be a non-empty string.`);
  }

  return value.trim();
}

function normalizeUrl(value: string): string {
  return value === '/' ? value : value.replace(/\/+$/, '');
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}
