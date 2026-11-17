# UMBRAL

Videojuego de terror en primera persona para el proyecto semestral de Animación y Producción Audiovisual (FISC, UTP).
Estás encerrado en un hotel abandonado: un solo mapa de 20 × 40 m con 10 habitaciones encadenadas. Para salir hay que abrir
las 10 puertas, y no todas se abren igual. Cuando las luces parpadean, el espectro va a recorrer las habitaciones abiertas:
escóndete en un clóset, porque si te encuentra dentro de la misma habitación te atrapa.

Integrantes: Angel Márquez y Eriol Tuñón.

## Cómo abrirlo

1. Instala **Unity 6000.6.6f1** desde Unity Hub (con otra versión de Unity 6 puede pedir actualizar el proyecto).
2. Clona el repositorio:
   ```
   git clone https://github.com/NgelGMV/UMBRAL.git
   ```
3. En Unity Hub: **Add > Add project from disk** y elige la carpeta clonada. La primera vez tarda unos minutos porque Unity genera la carpeta `Library`.
4. Abre la escena `Assets/UMBRAL/Scenes/Menu.unity` y presiona **Play**.

## Controles

| Tecla | Acción |
|---|---|
| W A S D | Moverse |
| Shift | Correr |
| Ratón | Mirar |
| E | Interactuar (puertas, cajones, clósets, palancas, notas, teclado, fusibles) |
| 0 a 9 | Escribir el código en un teclado (Retroceso borra, E lo suelta) |
| Esc | Pausa |
| R | Reintentar desde la última habitación alcanzada (al ser atrapado) |
| M | Volver al menú (en pausa, derrota o victoria) |

## Las 10 habitaciones

El recorrido va en zigzag de sur a norte. La recepción es zona segura.

| # | Habitación | Cómo se abre su puerta |
|---|---|---|
| 1 | Recepción | Puerta 001: se abre con E |
| 2 | Dormitorio | Puerta 002: llave escondida en uno de 3 cajones |
| 3 | Salón | Puerta 003: palanca escondida (1 de 3 sitios) |
| 4 | Cuarto de servicio | Puerta 004: se abre con E, pero la habitación está a oscuras hasta encontrar el interruptor |
| 5 | Oficina | Puerta 005: teclado con código de 3 dígitos; el código está en una nota (1 de 3 sitios). Al abrirla, el espectro aparece siempre |
| 6 | Guardarropa | Puerta 006: se abre con E |
| 7 | Cuarto eléctrico | Puerta 007: sin corriente; hay que reunir 3 fusibles (6 sitios posibles) y colocarlos en la caja |
| 8 | Dormitorio doble | Puerta 008: llave escondida en uno de 4 cajones |
| 9 | Biblioteca | Puerta 009: dos palancas (2 de 4 sitios), casi a oscuras |
| 10 | Sala final | Puerta 010: teclado; el código está repartido en 3 notas, un dígito en cada una |

La llave, las palancas, las notas, los fusibles y los códigos cambian de sitio o de valor en cada partida.

## Estructura

- `Assets/UMBRAL/Scenes`: `Menu` (pantalla de inicio) y `Hotel` (el mapa).
- `Assets/UMBRAL/Scripts`: lógica del juego.
- `Assets/UMBRAL/Editor/ConstructorNiveles.cs`: arma las dos escenas con el kit modular (menú **UMBRAL** del editor).
- `Assets/UMBRAL/Models`, `Prefabs`, `Materials`, `Textures`, `Audio`: assets del juego.
- `Blender/UMBRAL_Kit.blend`: kit modular (29 assets) y espectro modelados en Blender. `Blender/umbral_kit.py` lo genera y exporta los FBX.

Las escenas se generan con el constructor: los cambios hechos a mano en una escena se pierden al reconstruir.
