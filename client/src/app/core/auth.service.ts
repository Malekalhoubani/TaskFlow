import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

export interface AuthResult {
  succeeded: boolean;
  message?: string;
  errors: string[];
  accessToken?: string;
  expiresAtUtc?: string;
  userId?: string;
  fullName?: string;
  email?: string;
  roles: string[];
  requiresTwoFactor: boolean;
}

export interface UserProfile {
  userId: string;
  fullName: string;
  email: string;
  emailConfirmed: boolean;
  createdAtUtc: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly api = 'https://localhost:7001/api';
  readonly profile = signal<UserProfile | null>(null);
  readonly authenticated = signal(Boolean(sessionStorage.getItem('taskflow_access_token')));

  login(request: {
    email: string;
    password: string;
    twoFactorCode?: string;
    recoveryCode?: string;
  }): Observable<AuthResult> {
    return this.http
      .post<AuthResult>(`${this.api}/auth/login`, request, {
        withCredentials: true,
      })
      .pipe(tap((result) => this.accept(result)));
  }

  register(request: {
    fullName: string;
    email: string;
    password: string;
    confirmPassword: string;
  }): Observable<AuthResult> {
    return this.http.post<AuthResult>(`${this.api}/auth/register`, request);
  }

  refresh(): Observable<AuthResult> {
    return this.http
      .post<AuthResult>(`${this.api}/auth/refresh`, {}, { withCredentials: true })
      .pipe(tap((result) => this.accept(result)));
  }

  loadProfile(): Observable<UserProfile> {
    return this.http
      .get<UserProfile>(`${this.api}/auth/profile`)
      .pipe(tap((profile) => this.profile.set(profile)));
  }

  updateProfile(fullName: string): Observable<AuthResult> {
    return this.http
      .put<AuthResult>(`${this.api}/auth/profile`, { fullName })
      .pipe(tap(() => this.loadProfile().subscribe()));
  }

  changeEmail(newEmail: string, currentPassword: string): Observable<AuthResult> {
    return this.http.post<AuthResult>(`${this.api}/auth/change-email`, {
      newEmail,
      currentPassword,
    });
  }

  changePassword(
    currentPassword: string,
    newPassword: string,
    confirmPassword: string,
  ): Observable<AuthResult> {
    return this.http.post<AuthResult>(`${this.api}/auth/change-password`, {
      currentPassword,
      newPassword,
      confirmPassword,
    });
  }

  getTwoFactorSetup(): Observable<{
    sharedKey: string;
    authenticatorUri: string;
  }> {
    return this.http.get<{ sharedKey: string; authenticatorUri: string }>(
      `${this.api}/auth/two-factor/setup`,
    );
  }

  enableTwoFactor(code: string): Observable<{ codes: string[] }> {
    return this.http.post<{ codes: string[] }>(`${this.api}/auth/two-factor/enable`, { code });
  }

  loginHistory(): Observable<
    Array<{
      succeeded: boolean;
      failureReason?: string;
      ipAddress?: string;
      userAgent?: string;
      createdAtUtc: string;
    }>
  > {
    return this.http.get<
      Array<{
        succeeded: boolean;
        failureReason?: string;
        ipAddress?: string;
        userAgent?: string;
        createdAtUtc: string;
      }>
    >(`${this.api}/security/login-history`);
  }

  external(provider: 'Google' | 'Microsoft'): void {
    window.open(`${this.api}/auth/external/${provider}`, 'taskflow-oauth', 'width=560,height=720');
  }

  logout(allDevices = false): Observable<unknown> {
    const endpoint = allDevices ? 'logout-all-devices' : 'logout';
    return this.http
      .post(`${this.api}/auth/${endpoint}`, {}, { withCredentials: true })
      .pipe(tap(() => this.clear()));
  }

  accept(result: AuthResult): void {
    if (result.succeeded && result.accessToken) {
      sessionStorage.setItem('taskflow_access_token', result.accessToken);
      this.authenticated.set(true);
    }
  }

  clear(): void {
    sessionStorage.removeItem('taskflow_access_token');
    this.profile.set(null);
    this.authenticated.set(false);
  }
}
