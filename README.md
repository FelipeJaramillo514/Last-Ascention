# Kaisen Game

`Kaisen: El Ascenso del Ultimo Cazador` es un roguelite top-down hecho en Unity con URP, pixel art y progresion persistente entre runs.

## Controles

- `WASD`: mover a Kaisen
- `Mouse`: apuntar
- `Click izquierdo`: atacar o disparar
- `Shift`: dodge roll
- `Q`: cambiar arma
- `E`: interactuar, recoger, extraer sombras
- `Tab`: abrir/cerrar panel de misiones
- `Esc`: pausa

## Flujo de juego

1. `MainMenu` abre la experiencia principal.
2. `CityArken` funciona como hub de meta progresion.
3. Desde la `Asociacion` se inicia una run.
4. `TestScene` genera la mazmorra procedural, enemigos, boss y sombras.
5. Al morir o completar el piso aparece el resumen y luego se vuelve al hub.

## Sistemas principales

- `Assets/Scripts/Core`: guardado persistente, flujo de run y eventos
- `Assets/Scripts/Player`: movimiento, stats, vida y extraccion de sombras
- `Assets/Scripts/Enemies`: enemigos base, boss y combate
- `Assets/Scripts/Dungeon`: generacion procedural, salas, puertas y minimapa
- `Assets/Scripts/UI`: HUD, panel del sistema, menus, resumen y transiciones
- `Assets/Scripts/Audio`: musica, SFX y ambiente
- `Assets/Scripts/VFX` y `Assets/Scripts/Lighting`: feedback visual e iluminacion 2D

## Build

- Escena inicial: `MainMenu`
- Escenas de juego: `CityArken`, `TestScene`
- El script editor `ProjectFinalPolishConfigurator` aplica configuracion de proyecto, build settings y crea assets faltantes de polish cuando Unity recompila.

## Debug editor

Solo en `UNITY_EDITOR`:

- `F1`: god mode
- `F2`: level up inmediato
- `F3`: desbloquear extraccion de sombra
- `F4`: anadir `9999` gold
- `F5`: regenerar mazmorra con seed aleatoria

## Ejecucion

1. Abrir el proyecto en Unity.
2. Esperar a que el editor recomponga los scripts y aplique la configuracion final.
3. Ejecutar desde `MainMenu`.
