import { Injectable } from '@angular/core';
import type { Assessment } from './site-data';

// Provided by Account: each cabinet instance owns its test session.
@Injectable()
export class AssessmentService {
  readonly answers = ['Ни разу', 'Несколько дней', 'Больше половины дней', 'Почти каждый день'];
  private test: Assessment | null = null;
  private index = 0;
  private total = 0;
  private finished = false;
  private needsSupport = false;

  get activeTest(): Assessment | null { return this.test; }
  get questionIndex(): number { return this.index; }
  get score(): number { return this.total; }
  get completed(): boolean { return this.finished; }
  get sensitiveAnswer(): boolean { return this.needsSupport; }

  open(test: Assessment): void {
    this.close();
    this.test = test;
  }

  answer(value: number): void {
    const question = this.test?.questions[this.index];
    if (!question || this.finished || !Number.isInteger(value) || value < 0 || value >= this.answers.length) return;

    // The rule belongs to the question, independently of its title or position.
    if (question.sensitive && value > 0) this.needsSupport = true;
    this.total += value;
    if (this.index === this.test!.questions.length - 1) this.finished = true;
    else this.index++;
  }

  close(): void {
    this.test = null;
    this.index = 0;
    this.total = 0;
    this.finished = false;
    this.needsSupport = false;
  }

  get resultLabel(): string {
    const length = this.test?.questions.length ?? 1;
    return this.total >= length * 2 ? 'Выраженные признаки' : this.total >= length ? 'Умеренные признаки' : 'Минимальные признаки';
  }
}
