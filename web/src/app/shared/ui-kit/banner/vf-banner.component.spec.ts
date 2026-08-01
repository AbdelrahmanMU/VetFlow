import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { VfBannerComponent, VfBannerTone } from './vf-banner.component';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [VfBannerComponent],
  template: `<vf-banner [tone]="tone">الرسالة</vf-banner>`,
})
class HostComponent {
  tone: VfBannerTone = 'error';
}

/**
 * The shared operation-message surface (STD-UX-062/071/092): tone classes on
 * the standard tokens, focusable, and the right live semantics per tone.
 */
describe('VfBannerComponent', () => {
  function render(tone: VfBannerTone): HTMLElement {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.componentInstance.tone = tone;
    fixture.detectChanges();
    const banner = (fixture.nativeElement as HTMLElement).querySelector('vf-banner');
    if (!banner) {
      throw new Error('banner not rendered');
    }

    return banner as HTMLElement;
  }

  it('projects its content and is focusable (STD-UX-071)', () => {
    const banner = render('error');
    expect(banner.textContent).toContain('الرسالة');
    expect(banner.getAttribute('tabindex')).toBe('-1');
  });

  it('an error banner alerts; success and warning announce politely (STD-UX-092)', () => {
    expect(render('error').getAttribute('role')).toBe('alert');
    expect(render('success').getAttribute('role')).toBe('status');
    expect(render('warning').getAttribute('role')).toBe('status');
  });

  it('carries exactly its tone class', () => {
    const banner = render('success');
    expect(banner.classList.contains('vf-banner--success')).toBe(true);
    expect(banner.classList.contains('vf-banner--error')).toBe(false);
  });
});
