import { createApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { SpaNavigationService } from './app/spa-navigation.service';

createApplication(appConfig)
  .then((appRef) => {
    const injector = appRef.injector;
    injector.get(SpaNavigationService).start();
  })
  .catch((err) => console.error('[spa-shell] bootstrap failed', err));
