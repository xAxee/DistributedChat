import { HttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';

import { APP_CONFIG, AppConfig } from '../config/app-config';
import { StatusApiService } from './status-api.service';

const config: AppConfig = {
  applicationName: 'DistributedChat',
  apiBaseUrl: '/api',
  signalRHubUrl: '/hubs/chat',
};

describe('StatusApiService', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('should create', (http) => {
    TestBed.configureTestingModule({
      providers: [
        { provide: APP_CONFIG, useValue: config },
        { provide: HttpClient, useValue: http },
      ],
    });

    expect(TestBed.inject(StatusApiService)).toBeTruthy();
  });

});
