# DistributedChatWeb

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 22.0.6.

## Development server

To start a local development server, run:

```bash
ng serve
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

## Application configuration

Runtime settings are stored in `public/appconfig.json`:

```json
{
  "applicationName": "DistributedChat",
  "apiBaseUrl": "/api",
  "signalRHubUrl": "/hubs/chat"
}
```

The file is loaded before Angular starts and is copied to the root of the build output. It can therefore be replaced in a deployed container without rebuilding the Angular bundle. All properties are required. The frontend does not start when the file is missing or invalid.

During `ng serve`, relative `/api`, `/health`, and `/hubs` requests are forwarded to `http://localhost:5080` using `proxy.conf.json`. Absolute service URLs can also be entered directly in `appconfig.json`.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the project run:

```bash
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Running unit tests

To execute unit tests with the [Vitest](https://vitest.dev/) test runner, use the following command:

```bash
ng test
```

## Running end-to-end tests

For end-to-end (e2e) testing, run:

```bash
ng e2e
```

Angular CLI does not come with an end-to-end testing framework by default. You can choose one that suits your needs.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
