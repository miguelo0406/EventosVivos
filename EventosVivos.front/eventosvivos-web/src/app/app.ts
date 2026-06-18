import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

// Raíz de la aplicación: solo monta el <router-outlet />. Toda la composición (rutas,
// HttpClient, interceptor, locale) vive en app.config.ts, no aquí (SRP): este componente
// no tiene más responsabilidad que servir de punto de anclaje del enrutador.
@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  template: '<router-outlet />',
})
export class App {}
