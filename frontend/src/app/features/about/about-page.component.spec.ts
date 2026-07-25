import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { StatusApiService } from '../../core/api/status-api.service';
import { AboutPageComponent } from './about-page.component';

describe('AboutPageComponent', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('should create', () => {
    TestBed.configureTestingModule({
      providers: [
        {
          provide: StatusApiService,
          useValue: {
            getStatus: () => of(null),
            checkHealth: () => of(true),
          },
        },
      ],
    });

    const component = TestBed.runInInjectionContext(() => new AboutPageComponent());

    expect(component).toBeTruthy();
  });
});
