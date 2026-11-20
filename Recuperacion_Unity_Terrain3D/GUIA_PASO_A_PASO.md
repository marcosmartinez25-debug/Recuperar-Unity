# Guía paso a paso — Recuperación: Creación de entornos con Terrain

Esto es todo lo que hay que hacer, en orden. Cada paso tarda poco; lo largo (esculpir
el terreno, pintarlo, plantar árboles) ya lo hace solo el generador.

---

## Paso 1 — Crear el proyecto

1. Abre **Unity Hub** → **New project**.
2. Plantilla: **3D (Built-In Render Pipeline)**.
   - Es la que usa el paquete de personaje de Unity, así no salen materiales rosas.
   - Versión recomendada: **Unity 6 LTS** o **2022.3 LTS**.
3. Nombre: `Recuperacion_Terrain3D`.

---

## Paso 2 — Copiar la carpeta de la actividad

1. Descomprime el zip que te he pasado.
2. Copia la carpeta **`_Recuperacion`** entera dentro de la carpeta **`Assets`** de tu
   proyecto (puedes arrastrarla desde el explorador de archivos a la ventana Project de Unity).
3. Espera a que Unity termine de importar (barra abajo a la derecha).

Debe quedarte así:

```
Assets/
  _Recuperacion/
    Editor/      <- el generador de la escena
    Scripts/     <- los scripts de C# del juego
    Texturas/    <- texturas con sus normal maps
```

Si sale algún error rojo en la consola, mándamelo y lo miramos.

---

## Paso 3 — Instalar el personaje de tercera persona

El enunciado pide el paquete de controladores de Unity.

1. Entra en el **Asset Store** desde el navegador y busca
   **"Starter Assets: Character Controllers"** (de Unity Technologies, gratis).
   Es el que ahora reúne el de primera y el de tercera persona.
2. **Add to My Assets** → **Open in Unity**.
3. En Unity: **Window → Package Manager → My Assets** → busca el paquete →
   **Download** → **Import**.
4. Unity instalará solo el **Input System** y **Cinemachine**. Si te pregunta por
   reiniciar el editor, dile que sí.

---

## Paso 4 — Instalar Visual Scripting (si no está)

1. **Window → Package Manager → Unity Registry** → busca **Visual Scripting** → **Install**.
2. **Edit → Project Settings → Visual Scripting** → botón **Initialize Visual Scripting**
   (solo la primera vez, tarda un poco).

---

## Paso 5 — Generar la escena

1. Abre una escena nueva vacía: **File → New Scene → Basic (Built-in)**.
2. Guárdala ya: **File → Save As** → `Assets/_Recuperacion/Escena_Recuperacion.unity`.
3. En la barra de menús de Unity te habrá salido un menú nuevo: **Recuperacion**.
4. **Recuperacion → Abrir generador de escena**.
5. Pulsa **GENERAR ESCENA COMPLETA** y espera (entre 30 segundos y 2 minutos).

Cuando acabe tendrás montado:

- El Terrain esculpido: pradera con colinas suaves, río que cruza el mapa de lado a
  lado y zona montañosa con cumbres nevadas.
- 7 Terrain Layers con textura base **y normal map**: césped, tierra, camino de tierra,
  gravilla, barro de ribera, roca y nieve.
- El camino pintado y nivelado desde la pradera hasta el cofre, cruzando el puente.
- Árboles, hierba alta, flores y matorrales, rocas y troncos caídos.
- Agua del río, puente de madera con barandillas.
- La llave (en la pradera, junto al camino) y el cofre (arriba, en la montaña).
- La interfaz: aviso arriba, indicador de llave y panel de VICTORIA.
- Sol, cielo, niebla y los bordes del mapa cerrados con muros invisibles.

> **Nota sobre los árboles.** Por defecto usa tres árboles generados automáticamente.
> Funcionan y son tres especies distintas, pero para la nota de "estilo realista" quedan
> justitos. Mira el Paso 9 si quieres subirlos de nivel (son 5 minutos).

---

## Paso 6 — Poner el personaje

1. En la ventana Project, ve a `Assets/StarterAssets/ThirdPersonController/Prefabs`.
2. Arrastra el prefab **`PlayerArmature`** a la escena (en la ventana Hierarchy).
3. Selecciona en la Hierarchy el objeto **`PuntoDeInicio`** (está dentro de
   `ESCENA_RECUPERACION`), mira su **Position** en el Inspector y **copia esos tres
   números** en la Position del `PlayerArmature`. Súbele la Y un metro
   (por ejemplo, si pone `Y = 22.3`, ponle `23.3`) para que no nazca metido en el suelo.
4. Comprueba que el `PlayerArmature` tiene la etiqueta **`Player`** arriba del Inspector.
   Si no la tiene, pónsela: **Tag → Player**.
5. El paquete trae su propia cámara (`PlayerFollowCamera` + `MainCamera`). Si el generador
   te creó una **`Camara_Temporal`**, bórrala para que no haya dos cámaras.
6. Dale al **Play** y comprueba que puedes andar (WASD), correr (Shift) y saltar (Espacio).

---

## Paso 7 — Colocar la flecha guía

1. En la Hierarchy, arrastra **`FlechaGuia`** encima del **`PlayerArmature`**
   (para que quede dentro, como hijo suyo).
2. Con `FlechaGuia` seleccionada, ponle en el Inspector:
   - **Position:** `X = 0`, `Y = 2.5`, `Z = 0`
   - **Rotation:** `0, 0, 0`

Así la flecha flota sobre la cabeza del personaje y lo sigue a todas partes.

---

## Paso 8 — Montar el grafo de Visual Scripting

Está en el archivo **`GRAFO_VISUAL_SCRIPTING.md`**, con los nodos uno por uno.
Son unos 10 minutos.

---

## Paso 9 — (Opcional pero recomendado) Árboles y texturas más realistas

El criterio 3 pide **3 especies de árbol** y **3 de hierba/arbusto** distintas, y el
criterio 4 pide estilo realista. Lo que genero yo cumple el mínimo, pero con assets
reales sube bastante.

### Árboles

1. En el Asset Store busca packs gratuitos de naturaleza realista, por ejemplo
   **"Conifers [BOTD]"**, **"Realistic Tree 9"** o **"Nature Starter Kit 2"**.
2. Impórtalos y localiza 3 prefabs de **especies diferentes**
   (un pino, un roble/frondoso y un abedul, por ejemplo). No valen el mismo árbol
   repintado o más grande: el enunciado lo dice expresamente.
3. **Recuperacion → Abrir generador de escena** → marca
   **"Usar mis propios prefabs de árbol"** → arrastra los 3 → **GENERAR ESCENA COMPLETA**.

Ojo: volver a generar borra lo generado antes, así que hazlo **antes** del Paso 6
(o vuelve a colocar el personaje y la flecha después).

### Texturas

Las texturas que te he generado tienen su normal map y se ven bien, pero son
procedurales. Si quieres texturas fotográficas:

1. Descarga texturas **CC0** (gratis y sin licencia) de **polyhaven.com/textures** o
   **ambientcg.com**: césped, tierra, grava, barro, roca y nieve. Baja **Diffuse/Albedo**
   y **Normal (OpenGL)** de cada una.
2. Renómbralas con los nombres que ya uso y mete los archivos en
   `Assets/_Recuperacion/Texturas` sustituyendo a los míos:

   | Zona | Albedo | Normal |
   |---|---|---|
   | Pradera, césped | `pradera_cesped_albedo.png` | `pradera_cesped_normal.png` |
   | Pradera, tierra | `pradera_tierra_albedo.png` | `pradera_tierra_normal.png` |
   | Camino, tierra | `camino_tierra_albedo.png` | `camino_tierra_normal.png` |
   | Camino, gravilla | `camino_gravilla_albedo.png` | `camino_gravilla_normal.png` |
   | Ribera del río | `rio_barro_albedo.png` | `rio_barro_normal.png` |
   | Montaña, roca | `montana_roca_albedo.png` | `montana_roca_normal.png` |
   | Cumbre, nieve | `cumbre_nieve_albedo.png` | `cumbre_nieve_normal.png` |

3. Vuelve a generar la escena. El generador marca solo los `_normal` como **Normal Map**
   al importarlos.

---

## Paso 10 — Repasar antes de entregar

Dale al Play y comprueba esto:

- [ ] El personaje anda, corre y salta sin atascarse en el camino ni en el puente.
- [ ] No puedes salirte del mapa por ningún borde.
- [ ] La flecha apunta a la llave y, al cogerla, cambia y apunta al cofre.
- [ ] Al coger la llave desaparece y arriba a la izquierda pone `LLAVE: SI`.
- [ ] Si llegas al cofre **sin** la llave sale "Está cerrado. Necesitas la llave".
- [ ] Con la llave, la tapa gira y sale el panel de **VICTORIA**.
- [ ] La consola no tiene errores rojos.

Guarda la escena (**Ctrl+S**).

---

## Paso 11 — Compilar (build)

1. **File → Build Settings**.
2. **Add Open Scenes** (que esté solo tu escena y marcada).
3. Platform: **Windows, Mac, Linux** → Target: **Windows** → Architecture: **x86_64**.
4. **Build** → crea una carpeta nueva vacía, por ejemplo `Build_Recuperacion`.
5. Cuando acabe, prueba el `.exe` para asegurarte de que funciona.
6. Comprime **la carpeta entera del build** (el `.exe` y la carpeta `_Data` tienen que ir juntos).

---

## Paso 12 — Preparar los ZIP y el vídeo

Mira **`GUION_VIDEO.md`**: ahí están el guion del vídeo y los nombres exactos de los zip.
