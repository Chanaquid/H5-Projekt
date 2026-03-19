import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SessionToast } from './session-toast';

describe('SessionToast', () => {
  let component: SessionToast;
  let fixture: ComponentFixture<SessionToast>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SessionToast],
    }).compileComponents();

    fixture = TestBed.createComponent(SessionToast);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
