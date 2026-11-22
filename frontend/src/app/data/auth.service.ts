import { HttpClient, HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { catchError, firstValueFrom, throwError, timeout } from 'rxjs';

export type UserRole = 'User' | 'Admin';
export interface Session {
  accessToken: string;
  expiresAt: string;
  userName: string;
  role: UserRole;
}

const SESSION_KEY = 'ryadom-auth-session';

function readSession(): Session | null {
  try {
    const saved = sessionStorage.getItem(SESSION_KEY);
    if (!saved) return null;
    const data = JSON.parse(saved) as Session;
    if (typeof data.accessToken !== 'string' || !data.accessToken ||
        typeof data.expiresAt !== 'string' || Date.parse(data.expiresAt) <= Date.now() ||
        (data.role !== 'User' && data.role !== 'Admin')) {
      sessionStorage.removeItem(SESSION_KEY);
      return null;
    }
    return data;
  } catch {
    return null;
  }
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  readonly session = signal<Session | null>(readSession());
  readonly isAuthenticated = computed(() => {
    const session = this.session();
    return !!session?.accessToken && Date.parse(session.expiresAt) > Date.now();
  });
  readonly isAdmin = computed(() => this.isAuthenticated() && this.session()?.role === 'Admin');

  token(): string | null {
    const session = this.session();
    if (!session) return null;
    if (Date.parse(session.expiresAt) <= Date.now()) {
      this.logout();
      return null;
    }
    return session.accessToken;
  }

  async login(userName: string, password: string): Promise<void> {
    const response = await firstValueFrom(this.http.post<Session>('/api/auth/login', {
      userName, password,
    }).pipe(timeout(10000)));
    this.save(response);
  }

  async register(userName: string, password: string): Promise<void> {
    const response = await firstValueFrom(this.http.post<Session>('/api/auth/register', {
      userName, password,
    }).pipe(timeout(10000)));
    this.save(response);
  }

  logout(): void {
    this.session.set(null);
    try { sessionStorage.removeItem(SESSION_KEY); } catch { /* storage unavailable */ }
  }

  private save(session: Session): void {
    this.session.set(session);
    try { sessionStorage.setItem(SESSION_KEY, JSON.stringify(session)); } catch { /* tab-only login */ }
  }
}

export const bearerInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  if (!req.url.startsWith('/api/') || req.url.startsWith('/api/auth/')) return next(req);
  const token = auth.token();
  if (!token) return next(req);
  return next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401) auth.logout();
      return throwError(() => error);
    }),
  );
};
