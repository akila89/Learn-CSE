import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { vi } from 'vitest';
import { Login } from './login';
import { AuthService } from '../auth.service';

function makeAuthStub(signInResult: string | null = null) {
  return { signIn: vi.fn().mockResolvedValue(signInResult) };
}

describe('Login', () => {
  let fixture: ComponentFixture<Login>;
  let component: Login;
  let authStub: ReturnType<typeof makeAuthStub>;

  beforeEach(async () => {
    authStub = makeAuthStub();
    await TestBed.configureTestingModule({
      imports: [Login],
      providers: [provideRouter([]), { provide: AuthService, useValue: authStub }],
    }).compileComponents();
    fixture = TestBed.createComponent(Login);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('renders email and password inputs', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('input[type="email"]')).toBeTruthy();
    expect(el.querySelector('input[type="password"]')).toBeTruthy();
  });

  it('calls signIn with entered credentials on submit', async () => {
    component.email = 'a@b.com';
    component.password = 'secret';
    await component.onSubmit();
    expect(authStub.signIn).toHaveBeenCalledWith('a@b.com', 'secret');
  });

  it('shows error message when signIn returns an error', async () => {
    authStub.signIn.mockResolvedValue('Invalid login credentials');
    component.email = 'a@b.com';
    component.password = 'wrong';
    await component.onSubmit();
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('.error-msg')?.textContent).toContain('Invalid login credentials');
  });

  it('shows no error message on successful login', async () => {
    await component.onSubmit();
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('.error-msg')).toBeNull();
  });

  it('does not submit again while loading', async () => {
    component.loading.set(true);
    await component.onSubmit();
    expect(authStub.signIn).not.toHaveBeenCalled();
  });

  it('resets loading to false after submit completes', async () => {
    await component.onSubmit();
    expect(component.loading()).toBe(false);
  });
});
