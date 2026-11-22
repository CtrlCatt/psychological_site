import { Routes } from '@angular/router';
import { adminGuard, userGuard } from './data/auth.guards';
export const routes: Routes = [
  { path: '', loadComponent: () => import('./pages/home/home').then(m => m.Home), title: 'Рядом — главная' },
  { path: 'login', loadComponent: () => import('./pages/login/login').then(m => m.Login), title: 'Вход — Рядом' },
  { path: 'account', canActivate: [userGuard], loadComponent: () => import('./pages/account/account').then(m => m.Account), title: 'Личный кабинет — Рядом' },
  { path: 'admin', canActivate: [adminGuard], loadComponent: () => import('./pages/admin/admin').then(m => m.Admin), title: 'Администратор — Рядом' },
  { path: '**', redirectTo: '' },
];
