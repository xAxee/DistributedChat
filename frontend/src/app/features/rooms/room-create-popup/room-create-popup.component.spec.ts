import { TestBed } from '@angular/core/testing';

import { RoomCreatePopupComponent } from './room-create-popup.component';

describe('RoomCreatePopupComponent', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('should create', () => {
    TestBed.configureTestingModule({});

    const component = TestBed.runInInjectionContext(() => new RoomCreatePopupComponent());

    expect(component).toBeTruthy();
  });
});
