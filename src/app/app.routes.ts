import { Routes } from '@angular/router';
export const routes: Routes = [
  {path: '', loadComponent: () => import('./pages/home/home').then(m => m.Home), title: 'Рядом — главная'},
  {path: 'account', loadComponent: () => import('./pages/account/account').then(m => m.Account), title: 'Личный кабинет — Рядом'},
  {path: '**', redirectTo: ''},
];
