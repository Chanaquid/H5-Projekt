import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdminLoan } from './admin-loan';

describe('AdminLoan', () => {
  let component: AdminLoan;
  let fixture: ComponentFixture<AdminLoan>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminLoan],
    }).compileComponents();

    fixture = TestBed.createComponent(AdminLoan);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
