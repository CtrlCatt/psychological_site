import { HttpClient, HttpErrorResponse } from '@angular/common/http';
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
    return firstValueFrom(this.http.get<Page<Slot>>('/api/specialists/' + specialistId + '/slots', { params: { page, pageSize: 10 } }).pipe(timeout(10000)));
  }

  results(page = 1) {
    return firstValueFrom(this.http.get<Page<AssessmentResult>>('/api/results', {
      params: { page, pageSize: 10 },
    }).pipe(timeout(10000)));
  }

  submit(assessmentId: string, answers: number[]) {
    return firstValueFrom(this.http.post<AssessmentResult>(
      '/api/results', { assessmentId, answers },
    ).pipe(timeout(10000)));
  }

  deleteResult(id: string) {
    return firstValueFrom(this.http.delete<void>('/api/results/' + id).pipe(timeout(10000)));
  }

  bookings(page = 1) {
    return firstValueFrom(this.http.get<Page<Booking>>('/api/bookings', {
      params: { page, pageSize: 10 },
    }).pipe(timeout(10000)));
  }

  createBooking(specialistId: string, slotId: string) {
    return firstValueFrom(this.http.post<Booking>(
      '/api/bookings', { specialistId, slotId },
    ).pipe(timeout(10000)));
  }

  reschedule(id: string, slotId: string) {
    return firstValueFrom(this.http.put<Booking>(
      '/api/bookings/' + id, { slotId },
    ).pipe(timeout(10000)));
  }

  users(page = 1) {
    return firstValueFrom(this.http.get<Page<{ id: string; userName: string; role: string; createdAt: string }>>(
      '/api/admin/users', { params: { page, pageSize: 10 } },
    ).pipe(timeout(10000)));
  }

  deleteBooking(id: string) {
    return firstValueFrom(this.http.delete<void>('/api/bookings/' + id).pipe(timeout(10000)));
  }
}

export function apiError(error: unknown): string {
  if (error instanceof HttpErrorResponse && typeof error.error?.detail === 'string') return error.error.detail;
  if (error instanceof HttpErrorResponse && error.status === 401) return 'Нужно войти в аккаунт или заново авторизоваться.';
  if (error instanceof HttpErrorResponse && error.status === 403) return 'У вас нет доступа к этому разделу.';
  if (error instanceof HttpErrorResponse && error.status === 409) return 'Этот логин уже занят или запись конфликтует с существующей.';
  if (error instanceof HttpErrorResponse && error.status === 400) return 'Сервер отклонил данные запроса.';
  return 'Не удалось выполнить запрос. Проверьте, что API запущен, и повторите.';
}
