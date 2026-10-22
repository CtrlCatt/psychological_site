import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { NgClass } from '@angular/common';
import { IconComponent } from '../../shared/icon/icon.component';
import { questions, tests, specialists } from '../../data/site-data';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [IconComponent, NgClass],
  templateUrl: './home.html',
})
export class Home implements OnInit {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  readonly questions = questions;
  readonly tests = tests;
  readonly specialists = specialists;
  readonly quizAnswers = ['Совсем нет', 'Несколько дней', 'Часто', 'Почти каждый день'];
  readonly menuItems = [
    { id: 'tests', label: 'Тесты' },
    { id: 'specialists', label: 'Психологи' },
    { id: 'how', label: 'Как это работает' },
    { id: 'trust', label: 'О сервисе' },
  ];
  readonly steps = [
    { number: '01', title: 'Пройдите тест', description: 'Ответьте на вопросы о своём состоянии — честно и без спешки.' },
    { number: '02', title: 'Получите разбор', description: 'Увидите понятную оценку и рекомендации без пугающих ярлыков.' },
    { number: '03', title: 'Выберите поддержку', description: 'Изучите профили подходящих психологов и запишитесь онлайн.' },
  ];
  readonly bookingTimes = ['Сегодня 19:00', 'Завтра 12:30', 'Пт, 18:00'];
  mobileOpen = false;
  quizOpen = false;
  question = 0;
  score = 0;
  result = false;
  booking: string | null = null;
  selectedBookingTime: string | null = null;
  bookingConfirmed = false;

  ngOnInit(): void {
    this.route.fragment.subscribe((fragment: string | null) => {
      if (fragment) setTimeout(() => this.scrollTo(fragment), 50);
    });
  }

  navigateAccount(): void { void this.router.navigateByUrl('/account'); }
  scrollTo(id: string): void {
    document.getElementById(id)?.scrollIntoView({behavior: 'smooth', block: 'start'});
    this.mobileOpen = false;
  }
  startQuiz(): void {
    this.quizOpen = true;
    this.question = 0;
    this.score = 0;
    this.result = false;
  }
  answer(value: number): void {
    this.score += value;
    if (this.question === this.questions.length - 1) this.result = true;
    else this.question++;
  }
  closeQuiz(): void { this.quizOpen = false; }
  quizFindSpecialist(): void { this.closeQuiz(); this.scrollTo('specialists'); }
  openBooking(name: string): void {
    this.booking = name;
    this.selectedBookingTime = null;
    this.bookingConfirmed = false;
  }
  closeBooking(): void { this.booking = null; }
  confirmBooking(): void {
    if (this.selectedBookingTime) this.bookingConfirmed = true;
  }
}
