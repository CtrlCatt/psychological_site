import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom, timeout } from 'rxjs';

export interface Page<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
  hasNext: boolean;
}

export interface AssessmentSummary {
  id: string;
  title: string;
  label: string;
  description: string;
  time: string;
  questionCount: number;
}

export interface Assessment extends Omit<AssessmentSummary, 'questionCount'> {
  questions: { text: string }[];
  options: string[];
}

export interface AssessmentResult {
  id: string;
  assessmentId: string;
  title: string;
  score: number;
  maximum: number;
  level: 'low' | 'medium' | 'high';
  note: string;
  needsSupport: boolean;
  completedAt: string;
}

export interface Specialist {
  id: string;
  name: string;
  role: string;
  themes: string;
  experience: string;
  price: string;
  rating: string;
  image: string;
}

export interface Slot {
  id: string;
  startsAt: string;
}

export interface Booking {
  id: string;
  slotId: string;
  specialistId: string;
  specialistName: string;
  startsAt: string;
}

@Injectable({ providedIn: 'root' })
export class RyadomApi {
  private readonly http = inject(HttpClient);
  private readonly visitorHeaders = new HttpHeaders({ 'X-Visitor-Id': visitorId() });

  assessments(page = 1, pageSize = 4) {
    return firstValueFrom(this.http.get<Page<AssessmentSummary>>('/api/assessments', { params: { page, pageSize } }).pipe(timeout(10000)));
  }

  assessment(id: string) {
    return firstValueFrom(this.http.get<Assessment>('/api/assessments/' + id).pipe(timeout(10000)));
  }

  specialists(page = 1) {
    return firstValueFrom(this.http.get<Page<Specialist>>('/api/specialists', { params: { page, pageSize: 3 } }).pipe(timeout(10000)));
  }

  slots(specialistId: string, page = 1) {
    return firstValueFrom(this.http.get<Page<Slot>>('/api/specialists/' + specialistId + '/slots', { params: { page, pageSize: 6 } }).pipe(timeout(10000)));
  }

  results(page = 1) {
    return firstValueFrom(this.http.get<Page<AssessmentResult>>('/api/results', {
      headers: this.visitorHeaders,
      params: { page, pageSize: 3 },
    }).pipe(timeout(10000)));
  }

  submit(assessmentId: string, answers: number[]) {
    return firstValueFrom(this.http.post<AssessmentResult>(
      '/api/results', { assessmentId, answers }, { headers: this.visitorHeaders },
    ).pipe(timeout(10000)));
  }

  deleteResult(id: string) {
    return firstValueFrom(this.http.delete<void>('/api/results/' + id, { headers: this.visitorHeaders }).pipe(timeout(10000)));
  }

  bookings(page = 1) {
    return firstValueFrom(this.http.get<Page<Booking>>('/api/bookings', {
      headers: this.visitorHeaders,
      params: { page, pageSize: 3 },
    }).pipe(timeout(10000)));
  }

  createBooking(specialistId: string, slotId: string) {
    return firstValueFrom(this.http.post<Booking>(
      '/api/bookings', { specialistId, slotId }, { headers: this.visitorHeaders },
    ).pipe(timeout(10000)));
  }

  reschedule(id: string, slotId: string) {
    return firstValueFrom(this.http.put<Booking>(
      '/api/bookings/' + id, { slotId }, { headers: this.visitorHeaders },
    ).pipe(timeout(10000)));
  }

  deleteBooking(id: string) {
    return firstValueFrom(this.http.delete<void>('/api/bookings/' + id, { headers: this.visitorHeaders }).pipe(timeout(10000)));
  }
}

function visitorId(): string {
  const key = 'ryadom-visitor-id';
  const freshId = crypto.randomUUID();
  try {
    const saved = localStorage.getItem(key);
    if (saved && /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/.test(saved)) return saved;
    localStorage.setItem(key, freshId);
  } catch {
    // Use the same ID until this tab reloads if the browser blocks storage.
  }
  return freshId;
}

export function apiError(error: unknown): string {
  if (error instanceof HttpErrorResponse && typeof error.error?.detail === 'string') return error.error.detail;
  if (error instanceof HttpErrorResponse && error.status === 400) return 'Сервер отклонил данные запроса.';
  return 'Не удалось выполнить запрос. Проверьте, что API запущен, и повторите.';
}
