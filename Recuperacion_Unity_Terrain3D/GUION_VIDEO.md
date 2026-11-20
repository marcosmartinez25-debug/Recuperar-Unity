# Vídeo demostrativo y entrega

## El vídeo (1 a 2 minutos)

El enunciado pide cuatro cosas concretas. Si falta alguna, el criterio 8 baja de nota.

### Con qué grabar

**OBS Studio** (gratis) o, más rápido, la barra de juego de Windows: **Win + G** →
botón de grabar. Graba a **1920x1080** y con el **audio del micro** si quieres ir
explicando (no es obligatorio).

### Guion, minuto a minuto

**0:00 – 0:15 · Vista general del escenario**
En la ventana **Scene**, pon la cámara alta mirando hacia la montaña y ve girando
despacio. Tienen que verse de una pasada: la **pradera**, el **río** que cruza el mapa,
el **camino**, el **puente** y la **montaña nevada** al fondo.

> Truco: selecciona el Terrain y pulsa **F** para encuadrarlo entero.

**0:15 – 0:35 · Las Terrain Layers con sus Normal Maps**
Esto es lo que más se olvida y lo piden expresamente.

1. Selecciona el **Terreno** en la Hierarchy.
2. En el Inspector, en el componente **Terrain**, pulsa el icono de la **brocha**
   (*Paint Terrain*) → elige **Paint Texture**: ahí se ven las 7 capas.
3. Pulsa en **Edit Terrain Layers → Edit** sobre dos o tres capas (por ejemplo
   césped, roca y nieve) y **enseña el hueco del Normal Map relleno** en cada una.
4. Di en voz alta o enseña con el ratón que cada sector tiene entre 2 y 4 texturas:
   - Pradera → césped + tierra
   - Camino → tierra de camino + gravilla
   - Río → barro de ribera
   - Montaña → roca + nieve

**0:35 – 1:45 · El recorrido completo**
Dale al **Play** y graba sin cortar:

1. Arrancas en la pradera y se ve la **flecha naranja** apuntando a la llave.
2. Vas corriendo (**Shift**) por el camino hasta la **llave** y la coges.
   Se ve que desaparece, que sale el aviso y que arriba pone `LLAVE: SI`.
3. Se ve cómo **la flecha cambia sola** y ahora apunta a la montaña.
4. **Cruzas el río por el puente**.
5. Subes por el camino hasta el **cofre**.
6. Se abre la tapa y aparece el **panel de VICTORIA**.

> Si te pasas de los 2 minutos, acelera x2 el tramo de subida a la montaña al editar.
> Lo importante es que se vea entero el recorrido, no que vaya a velocidad real.
>
> Si quieres enseñar también el mensaje de "Está cerrado", hazlo **antes** de coger
> la llave: ve primero al cofre, enseña el aviso, y luego a por la llave. Suma para
> el criterio 6, pero alarga el vídeo: tú verás.

### Subirlo

YouTube → **Subir vídeo** → visibilidad **Público** u **Oculto** (no "Privado", que
entonces el profesor no puede verlo) → copia el enlace y pégalo en los comentarios de
la entrega de Classroom.

---

## Los ZIP

> **Ojo con esto.** El PDF se contradice: en la primera página llama al build
> `Apellido_Nombre_Recuperacion_Terrain3D.zip` y al proyecto
> `Apellido_Nombre_Recuperacion_01.zip`, y en el apartado final "Formato de entrega"
> llama al build `..._Terrain3D_Build.zip` y al proyecto `..._Terrain3D.zip`.
> **Pregúntale al profesor cuál vale.** Mientras tanto, yo me quedaría con los del
> apartado final, que es el que está más detallado:

| Qué | Nombre del archivo |
|---|---|
| Build ejecutable | `Martinez_Marcos_Recuperacion_Terrain3D_Build.zip` |
| Proyecto de Unity | `Martinez_Marcos_Recuperacion_Terrain3D.zip` |

(Si usas los dos apellidos: `MartinezCossio_Marcos_...`. Sin tildes y sin espacios.)

### Qué va dentro de cada uno

**ZIP del build:** la carpeta entera que te genera Unity al compilar. Tiene que llevar
el `.exe` **y** la carpeta `_Data` juntos, o no arranca.

**ZIP del proyecto:** solo estas tres carpetas, nada más:

```
Assets/
Packages/
ProjectSettings/
```

**NO metas** `Library/`, `Temp/`, `Logs/`, `obj/` ni `UserSettings/`. El criterio 8 dice
expresamente que incluir `Library` baja nota, y además pesa muchísimo.

Para hacerlo: abre la carpeta de tu proyecto, selecciona esas tres carpetas, clic
derecho → **Enviar a → Carpeta comprimida**, y renombra el zip.

---

## Repaso final antes de dar a entregar

- [ ] El `.exe` del build arranca y se puede jugar entero.
- [ ] El zip del proyecto tiene solo `Assets`, `Packages` y `ProjectSettings`.
- [ ] El vídeo dura entre 1 y 2 minutos.
- [ ] En el vídeo salen: vista general, Terrain Layers con normal map y recorrido completo.
- [ ] El vídeo está en YouTube en público u oculto, y el enlace puesto en la entrega.
- [ ] Los dos zip están bien nombrados.
