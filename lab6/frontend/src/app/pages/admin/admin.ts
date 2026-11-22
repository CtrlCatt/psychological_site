import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../data/auth.service';
import { apiError, Page, RyadomApi } from '../../data/ryadom-api';

export interface PublicUser { id: string; userName: string; role: string; createdAt: string; }

@Component({ selector: 'app-admin', imports: [RouterLink], templateUrl: './admin.html' })
export class Admin implements OnInit {
  private readonly api = inject(RyadomApi);
  private readonly changeDetector = inject(ChangeDetectorRef);
  readonly auth = inject(AuthService);
  users: Page<PublicUser> | null = null;
  error = '';
  async ngOnInit() { await this.load(); }
  async load(page = 1): Promise<void> {
    this.error = '';
    try { this.users = await this.api.users(page); }
    catch (error) { this.error = apiError(error); }
    finally { this.changeDetector.markForCheck(); }
  }
}
