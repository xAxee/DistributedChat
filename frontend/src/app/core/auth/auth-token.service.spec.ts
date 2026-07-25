import { TestBed } from '@angular/core/testing';

import { AuthTokenService } from './auth-token.service';

describe('AuthTokenService', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('should create', () => {
    expect(TestBed.inject(AuthTokenService)).toBeTruthy();
  });
});
