import '@angular/compiler';

import { TestBed } from '@angular/core/testing';
import { BrowserTestingModule, platformBrowserTesting } from '@angular/platform-browser/testing';

const testEnvironmentKey = Symbol.for('distributed-chat.angular-test-environment');
const testEnvironmentState = globalThis as unknown as Record<symbol, boolean | undefined>;

if (!testEnvironmentState[testEnvironmentKey]) {
  TestBed.initTestEnvironment(BrowserTestingModule, platformBrowserTesting());
  testEnvironmentState[testEnvironmentKey] = true;
}
