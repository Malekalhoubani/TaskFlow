import { CommonModule } from "@angular/common";
import { HttpErrorResponse } from "@angular/common/http";
import { Component, inject, signal } from "@angular/core";
import { FormBuilder, ReactiveFormsModule, Validators } from "@angular/forms";
import { Router } from "@angular/router";
import { AuthService } from "../../core/auth.service";

interface LoginActivity {
  succeeded: boolean;
  failureReason?: string;
  ipAddress?: string;
  userAgent?: string;
  createdAtUtc: string;
}

@Component({
  selector: "app-security",
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: "./security.component.html",
})
export class SecurityComponent {
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  readonly history = signal<LoginActivity[]>([]);
  readonly setupKey = signal("");
  readonly recoveryCodes = signal<string[]>([]);
  readonly notice = signal("");
  readonly error = signal("");
  readonly twoFactorForm = this.fb.nonNullable.group({ code: ["", Validators.required] });

  constructor() {
    this.auth.loginHistory().subscribe({ next: (history) => this.history.set(history), error: (error) => this.showError(error) });
  }

  setupTwoFactor(): void {
    this.auth.getTwoFactorSetup().subscribe({ next: (setup) => this.setupKey.set(setup.sharedKey), error: (error) => this.showError(error) });
  }

  enableTwoFactor(): void {
    if (this.twoFactorForm.invalid) return;
    this.auth.enableTwoFactor(this.twoFactorForm.controls.code.value).subscribe({
      next: (result) => {
        this.recoveryCodes.set(result.codes);
        this.notice.set("Two-factor authentication is active. Save your recovery codes.");
      },
      error: (error) => this.showError(error),
    });
  }

  signOutEverywhere(): void {
    this.auth.logout(true).subscribe(() => void this.router.navigateByUrl("/login"));
  }

  private showError(error: HttpErrorResponse): void {
    this.error.set(error.error?.errors?.join(" ") || error.error?.message || "Could not load security settings.");
  }
}
