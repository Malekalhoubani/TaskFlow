import { HttpErrorResponse } from "@angular/common/http";
import { Component, inject, signal } from "@angular/core";
import { FormBuilder, ReactiveFormsModule, Validators } from "@angular/forms";
import { RouterLink } from "@angular/router";
import { AuthService } from "../../../core/auth.service";

@Component({
  selector: "app-register",
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: "./register.component.html",
})
export class RegisterComponent {
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  readonly busy = signal(false);
  readonly notice = signal("");
  readonly error = signal("");

  readonly form = this.fb.nonNullable.group({
    fullName: ["", [Validators.required, Validators.minLength(2)]],
    email: ["", [Validators.required, Validators.email]],
    password: ["", [Validators.required, Validators.minLength(8)]],
    confirmPassword: ["", Validators.required],
  });

  submit(): void {
    const request = this.form.getRawValue();
    if (this.form.invalid || request.password !== request.confirmPassword) {
      this.error.set("Check the form and make sure both passwords match.");
      return;
    }

    this.busy.set(true);
    this.error.set("");
    this.auth.register(request).subscribe({
      next: (result) => {
        this.busy.set(false);
        if (result.succeeded) this.notice.set(result.message ?? "Account created. Check your email, then sign in.");
        else this.error.set(result.errors?.join(" ") || "Registration failed.");
      },
      error: (error: HttpErrorResponse) => {
        this.busy.set(false);
        this.error.set(error.error?.errors?.join(" ") || error.error?.message || "Could not connect to TaskFlow.");
      },
    });
  }
}
