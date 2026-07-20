# Documentación - Hunters: Fracture in Time

JRPG roguelite en 3D con cámara fija en tercera persona, con exploración, investigación, gestión del tiempo, combates basados en turnos dinámicos (ATB) y narrativa basada en bucles temporales.

---

## Especificaciones Técnicas

* **Versión de Unity:** 6000.3.8f1
* **Pipeline de Renderizado:** Universal Render Pipeline (URP)
* **Arquitectura:** Event-Driven Architecture, ScriptableObjects (Data-Driven) y patrón Singleton para sistemas globales.
* **Dependencias principales:**
  * Unity Input System (1.19.0) (com.unity.inputsystem)
  * DOTween (1.2.825)
  * Unity Localization (1.5.12) (com.unity.localization)

---

## Sistema de Inputs

El proyecto utiliza el nuevo **Unity Input System** (`com.unity.inputsystem`).

- **Configuración:** Los mapas de acción se definen en `Assets/Inputs/InputSystem_Actions.inputactions`.

- **Estructura:** La gestión de entradas está centralizada en `Core.InputManager`, un Singleton persistente responsable de instanciar el asset `InputSystem_Actions`, habilitar o deshabilitar los distintos mapas de acciones (Exploration, Combat, Dialogue, UI y Minigame) y proporcionar un punto único de acceso al sistema de inputs.

- **Flujo de datos:**
  `Periférico → Unity Input System → InputManager → Action Map activo → Sistema consumidor (PlayerController, CombatManager, DialogueManager, etc.)`

---

## Sistema de Combate

[Pendiente a redacción]

---

## Sistema de Diálogo

El sistema de diálogo está basado en una arquitectura **Data-Driven** utilizando **ScriptableObjects** para desacoplar los datos de la lógica de reproducción. Cada conversación se representa mediante un `ConversationSO`, el cual almacena un conjunto ordenado de `DialogueLine`. Cada línea puede contener un hablante (`SpeakerSO`), texto localizado, una línea de voz opcional (`AudioClip`), hasta cuatro respuestas posibles (`AnswerOption`), un tiempo límite para responder y una conversación alternativa que se ejecuta cuando el jugador no realiza ninguna elección.

Tanto el texto de la línea (`dialogueText`) como el de cada respuesta (`answerText`) son de tipo `LocalizedString` (Unity Localization), por lo que no almacenan el texto directamente sino una referencia a una entrada dentro de una String Table. Ver la sección [Sistema de Localización](#sistema-de-localización) para más detalle.

La reproducción de las conversaciones está centralizada en `DialogueManager`, un sistema global persistente encargado de iniciar y finalizar diálogos, mostrar el texto y la interfaz correspondiente (incluyendo nombre y retrato del hablante), reproducir líneas de voz, gestionar las respuestas del jugador y controlar la transición entre conversaciones mediante *Coroutines*. Su funcionamiento es completamente independiente de los NPCs, permitiendo que cualquier sistema pueda iniciar una conversación proporcionando únicamente una referencia a un `ConversationSO`.

Cada NPC dispone de un componente `ConversationTrigger`, responsable de detectar la entrada del jugador mediante un **Trigger Collider** e iniciar la conversación correspondiente. Además, cada NPC posee un identificador único (`NPC_ID`) y un índice de progreso (`conversationIndex`) que registra cuántas veces el jugador ha interactuado con él. Este índice permite presentar distintas conversaciones conforme avanza la relación entre el jugador y el personaje, almacenando el progreso de forma individual para cada NPC.

El `ConversationTrigger` también incorpora una bandera de estado (`request`) que bloquea el avance del `conversationIndex` mientras exista una solicitud o misión pendiente asociada al personaje. De esta forma, un NPC mantiene el mismo diálogo durante el desarrollo de una misión y únicamente avanza a la siguiente conversación cuando la condición correspondiente ha sido resuelta, garantizando la continuidad narrativa.

### Flujo del sistema

```
Jugador entra al Trigger Collider
            │
            ▼
ConversationTrigger
            │
            ▼
Obtiene NPC_ID y conversationIndex
            │
            ▼
Selecciona ConversationSO correspondiente
            │
            ▼
DialogueManager.StartConversation()
            │
            ▼
Reproduce DialogueLine
            │
            ├── Texto
            ├── Voz (opcional)
            └── Respuestas (opcional)
            │
            ▼
¿Finalizó la conversación?
            │
            ▼
¿request == false?
            │
      Sí ───────► conversationIndex++
      No ───────► Mantiene el mismo índice
            │
            ▼
Guarda el progreso del NPC
```

### Componentes principales

- **ConversationSO:** Contenedor de datos que representa una conversación completa.
- **SpeakerSO:** Contenedor de datos reutilizable para un personaje (nombre localizado y retrato), referenciado desde cualquier `DialogueLine` para identificar quién habla.
- **DialogueManager:** Sistema global encargado de reproducir y controlar el flujo completo de las conversaciones.
- **ConversationTrigger:** Gestiona el inicio de la conversación y el progreso individual de cada NPC mediante `NPC_ID` y `conversationIndex`.


>Nota: Actualmente el progreso de las conversaciones se almacena con PlayerPrefs por NPC utilizando la combinación NPC_ID + conversationIndex. En futuras iteraciones este sistema será integrado con el gestor de guardado y el sistema de misiones, permitiendo persistencia completa entre partidas y una gestión centralizada del estado narrativo.

---

## Sistema de Localización

El proyecto utiliza el paquete oficial **Unity Localization** (`com.unity.localization`) como capa de datos para todo el texto traducible del juego (diálogos, UI y wiki). La edición de traducciones se realiza directamente con la ventana nativa del paquete (`Window > Asset Management > Localization Tables`); Tambien puede editarse directamente desde la Google SpreadSheet `Hunters: Fracture in Time - Localization`.
- **Locales:** definidos en `Edit > Project Settings > Localization`.
- **String Tables:** en vez de una única tabla global, las tablas se organizan por dominio funcional para evitar cargar en memoria contenido que el jugador no está usando (ej. cargar toda la wiki al iniciar el juego). Convención actual:
  - `Dialogue_<NPC o Capítulo>` — diálogos agrupados por NPC principal o por capítulo/zona cuando varios NPCs secundarios comparten contexto.
  - `UI_Common` — textos genéricos repetidos en toda la interfaz (Aceptar, Cancelar, Volver...).
  - `UI_Menus` — textos específicos de cada menú (Main Menu, Pause, Settings, Inventario...).
  - `Wiki_<Categoría>` — una tabla por sección grande de la wiki (Bestiario, Objetos, Lore...).
- **Convención de keys:** prefijo por dominio + identificador descriptivo, ej. `npc_flora_line_003`, `menu_pause_resume`, `wiki_bestiary_slime_desc`.
- **Integración con Google Sheets:** habilitada como flujo de sincronización opcional sobre las String Tables existentes (no reemplaza las tablas, permite editarlas de forma colaborativa fuera del Editor).
- **Uso en código:** cualquier texto traducible se expone como `LocalizedString` (ver `DialogueLine.dialogueText`, `AnswerOption.answerText` y `SpeakerSO.speakerName`), resuelto en runtime según el `Locale` activo.

> Nota: por ahora la organización por tablas está aplicada al sistema de diálogo. La extensión a menús y wiki sigue el mismo paquete y convención, pero su integración en código aún está pendiente.

---

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