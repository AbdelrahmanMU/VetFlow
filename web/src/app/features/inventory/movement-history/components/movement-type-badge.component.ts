import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';

import { MessageKey } from '../../../../core/i18n/ar';
import { TranslationService } from '../../../../core/i18n/translation.service';
import { VfBadgeComponent } from '../../../../shared/ui-kit/badge/vf-badge.component';
import { MovementType } from '../movement-history.models';

type Tone = 'success' | 'warning' | 'danger' | 'neutral';

interface TypeStyle {
  readonly labelKey: MessageKey;
  readonly tone: Tone;
  readonly icon: string;
}

/**
 * Movement type badge (inventory ui.md) — text + colour + icon, never colour alone (design
 * language §6, §11). The map covers exactly the closed set of BR-INV-065: no type is styled that
 * has no writing path, and none is missing.
 *
 * Tone follows the direction of the movement, which is what the reader is scanning for: stock in
 * is success, stock out is warning, and a write-off — stock destroyed — is danger.
 */
const TYPE_STYLES: Readonly<Record<MovementType, TypeStyle>> = {
  receive: { labelKey: 'history.type.receive', tone: 'success', icon: 'pi-download' },
  consume: { labelKey: 'history.type.consume', tone: 'warning', icon: 'pi-shopping-cart' },
  adjustment: { labelKey: 'history.type.adjustment', tone: 'neutral', icon: 'pi-sliders-h' },
  writeOff: { labelKey: 'history.type.writeOff', tone: 'danger', icon: 'pi-trash' },
  purchaseReturn: { labelKey: 'history.type.purchaseReturn', tone: 'warning', icon: 'pi-reply' },
  salesReturn: { labelKey: 'history.type.salesReturn', tone: 'success', icon: 'pi-replay' },
};

@Component({
  selector: 'app-movement-type-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [VfBadgeComponent],
  template: `
    <vf-badge [tone]="style().tone" [icon]="style().icon">{{ t.t(style().labelKey) }}</vf-badge>
  `,
})
export class MovementTypeBadgeComponent {
  protected readonly t = inject(TranslationService);

  readonly type = input.required<MovementType>();

  protected readonly style = computed<TypeStyle>(() => TYPE_STYLES[this.type()]);
}
