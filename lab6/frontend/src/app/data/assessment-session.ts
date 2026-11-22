import { inject, Injectable, signal } from '@angular/core';
import { apiError, Assessment, AssessmentResult, RyadomApi } from './ryadom-api';

@Injectable()
export class AssessmentSession {
  private readonly api = inject(RyadomApi);
  private answers: number[] = [];

  private readonly current = signal<Assessment | null>(null);
  get assessment(): Assessment | null { return this.current(); }
  private readonly completed = signal<AssessmentResult | null>(null);
  get result(): AssessmentResult | null { return this.completed(); }
  private readonly fetching = signal(false);
  get loading(): boolean { return this.fetching(); }
  private readonly sending = signal(false);
  get submitting(): boolean { return this.sending(); }
  private readonly failure = signal('');
  get error(): string { return this.failure(); }

  get index(): number {
    return this.answers.length;
  }

  get readyToSubmit(): boolean {
    return this.assessment !== null && this.index === this.assessment.questions.length;
  }

  async open(id: string): Promise<void> {
    if (this.loading || this.submitting) return;
    this.close();
    this.fetching.set(true);
    try {
      this.current.set(await this.api.assessment(id));
    } catch (error) {
      this.failure.set(apiError(error));
    } finally {
      this.fetching.set(false);
    }
  }

  answer(value: number): void {
    if (this.assessment && !this.readyToSubmit) this.answers.push(value);
  }

  async submit(): Promise<void> {
    if (!this.assessment || !this.readyToSubmit || this.submitting || this.result) return;
    this.sending.set(true);
    this.failure.set('');
    try {
      this.completed.set(await this.api.submit(this.assessment.id, this.answers));
    } catch (error) {
      this.failure.set(apiError(error));
    } finally {
      this.sending.set(false);
    }
  }

  close(): void {
    this.current.set(null);
    this.completed.set(null);
    this.answers = [];
    this.failure.set('');
  }
}
