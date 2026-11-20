"""Genera texturas PBR tileables (albedo + normal map) y billboards de vegetacion
para el proyecto de Unity 'Recuperacion - Creacion de entornos con Terrain'.
Estilo realista segun las referencias visuales del enunciado.
"""
import os
import numpy as np
from PIL import Image

SALIDA = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
                      "proyecto", "_Recuperacion", "Texturas")
os.makedirs(SALIDA, exist_ok=True)

TAM = 1024
rng = np.random.default_rng(20261120)


# ----------------------------------------------------------------------------
# Ruido
# ----------------------------------------------------------------------------
def perlin(size, res, gen):
    """Perlin 2D que hace tiling perfecto (res debe dividir a size)."""
    d = size // res
    lin = np.arange(0, 1, 1.0 / d)
    gx, gy = np.meshgrid(lin, lin, indexing="ij")
    grid = np.stack([np.tile(gx, (res, res)), np.tile(gy, (res, res))], axis=-1)

    ang = 2 * np.pi * gen.random((res + 1, res + 1))
    grad = np.dstack((np.cos(ang), np.sin(ang)))
    grad[-1, :] = grad[0, :]
    grad[:, -1] = grad[:, 0]

    g00 = grad[0:-1, 0:-1].repeat(d, 0).repeat(d, 1)
    g10 = grad[1:, 0:-1].repeat(d, 0).repeat(d, 1)
    g01 = grad[0:-1, 1:].repeat(d, 0).repeat(d, 1)
    g11 = grad[1:, 1:].repeat(d, 0).repeat(d, 1)

    n00 = np.sum(grid * g00, 2)
    n10 = np.sum(np.dstack((grid[:, :, 0] - 1, grid[:, :, 1])) * g10, 2)
    n01 = np.sum(np.dstack((grid[:, :, 0], grid[:, :, 1] - 1)) * g01, 2)
    n11 = np.sum(np.dstack((grid[:, :, 0] - 1, grid[:, :, 1] - 1)) * g11, 2)

    t = 6 * grid ** 5 - 15 * grid ** 4 + 10 * grid ** 3
    a = n00 + t[:, :, 0] * (n10 - n00)
    b = n01 + t[:, :, 0] * (n11 - n01)
    return np.sqrt(2) * (a + t[:, :, 1] * (b - a))


def fbm(size, res_base, octavas, gen, persistencia=0.5):
    """Suma de octavas de Perlin, normalizada a 0..1."""
    total = np.zeros((size, size))
    amp, norm, res = 1.0, 0.0, res_base
    for _ in range(octavas):
        if size % res != 0:
            break
        total += amp * perlin(size, res, gen)
        norm += amp
        amp *= persistencia
        res *= 2
    total /= max(norm, 1e-6)
    return np.clip(total * 0.5 + 0.5, 0, 1)


def ridged(size, res_base, octavas, gen):
    """Ruido con crestas: ideal para grietas de roca."""
    total = np.zeros((size, size))
    amp, norm, res = 1.0, 0.0, res_base
    for _ in range(octavas):
        if size % res != 0:
            break
        total += amp * (1.0 - np.abs(perlin(size, res, gen)))
        norm += amp
        amp *= 0.5
        res *= 2
    return np.clip(total / max(norm, 1e-6), 0, 1)


def piedras(size, n, rmin, rmax, gen):
    """Mapa de altura con guijarros redondeados, con wrap en los bordes."""
    h = np.zeros((size, size))
    for _ in range(n):
        cx, cy = gen.integers(0, size, 2)
        r = int(gen.integers(rmin, rmax))
        yy, xx = np.mgrid[-r:r + 1, -r:r + 1]
        d = np.sqrt(xx ** 2 + yy ** 2) / r
        blob = np.clip(1.0 - d ** 2, 0, 1) ** 0.6 * gen.uniform(0.5, 1.0)
        xs = (np.arange(cx - r, cx + r + 1)) % size
        ys = (np.arange(cy - r, cy + r + 1)) % size
        h[np.ix_(ys, xs)] = np.maximum(h[np.ix_(ys, xs)], blob)
    return h


def suavizar(a, k=2):
    """Desenfoque sencillo por desplazamientos (mantiene el tiling)."""
    out = np.zeros_like(a)
    cuenta = 0
    for dy in range(-k, k + 1):
        for dx in range(-k, k + 1):
            out += np.roll(np.roll(a, dy, 0), dx, 1)
            cuenta += 1
    return out / cuenta


# ----------------------------------------------------------------------------
# Guardado
# ----------------------------------------------------------------------------
def guardar_albedo(nombre, rgb):
    img = Image.fromarray((np.clip(rgb, 0, 1) * 255).astype(np.uint8), "RGB")
    img.save(os.path.join(SALIDA, nombre + "_albedo.png"))


def guardar_normal(nombre, altura, fuerza=3.0):
    """Normal map en espacio tangente (convencion OpenGL, la de Unity)."""
    h = altura.astype(np.float64)
    dx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * 0.5
    dy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) * 0.5
    nx, ny, nz = -dx * fuerza, -dy * fuerza, np.ones_like(h)
    largo = np.sqrt(nx ** 2 + ny ** 2 + nz ** 2)
    rgb = np.dstack([nx / largo, ny / largo, nz / largo]) * 0.5 + 0.5
    Image.fromarray((rgb * 255).astype(np.uint8), "RGB").save(
        os.path.join(SALIDA, nombre + "_normal.png"))


def colorear(base, variacion, mezcla):
    """Interpola entre dos colores RGB segun un mapa 0..1."""
    base = np.array(base)
    variacion = np.array(variacion)
    m = mezcla[:, :, None]
    return base[None, None, :] * (1 - m) + variacion[None, None, :] * m


# ----------------------------------------------------------------------------
# 1. CESPED de pradera (verde intenso, referencias pagina 8)
# ----------------------------------------------------------------------------
def cesped():
    manchas = fbm(TAM, 2, 6, rng)
    medio = fbm(TAM, 16, 4, rng)
    briznas = fbm(TAM, 128, 3, rng)
    fino = fbm(TAM, 256, 2, rng)
    mezcla = np.clip(manchas * 0.40 + medio * 0.25 + briznas * 0.25 + fino * 0.10, 0, 1)
    mezcla = np.clip((mezcla - 0.5) * 1.5 + 0.5, 0, 1)
    rgb = colorear((0.10, 0.24, 0.05), (0.45, 0.62, 0.18), mezcla)
    seco = (fbm(TAM, 8, 4, rng) > 0.72).astype(float)
    seco = suavizar(seco, 3)
    rgb = rgb * (1 - seco[:, :, None] * 0.5) + \
        np.array([0.48, 0.47, 0.22])[None, None, :] * (seco[:, :, None] * 0.5)
    rgb *= (0.82 + 0.3 * briznas)[:, :, None]
    guardar_albedo("pradera_cesped", rgb)
    guardar_normal("pradera_cesped", briznas * 0.7 + manchas * 0.3, 4.0)


# ----------------------------------------------------------------------------
# 2. TIERRA de pradera (calvas entre la hierba)
# ----------------------------------------------------------------------------
def tierra():
    grano = fbm(TAM, 64, 4, rng)
    grumos = fbm(TAM, 16, 4, rng)
    pebbles = piedras(TAM, 260, 3, 9, rng)
    mezcla = np.clip(grano * 0.5 + grumos * 0.5, 0, 1)
    rgb = colorear((0.21, 0.15, 0.10), (0.44, 0.34, 0.23), mezcla)
    rgb = rgb * (1 - pebbles[:, :, None] * 0.55) + \
        np.array([0.38, 0.35, 0.31])[None, None, :] * (pebbles[:, :, None] * 0.55)
    guardar_albedo("pradera_tierra", rgb)
    guardar_normal("pradera_tierra", grano * 0.45 + pebbles * 0.55, 3.5)


# ----------------------------------------------------------------------------
# 3. CAMINO de tierra rojiza (el sendero de las referencias)
# ----------------------------------------------------------------------------
def camino_tierra():
    grano = fbm(TAM, 48, 4, rng)
    rodadas = fbm(TAM, 4, 4, rng)
    pebbles = piedras(TAM, 320, 2, 9, rng)
    mezcla = np.clip(grano * 0.45 + rodadas * 0.55, 0, 1)
    mezcla = np.clip((mezcla - 0.5) * 1.45 + 0.5, 0, 1)
    rgb = colorear((0.27, 0.16, 0.11), (0.58, 0.40, 0.28), mezcla)
    rgb = rgb * (1 - pebbles[:, :, None] * 0.45) + \
        np.array([0.52, 0.46, 0.40])[None, None, :] * (pebbles[:, :, None] * 0.45)
    polvo = suavizar((fbm(TAM, 12, 3, rng) > 0.62).astype(float), 4)
    rgb += polvo[:, :, None] * 0.07
    guardar_albedo("camino_tierra", rgb)
    guardar_normal("camino_tierra", grano * 0.5 + pebbles * 0.5, 3.0)


# ----------------------------------------------------------------------------
# 4. GRAVILLA (bordes del camino)
# ----------------------------------------------------------------------------
def gravilla():
    base = fbm(TAM, 64, 3, rng)
    pebbles = piedras(TAM, 520, 6, 20, rng)
    mezcla = np.clip(pebbles * 0.75 + base * 0.25, 0, 1)
    rgb = colorear((0.24, 0.22, 0.20), (0.62, 0.60, 0.56), mezcla)
    tinte = fbm(TAM, 16, 3, rng)[:, :, None]
    rgb = rgb * (0.88 + 0.24 * tinte)
    guardar_albedo("camino_gravilla", rgb)
    guardar_normal("camino_gravilla", pebbles * 0.85 + base * 0.15, 4.5)


# ----------------------------------------------------------------------------
# 5. BARRO de la ribera del rio (mojado y oscuro)
# ----------------------------------------------------------------------------
def barro():
    grano = fbm(TAM, 32, 4, rng)
    charcos = fbm(TAM, 8, 3, rng)
    pebbles = piedras(TAM, 420, 2, 8, rng)
    mezcla = np.clip(grano * 0.6 + charcos * 0.4, 0, 1)
    rgb = colorear((0.14, 0.11, 0.08), (0.37, 0.30, 0.21), mezcla)
    rgb = rgb * (1 - pebbles[:, :, None] * 0.5) + \
        np.array([0.33, 0.32, 0.30])[None, None, :] * (pebbles[:, :, None] * 0.5)
    humedo = (charcos < 0.4)[:, :, None]
    rgb = np.where(humedo, rgb * 0.78, rgb)
    guardar_albedo("rio_barro", rgb)
    guardar_normal("rio_barro", grano * 0.45 + pebbles * 0.55, 3.2)


# ----------------------------------------------------------------------------
# 6. ROCA de montana (gris, con grietas marcadas)
# ----------------------------------------------------------------------------
def roca():
    grietas = ridged(TAM, 8, 5, rng)
    estratos = fbm(TAM, 4, 3, rng)
    detalle = fbm(TAM, 128, 3, rng)
    altura = np.clip(grietas * 0.6 + estratos * 0.25 + detalle * 0.15, 0, 1)
    rgb = colorear((0.17, 0.17, 0.18), (0.56, 0.55, 0.53), altura)
    oxido = suavizar((fbm(TAM, 6, 3, rng) > 0.68).astype(float), 3)[:, :, None]
    rgb = rgb * (1 - oxido * 0.35) + \
        np.array([0.40, 0.31, 0.24])[None, None, :] * (oxido * 0.35)
    guardar_albedo("montana_roca", rgb)
    guardar_normal("montana_roca", altura, 5.0)


# ----------------------------------------------------------------------------
# 7. NIEVE de las cumbres
# ----------------------------------------------------------------------------
def nieve():
    dunas = fbm(TAM, 8, 4, rng)
    grano = fbm(TAM, 256, 2, rng)
    altura = np.clip(dunas * 0.8 + grano * 0.2, 0, 1)
    rgb = colorear((0.84, 0.88, 0.95), (1.0, 1.0, 1.0), altura)
    rgb = np.clip(rgb * 1.02, 0, 1)
    brillo = (rng.random((TAM, TAM)) > 0.9985).astype(float)
    rgb += brillo[:, :, None] * 0.25
    guardar_albedo("cumbre_nieve", rgb)
    guardar_normal("cumbre_nieve", altura, 2.0)


# ----------------------------------------------------------------------------
# 8. CORTEZA y HOJA para los arboles de respaldo
# ----------------------------------------------------------------------------
def corteza_y_hoja():
    vetas = fbm(512, 128, 3, rng) * 0.6 + fbm(512, 8, 3, rng) * 0.4
    vetas = np.clip(vetas, 0, 1)
    rgb = colorear((0.16, 0.11, 0.07), (0.44, 0.33, 0.22), vetas)
    Image.fromarray((np.clip(rgb, 0, 1) * 255).astype(np.uint8), "RGB").save(
        os.path.join(SALIDA, "arbol_corteza_albedo.png"))
    h = vetas
    dx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * 0.5
    dy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) * 0.5
    nx, ny, nz = -dx * 4, -dy * 4, np.ones_like(h)
    L = np.sqrt(nx ** 2 + ny ** 2 + nz ** 2)
    n = np.dstack([nx / L, ny / L, nz / L]) * 0.5 + 0.5
    Image.fromarray((n * 255).astype(np.uint8), "RGB").save(
        os.path.join(SALIDA, "arbol_corteza_normal.png"))

    hojas = fbm(512, 32, 4, rng)
    rgb = colorear((0.09, 0.22, 0.06), (0.31, 0.48, 0.14), hojas)
    Image.fromarray((np.clip(rgb, 0, 1) * 255).astype(np.uint8), "RGB").save(
        os.path.join(SALIDA, "arbol_hoja_albedo.png"))


# ----------------------------------------------------------------------------
# 9. BILLBOARDS de vegetacion (con transparencia)
# ----------------------------------------------------------------------------
def billboard_hierba(nombre, n_briznas, color_a, color_b, altura_rel,
                     flores=None, ancho_brizna=(2, 5)):
    S = 512
    rgba = np.zeros((S, S, 4))
    gen = np.random.default_rng(abs(hash(nombre)) % (2 ** 32))
    for _ in range(n_briznas):
        base_x = gen.integers(int(S * 0.08), int(S * 0.92))
        alto = gen.integers(int(S * altura_rel[0]), int(S * altura_rel[1]))
        curva = gen.uniform(-0.45, 0.45)
        ancho0 = gen.integers(ancho_brizna[0], ancho_brizna[1] + 1)
        t_col = gen.uniform(0, 1)
        col = np.array(color_a) * (1 - t_col) + np.array(color_b) * t_col
        for i in range(alto):
            t = i / alto
            y = S - 1 - i
            x = int(base_x + curva * alto * (t ** 2))
            w = max(1, int(ancho0 * (1 - t * 0.85)))
            sombra = 0.55 + 0.45 * t          # mas oscuro en la base
            for dx in range(-w, w + 1):
                px = x + dx
                if 0 <= px < S and 0 <= y < S:
                    rgba[y, px, :3] = col * sombra
                    rgba[y, px, 3] = 1.0
    if flores is not None:
        color_flor, n_flores = flores
        for _ in range(n_flores):
            cx = gen.integers(20, S - 20)
            cy = gen.integers(int(S * 0.15), int(S * 0.70))
            r = gen.integers(4, 8)
            yy, xx = np.mgrid[-r:r + 1, -r:r + 1]
            mask = (xx ** 2 + yy ** 2) <= r ** 2
            ys = np.clip(np.arange(cy - r, cy + r + 1), 0, S - 1)
            xs = np.clip(np.arange(cx - r, cx + r + 1), 0, S - 1)
            sub = np.ix_(ys, xs)
            zona = rgba[sub]
            zona[..., :3] = np.where(mask[..., None], np.array(color_flor), zona[..., :3])
            zona[..., 3] = np.where(mask, 1.0, zona[..., 3])
            rgba[sub] = zona
    img = Image.fromarray((np.clip(rgba, 0, 1) * 255).astype(np.uint8), "RGBA")
    img.save(os.path.join(SALIDA, nombre + ".png"))


def vegetacion():
    # 1) hierba alta de pradera
    billboard_hierba("veg_hierba_alta", 150, (0.13, 0.30, 0.06), (0.40, 0.60, 0.16),
                     (0.55, 0.95))
    # 2) hierba con flores blancas y amarillas (referencia pagina 8)
    billboard_hierba("veg_flores", 110, (0.16, 0.34, 0.08), (0.38, 0.56, 0.18),
                     (0.35, 0.70), flores=((0.97, 0.97, 0.92), 26))
    billboard_hierba("_tmp", 0, (0, 0, 0), (0, 0, 0), (0, 1))
    # 3) matorral bajo y tupido
    billboard_hierba("veg_matorral", 260, (0.05, 0.15, 0.04), (0.20, 0.33, 0.09),
                     (0.22, 0.48), ancho_brizna=(7, 16))
    os.remove(os.path.join(SALIDA, "_tmp.png"))


if __name__ == "__main__":
    cesped(); print("ok cesped")
    tierra(); print("ok tierra")
    camino_tierra(); print("ok camino")
    gravilla(); print("ok gravilla")
    barro(); print("ok barro")
    roca(); print("ok roca")
    nieve(); print("ok nieve")
    corteza_y_hoja(); print("ok corteza/hoja")
    vegetacion(); print("ok vegetacion")
    print("Texturas en:", SALIDA)
