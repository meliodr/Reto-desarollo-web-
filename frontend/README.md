# SISPLAN · Frontend del primer incremento

Aplicación Angular 22 para formular y publicar el PEI, registrar ejes, objetivos e indicadores y consultar los POA generados. El flujo de revisión requiere acceso institucional local. [Guía del incremento](../docs/plan-sisplan/06-PRIMER-INCREMENTO.md).

## Ejecución local

Requiere Node 24.22.1 y npm (lockfile incluido).

```bash
npm ci
npm start
```

Abrir `http://127.0.0.1:4200/peis`. La API debe escuchar en `http://127.0.0.1:5080`. El proxy de desarrollo reenvía `/api/**`; no es una configuración de publicación.

Rutas: `/peis`, `/peis/nuevo` y `/peis/:id`.

## Verificación

```bash
npm run build
npm test -- --watch=false
npx prettier --check "src/**/*.{ts,html,css}" "*.json" README.md
```

Las pruebas Vitest verifican el contrato HTTP, la preservación de ETag/If-Match, los límites del período, errores 422 y la conservación de la edición cuando hay un conflicto 412. La verificación integrada con la API y PostgreSQL corresponde al entorno completo del incremento.

## Contrato y decisiones

- `GET/POST /api/v1/peis`, `GET /api/v1/peis/{id}` y `PATCH /api/v1/pei-versiones/{versionId}`.
- Las escrituras envían exclusivamente nombre, tipo, fechas, misión, visión y valores. Los identificadores y el estado vienen del servidor.
- El PATCH conserva el ETag exacto recibido y lo envía en `If-Match`. Un 412 mantiene los cambios locales; la comparación consulta el servidor sin reemplazarlos. Cargar su versión requiere confirmación explícita.
- Un resultado incierto de red bloquea el reenvío inmediato y pide comprobar el registro para evitar duplicados.
- Regla provisional alineada con la API: ordinario de 4 años calendario inclusivos, extraordinario de 5. Nombre de 3–200 caracteres; misión, visión y valores requeridos, hasta 4.000 caracteres cada uno.
- Formularios con labels, errores enlazados, foco en el resumen, avisos de cambios sin guardar y layout responsivo. Es una base accesible, no una certificación WCAG.
- Tipografías locales de Fontsource, con licencia OFL en sus paquetes; no se requieren Google Fonts ni otros servicios externos en ejecución.

Los archivos de `src/app/pei/` concentran este dominio. `pei-api.ts` contiene los DTO y cliente HTTP; `pei-validation.ts` las validaciones locales; `pei-list` y `pei-form` las pantallas. Los estilos institucionales son compartidos en `src/styles.css`.
