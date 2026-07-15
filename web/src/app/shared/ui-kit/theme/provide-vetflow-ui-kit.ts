import { EnvironmentProviders } from '@angular/core';
import { providePrimeNG } from 'primeng/config';

import { vetflowPreset } from './vetflow-preset';

/**
 * UI Kit bootstrap: the only provider the application registers. The
 * component foundation and its theme stay an implementation detail of the
 * kit (ADR-0012, STD-FE-003).
 */
export function provideVetFlowUiKit(): EnvironmentProviders {
  return providePrimeNG({
    ripple: false,
    theme: {
      preset: vetflowPreset,
      options: {
        darkModeSelector: 'none',
      },
    },
  });
}
