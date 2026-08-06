import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * The single section container (STD-UX-127, registered in `docs/ui/components.md`
 * 2026-08-03 with owner approval).
 *
 * A grouping device, **not decoration** (design language §5): a thin border, no heavy
 * shadow, and **no coloured background** (§11 — «لا خلفيات ملونة للبطاقات»). Separation is
 * by space first; a card is the last resort, never space *and* rule *and* card together
 * (§15.4).
 *
 * It exists as a UI-Kit component rather than a local dashboard element because §17 forbids
 * a module patching a gap in the design language itself: «الانحراف البصريّ يبدأ دائمًا
 * باستثناء صغير محلّيّ لمرّة واحدة».
 */
@Component({
  selector: 'vf-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="card">
      @if (heading(); as title) {
        <header class="card-header">
          @if (headingLevel() === 'h2') {
            <h2 class="card-heading">{{ title }}</h2>
          } @else {
            <h3 class="card-heading">{{ title }}</h3>
          }
          <div class="card-actions">
            <ng-content select="[card-actions]" />
          </div>
        </header>
      }
      <div class="card-body">
        <ng-content />
      </div>
    </section>
  `,
  styles: `
    .card {
      display: flex;
      flex-direction: column;
      gap: var(--vf-space-3);
      padding: var(--vf-space-5);
      background: var(--vf-surface);
      border: 1px solid var(--vf-border);
      border-radius: var(--vf-radius);
      block-size: 100%;
    }

    .card-header {
      display: flex;
      align-items: baseline;
      justify-content: space-between;
      gap: var(--vf-space-3);
    }

    /* Weight carries the hierarchy, not colour or size alone (§10). */
    .card-heading {
      margin: 0;
      font-size: 1rem;
      font-weight: 600;
      color: var(--vf-text);
    }

    .card-actions {
      display: flex;
      gap: var(--vf-space-2);
      font-size: 0.875rem;
    }

    .card-body {
      display: flex;
      flex-direction: column;
      gap: var(--vf-space-3);
    }
  `,
})
export class VfCardComponent {
  /** Optional section title. Omitted when the surrounding page already names the group. */
  readonly heading = input<string | null>(null);

  /**
   * Which heading element to render, so a page keeps one sensible outline for assistive
   * technology (§14) instead of every card claiming the same level.
   */
  readonly headingLevel = input<'h2' | 'h3'>('h2');
}
