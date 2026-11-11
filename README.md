# UMBRAL

Videojuego de terror en primera persona para el proyecto semestral de Animación y Producción Audiovisual (FISC, UTP).
Recorres los 10 pisos de un hotel abandonado: en cada piso hay que encontrar la llave en un cajón, abrir la puerta con seguro,
encender la luz de la sala final y salir. Cuando las luces parpadean, el espectro va a cruzar el pasillo: escóndete en un clóset.

Integrantes: Angel Márquez y Eriol Tuñón.

## Cómo abrirlo

1. Instala **Unity 6000.6.6f1** desde Unity Hub (con otra versión de Unity 6 puede pedir actualizar el proyecto).
2. Clona el repositorio:
   ```
   git clone <URL de este repositorio>
   ```
3. En Unity Hub: **Add > Add project from disk** y elige la carpeta clonada. La primera vez tarda unos minutos porque Unity genera la carpeta `Library`.
4. Abre la escena `Assets/UMBRAL/Scenes/Nivel01.unity` y presiona **Play**.

## Controles

| Tecla | Acción |
|---|---|
| W A S D | Moverse |
| Shift | Correr |
| Ratón | Mirar |
| E | Interactuar (puertas, cajones, clósets, interruptor) |
| R | Reintentar el piso o volver a empezar al terminar |

## Estructura

- `Assets/UMBRAL/Scenes`: los 10 pisos (`Nivel01` a `Nivel10`).
- `Assets/UMBRAL/Scripts`: lógica del juego.
- `Assets/UMBRAL/Editor/ConstructorNiveles.cs`: arma las 10 escenas con el kit modular (menú **UMBRAL** del editor).
- `Assets/UMBRAL/Models`, `Prefabs`, `Materials`, `Textures`, `Audio`: assets del juego.
- `Blender/UMBRAL_Kit.blend`: kit modular y espectro modelados en Blender. `Blender/umbral_kit.py` lo genera y exporta los FBX.

Las escenas se generan con el constructor: los cambios hechos a mano en una escena se pierden al reconstruir.
