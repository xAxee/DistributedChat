import { AsyncPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { ToastModule } from 'primeng/toast';

import { AuthService } from './core/auth/auth.service';
import { ChatRealtimeService } from './core/chat/chat-realtime.service';

@Component({
  selector: 'app-root',
  imports: [AsyncPipe, RouterLink, RouterLinkActive, RouterOutlet, ToastModule],
  templateUrl: './app.html',
  styleUrl: './app.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  private readonly authService = inject(AuthService);
  private readonly chatRealtimeService = inject(ChatRealtimeService);
  private readonly router = inject(Router);

  protected readonly currentUser$ = this.authService.currentUser$;
  protected readonly connectionState$ = this.chatRealtimeService.connectionState$;
  protected readonly isAuthPage = signal(this.isAuthRoute(this.router.url));

  constructor() {
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe((event) => this.isAuthPage.set(this.isAuthRoute(event.urlAfterRedirects)));
  }

  protected userInitial(username: string): string {
    return username.trim().charAt(0).toUpperCase() || '?';
  }

  protected logout(): void {
    void this.chatRealtimeService.disconnect().finally(() => {
      this.authService.logout();
      void this.router.navigate(['/login']);
    });
  }

  private isAuthRoute(url: string): boolean {
    const [path] = url.split(/[?#]/, 1);

    return path === '/login' || path === '/register';
  }
}
