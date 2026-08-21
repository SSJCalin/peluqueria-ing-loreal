# Peluquería y Manicura ING — Sistema de reservas

Aplicación web para la gestión de citas de un salón de peluquería y manicura.
Consta de dos partes: una **web pública** donde los clientes reservan su cita, y
un **panel interno** donde el personal gestiona la agenda.

Desarrollada con **Blazor Server (.NET 8)** y **SQLite**.

---

## Características

### Web pública de reservas
- Reserva en 5 pasos: categoría → servicio → fecha y hora → datos → confirmación
- Cálculo de disponibilidad en tiempo real según el servicio elegido
- **Cumplimiento RGPD**: las horas ocupadas nunca revelan datos de otros clientes
- Diseño adaptado a móvil

### Panel de gestión
- Acceso restringido con usuario y contraseña
- Vista de día (línea horaria) y vista de semana
- Crear, editar, cancelar y marcar citas como atendidas
- Bloqueo de huecos puntuales (descansos)
- Gestión de ausencias y vacaciones por periodos

### Reglas de negocio implementadas
- **Horarios por profesional**: cada una trabaja unos días concretos de la semana
- **Anti-solapamiento**: una profesional no puede tener dos citas a la vez
- **Cierre por horario**: un servicio no se ofrece si no cabe antes del cierre
- **Categorías cerradas**: si no trabaja nadie ese día, no hay disponibilidad
- **Ausencias**: día completo o media jornada, en rangos de fechas

---

## Estructura del proyecto

```
PeluqueriaApp/
├── Components/
│   ├── Layout/          Plantillas de página
│   └── Pages/           Reservas, Panel, Ausencias, Login
├── Datos/               Contexto de base de datos y datos iniciales
├── Logica/              Reglas de negocio (cálculo de huecos, solapamientos)
├── Modelos/             Entidades: Cita, Servicio, Profesional, Bloqueo...
├── Servicios/           Capa entre las páginas y la base de datos
└── Migrations/          Historial de cambios de la base de datos

PeluqueriaApp.Tests/     Pruebas automáticas de la lógica de negocio
```

La lógica de negocio está aislada en `Logica/` y cubierta por pruebas
automáticas, de forma que las reglas se pueden verificar sin necesidad de
levantar la aplicación ni tocar la base de datos.

---

## Puesta en marcha

### Requisitos
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Instalación

```bash
git clone https://github.com/USUARIO/REPOSITORIO.git
cd REPOSITORIO/PeluqueriaApp
```

Crea el archivo `appsettings.Development.json` a partir del ejemplo incluido:

```bash
cp appsettings.Development.json.ejemplo appsettings.Development.json
```

Edita ese archivo y define las contraseñas de los usuarios del panel.

Crea la base de datos y arranca:

```bash
dotnet ef database update
dotnet run
```

La aplicación estará disponible en `http://localhost:5xxx`:

| Ruta | Descripción |
|---|---|
| `/reservas` | Web pública de reservas |
| `/panel` | Panel de gestión (requiere sesión) |
| `/ausencias` | Gestión de vacaciones y ausencias |

### Ejecutar las pruebas

```bash
dotnet test
```

---

## Seguridad y privacidad

- Las contraseñas se almacenan **cifradas** (hash), nunca en texto plano
- Las contraseñas de configuración **no están en el repositorio**: se leen de
  `appsettings.Development.json`, excluido mediante `.gitignore`
- La base de datos (`*.db`) está excluida del repositorio por contener datos
  personales de clientes
- El panel está protegido con autenticación por cookies y marcado con
  `noindex` para no aparecer en buscadores
- La API pública de disponibilidad devuelve únicamente hora y estado
  (libre/ocupado), sin exponer información de otros clientes

---

## Estado del proyecto

En desarrollo. Funcionalidad completa en entorno local; pendiente de
despliegue en producción.

---

## Licencia

Proyecto privado. Todos los derechos reservados.