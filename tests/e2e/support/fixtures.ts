import { test as base, expect, type Page } from '@playwright/test';

export interface HealthIssue {
  readonly kind: 'console' | 'pageerror' | 'requestfailed' | 'http5xx' | 'http4xx';
  readonly text: string;
}

export interface Health {
  /** Everything observed so far (console errors, page errors, failed requests, 4xx/5xx API responses). */
  readonly issues: HealthIssue[];
  /**
   * Asserts there is no page error, no failed request, no 5xx and no console
   * error, except API 4xx responses whose `METHOD /path status` matches one of
   * `expected4xx` (and the browser's matching "Failed to load resource" line).
   */
  expectClean(expected4xx?: readonly RegExp[]): void;
}

function attach(page: Page, issues: HealthIssue[]): void {
  page.on('console', (msg) => {
    if (msg.type() === 'error') issues.push({ kind: 'console', text: msg.text() });
  });
  page.on('pageerror', (err) => issues.push({ kind: 'pageerror', text: String(err) }));
  page.on('requestfailed', (req) => {
    const failure = req.failure()?.errorText ?? '';
    // Navigations/aborted object-URL loads cancelled by the app are not defects.
    if (/ERR_ABORTED|NS_BINDING_ABORTED/.test(failure)) return;
    issues.push({ kind: 'requestfailed', text: `${req.method()} ${req.url()} ${failure}` });
  });
  page.on('response', (res) => {
    const url = new URL(res.url());
    if (!url.pathname.startsWith('/api/')) return;
    const line = `${res.request().method()} ${url.pathname} ${res.status()}`;
    if (res.status() >= 500) issues.push({ kind: 'http5xx', text: line });
    else if (res.status() >= 400) issues.push({ kind: 'http4xx', text: line });
  });
}

export const test = base.extend<{ health: Health }>({
  health: async ({ page }, use) => {
    const issues: HealthIssue[] = [];
    attach(page, issues);
    await use({
      issues,
      expectClean(expected4xx = []) {
        const allowedHttp = issues.filter((i) => i.kind === 'http4xx' && expected4xx.some((re) => re.test(i.text)));
        const unexpected = issues.filter((i) => {
          if (i.kind === 'http4xx') return !allowedHttp.includes(i);
          if (i.kind === 'console' && /Failed to load resource: the server responded with a status of 4\d\d/.test(i.text)) {
            // Chrome logs every 4xx fetch; accepted only when an expected 4xx API response was seen.
            return allowedHttp.length === 0;
          }
          return true;
        });
        expect(unexpected, `unexpected console/network issues:\n${unexpected.map((u) => `${u.kind}: ${u.text}`).join('\n')}`).toEqual([]);
      },
    });
  },
});

export { expect };
