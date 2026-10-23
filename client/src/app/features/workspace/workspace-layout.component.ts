import { Component, inject } from "@angular/core";
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from "@angular/router";
import { AuthService } from "../../core/auth.service";

@Component({
  selector: "app-workspace-layout",
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: "./workspace-layout.component.html",
})
export class WorkspaceLayoutComponent {
  readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  constructor() {
    this.auth.loadProfile().subscribe({ error: () => this.leaveWorkspace() });
  }

  signOut(allDevices = false): void {
    this.auth.logout(allDevices).subscribe(() => void this.router.navigateByUrl("/login"));
  }

  private leaveWorkspace(): void {
    this.auth.clear();
    void this.router.navigateByUrl("/login");
  }
}
