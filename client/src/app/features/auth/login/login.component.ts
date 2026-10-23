import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnDestroy, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthResult, AuthService } from '../../../core/auth.service';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html',
})
export class LoginComponent implements OnDestroy {
  readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  readonly busy = signal(false);
  readonly error = signal('');

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    twoFactorCode: [''],
  });

  private readonly externalLoginHandler = (event: MessageEvent) => {
    if (event.origin !== 'https://localhost:7001' || event.data?.type !== 'taskflow-auth') return;
    this.auth.accept(event.data.payload as AuthResult);
    void this.router.navigateByUrl('/dashboard');
  };

  constructor() {
    window.addEventListener('message', this.externalLoginHandler);
    if (this.auth.authenticated()) void this.router.navigateByUrl('/dashboard');
  }

  ngOnDestroy(): void {
    window.removeEventListener('message', this.externalLoginHandler);
  }

  submit(): void {
    if (this.form.invalid) {
      this.error.set('Please complete your sign-in details.');
      return;
    }

    this.busy.set(true);
    this.error.set('');
    this.auth.login(this.form.getRawValue()).subscribe({
      next: (result) => {
        this.busy.set(false);
        if (result.succeeded) void this.router.navigateByUrl('/dashboard');
        else this.error.set(result.errors?.join(' ') || 'Sign in failed.');
      },
      error: (error: HttpErrorResponse) => {
        this.busy.set(false);
        this.error.set(
          error.error?.errors?.join(' ') ||
            error.error?.message ||
            'Could not connect to TaskFlow.',
        );
      },
    });
  }
}
