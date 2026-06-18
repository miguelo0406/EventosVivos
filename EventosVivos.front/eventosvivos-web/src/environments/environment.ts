// El navegador siempre habla con la API por el puerto publicado en el host (8081),
// tanto en `ng serve` como con el front dockerizado. Para producción (Azure) se
// reemplaza este archivo vía fileReplacements en angular.json.
export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:8081/api',
};
