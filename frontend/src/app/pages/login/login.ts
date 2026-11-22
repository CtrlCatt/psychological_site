import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../data/auth.service';
import { apiError } from '../../data/ryadom-api';

@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink],
  templateUrl: './login.html',
})
export class Login {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly auth = inject(AuthService);
  userName = '';
  password = '';
  registerMode = false;
  error = '';
  busy = false;

  async submit(): Promise<void> {
    if (this.busy) return;
    this.busy = true;
    this.error = '';
    try {
      if (this.registerMode) await this.auth.register(this.userName, this.password);
      else await this.auth.login(this.userName, this.password);
      const next = this.route.snapshot.queryParamMap.get('next');
      // Защита от открытого redirect: только относительные локальные маршруты.
      const destination = next?.startsWith('/') && !next.startsWith('//') ? next : '/account';
      await this.router.navigateByUrl(destination);
    } catch (error) {
      this.error = apiError(error);
    } finally {
      this.busy = false;
    }
  }
}
