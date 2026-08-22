import { Injectable } from '@angular/core';

const DASH_PREFIXES = ['/admin', '/clinic', '/doctor', '/staff'];
const CONTENT_SELECTOR = 'main.content-body';
const SIDEBAR_SELECTOR = '.sidebar';
const PROGRESS_ID = 'spa-progress-bar';
const NAVIGATED_EVENT = 'spa:navigated';

interface SpaState {
  spa?: boolean;
}

@Injectable({ providedIn: 'root' })
export class SpaNavigationService {
  private readonly chromeScriptKeys = new Set<string>();
  private readonly provisionalChromeKeys = new Set<string>();
  private readonly loadedExternalSrcs = new Set<string>();
  private navigating = false;
  private pendingUrl: string | null = null;

  start(): void {
    this.fingerprintChrome();
    this.ensureProgressBar();
    document.addEventListener('click', this.onClick, true);
    window.addEventListener('popstate', this.onPopState);
  }

  private fingerprintChrome(): void {
    document.querySelectorAll('script').forEach((el) => {
      const src = el.getAttribute('src');
      if (src) {
        this.loadedExternalSrcs.add(this.abs(src));
        this.chromeScriptKeys.add('ext:' + this.abs(src));
      } else {
        const key = 'inline:' + (el.textContent ?? '').trim();
        this.chromeScriptKeys.add(key);
        this.provisionalChromeKeys.add(key);
      }
    });
  }

  private reconcileChrome(doc: Document): void {
    const docInlineKeys = new Set<string>();
    doc.querySelectorAll('script').forEach((el) => {
      if (!el.getAttribute('src')) {
        docInlineKeys.add('inline:' + (el.textContent ?? '').trim());
      }
    });

    for (const key of Array.from(this.provisionalChromeKeys)) {
      if (!docInlineKeys.has(key)) {
        this.provisionalChromeKeys.delete(key);
        this.chromeScriptKeys.delete(key);
      }
    }
  }

  private readonly onClick = (event: MouseEvent): void => {
    if (event.defaultPrevented || event.button !== 0) return;
    if (event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;

    const anchor = (event.target as HTMLElement | null)?.closest?.('a');
    if (!anchor) return;

    const href = anchor.getAttribute('href');
    if (!href || href === '#') return;
    if (anchor.getAttribute('target') && anchor.getAttribute('target') !== '_self') return;
    if (anchor.hasAttribute('download')) return;
    const rel = anchor.getAttribute('rel') ?? '';
    if (rel.split(/\s+/).includes('external')) return;
    if (/^(javascript:|mailto:|tel:)/i.test(href)) return;

    const url = new URL(anchor.href, location.href);
    if (url.origin !== location.origin) return;
    if (!DASH_PREFIXES.some((p) => url.pathname.toLowerCase().startsWith(p))) return;
    if (url.pathname === location.pathname && !!url.hash) return;

    event.preventDefault();
    void this.navigate(url.pathname + url.search, true);
  };

  private readonly onPopState = (): void => {
    void this.navigate(location.pathname + location.search, false);
  };

  private async navigate(url: string, push: boolean): Promise<void> {
    if (this.navigating) {
      this.pendingUrl = url;
      return;
    }
    this.navigating = true;
    this.showProgress();

    try {
      const res = await fetch(url, {
        headers: { 'X-Spa-Navigation': '1' },
        credentials: 'same-origin',
      });
      if (!res.ok) throw new Error('HTTP ' + res.status);

      const html = await res.text();
      const doc = new DOMParser().parseFromString(html, 'text/html');

      const newContent = doc.querySelector(CONTENT_SELECTOR);
      const currentContent = document.querySelector(CONTENT_SELECTOR);
      if (!newContent || !currentContent) throw new Error('content region missing');

      currentContent.innerHTML = newContent.innerHTML;

      const newSidebar = doc.querySelector(SIDEBAR_SELECTOR);
      const currentSidebar = document.querySelector(SIDEBAR_SELECTOR);
      if (newSidebar && currentSidebar) {
        currentSidebar.replaceWith(newSidebar.cloneNode(true));
      }

      if (doc.title) document.title = doc.title;

      this.reconcileChrome(doc);
      this.executePageScripts(doc);

      if (push) {
        const state: SpaState = { spa: true };
        history.pushState(state, '', url);
      }

      window.scrollTo(0, 0);
      document.dispatchEvent(new CustomEvent(NAVIGATED_EVENT, { detail: { url } }));
    } catch {
      window.location.href = url;
      return;
    } finally {
      this.hideProgress();
      this.navigating = false;
      if (this.pendingUrl) {
        const next = this.pendingUrl;
        this.pendingUrl = null;
        void this.navigate(next, true);
      }
    }
  }

  private executePageScripts(doc: Document): void {
    const scripts = Array.from(doc.querySelectorAll('script'));

    for (const original of scripts) {
      const src = original.getAttribute('src');
      if (src) {
        const abs = this.abs(src);
        const key = 'ext:' + abs;
        if (this.chromeScriptKeys.has(key)) continue;
        if (this.loadedExternalSrcs.has(abs)) continue;

        this.loadedExternalSrcs.add(abs);
        const el = document.createElement('script');
        el.src = abs;
        el.async = false;
        if (original.hasAttribute('defer')) el.defer = true;
        el.onerror = () => console.error('[spa-shell] failed to load script: ' + abs);
        document.body.appendChild(el);
      } else {
        const key = 'inline:' + (original.textContent ?? '').trim();
        if (this.chromeScriptKeys.has(key)) continue;

        const el = document.createElement('script');
        el.textContent = original.textContent;
        document.body.appendChild(el);
      }
    }
  }

  private abs(src: string): string {
    try {
      return new URL(src, location.href).href;
    } catch {
      return src;
    }
  }

  private ensureProgressBar(): void {
    if (document.getElementById(PROGRESS_ID)) return;
    const style = document.createElement('style');
    style.textContent =
      '#' + PROGRESS_ID +
      '{position:fixed;top:0;left:0;right:0;height:3px;width:0;background:#2F9CCA;' +
      'z-index:99999;transition:width .25s ease,opacity .3s ease;opacity:0;pointer-events:none}';
    document.head.appendChild(style);

    const bar = document.createElement('div');
    bar.id = PROGRESS_ID;
    document.body.appendChild(bar);
  }

  private showProgress(): void {
    const bar = document.getElementById(PROGRESS_ID);
    if (!bar) return;
    bar.style.opacity = '1';
    bar.style.width = '70%';
  }

  private hideProgress(): void {
    const bar = document.getElementById(PROGRESS_ID);
    if (!bar) return;
    bar.style.width = '100%';
    setTimeout(() => {
      bar.style.opacity = '0';
      bar.style.width = '0';
    }, 250);
  }
}
