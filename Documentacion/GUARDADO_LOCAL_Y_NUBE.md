# Están Dentro — Guardado local (y cómo pasarlo a la nube)

## Hoy: todo se guarda en el PC
- Archivo: `C:\Users\<usuario>\AppData\LocalLow\DefaultCompany\Estan-Dentro\estan_dentro_datos.json`. Para borrar el progreso, se borra ese archivo.
- Guarda lo mismo que tenía la base de datos de Duvan en somee:

  | Tabla | Qué guarda |
  |---|---|
  | **Jugadores** | perfil creado solo con el nombre de Windows |
  | **Partidas** | fecha, capítulo alcanzado, tiempo |
  | **Logros** | catálogo fijo de 5 |
  | **LogroXPartida** | qué logro se ganó en qué partida |

- Código: `Assets/Scripts/Network/AlmacenLocal.cs`. `ApiClient.cs` decide si guarda local o en la nube; el resto del juego (menú, perfil, partidas, logros) no cambia.
- Si se entra al juego sin pasar por el menú, igual se crea la partida (`ApiBootstrapper.AsegurarPartida`).

## Mañana: pasarlo a la nube (si se publica el juego)
1. Montar una API con estas rutas, que son las mismas que tenía la de somee:
   - `GET/POST /api/Jugadores`, `GET/PUT /api/Jugadores/{id}`
   - `GET/POST /api/Partidas`, `GET/PUT/DELETE /api/Partidas/{id}`
   - `GET /api/Logros`
   - `GET/POST /api/LogroXPartida`
   - Los JSON con la forma de `Assets/Scripts/Network/Dtos.cs`.

   La de Duvan era ASP.NET Core 8 + EF Core + SQL Server y se puede reutilizar si él tiene el proyecto (`ApiJuego`).
2. Poner su dirección en `Assets/StreamingAssets/nube.json`:
   ```json
   { "url": "https://la-api.com" }
   ```
   Con `"url": ""` el juego guarda local. En un juego ya compilado, el archivo está en `EstanDentro_Colegio_Data/StreamingAssets/nube.json`.
3. **Usar HTTPS.** El servidor viejo era HTTP y los juegos compilados lo bloquean.
4. **Nunca** poner contraseñas ni la cadena de conexión de la base de datos en el juego ni en el repositorio: el juego solo conoce la URL pública de la API.
5. Pendiente para ese día: subir lo guardado localmente la primera vez que haya conexión, si se quiere conservar.
