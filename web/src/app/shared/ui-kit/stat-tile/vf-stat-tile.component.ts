import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Params, RouterLink } from '@angular/router';

/** Severity of a tile. Never the only carrier of meaning — see the component notes. */
export type VfStatTileTone = 'neutral' | 'warning' | 'danger';

/**
 * A clickable labelled number (STD-UX-127, registered in `docs/ui/components.md`
 * 2026-08-03 with owner approval).
 *
 * **The whole tile is one link.** Not the number, not a trailing chevron — the tile, so the
 * touch target clears 44×44 on every tier (§14) and the keyboard reaches it once rather than
 * three times. It is a real anchor, so `Enter` opens it and the browser's own affordances
 * (focus ring, middle-click, status bar) come for free.
 *
 * **Meaning never rides on colour alone** (§11, §14): a tone always arrives with an icon and
 * a label, so the tile reads identically in greyscale.
 *
 * **No trend arrow, no percentage delta, no comparison with yesterday.** Those answer «how are
 * things going?»; this component exists on a board that answers «what needs my attention right
 * now?» (BR-DSH-017, DEC-DSH-004).
 */
@Component({
  selector: 'vf-stat-tile',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  template: `
    <a
      class="tile tile--{{ tone() }}"
      [routerLink]="routerLink()"
      [queryParams]="queryParams()"
      [attr.aria-label]="ariaLabel()"
    >
      <span class="tile-label">
        <i class="pi {{ icon() }} tile-icon" aria-hidden="true"></i>
        {{ label() }}
      </span>
      <span class="tile-value vf-num">{{ value() }}</span>
      @if (caption(); as captionText) {
        <span class="tile-caption">{{ captionText }}</span>
      }
      <span class="tile-action">
        {{ actionLabel() }}
        <i class="pi pi-angle-left tile-chevron" aria-hidden="true"></i>
      </span>
    </a>
  `,
  styles: `
    .tile {
      display: flex;
      flex-direction: column;
      gap: var(--vf-space-2);
      /* Comfortably past the 44px floor even before content (§14). */
      min-block-size: 44px;
      padding: var(--vf-space-4);
      background: var(--vf-surface);
      border: 1px solid var(--vf-border);
      border-radius: var(--vf-radius);
      color: inherit;
      text-decoration: none;
      block-size: 100%;
    }

    .tile:hover {
      border-color: var(--vf-text-faint);
    }

    /* Never removed, never delayed (§13, §14). */
    .tile:focus-visible {
      outline: 2px solid var(--vf-primary);
      outline-offset: 2px;
    }

    .tile-label {
      display: flex;
      align-items: center;
      gap: var(--vf-space-2);
      font-size: 0.875rem;
      color: var(--vf-text-muted);
    }

    .tile-icon {
      font-size: 1rem;
    }

    /* The one focal element of the tile (§4). */
    .tile-value {
      font-size: 1.75rem;
      font-weight: 700;
      line-height: 1.1;
      color: var(--vf-text);
    }

    .tile-caption {
      font-size: 0.8125rem;
      color: var(--vf-text-muted);
    }

    .tile-action {
      display: flex;
      align-items: center;
      gap: var(--vf-space-1);
      margin-block-start: auto;
      padding-block-start: var(--vf-space-2);
      font-size: 0.8125rem;
      color: var(--vf-primary);
    }

    /* Direction is mirrored in RTL (§12): the chevron points the way reading goes. */
    .tile-chevron {
      font-size: 0.75rem;
    }

    /* Tone is a support, never the sole signal — the icon and label already say it. */
    .tile--warning .tile-icon {
      color: var(--vf-warning);
    }

    .tile--danger .tile-icon {
      color: var(--vf-danger);
    }

    .tile--danger .tile-value {
      color: var(--vf-danger);
    }
  `,
})
export class VfStatTileComponent {
  readonly label = input.required<string>();

  /** Pre-formatted for display — the caller owns formatting (`FormatService`, STD-FE-042). */
  readonly value = input.required<string>();

  readonly icon = input('pi-info-circle');

  readonly tone = input<VfStatTileTone>('neutral');

  /** Optional second line, e.g. the money figure under today's invoice count. */
  readonly caption = input<string | null>(null);

  readonly routerLink = input.required<string>();

  /**
   * The destination's filter. Every value passed here comes from an approved whitelist
   * (BR-INV-014 / BR-INV-035 / BR-PUR-004 / BR-SAL-019) — the tile never invents a filter.
   */
  readonly queryParams = input<Params | null>(null);

  readonly actionLabel = input.required<string>();

  /**
   * Spoken name: the label and the value together, so the tile is not announced as a bare
   * number with no subject (§14).
   */
  readonly ariaLabel = input<string | null>(null);
}
