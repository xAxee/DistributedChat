import { Routes } from '@angular/router';

import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login-page.component').then(
        (component) => component.LoginPageComponent,
      ),
    title: 'Sign in | DistributedChat',
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./features/auth/register/register-page.component').then(
        (component) => component.RegisterPageComponent,
      ),
    title: 'Create account | DistributedChat',
  },
  {
    path: 'rooms',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/rooms/rooms-page.component').then(
        (component) => component.RoomsPageComponent,
      ),
    title: 'Rooms | DistributedChat',
  },
  {
    path: 'about',
    loadComponent: () =>
      import('./features/about/about-page.component').then(
        (component) => component.AboutPageComponent,
      ),
    title: 'About | DistributedChat',
  },
  {
    path: 'rooms/:roomId',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/chat/room-chat-page.component').then(
        (component) => component.RoomChatPageComponent,
      ),
    title: 'Chat | DistributedChat',
  },
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'rooms',
  },
  {
    path: '**',
    redirectTo: 'rooms',
  },
];
