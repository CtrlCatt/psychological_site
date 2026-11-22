import { DatePipe } from '@angular/common';
import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../data/auth.service';
import { AssessmentSession } from '../../data/assessment-session';
import { apiError, AssessmentResult, AssessmentSummary, Booking, Page, RyadomApi, Slot } from '../../data/ryadom-api';
import { IconComponent } from '../../shared/icon/icon.component';

@Component({
  selector: 'app-account',
  imports: [IconComponent, DatePipe],
  providers: [AssessmentSession],
  templateUrl: './account.html',
})
export class Account implements OnInit {
  private readonly router = inject(Router);
  private readonly view = inject(ChangeDetectorRef);
  private readonly api = inject(RyadomApi);
  readonly test = inject(AssessmentSession);
  readonly auth = inject(AuthService);
  assessments: Page<AssessmentSummary> | null = null;
  history: Page<AssessmentResult> | null = null;
  bookings: Page<Booking> | null = null;
  slots: Page<Slot> | null = null;
  editing: Booking | null = null;
  testOpen = false;
  testStarted = false;
  helpOpen = false;
  testsError = '';
  historyError = '';
  bookingError = '';
  editError = '';
  busy = false;

  ngOnInit(): void {
    void this.loadTests();
    void this.loadHistory();
    void this.loadBookings();
  }

  async loadTests(page = 1): Promise<void> {
    this.testsError = '';
    try {
      this.assessments = await this.api.assessments(page);
    } catch (error) {
      this.testsError = apiError(error);
    } finally {
      this.view.markForCheck();
    }
  }

  async loadHistory(page = 1): Promise<void> {
    this.historyError = '';
    try {
      this.history = await this.api.results(page);
    } catch (error) {
      this.historyError = apiError(error);
    } finally {
      this.view.markForCheck();
    }
  }

  async loadBookings(page = 1): Promise<void> {
    this.bookingError = '';
    try {
      this.bookings = await this.api.bookings(page);
    } catch (error) {
      this.bookingError = apiError(error);
    } finally {
      this.view.markForCheck();
    }
  }

  logout(): void {
    this.auth.logout();
    void this.router.navigateByUrl('/login');
  }

  navigateAdmin(): void { void this.router.navigateByUrl('/admin'); }

  navigateHome(): void {
    void this.router.navigate(['/']);
  }

  findPsychologist(): void {
    void this.router.navigate(['/'], { fragment: 'specialists' });
  }

  scrollTo(id: string): void {
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth' });
  }

  openTest(id: string): void {
    this.testOpen = true;
    this.testStarted = false;
    void this.test.open(id);
  }

  closeTest(): void {
    this.testOpen = false;
    this.test.close();
  }

  async submit(): Promise<void> {
    await this.test.submit();
    if (this.test.result) void this.loadHistory();
  }

  async deleteResult(id: string): Promise<void> {
    if (this.busy) return;
    this.busy = true;
    try {
      await this.api.deleteResult(id);
      await this.loadHistory(this.history && this.history.items.length === 1 ? Math.max(1, this.history.page - 1) : this.history?.page);
    } catch (error) {
      this.historyError = apiError(error);
    } finally {
      this.busy = false;
      this.view.markForCheck();
    }
  }

  async cancelBooking(id: string): Promise<void> {
    if (this.busy) return;
    this.busy = true;
    try {
      await this.api.deleteBooking(id);
      await this.loadBookings(this.bookings && this.bookings.items.length === 1 ? Math.max(1, this.bookings.page - 1) : this.bookings?.page);
    } catch (error) {
      this.bookingError = apiError(error);
    } finally {
      this.busy = false;
      this.view.markForCheck();
    }
  }

  openEdit(booking: Booking): void {
    this.editing = booking;
    this.slots = null;
    void this.loadSlots();
  }

  async loadSlots(page = 1): Promise<void> {
    if (!this.editing || this.busy) return;
    this.busy = true;
    this.editError = '';
    try {
      this.slots = await this.api.slots(this.editing.specialistId, page);
    } catch (error) {
      this.editError = apiError(error);
    } finally {
      this.busy = false;
      this.view.markForCheck();
    }
  }

  async reschedule(slotId: string): Promise<void> {
    if (!this.editing || this.busy) return;
    this.busy = true;
    this.editError = '';
    try {
      await this.api.reschedule(this.editing.id, slotId);
      this.editing = null;
      await this.loadBookings();
    } catch (error) {
      this.editError = apiError(error);
    } finally {
      this.busy = false;
      this.view.markForCheck();
    }
  }
}
