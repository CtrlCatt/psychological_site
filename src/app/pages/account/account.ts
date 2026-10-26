import { Component, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { assessments, type Assessment } from '../../data/site-data';
import { ResultsService, type SavedResult } from '../../data/results.service';
import { AssessmentService } from '../../data/assessment.service';
import { IconComponent } from '../../shared/icon/icon.component';

@Component({selector: 'app-account', standalone: true, imports: [IconComponent], providers: [AssessmentService], templateUrl: './account.html'})
export class Account implements OnInit {
  private readonly router = inject(Router);
  private readonly resultsService = inject(ResultsService);
  private readonly assessmentService = inject(AssessmentService);
  readonly assessments = assessments;
  readonly answers = this.assessmentService.answers;
  readonly bars = [42, 54, 48, 64, 57, 72, 78];
  testStarted = false;
  get activeTest(): Assessment | null { return this.assessmentService.activeTest; }
  get testQuestion(): number { return this.assessmentService.questionIndex; }
  get testScore(): number { return this.assessmentService.score; }
  get testResult(): boolean { return this.assessmentService.completed; }
  get sensitiveAnswer(): boolean { return this.assessmentService.sensitiveAnswer; }
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
    this.assessmentService.open(test);
    this.testStarted = false;
  }
  testAnswer(value: number): void {
    if (this.testStarted) this.assessmentService.answer(value);
  }
  closeTest(): void {
    this.assessmentService.close();
    this.testStarted = false;
  }
  get resultLabel(): string { return this.assessmentService.resultLabel; }
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
