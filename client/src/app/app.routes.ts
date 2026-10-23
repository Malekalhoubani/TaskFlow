import { Routes } from "@angular/router";
import { authGuard } from "./core/auth.guard";
import { LoginComponent } from "./features/auth/login/login.component";
import { RegisterComponent } from "./features/auth/register/register.component";
import { DashboardComponent } from "./features/dashboard/dashboard.component";
import { ProfileComponent } from "./features/profile/profile.component";
import { SecurityComponent } from "./features/security/security.component";
import { WorkspaceLayoutComponent } from "./features/workspace/workspace-layout.component";

export const routes: Routes = [
  { path: "login", component: LoginComponent },
  { path: "register", component: RegisterComponent },
  {
    path: "",
    component: WorkspaceLayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: "dashboard", component: DashboardComponent },
      { path: "profile", component: ProfileComponent },
      { path: "security", component: SecurityComponent },
      { path: "", pathMatch: "full", redirectTo: "dashboard" },
    ],
  },
  { path: "**", redirectTo: "" },
];
