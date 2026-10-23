import { Component, inject } from '@angular/core';
import { AuthService } from '../../core/auth.service';

@Component({ selector: 'app-dashboard', templateUrl: './dashboard.component.html' })
export class DashboardComponent {
  readonly auth = inject(AuthService);
}
