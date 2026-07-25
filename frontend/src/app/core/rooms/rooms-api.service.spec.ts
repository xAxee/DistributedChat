import { HttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';

import { APP_CONFIG } from '../config/app-config';
import { RoomsApiService } from './rooms-api.service';

describe('RoomsApiService', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('should create', () => {
    TestBed.configureTestingModule({
      providers: [
        { provide: APP_CONFIG, useValue: { apiBaseUrl: '/api' } },
        { provide: HttpClient, useValue: {} },
      ],
    });

    expect(TestBed.inject(RoomsApiService)).toBeTruthy();
  });
});
