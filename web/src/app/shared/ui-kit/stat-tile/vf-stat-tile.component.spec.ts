import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { VfStatTileComponent } from './vf-stat-tile.component';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [VfStatTileComponent],
  template: `
    <vf-stat-tile
      label="دفعات منتهية الصلاحية"
      value="٧"
      tone="danger"
      icon="pi-ban"
      routerLink="/inventory/expiry"
      [queryParams]="{ expired: 'true' }"
      actionLabel="عرض"
      ariaLabel="دفعات منتهية الصلاحية: ٧"
    />
  `,
})
class HostComponent {}

describe('VfStatTileComponent', () => {
  function render(): HTMLAnchorElement {
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    return fixture.nativeElement.querySelector('a.tile') as HTMLAnchorElement;
  }

  it('renders the whole tile as one link, so the hit target is the tile (§14)', () => {
    const tile = render();

    expect(tile).not.toBeNull();
    expect(tile.getAttribute('href')).toBe('/inventory/expiry?expired=true');
  });

  it('carries the filter into the destination, so the screen shows what was counted (BR-DSH-018)', () => {
    expect(render().getAttribute('href')).toContain('expired=true');
  });

  it('never lets colour alone carry the meaning (§11, §14)', () => {
    const tile = render();

    // The tone is present as a class, but the icon and the label are there too — the tile
    // reads identically in greyscale.
    expect(tile.className).toContain('tile--danger');
    expect(tile.querySelector('.tile-icon')?.className).toContain('pi-ban');
    expect(tile.querySelector('.tile-label')?.textContent).toContain('دفعات منتهية الصلاحية');
  });

  it('is announced with its subject, not as a bare number (§14)', () => {
    expect(render().getAttribute('aria-label')).toBe('دفعات منتهية الصلاحية: ٧');
  });

  it('shows no trend arrow or comparison — that is a statistics board (BR-DSH-017)', () => {
    const tile = render();

    expect(tile.querySelector('.tile-trend')).toBeNull();
    expect(tile.textContent).not.toContain('%');
  });
});
