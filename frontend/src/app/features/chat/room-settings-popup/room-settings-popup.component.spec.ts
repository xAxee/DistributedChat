import { TestBed } from '@angular/core/testing';

import { RoomSettingsPopupComponent } from './room-settings-popup.component';

describe('RoomSettingsPopupComponent', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('should create', () => {
    TestBed.configureTestingModule({});

    const component = TestBed.runInInjectionContext(() => new RoomSettingsPopupComponent());

    expect(component).toBeTruthy();
  });
});
