import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot, provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { authGuard, redirectIfAuthenticatedGuard } from './auth.guard';
import { AuthService } from './auth.service';

const mockSession = { user: { id: 'u1' } } as any;

function runGuard(guard: any, sessionValue: any) {
  const authStub = { session: signal(sessionValue), ready: Promise.resolve() };
  TestBed.configureTestingModule({
    providers: [provideRouter([]), { provide: AuthService, useValue: authStub }],
  });
  return TestBed.runInInjectionContext(() =>
    guard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
  );
}

describe('authGuard', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('returns true when session is active', async () => {
    const result = await runGuard(authGuard, mockSession);
    expect(result).toBe(true);
  });

  it('redirects to /login when no session', async () => {
    const result = (await runGuard(authGuard, null)) as any;
    expect(result.toString()).toBe('/login');
  });
});

describe('redirectIfAuthenticatedGuard', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('returns true when no session', async () => {
    const result = await runGuard(redirectIfAuthenticatedGuard, null);
    expect(result).toBe(true);
  });

  it('redirects to /dashboard when session is active', async () => {
    const result = (await runGuard(redirectIfAuthenticatedGuard, mockSession)) as any;
    expect(result.toString()).toBe('/dashboard');
  });
});
