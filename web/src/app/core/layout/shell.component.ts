import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { TranslationService } from '../i18n/translation.service';

/**
 * The application shell (design language §5): the navigation sidebar sits on
 * the right — where Arabic reading starts — calm, without color blocks; the
 * active module is marked by a thin accent. Cross-module top-bar concerns
 * (global search, notifications, account) arrive with their own slices.
 */
@Component({
  selector: 'app-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="shell">
      <nav class="sidebar" [attr.aria-label]="t.t('nav.catalog')">
        <div class="brand">{{ t.t('app.name') }}</div>
        <ul class="nav-list">
          <li class="nav-module">{{ t.t('nav.catalog') }}</li>
          <li>
            <a
              class="nav-link"
              routerLink="/catalog/products"
              routerLinkActive="nav-link--active"
              [routerLinkActiveOptions]="{ exact: false }"
            >
              <i class="pi pi-box nav-icon" aria-hidden="true"></i>
              {{ t.t('nav.products') }}
            </a>
          </li>
          <li>
            <a
              class="nav-link"
              routerLink="/categories"
              routerLinkActive="nav-link--active"
              [routerLinkActiveOptions]="{ exact: false }"
            >
              <i class="pi pi-tags nav-icon" aria-hidden="true"></i>
              {{ t.t('nav.categories') }}
            </a>
          </li>
          <li>
            <a
              class="nav-link"
              routerLink="/manufacturers"
              routerLinkActive="nav-link--active"
              [routerLinkActiveOptions]="{ exact: false }"
            >
              <i class="pi pi-building nav-icon" aria-hidden="true"></i>
              {{ t.t('nav.manufacturers') }}
            </a>
          </li>
        </ul>
      </nav>
      <main class="content">
        <router-outlet />
      </main>
    </div>
  `,
  styles: `
    /* Grid columns follow the inline direction: in RTL the first column is the
       right edge — where the navigation lives (design language §5). */
    .shell {
      display: grid;
      grid-template-columns: var(--vf-sidebar-width) 1fr;
      grid-template-areas: 'sidebar content';
      min-block-size: 100dvh;
    }

    .sidebar {
      grid-area: sidebar;
      background: var(--vf-surface);
      border-inline-start: 1px solid var(--vf-border);
      padding: var(--vf-space-5) var(--vf-space-4);
    }

    .brand {
      font-size: var(--vf-text-section-title);
      font-weight: 700;
      color: var(--vf-primary);
      margin-block-end: var(--vf-space-6);
      padding-inline: var(--vf-space-2);
    }

    .nav-list {
      list-style: none;
      margin: 0;
      padding: 0;
      display: flex;
      flex-direction: column;
      gap: var(--vf-space-1);
    }

    .nav-module {
      font-size: var(--vf-text-caption);
      color: var(--vf-text-faint);
      padding-inline: var(--vf-space-2);
      margin-block-end: var(--vf-space-1);
    }

    .nav-link {
      display: flex;
      align-items: center;
      gap: var(--vf-space-2);
      padding: var(--vf-space-2) var(--vf-space-2);
      border-radius: var(--vf-radius-small);
      color: var(--vf-text-secondary);
      text-decoration: none;
      border-inline-start: 2px solid transparent;
    }

    .nav-link:hover {
      background: var(--vf-bg);
      color: var(--vf-text);
    }

    .nav-link--active {
      color: var(--vf-primary-strong);
      background: var(--vf-primary-soft);
      border-inline-start-color: var(--vf-primary);
      font-weight: 600;
    }

    .nav-icon {
      font-size: 1rem;
    }

    .content {
      grid-area: content;
      min-inline-size: 0;
      display: flex;
      flex-direction: column;
    }

    @media (max-width: 768px) {
      .shell {
        grid-template-columns: 1fr;
        grid-template-areas: 'content';
      }

      .sidebar {
        display: none;
      }
    }
  `,
})
export class ShellComponent {
  protected readonly t = inject(TranslationService);
}
