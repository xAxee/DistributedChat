import { mergeApplicationConfig } from '@angular/core';
import { bootstrapApplication } from '@angular/platform-browser';

import { appConfig } from './app/app.config';
import { App } from './app/app';
import { APP_CONFIG, loadAppConfig } from './app/core/config/app-config';

loadAppConfig()
  .then((runtimeConfig) =>
    bootstrapApplication(
      App,
      mergeApplicationConfig(appConfig, {
        providers: [{ provide: APP_CONFIG, useValue: runtimeConfig }],
      }),
    ),
  )
  .catch((error: unknown) => console.error('Application startup failed.', error));
