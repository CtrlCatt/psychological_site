import { Component, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { assessments, type Assessment } from '../../data/site-data';
import { ResultsService, type SavedResult } from '../../data/results.service';
import { IconComponent } from '../../shared/icon/icon.component';

@Component({selector: 'app-account', standalone: true, imports: [IconComponent], templateUrl: './account.html'})
export class Account implements OnInit {
  private readonly router = inject(Router);
  private readonly resultsService = inject(ResultsService);
  readonly assessments = assessments;
  readonly answers = ['Ни разу', 'Несколько дней', 'Больше половины дней', 'Почти каждый день'];
  readonly bars = [42, 54, 48, 64, 57, 72, 78];
  activeTest: Assessment | null = null;
  testStarted = false;
  testQuestion = 0;
  testScore = 0;
  testResult = false;
  sensitiveAnswer = false;
  savedResults: SavedResult[] = [];
  showAll = false;
  helpOpen = false;
  callInfoOpen = false;

  ngOnInit(): void { this.reloadHistory(); }
  private reloadHistory(): void { this.savedResults = this.resultsService.getAll(); }
  navigateHome(): void { void this.router.navigate(['/']); }
  findPsychologist(): void {
    this.closeTest();
    void this.router.navigate(['/'], {fragment: 'specialists'});
  }
  scrollTo(id: string): void { document.getElementById(id)?.scrollIntoView({behavior:'smooth'}); }
  openTest(test: Assessment): void {
    this.activeTest = test;
    this.testStarted = false;
    this.testQuestion = 0;
    this.testScore = 0;
    this.testResult = false;
    this.sensitiveAnswer = false;
  }
  testAnswer(value: number): void {
    const active = this.activeTest;
    if (!active) return;
    if (active.title.includes('PHQ-9') && this.testQuestion === active.questions.length - 1 && value > 0) this.sensitiveAnswer = true;
    this.testScore += value;
    if (this.testQuestion === active.questions.length - 1) this.testResult = true;
    else this.testQuestion++;
  }
  closeTest(): void { this.activeTest = null; this.testStarted = false; }
  get resultLabel(): string {
    const length = this.activeTest?.questions.length ?? 1;
    return this.testScore >= length * 2 ? 'Выраженные признаки' : this.testScore >= length ? 'Умеренные признаки' : 'Минимальные признаки';
  }
  saveResult(): void {
    const test = this.activeTest;
    if (!test || !this.testResult) return;
    this.resultsService.save({title:test.title, score:this.testScore, questions:test.questions.length, date:new Date().toLocaleDateString('ru-RU'), note:this.resultLabel});
    this.reloadHistory();
    this.closeTest();
    setTimeout(() => this.scrollTo('history'), 0);
  }
  get visibleResults(): SavedResult[] { return this.showAll ? this.savedResults : this.savedResults.slice(0, 3); }
}
