import { DOCUMENT, Injectable, inject } from '@angular/core';

/**
 * The ValidationFocusService (validation-and-guidance.md §8): after a
 * rejected submit, focus moves to the first invalid control in DOM order,
 * scrolled into view with headroom (STD-UX-070); an operation banner with no
 * field target receives focus itself (STD-UX-071). No other scroll-jacking
 * (STD-UX-075).
 *
 * Before focusing, a `vf-reveal-request` event bubbles from the target so a
 * containing tab, accordion, or collapsed section can open itself first
 * (STD-UX-074) — the forward hook; no such container exists today.
 */
const INVALID_TARGET_SELECTOR = [
  '.vf-field--invalid input',
  '.vf-field--invalid textarea',
  '.vf-field--invalid [role="combobox"]',
  '.vf-field--invalid select',
].join(', ');

@Injectable({ providedIn: 'root' })
export class ValidationFocusService {
  private readonly document = inject(DOCUMENT);

  /** Focus the first invalid control under `root`; false when none is rendered. */
  focusFirstInvalid(root: HTMLElement): boolean {
    const target = root.querySelector<HTMLElement>(INVALID_TARGET_SELECTOR);
    if (!target) {
      return false;
    }

    this.reveal(target);
    return true;
  }

  /** Focus a control by its `vf-form-field` control id — the summary's navigation (STD-UX-076). */
  focusControlId(controlId: string): boolean {
    const target = this.document.getElementById(controlId);
    if (!target) {
      return false;
    }

    this.reveal(target);
    return true;
  }

  /** Focus an operation-level message element (it must carry `tabindex="-1"`, STD-UX-071). */
  focusMessage(element: HTMLElement): void {
    this.reveal(element);
  }

  private reveal(element: HTMLElement): void {
    element.dispatchEvent(new CustomEvent('vf-reveal-request', { bubbles: true }));
    // `scroll-margin` on the target provides the headroom (STD-UX-070);
    // jsdom has no scrollIntoView, hence the guard.
    if (typeof element.scrollIntoView === 'function') {
      element.scrollIntoView({ block: 'center' });
    }

    element.focus({ preventScroll: true });
  }
}
