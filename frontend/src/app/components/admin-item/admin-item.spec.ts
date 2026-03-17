import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdminItem } from './admin-item';

describe('AdminItem', () => {
  let component: AdminItem;
  let fixture: ComponentFixture<AdminItem>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminItem],
    }).compileComponents();

    fixture = TestBed.createComponent(AdminItem);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
