import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Dispute } from './dispute';

describe('Dispute', () => {
  let component: Dispute;
  let fixture: ComponentFixture<Dispute>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Dispute],
    }).compileComponents();

    fixture = TestBed.createComponent(Dispute);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
