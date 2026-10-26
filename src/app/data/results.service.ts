import { Injectable } from '@angular/core';
export interface SavedResult { title: string; score: number; questions: number; date: string; note: string; }
const STORAGE_KEY = 'ryadom-demo-results-v1';
@Injectable({providedIn:'root'})
export class ResultsService {
  getAll(): SavedResult[] {
    try { return JSON.parse(localStorage.getItem(STORAGE_KEY) ?? '[]') as SavedResult[]; } catch { return []; }
  }
  save(result: SavedResult): void {
    try { localStorage.setItem(STORAGE_KEY, JSON.stringify([result, ...this.getAll()].slice(0, 30))); } catch { /* Storage can be disabled */ }
  }
}
