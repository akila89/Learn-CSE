import { test, expect } from '@playwright/test';

// These tests run against the real app with a real Supabase session.
// Set E2E_EMAIL and E2E_PASSWORD env vars to a valid test account.
const EMAIL = process.env['E2E_EMAIL'] ?? '';
const PASSWORD = process.env['E2E_PASSWORD'] ?? '';

test.describe('Auth — unauthenticated', () => {
  test('visiting /dashboard redirects to /login', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page).toHaveURL('/login');
  });

  test('visiting / redirects to /login', async ({ page }) => {
    await page.goto('/');
    await expect(page).toHaveURL('/login');
  });

  test('login page renders email and password fields', async ({ page }) => {
    await page.goto('/login');
    await expect(page.getByLabel('Email')).toBeVisible();
    await expect(page.getByLabel('Password')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Sign In' })).toBeVisible();
  });

  test('invalid credentials show inline error', async ({ page }) => {
    await page.goto('/login');
    await page.getByLabel('Email').fill('wrong@example.com');
    await page.getByLabel('Password').fill('wrongpassword');
    await page.getByRole('button', { name: 'Sign In' }).click();
    await expect(page.locator('.error-msg')).toBeVisible();
  });
});

async function signIn(page: import('@playwright/test').Page) {
  await page.goto('/login');
  await page.getByLabel('Email').fill(EMAIL);
  await page.getByLabel('Password').fill(PASSWORD);
  await page.getByRole('button', { name: 'Sign In' }).click();
  await expect(page).toHaveURL('/dashboard');
}

test.describe('Auth — authenticated', () => {
  test.skip(!EMAIL || !PASSWORD, 'E2E_EMAIL and E2E_PASSWORD not set');

  test('valid credentials navigate to /dashboard', async ({ page }) => {
    await signIn(page);
  });

  test('visiting /login when authenticated redirects to /dashboard', async ({ page }) => {
    await signIn(page);
    await page.goto('/login');
    await expect(page).toHaveURL('/dashboard');
  });

  test('sign out redirects to /login', async ({ page }) => {
    await signIn(page);
    await page.getByRole('button', { name: 'Sign out' }).click();
    await expect(page).toHaveURL('/login');
  });
});
