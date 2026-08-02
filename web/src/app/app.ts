import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

/**
 * The application root renders the routed screen and nothing else. The shell used to sit here, but
 * the login screen is outside it — there is no navigation to show someone who is not signed in
 * (identity/ui.md S1). The shell is now the layout component of the guarded branch in
 * `app.routes.ts`, so every business screen still renders inside it exactly as before.
 */
@Component({
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet],
  template: '<router-outlet />',
})
export class App {}
