import { HttpErrorResponse } from "@angular/common/http";
import { Component, inject, signal } from "@angular/core";
import { FormBuilder, ReactiveFormsModule, Validators } from "@angular/forms";
import { AuthResult, AuthService } from "../../core/auth.service";

@Component({
  selector: "app-profile",
  imports: [ReactiveFormsModule],
  templateUrl: "./profile.component.html",
})
export class ProfileComponent {
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  readonly notice = signal("");
  readonly error = signal("");

  readonly profileForm = this.fb.nonNullable.group({ fullName: ["", Validators.required] });
  readonly emailForm = this.fb.nonNullable.group({ newEmail: ["", [Validators.required, Validators.email]], currentPassword: ["", Validators.required] });
  readonly passwordForm = this.fb.nonNullable.group({ currentPassword: ["", Validators.required], newPassword: ["", [Validators.required, Validators.minLength(8)]], confirmPassword: ["", Validators.required] });

  constructor() {
    this.auth.loadProfile().subscribe((profile) => this.profileForm.patchValue({ fullName: profile.fullName }));
  }

  saveProfile(): void {
    if (this.profileForm.invalid) return;
    this.auth.updateProfile(this.profileForm.controls.fullName.value).subscribe({ next: (result) => this.show(result), error: (error) => this.showError(error) });
  }

  changeEmail(): void {
    if (this.emailForm.invalid) return;
    const value = this.emailForm.getRawValue();
    this.auth.changeEmail(value.newEmail, value.currentPassword).subscribe({ next: (result) => this.show(result), error: (error) => this.showError(error) });
  }

  changePassword(): void {
    const value = this.passwordForm.getRawValue();
    if (this.passwordForm.invalid || value.newPassword !== value.confirmPassword) {
      this.error.set("New passwords do not match.");
      return;
    }
    this.auth.changePassword(value.currentPassword, value.newPassword, value.confirmPassword).subscribe({ next: (result) => this.show(result), error: (error) => this.showError(error) });
  }

  private show(result: AuthResult): void {
    this.error.set("");
    result.succeeded ? this.notice.set(result.message ?? "Saved.") : this.notice.set("");
    if (!result.succeeded) this.error.set(result.errors?.join(" ") || "Could not save changes.");
  }

  private showError(error: HttpErrorResponse): void {
    this.notice.set("");
    this.error.set(error.error?.errors?.join(" ") || error.error?.message || "Could not save changes.");
  }
}
