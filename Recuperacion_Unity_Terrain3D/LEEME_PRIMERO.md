# Recuperación — Creación de entornos con Terrain (Unity)

Aquí está montado todo el proyecto. Lo que hace el generador es el trabajo pesado:
esculpir el terreno, crear las capas de textura con sus normal maps, pintar el camino,
plantar los árboles y la hierba, poner el río, el puente, la llave, el cofre, la interfaz
y el ambiente.

## Por dónde empiezo

1. **`GUIA_PASO_A_PASO.md`** — los 12 pasos, de crear el proyecto a compilar. Empieza aquí.
2. **`GRAFO_VISUAL_SCRIPTING.md`** — el grafo de la flecha, nodo a nodo.
3. **`GUION_VIDEO.md`** — qué grabar y cómo se llaman los zip.

## Qué hay en la carpeta `_Recuperacion`

```
_Recuperacion/
  Editor/      El generador de la escena (menú "Recuperacion" en Unity)
  Scripts/     Los scripts de C# del juego, comentados
  Texturas/    7 texturas con albedo + normal map, 3 billboards de vegetación
               y texturas de corteza y hoja
```

## Qué queda por hacer a mano

Son 4 cosas, y están todas explicadas en la guía:

1. Importar el paquete de personaje de tercera persona del Asset Store (Paso 3).
2. Colocar el personaje en `PuntoDeInicio` y meter la flecha dentro de él (Pasos 6 y 7).
3. Montar el grafo de Visual Scripting (Paso 8). Es lo único que hay que hacer con el ratón
   nodo a nodo, porque un grafo no se puede generar por código de forma fiable.
4. Grabar el vídeo y hacer los dos zip.

Opcional pero recomendado: cambiar los árboles y las texturas por assets reales
(Paso 9). Lo que hay funciona y cumple los mínimos, pero con assets de la Asset Store
y texturas de Poly Haven el apartado de "estilo realista" sube bastante.

---

## Cómo queda cubierto cada criterio

| Criterio | Cómo se cumple |
|---|---|
| **1. Geometría y relieve** (2,0) | Pradera de colinas suaves, cauce que cruza el mapa de extremo a extremo con puente transitable, y montañas con cumbres. Terreno de 500 x 500 m y 160 m de alto. El camino va nivelado: pendiente media de 8° y máxima de 26°, así que se anda sin atascarse. |
| **2. Texturas y PBR** (2,0) | 7 Terrain Layers, **todas con su Normal Map**: césped y tierra (pradera), tierra de camino y gravilla (camino), barro (ribera), roca y nieve (montaña). Eso son de 2 a 4 texturas por sector. Las transiciones se mezclan con ruido, no con cortes rectos. |
| **3. Árboles y assets** (1,0) | 3 tipos distintos de árbol y 3 de hierba/arbusto (hierba alta, flores y matorral), más rocas y troncos. Todo se coloca a la altura del terreno, sin flotar, y esquiva el camino y el río. |
| **4. Ambientación** (1,0) | Estilo realista: sol direccional cálido con sombras suaves, cielo procedural, niebla exponencial para dar distancia a la montaña y agua azul semitransparente. Sin nada cartoon. |
| **5. Controlador 3ª persona** (1,0) | Paquete oficial de Unity, personaje en el punto de inicio y bordes del mapa cerrados dos veces: por relieve elevado y por muros invisibles. |
| **6. Script en C#** (1,0) | `RecogerLlave` (OnTriggerEnter, la llave desaparece y se guarda), `CofreFinal` (bloqueado sin llave con aviso, y si la tienes gira la tapa), `InterfazJuego` (avisos, indicador y panel de victoria) y `EstadoJuego`. Todo comentado en español. |
| **7. Visual Scripting** (1,0) | Grafo con variables (`tieneLlave`, `objetivoLlave`, `objetivoCofre`), evento `On Update`, condición `If` y dos `Look At`: la flecha apunta a la llave y cambia al cofre al cogerla. |
| **8. Formato de entrega** (1,0) | Está en `GUION_VIDEO.md`: qué grabar, los nombres de los zip y qué carpetas meter (y cuáles no). |
