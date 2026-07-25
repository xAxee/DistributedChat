import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { ErrorNotificationService } from '../../core/notifications/error-notification.service';
import { RoomsApiService } from '../../core/rooms/rooms-api.service';
import { RoomsPageComponent } from './rooms-page.component';

describe('RoomsPageComponent', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('should create', () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: RoomsApiService, useValue: { getRooms: () => of([]) } },
        { provide: ErrorNotificationService, useValue: { show: vi.fn() } },
      ],
    });

    const component = TestBed.runInInjectionContext(() => new RoomsPageComponent());

    expect(component).toBeTruthy();
  });
});
