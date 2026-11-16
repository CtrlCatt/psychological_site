import { DatePipe, NgClass } from '@angular/common';
import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AssessmentSession } from '../../data/assessment-session';
import { apiError, AssessmentSummary, Booking, Page, RyadomApi, Slot, Specialist } from '../../data/ryadom-api';
import { IconComponent } from '../../shared/icon/icon.component';

@Component({
  selector: 'app-home',
  imports: [IconComponent, NgClass, DatePipe],
  providers: [AssessmentSession],
  templateUrl: './home.html',
})
export class Home implements OnInit {
  private readonly router = inject(Router);
  private readonly view = inject(ChangeDetectorRef);
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(RyadomApi);
  readonly quiz = inject(AssessmentSession);
  tests: AssessmentSummary[] = [];
  specialists: Page<Specialist> | null = null;
  testsError = '';
  specialistsError = '';
  readonly menuItems = [
    { id: 'tests', label: 'Тесты' },
    { id: 'specialists', label: 'Психологи' },
    { id: 'how', label: 'Как это работает' },
    { id: 'trust', label: 'О сервисе' },
  ];
  readonly steps = [
    { number: '01', title: 'Пройдите тест', description: 'Ответьте на вопросы о своём состоянии — честно и без спешки.' },
    { number: '02', title: 'Получите разбор', description: 'Посмотрите свой балл после прохождения опроса.' },
    { number: '03', title: 'Выберите поддержку', description: 'Изучите профили психологов и выберите время консультации.' },
  ];
  mobileOpen = false;
  quizOpen = false;
  booking: Specialist | null = null;
  slots: Page<Slot> | null = null;
  selectedSlot = '';
  bookingBusy = false;
  bookingError = '';
  confirmed: Booking | null = null;

  ngOnInit(): void {
    void this.loadTests();
    void this.loadSpecialists().then(() => {
      if (this.route.snapshot.fragment === 'specialists') requestAnimationFrame(() => this.scrollTo('specialists'));
    });
  }

  async loadTests(): Promise<void> {
    this.testsError = '';
    try {
      this.tests = (await this.api.assessments(1, 3)).items;
    } catch (error) {
      this.testsError = apiError(error);
    } finally {
      this.view.markForCheck();
    }
  }

  async loadSpecialists(page = 1): Promise<void> {
    this.specialistsError = '';
    try {
      this.specialists = await this.api.specialists(page);
    } catch (error) {
      this.specialistsError = apiError(error);
    } finally {
      this.view.markForCheck();
    }
  }

  navigateAccount(): void {
    void this.router.navigateByUrl('/account');
  }

  scrollTo(id: string): void {
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    this.mobileOpen = false;
  }

  startQuiz(id = 'express'): void {
    this.quizOpen = true;
    void this.quiz.open(id);
  }

  closeQuiz(): void {
    this.quizOpen = false;
    this.quiz.close();
  }

  openBooking(specialist: Specialist): void {
    this.booking = specialist;
    this.slots = null;
    this.confirmed = null;
    void this.loadSlots();
  }

  async loadSlots(page = 1): Promise<void> {
    if (!this.booking || this.bookingBusy) return;
    this.bookingBusy = true;
    this.bookingError = '';
    this.selectedSlot = '';
    try {
      this.slots = await this.api.slots(this.booking.id, page);
    } catch (error) {
      this.bookingError = apiError(error);
    } finally {
      this.bookingBusy = false;
      this.view.markForCheck();
    }
  }

  closeBooking(): void {
    this.booking = null;
  }

  async confirmBooking(): Promise<void> {
    if (!this.booking || !this.selectedSlot || this.bookingBusy) return;
    this.bookingBusy = true;
    this.bookingError = '';
    try {
      this.confirmed = await this.api.createBooking(this.booking.id, this.selectedSlot);
    } catch (error) {
      this.bookingError = apiError(error);
    } finally {
      this.bookingBusy = false;
      this.view.markForCheck();
    }
  }
}
