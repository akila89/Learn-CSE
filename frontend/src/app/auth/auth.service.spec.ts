import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Router } from '@angular/router';
import { vi } from 'vitest';
import { AuthService } from './auth.service';
import { SUPABASE_CLIENT } from '../supabase.client';

const mockSession = { user: { id: 'u1' } } as any;

function makeSupabaseMock(overrides: Record<string, any> = {}) {
  return {
    auth: {
      getSession: vi.fn().mockResolvedValue({ data: { session: null } }),
      onAuthStateChange: vi
        .fn()
        .mockReturnValue({ data: { subscription: { unsubscribe: vi.fn() } } }),
      signInWithPassword: vi.fn().mockResolvedValue({ error: null }),
      signOut: vi.fn().mockResolvedValue({}),
      ...overrides,
    },
  };
}

describe('AuthService', () => {
  let service: AuthService;
  let router: Router;
  let supabase: ReturnType<typeof makeSupabaseMock>;

  beforeEach(() => {
    supabase = makeSupabaseMock();
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: SUPABASE_CLIENT, useValue: supabase }],
    });
    service = TestBed.inject(AuthService);
    router = TestBed.inject(Router);
  });

  it('initialises session to null when no active session', async () => {
    await TestBed.flushEffects?.();
    expect(service.session()).toBeNull();
  });

  it('sets session from getSession on construction', async () => {
    supabase.auth.getSession.mockResolvedValue({ data: { session: mockSession } });
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: SUPABASE_CLIENT, useValue: supabase }],
    });
    const svc = TestBed.inject(AuthService);
    await new Promise((r) => setTimeout(r, 0));
    expect(svc.session()).toEqual(mockSession);
  });

  it('updates session when onAuthStateChange fires', () => {
    let changeCallback: (event: string, session: any) => void = () => {};
    supabase.auth.onAuthStateChange.mockImplementation((cb: any) => {
      changeCallback = cb;
      return { data: { subscription: { unsubscribe: vi.fn() } } };
    });
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: SUPABASE_CLIENT, useValue: supabase }],
    });
    const svc = TestBed.inject(AuthService);
    changeCallback('SIGNED_IN', mockSession);
    expect(svc.session()).toEqual(mockSession);
  });

  it('signIn navigates to /dashboard on success', async () => {
    const spy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    const result = await service.signIn('a@b.com', 'pass');
    expect(result).toBeNull();
    expect(spy).toHaveBeenCalledWith(['/dashboard']);
  });

  it('signIn returns error message on failure', async () => {
    supabase.auth.signInWithPassword.mockResolvedValue({
      error: { message: 'Invalid credentials' },
    });
    const spy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    const result = await service.signIn('a@b.com', 'wrong');
    expect(result).toBe('Invalid credentials');
    expect(spy).not.toHaveBeenCalled();
  });

  it('signOut navigates to /login', async () => {
    const spy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    await service.signOut();
    expect(supabase.auth.signOut).toHaveBeenCalled();
    expect(spy).toHaveBeenCalledWith(['/login']);
  });
});
