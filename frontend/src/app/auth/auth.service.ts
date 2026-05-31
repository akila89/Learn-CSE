import { inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Session } from '@supabase/supabase-js';
import { SUPABASE_CLIENT } from '../supabase.client';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly supabase = inject(SUPABASE_CLIENT);
  private readonly router = inject(Router);

  readonly session = signal<Session | null>(null);

  // Resolves once the initial getSession() call completes — guards await this
  // before reading session() to avoid a race on page load.
  readonly ready: Promise<void>;

  constructor() {
    this.ready = this.supabase.auth
      .getSession()
      .then(({ data }) => this.session.set(data.session))
      .catch(() => this.session.set(null));

    this.supabase.auth.onAuthStateChange((_event, session) => {
      this.session.set(session);
    });
  }

  async signIn(email: string, password: string): Promise<string | null> {
    const { error } = await this.supabase.auth.signInWithPassword({ email, password });
    if (error) return error.message;
    await this.router.navigate(['/dashboard']);
    return null;
  }

  async signOut(): Promise<void> {
    await this.supabase.auth.signOut();
    await this.router.navigate(['/login']);
  }
}
