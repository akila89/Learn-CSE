import { Component, inject } from '@angular/core';
import { AuthService } from '../auth/auth.service';

@Component({
  selector: 'app-dashboard',
  template: `
    <div style="padding: 2rem; font-family: sans-serif;">
      <h1>Dashboard</h1>
      <p>Watchlist coming soon.</p>
      <button (click)="auth.signOut()">Sign out</button>
    </div>
  `,
})
export class Dashboard {
  readonly auth = inject(AuthService);
}
