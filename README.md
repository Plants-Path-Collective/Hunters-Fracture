# Documentación - Hunters: Fracture in Time

JRPG roguelite en 3D con cámara fija en tercera persona, con exploración, investigación, gestión del tiempo, combates basados en turnos dinámicos (ATB) y narrativa basada en bucles temporales.

## Especificaciones Técnicas

* **Versión de Unity:** 6000.3.8f1
* **Pipeline de Renderizado:** Universal Render Pipeline (URP)
* **Arquitectura:** Event-Driven Architecture, ScriptableObjects (Data-Driven) y patrón Singleton para sistemas globales.
* **Dependencias principales:** 
  * Unity Input System (1.19.0) (com.unity.inputsystem)
  * DOTween (1.2.825)

## Sistema de Inputs

El proyecto utiliza el nuevo **Unity Input System** (`com.unity.inputsystem`).

- **Configuración:** Los mapas de acción se definen en `Assets/Inputs/InputSystem_Actions.inputactions`.

- **Estructura:** La gestión de entradas está centralizada en `Core.InputManager`, un Singleton persistente responsable de instanciar el asset `InputSystem_Actions`, habilitar o deshabilitar los distintos mapas de acciones (Exploration, Combat, Dialogue, UI y Minigame) y proporcionar un punto único de acceso al sistema de inputs.

- **Flujo de datos:**
  `Periférico → Unity Input System → InputManager → Action Map activo → Sistema consumidor (PlayerController, CombatManager, DialogueManager, etc.)`

## Sistema de Combate

[Pendiente a redacción]

## Arquitectura y Flujo de Escenas

La aplicación utiliza un **GameObject persistente** denominado
**`[ GameManager ]`**, marcado con `DontDestroyOnLoad()`, el cual
permanece activo durante toda la ejecución del juego. Este objeto
centraliza los principales sistemas globales mediante componentes
*Singleton*, incluyendo `GameManager`, `InputManager`, `UIManager` y
`DialogueManager`. Su propósito es mantener el estado global del juego y
evitar la reinicialización de estos sistemas durante las transiciones
entre escenas.

La exploración se desarrolla sobre una escena principal denominada
**`Overworld`**, la cual actúa como contenedor permanente del mundo. Las
distintas áreas del juego (por ejemplo, Mercado, Alcantarillas o
Hoteles) se cargan y descargan como **escenas aditivas**, permitiendo
mantener en memoria únicamente las zonas cercanas al jugador.

Cada escena contiene un componente derivado de la clase base **`SceneSetter`** (por ejemplo, `MarketSetter`, `SewersSetter`, `ChurchSetter`). Este componente actúa como punto de configuración de la escena, proporcionando a los sistemas globales la información necesaria para inicializar el área, como los NPCs presentes, la cámara a activar u otros parámetros específicos. De esta manera, el **`SceneSetter`** funciona como una capa de comunicación entre la escena cargada y los distintos *Managers*, evitando que estos dependan directamente de buscar los componentes uno a uno cada vez que el jugador entra a una zona.

La carga y descarga de estas escenas se controla mediante **Trigger
Colliders** ubicados en las transiciones entre áreas. Cuando el jugador
ingresa a una nueva zona, el sistema carga las escenas adyacentes y
descarga aquellas que se encuentran a más de dos zonas de distancia,
reduciendo el consumo de memoria y minimizando los tiempos de carga
durante la exploración.

El combate se desarrolla en una escena independiente denominada
**`CombatStage`**. Al detectar un encuentro con un enemigo en el
`Overworld`, el sistema inicia la transición hacia esta escena,
descargando temporalmente la escena de exploración. Una vez finalizado
el combate, el flujo retorna al `Overworld` para continuar la
exploración.

## Estructura de Directorios Clave

* `Assets/Scripts/00_Core/`: Sistemas globales, y gestores principales.
* `Assets/Scripts/01_Combat/`: Lógica de daño, sistema de units, etc.
* `Assets/00_Units/`: ScriptableObjects de Units, Skills, y las diferentes Parties.
* `Assets/Inputs/`: Inputs del Juego.

## Guía de Contribución y Buenas Prácticas

* **Nomenclatura:** Comentarios y Documentación en Español. Código en inglés, siguiendo las convenciones estándar de C# (PascalCase para métodos/clases, camelCase para variables locales).
* **Commits:** Siguiendo la nomenclatura, todos los commits deben ser en inglés y deben seguir el siguiente formato a.b.c - [summary] (a -> versión de build; b -> cambios de tamaño grande-medio; c -> cambios pequeños; [summary] -> resumen o título del commit, luego en la descripción del commit puedes explicar a detalle cada cambio).