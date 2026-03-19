import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ResolutionCenter } from './resolution-center';

describe('ResolutionCenter', () => {
  let component: ResolutionCenter;
  let fixture: ComponentFixture<ResolutionCenter>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ResolutionCenter],
    }).compileComponents();

    fixture = TestBed.createComponent(ResolutionCenter);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
