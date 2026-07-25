import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { catchError, finalize, forkJoin, of } from 'rxjs';

import { StatusApiService } from '../../core/api/status-api.service';
import { ApplicationStatusResponse } from '../../core/models/status.models';

@Component({
  selector: 'app-about-page',
  imports: [DatePipe],
  templateUrl: './about-page.component.html',
  styleUrl: './about-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AboutPageComponent implements OnInit {
  private readonly statusApi = inject(StatusApiService);
  protected readonly systemStatus = signal<ApplicationStatusResponse | null>(null);
  protected readonly liveHealthy = signal<boolean | null>(null);
  protected readonly readyHealthy = signal<boolean | null>(null);
  protected readonly loadingStatus = signal(false);

  ngOnInit(): void {
    this.loadSystemStatus();
  }

  protected formatUptime(totalSeconds: number): string {
    const days = Math.floor(totalSeconds / 86_400);
    const hours = Math.floor((totalSeconds % 86_400) / 3_600);
    const minutes = Math.floor((totalSeconds % 3_600) / 60);
    return days > 0 ? `${days}d ${hours}h` : hours > 0 ? `${hours}h ${minutes}m` : `${minutes}m`;
  }

  protected loadSystemStatus(): void {
    this.loadingStatus.set(true);
    forkJoin({
      status: this.statusApi.getStatus().pipe(catchError(() => of(null))),
      live: this.statusApi.checkHealth('live').pipe(catchError(() => of(false))),
      ready: this.statusApi.checkHealth('ready').pipe(catchError(() => of(false))),
    })
      .pipe(finalize(() => this.loadingStatus.set(false)))
      .subscribe(({ status, live, ready }) => {
        this.systemStatus.set(status);
        this.liveHealthy.set(live);
        this.readyHealthy.set(ready);
      });
  }
}
