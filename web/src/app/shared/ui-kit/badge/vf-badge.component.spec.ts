import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { VfBadgeComponent } from './vf-badge.component';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [VfBadgeComponent],
  template: '<vf-badge tone="warning" icon="pi-exclamation-triangle">بلا سعر</vf-badge>',
})
class HostComponent {}

describe('VfBadgeComponent', () => {
  it('always renders text plus icon — color never carries the meaning alone', async () => {
    await TestBed.configureTestingModule({ imports: [HostComponent] }).compileComponents();
    const fixture = TestBed.createComponent(HostComponent);
    await fixture.whenStable();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('.badge')?.textContent).toContain('بلا سعر');
    expect(element.querySelector('.pi-exclamation-triangle')).toBeTruthy();
    expect(element.querySelector('.badge--warning')).toBeTruthy();
  });
});
