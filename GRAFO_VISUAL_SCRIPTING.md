# Grafo de Visual Scripting — la flecha guía

Lo que pide el enunciado: una flecha que apunte a la **llave** y que, en cuanto la
cojas, cambie sola y apunte al **cofre**. Tiene que llevar variables, un evento y
al menos una condición **If**.

El grafo queda así de corto:

```
   On Update ──► If ──(True)──► Look At  (objetivoCofre)
                  │
                  └──(False)──► Look At  (objetivoLlave)
                  ▲
          Get Variable (Scene) "tieneLlave"
```

---

## 1. Crear las variables de objeto

1. Selecciona **`FlechaGuia`** en la Hierarchy.
2. **Add Component → Variables**.
3. Pulsa **+** dos veces y crea estas dos:

   | Nombre | Tipo | Valor |
   |---|---|---|
   | `objetivoLlave` | `Transform` | arrastra el objeto **`Llave`** de la Hierarchy |
   | `objetivoCofre` | `Transform` | arrastra el objeto **`CofreFinal`** de la Hierarchy |

   > Para cambiar el tipo, pulsa en el desplegable de la derecha del nombre y busca
   > `Transform`.

---

## 2. Crear el Script Machine

1. Con `FlechaGuia` seleccionada: **Add Component → Script Machine**.
2. En **Source** deja **Graph**.
3. Pulsa **New** y guarda el grafo como
   `Assets/_Recuperacion/Grafos/GrafoFlechaGuia.asset`
   (crea la carpeta `Grafos` si te la pide).
4. Pulsa **Edit Graph**: se abre la ventana del grafo.

---

## 3. Crear la variable de escena

Dentro de la ventana del grafo, arriba a la izquierda, está el **Blackboard** con las
pestañas `Graph / Object / Scene / App / Saved`.

1. Pestaña **Scene**.
2. Pulsa **+** y crea:

   | Nombre | Tipo | Valor inicial |
   |---|---|---|
   | `tieneLlave` | `Boolean` | desmarcado (false) |

Al crearla, Unity añade solo un objeto `Scene Variables` a la escena. Es normal.

> Esta variable la va rellenando el script `PuenteVisualScripting.cs` (está en el objeto
> `GestorJuego`): copia en ella, cada frame, si el jugador tiene la llave o no.

---

## 4. Montar los nodos

En el grafo nuevo ya vienen dos eventos, **On Start** y **On Update**. El On Start no
lo vamos a usar (puedes dejarlo suelto o borrarlo).

Haz **clic derecho en el fondo** del grafo para ir añadiendo cada nodo:

1. **Get Variable** → en el nodo, cambia el ámbito a **Scene** y escribe `tieneLlave`.
2. **If** (está en *Control → If*).
3. **Look At** (busca "Look At", es *Transform → Look At*). Añade **dos**.
4. **Get Variable** → ámbito **Object** → `objetivoLlave`.
5. **Get Variable** → ámbito **Object** → `objetivoCofre`.

### Conexiones

| Desde | Hasta |
|---|---|
| `On Update` (flecha blanca de salida) | `If` (entrada blanca) |
| `Get Variable (Scene) tieneLlave` (salida verde) | `If` → entrada **Condition** |
| `If` → salida **True** | primer `Look At` (entrada blanca) |
| `If` → salida **False** | segundo `Look At` (entrada blanca) |
| `Get Variable (Object) objetivoCofre` | primer `Look At` → entrada **Target** |
| `Get Variable (Object) objetivoLlave` | segundo `Look At` → entrada **Target** |

Los dos nodos `Look At` actúan sobre el objeto donde está el grafo, o sea la propia
flecha, así que no hay que indicarles nada más.

---

## 5. Probar

Dale al **Play**:

- Al empezar, la flecha apunta hacia la llave.
- Cuando recoges la llave, el indicador de arriba pasa a `LLAVE: SI` y **la flecha gira
  sola** hacia el cofre de la montaña.

---

## Si algo no va

**La flecha no se mueve.**
Mira si el objeto `GestorJuego` tiene el componente `Puente Visual Scripting`, y si en
la consola hay un aviso diciendo que no existe la variable de escena `tieneLlave`.
Suele ser que la variable está escrita distinta (ojo a mayúsculas) o que está creada en
la pestaña `Graph` en vez de en `Scene`.

**La flecha apunta bien pero se inclina mucho hacia abajo.**
Es normal: el cofre está mucho más alto que tú al principio, y `Look At` apunta al punto
exacto. Si lo prefieres plano, en vez del `Look At` normal puedes usar el nodo
**Look At (Vector3)** y pasarle la posición del objetivo con la **Y de la flecha**.
No hace falta para aprobar el criterio.

**En la consola sale que no encuentra el tipo `EstadoJuego`.**
Ve a **Edit → Project Settings → Visual Scripting** y pulsa **Regenerate Nodes**.

---

## Alternativa sin la variable de escena

Si te da guerra el puente, el grafo también puede leer el booleano directamente del
script de C#:

1. En `FlechaGuia`, añade una variable de objeto `gestor` de tipo **`EstadoJuego`** y
   arrástrale el objeto `GestorJuego`.
2. En el grafo, sustituye el `Get Variable (Scene) tieneLlave` por:
   `Get Variable (Object) gestor` → nodo **Get Member → EstadoJuego → tieneLlave**.
3. Esa salida va a la entrada **Condition** del `If`, igual que antes.

Para que aparezca `EstadoJuego` en la lista de nodos: **Edit → Project Settings →
Visual Scripting → Node Library → +** → añade `Assembly-CSharp`, y después
**Regenerate Nodes**.
