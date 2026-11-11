
# UMBRAL - Kit modular del Nivel 1 (Hotel). Unidades: metros. Ejes Blender: X ancho, Y profundidad, Z alto.
# Genera los 20 assets de la matriz del primer avance (medidas, orígenes, UV y texturas) y el espectro, y los exporta a FBX para Unity.
import bpy, bmesh, math, os
from mathutils import Matrix, Vector

ROOT = r"C:\Users\angel\UMBRAL"
FBX_DIR = os.path.join(ROOT, "Assets", "UMBRAL", "Models")
BLEND = os.path.join(ROOT, "Blender", "UMBRAL_Kit.blend")
TEX_DIR = os.path.join(ROOT, "Assets", "UMBRAL", "Textures")

# Texturas: material -> (color, normales, metros que cubre una repetición). Las UV se generan a 1 unidad = 1 metro.
TEX = {
 "MAT_Papel":         ("T_Papel",         "T_Papel_N",  1.0),
 "MAT_Madera":        ("T_Madera",        "T_Madera_N", 1.0),
 "MAT_MaderaOscura":  ("T_MaderaOscura",  "T_Madera_N", 1.0),
 "MAT_PisoClaro":     ("T_PisoClaro",     "T_Piso_N",   1.0),
 "MAT_PisoOscuro":    ("T_PisoOscuro",    "T_Piso_N",   1.0),
 "MAT_Techo":         ("T_Techo",         "T_Techo_N",  2.0),
 "MAT_TelaRoja":      ("T_TelaRoja",      "T_Tela_N",   1.0),
 "MAT_AlfombraBorde": ("T_AlfombraBorde", "T_Tela_N",   0.5),
 "MAT_Sabana":        ("T_Sabana",        "T_Sabana_N", 1.0),
}

MATS = {  # nombre: (color, metallic, roughness, emission)
 "MAT_Papel":        ((0.33, 0.30, 0.20), 0.0, 0.90, 0),
 "MAT_Madera":       ((0.21, 0.11, 0.055), 0.0, 0.60, 0),
 "MAT_MaderaOscura": ((0.085, 0.045, 0.025), 0.0, 0.55, 0),
 "MAT_PisoClaro":    ((0.26, 0.16, 0.085), 0.0, 0.50, 0),
 "MAT_PisoOscuro":   ((0.20, 0.12, 0.065), 0.0, 0.50, 0),
 "MAT_Techo":        ((0.27, 0.25, 0.21), 0.0, 0.95, 0),
 "MAT_Laton":        ((0.78, 0.57, 0.22), 1.0, 0.35, 0),
 "MAT_MetalOscuro":  ((0.07, 0.07, 0.08), 0.9, 0.45, 0),
 "MAT_TelaRoja":     ((0.33, 0.035, 0.045), 0.0, 0.95, 0),
 "MAT_AlfombraBorde":((0.52, 0.39, 0.15), 0.0, 0.95, 0),
 "MAT_Sabana":       ((0.70, 0.67, 0.60), 0.0, 0.95, 0),
 "MAT_Vidrio":       ((0.02, 0.03, 0.06), 0.0, 0.08, 0),
 "MAT_Pantalla":     ((1.00, 0.78, 0.45), 0.0, 0.60, 3.0),
 "MAT_Llama":        ((1.00, 0.62, 0.20), 0.0, 0.50, 6.0),
 "MAT_Cera":         ((0.80, 0.76, 0.66), 0.0, 0.70, 0),
 "MAT_EntManto":     ((0.012, 0.012, 0.016), 0.0, 0.85, 0),
 "MAT_EntHueso":     ((0.34, 0.30, 0.24), 0.0, 0.80, 0),
 "MAT_EntOjos":      ((1.00, 0.90, 0.65), 0.0, 0.50, 9.0),
 "MAT_EntCara":      ((0.25, 0.24, 0.18), 0.0, 0.75, 0),
 "MAT_EntDiente":    ((0.50, 0.44, 0.28), 0.0, 0.45, 0),
 "MAT_EntNegro":     ((0.0, 0.0, 0.0), 0.0, 1.0, 0),
}

def get_mat(name):
    m = bpy.data.materials.get(name)
    if m: return m
    col, met, rough, emi = MATS[name]
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes.get("Principled BSDF")
    b.inputs["Base Color"].default_value = (*col, 1)
    b.inputs["Metallic"].default_value = met
    b.inputs["Roughness"].default_value = rough
    if emi:
        b.inputs["Emission Color"].default_value = (*col, 1)
        b.inputs["Emission Strength"].default_value = emi
    m.diffuse_color = (*col, 1)
    if name in TEX: texturizar(m, b, *TEX[name])
    if name == "MAT_EntCara":   # la textura de la cara cubre x de -0.15 a 0.15 m y z de 2.38 a 2.72 m (UV = metros)
        nt = m.node_tree
        coord = nt.nodes.new("ShaderNodeTexCoord"); mapa = nt.nodes.new("ShaderNodeMapping"); img = nt.nodes.new("ShaderNodeTexImage")
        mapa.inputs["Scale"].default_value = (1 / 0.30, 1 / 0.34, 1); mapa.inputs["Location"].default_value = (0.5, -7.0, 0)
        img.image = bpy.data.images.load(os.path.join(TEX_DIR, "T_EspectroCara.png"), check_existing=True)
        nt.links.new(coord.outputs["UV"], mapa.inputs["Vector"]); nt.links.new(mapa.outputs["Vector"], img.inputs["Vector"])
        nt.links.new(img.outputs["Color"], b.inputs["Base Color"])
    return m

def texturizar(m, bsdf, color, normales, metros):
    """Conecta la textura de color y el mapa de normales al Principled BSDF, con la escala en metros."""
    nt = m.node_tree
    coord = nt.nodes.new("ShaderNodeTexCoord"); coord.location = (-900, 200)
    mapa = nt.nodes.new("ShaderNodeMapping"); mapa.location = (-700, 200)
    mapa.inputs["Scale"].default_value = (1 / metros, 1 / metros, 1)
    nt.links.new(coord.outputs["UV"], mapa.inputs["Vector"])
    img = nt.nodes.new("ShaderNodeTexImage"); img.location = (-480, 300)
    img.image = bpy.data.images.load(os.path.join(TEX_DIR, color + ".png"), check_existing=True)
    nt.links.new(mapa.outputs["Vector"], img.inputs["Vector"])
    nt.links.new(img.outputs["Color"], bsdf.inputs["Base Color"])
    imgn = nt.nodes.new("ShaderNodeTexImage"); imgn.location = (-480, -50)
    imgn.image = bpy.data.images.load(os.path.join(TEX_DIR, normales + ".png"), check_existing=True)
    imgn.image.colorspace_settings.name = "Non-Color"
    nt.links.new(mapa.outputs["Vector"], imgn.inputs["Vector"])
    nm = nt.nodes.new("ShaderNodeNormalMap"); nm.location = (-200, -50)
    nt.links.new(imgn.outputs["Color"], nm.inputs["Color"])
    nt.links.new(nm.outputs["Normal"], bsdf.inputs["Normal"])

def uv_caja(bm):
    """UV por proyección de caja a escala real: cada cara se proyecta sobre el plano de su eje dominante (1 unidad UV = 1 m)."""
    bm.normal_update()
    capa = bm.loops.layers.uv.verify()
    for f in bm.faces:
        n = f.normal
        eje = max(range(3), key=lambda i: abs(n[i]))
        for l in f.loops:
            x, y, z = l.vert.co
            l[capa].uv = (y, z) if eje == 0 else (x, z) if eje == 1 else (x, y)

def B(x0, x1, y0, y1, z0, z1, mat):            # caja
    return ("box", x0, x1, y0, y1, z0, z1, mat)
def C(cx, cy, cz, r1, r2, depth, mat, axis="Z", segs=12):   # cilindro / cono centrado
    return ("cyl", cx, cy, cz, r1, r2, depth, mat, axis, segs)

def build(name, parts, parent=None, loc=(0, 0, 0)):
    me = bpy.data.meshes.new(name)
    bm = bmesh.new()
    names = []
    for p in parts:
        mat = p[7]
        if mat not in names: names.append(mat)
        idx = names.index(mat)
        if p[0] == "box":
            _, x0, x1, y0, y1, z0, z1, _ = p
            M = Matrix.Translation(((x0+x1)/2, (y0+y1)/2, (z0+z1)/2)) @ Matrix.Diagonal((x1-x0, y1-y0, z1-z0, 1))
            r = bmesh.ops.create_cube(bm, size=1.0, matrix=M)
        else:
            _, cx, cy, cz, r1, r2, depth, _, axis, segs = p
            R = Matrix.Identity(4)
            if axis == "X": R = Matrix.Rotation(math.radians(90), 4, "Y")
            if axis == "Y": R = Matrix.Rotation(math.radians(90), 4, "X")
            r = bmesh.ops.create_cone(bm, cap_ends=True, segments=segs, radius1=r1, radius2=r2, depth=depth,
                                      matrix=Matrix.Translation((cx, cy, cz)) @ R)
        for f in {f for v in r["verts"] for f in v.link_faces}:
            f.material_index = idx
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    uv_caja(bm)
    bm.to_mesh(me); bm.free()
    me.uv_layers[0].name = "UVMap"
    for n in names: me.materials.append(get_mat(n))
    ob = bpy.data.objects.new(name, me)
    COL.objects.link(ob)
    if parent: ob.parent = parent
    ob.location = loc
    return ob

# ---------- perfil de muro (compartido por muros y pilar) ----------
T = 0.25   # medio espesor del muro
def perfil(x0, x1, z_from=0.0, z_to=3.0, t=T):
    """Zócalo, friso de madera, moldura, papel tapiz y cornisa entre z_from y z_to."""
    bands = [(0.00, 0.15, t,        "MAT_MaderaOscura"),
             (0.15, 1.00, t - 0.01, "MAT_Madera"),
             (1.00, 1.06, t,        "MAT_MaderaOscura"),
             (1.06, 2.85, t - 0.02, "MAT_Papel"),
             (2.85, 3.00, t,        "MAT_MaderaOscura")]
    out = []
    for a, b, th, m in bands:
        a2, b2 = max(a, z_from), min(b, z_to)
        if b2 - a2 > 1e-6: out.append(B(x0, x1, -th, th, a2, b2, m))
    return out

def crear_kit():
    A = {}
    # ===== ESTRUCTURA =====  origen de muros y marco: base, un extremo, centro del espesor
    A["EST_MuroEstandar"] = build("EST_MuroEstandar", perfil(0, 2))
    A["EST_MuroMedio"]    = build("EST_MuroMedio", perfil(0, 1))
    v = perfil(0, 2, 0, 1.06) + perfil(0, 0.4, 1.06, 3) + perfil(1.6, 2, 1.06, 3) + perfil(0.4, 1.6, 2.3, 3)
    v += [B(0.40, 0.46, -T, T, 1.06, 2.30, "MAT_MaderaOscura"), B(1.54, 1.60, -T, T, 1.06, 2.30, "MAT_MaderaOscura"),
          B(0.46, 1.54, -T, T, 1.06, 1.12, "MAT_MaderaOscura"), B(0.46, 1.54, -T, T, 2.24, 2.30, "MAT_MaderaOscura"),
          B(0.975, 1.025, -0.04, 0.04, 1.12, 2.24, "MAT_MaderaOscura"), B(0.46, 1.54, -0.04, 0.04, 1.655, 1.705, "MAT_MaderaOscura"),
          B(0.46, 1.54, -0.01, 0.01, 1.12, 2.24, "MAT_Vidrio")]
    A["EST_MuroVentana"]  = build("EST_MuroVentana", v)
    A["EST_MuroVano"]     = build("EST_MuroVano", perfil(0, 0.25) + perfil(1.75, 2) + perfil(0.25, 1.75, 2.5, 3))
    A["EST_MarcoPuerta"]  = build("EST_MarcoPuerta", [B(0, 0.15, -T, T, 0, 2.3, "MAT_MaderaOscura"),
          B(1.35, 1.5, -T, T, 0, 2.3, "MAT_MaderaOscura"), B(0, 1.5, -T, T, 2.3, 2.5, "MAT_MaderaOscura")])
    # pilar, piso y techo: origen en el centro de la base
    p = perfil(-0.5, 0.5, t=0.5)
    A["EST_Pilar"] = build("EST_Pilar", p)
    piso = [B(-1 + 0.4*i, -1 + 0.4*(i+1), -1, 1, 0, 0.1, "MAT_PisoClaro" if i % 2 == 0 else "MAT_PisoOscuro") for i in range(5)]
    A["EST_Piso"]  = build("EST_Piso", piso)
    A["EST_Techo"] = build("EST_Techo", [B(-1, 1, -1, 1, 0, 0.1, "MAT_Techo")])

    # ===== INTERACTIVOS =====
    # Puerta 1.20 x 2.30 x 0.10: origen en la base, borde de la bisagra, centro del espesor
    d = [B(0, 1.2, -0.035, 0.035, 0, 2.3, "MAT_Madera"),
         B(0, 0.15, -0.05, 0.05, 0, 2.3, "MAT_MaderaOscura"), B(1.05, 1.2, -0.05, 0.05, 0, 2.3, "MAT_MaderaOscura"),
         B(0.15, 1.05, -0.05, 0.05, 0, 0.2, "MAT_MaderaOscura"), B(0.15, 1.05, -0.05, 0.05, 2.15, 2.3, "MAT_MaderaOscura"),
         B(0.15, 1.05, -0.05, 0.05, 1.10, 1.30, "MAT_MaderaOscura")]
    for z in (0.25, 1.15, 2.05): d.append(B(0.0, 0.03, -0.056, 0.056, z - 0.05, z + 0.05, "MAT_Laton"))
    d += [C(1.12, 0, 1.0, 0.018, 0.018, 0.20, "MAT_Laton", "Y", 10),
          C(1.12, -0.11, 1.0, 0.04, 0.04, 0.03, "MAT_Laton", "Y", 12), C(1.12, 0.11, 1.0, 0.04, 0.04, 0.03, "MAT_Laton", "Y", 12)]
    A["INT_Puerta"] = build("INT_Puerta", d)

    # Clóset 1.20 x 2.20 x 0.60: frente hacia -Y, hueco por dentro, dos hojas con bisagra y rendija central
    c = [B(-0.6, 0.6, 0.27, 0.30, 0, 2.2, "MAT_Madera"), B(-0.6, -0.57, -0.27, 0.27, 0, 2.2, "MAT_Madera"),
         B(0.57, 0.6, -0.27, 0.27, 0, 2.2, "MAT_Madera"), B(-0.6, 0.6, -0.30, 0.30, 2.12, 2.2, "MAT_MaderaOscura"),
         B(-0.6, 0.6, -0.30, 0.30, 0, 0.08, "MAT_MaderaOscura")]
    A["INT_Closet"] = closet = build("INT_Closet", c)
    for side, name in ((-1, "HojaIzq"), (1, "HojaDer")):
        s = side
        h = [B(min(0, -s*0.575), max(0, -s*0.575), -0.015, 0.015, 0, 2.04, "MAT_Madera")]
        for z0, z1 in ((0.10, 0.95), (1.08, 1.94)):
            a, b = sorted((-s*0.07, -s*0.50)); h.append(B(a, b, -0.022, -0.015, z0, z1, "MAT_MaderaOscura"))
        a, b = sorted((-s*0.52, -s*0.55)); h.append(B(a, b, -0.05, -0.015, 0.95, 1.10, "MAT_Laton"))
        build(name, h, parent=closet, loc=(s*0.59, -0.285, 0.08))

    # Mesa de noche 0.50 x 0.60 x 0.50: frente hacia -Y, cajón como hijo (se desliza en -Y)
    m = [B(-0.25, 0.25, -0.25, 0.25, 0.57, 0.60, "MAT_MaderaOscura"), B(-0.25, -0.22, -0.24, 0.25, 0.12, 0.57, "MAT_Madera"),
         B(0.22, 0.25, -0.24, 0.25, 0.12, 0.57, "MAT_Madera"), B(-0.22, 0.22, 0.22, 0.25, 0.12, 0.57, "MAT_Madera"),
         B(-0.22, 0.22, -0.24, 0.22, 0.12, 0.15, "MAT_Madera"), B(-0.22, 0.22, -0.24, 0.22, 0.36, 0.38, "MAT_Madera"),
         B(-0.22, 0.22, -0.24, -0.22, 0.15, 0.36, "MAT_MaderaOscura")]
    for sx in (-1, 1):
        for sy in (-1, 1):
            m.append(B(sx*0.25 - (0.04 if sx > 0 else 0), sx*0.25 + (0.04 if sx < 0 else 0),
                       sy*0.25 - (0.04 if sy > 0 else 0), sy*0.25 + (0.04 if sy < 0 else 0), 0, 0.12, "MAT_MaderaOscura"))
    A["INT_MesaNoche"] = mesa = build("INT_MesaNoche", m)
    cj = [B(-0.215, 0.215, -0.02, 0, 0, 0.18, "MAT_MaderaOscura"), B(-0.20, 0.20, 0, 0.40, 0, 0.012, "MAT_Madera"),
          B(-0.20, -0.188, 0, 0.40, 0.012, 0.15, "MAT_Madera"), B(0.188, 0.20, 0, 0.40, 0.012, 0.15, "MAT_Madera"),
          B(-0.188, 0.188, 0.388, 0.40, 0.012, 0.15, "MAT_Madera"), C(0, -0.035, 0.09, 0.02, 0.02, 0.03, "MAT_Laton", "Y", 10)]
    build("Cajon", cj, parent=mesa, loc=(0, -0.24, 0.385))

    # Lámpara de pared 0.25 x 0.35 x 0.20: origen en el centro de la cara que toca el muro (y = 0), sale hacia -Y
    l = [B(-0.05, 0.05, -0.015, 0, -0.175, 0.0, "MAT_Laton"), C(0, -0.06, -0.10, 0.012, 0.012, 0.10, "MAT_Laton", "Y", 8),
         C(0, -0.11, -0.06, 0.012, 0.012, 0.09, "MAT_Laton", "Z", 8), C(0, -0.11, 0.075, 0.09, 0.125, 0.20, "MAT_Pantalla", "Z", 12)]
    A["INT_Lampara"] = build("INT_Lampara", l)

    # Interruptor 0.30 x 0.40 x 0.10: origen en el centro de la cara trasera, palanca como hijo
    i = [B(-0.15, 0.15, -0.06, 0, -0.2, 0.2, "MAT_MetalOscuro"), B(-0.12, 0.12, -0.07, -0.06, -0.17, 0.17, "MAT_Laton"),
         B(-0.02, 0.02, -0.075, -0.07, -0.10, 0.10, "MAT_MetalOscuro")]
    A["INT_Interruptor"] = inter = build("INT_Interruptor", i)
    build("Palanca", [C(0, -0.015, 0.045, 0.012, 0.012, 0.09, "MAT_MetalOscuro", "Z", 8), C(0, -0.015, 0.09, 0.022, 0.022, 0.03, "MAT_TelaRoja", "Z", 10)],
          parent=inter, loc=(0, -0.07, 0))

    # Llave 0.10 x 0.01 x 0.03: acostada, origen en el centro de la base
    k = [C(-0.035, 0, 0.005, 0.015, 0.015, 0.01, "MAT_Laton", "Z", 12), B(-0.022, 0.05, -0.004, 0.004, 0, 0.01, "MAT_Laton"),
         B(0.030, 0.038, -0.015, -0.004, 0, 0.01, "MAT_Laton"), B(0.042, 0.050, -0.012, -0.004, 0, 0.01, "MAT_Laton")]
    A["INT_Llave"] = build("INT_Llave", k)

    # ===== PROPS =====
    A["PRP_PlacaNumero"] = build("PRP_PlacaNumero", [B(-0.15, 0.15, -0.015, 0, -0.075, 0.075, "MAT_Laton"),
          B(-0.135, 0.135, -0.02, -0.015, -0.06, 0.06, "MAT_MetalOscuro")])
    cama = [B(-0.8, 0.8, -1.05, 1.0, 0.12, 0.28, "MAT_MaderaOscura"), B(-0.8, 0.8, 1.0, 1.05, 0, 0.60, "MAT_MaderaOscura"),
            B(-0.76, 0.76, -1.03, 0.98, 0.28, 0.44, "MAT_Sabana"), B(-0.78, 0.78, -1.04, 0.30, 0.30, 0.46, "MAT_TelaRoja"),
            B(-0.55, 0.55, 0.52, 0.92, 0.44, 0.53, "MAT_Sabana")]
    for sx in (-0.74, 0.74):
        for sy in (-0.99, 0.94): cama.append(B(sx - 0.06, sx + 0.06, sy - 0.06, sy + 0.06, 0, 0.12, "MAT_MaderaOscura"))
    A["PRP_Cama"] = build("PRP_Cama", cama)
    mo = [B(-1.95, 1.95, -0.42, 0.42, 0, 1.04, "MAT_Madera"), B(-2.0, 2.0, -0.5, 0.5, 1.04, 1.10, "MAT_MaderaOscura"),
          B(-2.0, 2.0, -0.46, 0.46, 0, 0.12, "MAT_MaderaOscura")]
    for j in range(5): mo.append(B(-1.85 + 0.76*j, -1.85 + 0.76*j + 0.66, -0.435, -0.42, 0.22, 0.92, "MAT_MaderaOscura"))
    A["PRP_Mostrador"] = build("PRP_Mostrador", mo)
    so = [B(-1.0, 1.0, -0.45, 0.45, 0.10, 0.32, "MAT_TelaRoja"), B(-0.78, 0.78, -0.43, 0.22, 0.32, 0.45, "MAT_TelaRoja"),
          B(-1.0, 1.0, 0.22, 0.45, 0.32, 0.85, "MAT_TelaRoja"), B(-1.0, -0.78, -0.45, 0.22, 0.32, 0.62, "MAT_TelaRoja"),
          B(0.78, 1.0, -0.45, 0.22, 0.32, 0.62, "MAT_TelaRoja")]
    for sx in (-0.92, 0.92):
        for sy in (-0.38, 0.38): so.append(B(sx - 0.05, sx + 0.05, sy - 0.05, sy + 0.05, 0, 0.10, "MAT_MaderaOscura"))
    A["PRP_Sofa"] = build("PRP_Sofa", so)
    # Candelabro 0.90 x 0.80 x 0.90: cuelga del techo, origen en el punto de anclaje (arriba)
    ca = [C(0, 0, -0.02, 0.07, 0.07, 0.04, "MAT_Laton"), C(0, 0, -0.26, 0.012, 0.012, 0.44, "MAT_MetalOscuro", "Z", 8),
          C(0, 0, -0.56, 0.09, 0.05, 0.18, "MAT_Laton"), C(0, 0, -0.76, 0.03, 0.03, 0.08, "MAT_Laton", "Z", 10)]
    for n in range(6):
        a = n * math.pi / 3; cx, cy = math.cos(a), math.sin(a)
        ca.append(build_arm(cx * 0.21, cy * 0.21, -0.60, a))
        ca += [C(cx*0.39, cy*0.39, -0.585, 0.055, 0.03, 0.03, "MAT_Laton", "Z", 10),
               C(cx*0.39, cy*0.39, -0.515, 0.015, 0.015, 0.11, "MAT_Cera", "Z", 8),
               C(cx*0.39, cy*0.39, -0.435, 0.014, 0.002, 0.05, "MAT_Llama", "Z", 8)]
    A["PRP_Candelabro"] = build("PRP_Candelabro", ca)
    A["PRP_Alfombra"] = build("PRP_Alfombra", [B(-1.5, 1.5, -1, 1, 0, 0.015, "MAT_AlfombraBorde"), B(-1.35, 1.35, -1, 1, 0.015, 0.02, "MAT_TelaRoja")])
    A["PRP_Digitos"] = digitos()
    A["ENT_Espectro"] = crear_entidad()
    return A

def digitos():
    """Dígitos 0-9 en latón para las placas (8 cm de alto, de pie, mirando a -Y, origen centrado en la base)."""
    root = bpy.data.objects.new("PRP_Digitos", None); COL.objects.link(root)
    dg = bpy.context.evaluated_depsgraph_get()
    for n in range(10):
        cu = bpy.data.curves.new("tmp_txt", "FONT"); cu.body = str(n); cu.align_x = "CENTER"; cu.size = 1.0; cu.extrude = 0.02
        tmp = bpy.data.objects.new("tmp_txt", cu); COL.objects.link(tmp)
        bpy.context.view_layer.update(); dg = bpy.context.evaluated_depsgraph_get()
        me = bpy.data.meshes.new_from_object(tmp.evaluated_get(dg))
        bpy.data.objects.remove(tmp, do_unlink=True); bpy.data.curves.remove(cu)
        ys = [v.co.y for v in me.vertices]; h = max(ys) - min(ys); k = 0.08 / h; y0 = min(ys)
        for v in me.vertices:
            x, y, z = v.co
            v.co = (x * k, -(z + 0.02) * 0.003 / 0.04, (y - y0) * k)     # de pie, grosor 3 mm hacia -Y
        me.name = "D%d" % n; me.materials.append(get_mat("MAT_Laton")); me.update()
        ob = bpy.data.objects.new("D%d" % n, me); COL.objects.link(ob); ob.parent = root; ob.location = (0.09 * n, 0, 0)
    return root

def build_arm(cx, cy, cz, ang):   # brazo horizontal del candelabro (caja girada no hace falta: cilindro sobre X/Y aproximado por segmentos)
    L = 0.36
    return ("arm", cx, cy, cz, ang, L, "MAT_Laton", None, None, None)

# soporte para la pieza "arm" (cilindro horizontal orientado según un ángulo)
_build_orig = build
def build(name, parts, parent=None, loc=(0, 0, 0)):
    arms = [p for p in parts if p[0] == "arm"]
    ob = _build_orig(name, [p for p in parts if p[0] != "arm"], parent, loc)
    if arms:
        bm = bmesh.new(); bm.from_mesh(ob.data)
        idx = list(ob.data.materials).index(get_mat("MAT_Laton")) if get_mat("MAT_Laton") in list(ob.data.materials) else 0
        for _, cx, cy, cz, ang, L, *_ in arms:
            M = Matrix.Translation((cx, cy, cz)) @ Matrix.Rotation(ang, 4, "Z") @ Matrix.Rotation(math.radians(90), 4, "Y")
            r = bmesh.ops.create_cone(bm, cap_ends=True, segments=8, radius1=0.012, radius2=0.012, depth=L, matrix=M)
            for f in {f for v in r["verts"] for f in v.link_faces}: f.material_index = idx
        uv_caja(bm)
        bm.to_mesh(ob.data); bm.free()
    return ob

# ---------- entidad: espectro del hotel (diseño propio) ----------
def _hueso(bm, a, b, r0, r1, seg=8, mat_idx=0):
    """Cono entre dos puntos (r0 en a, r1 en b)."""
    a, b = Vector(a), Vector(b); d = b - a
    M = Matrix.Translation((a + b) / 2) @ d.to_track_quat("Z", "Y").to_matrix().to_4x4()
    res = bmesh.ops.create_cone(bm, cap_ends=True, segments=seg, radius1=r0, radius2=max(r1, 0.0005), depth=d.length, matrix=M)
    for f in {f for v in res["verts"] for f in v.link_faces}: f.material_index = mat_idx

def _elipsoide(bm, c, r, mat_idx, u=14, v=10, giro_y=0.0):
    res = bmesh.ops.create_uvsphere(bm, u_segments=u, v_segments=v, radius=1.0,
                                    matrix=Matrix.Translation(c) @ Matrix.Rotation(math.radians(giro_y), 4, "Y") @ Matrix.Diagonal((r[0], r[1], r[2], 1)))
    for f in {f for vv in res["verts"] for f in vv.link_faces}: f.material_index = mat_idx
    return res["verts"]

def _objeto(name, bm, mats, parent=None, loc=(0, 0, 0)):
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    uv_caja(bm)
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    for p in me.polygons: p.use_smooth = True
    for m in mats: me.materials.append(get_mat(m))
    ob = bpy.data.objects.new(name, me); COL.objects.link(ob)
    if parent: ob.parent = parent
    ob.location = loc
    return ob

def crear_entidad():
    """Espectro: manto raído con capucha, cráneo de cuencas vacías con mandíbula móvil, brazos largos y jirones que cuelgan.
    Mira hacia +Y, origen en el suelo. Brazos y jirones son objetos hijos con el origen en su articulación para animarlos en Unity."""
    import random
    rnd = random.Random(13)
    Z0, SEG = 0.30, 36
    anillos = [(2.62, 0.015, 0.015, -0.10), (2.55, 0.12, 0.13, -0.08), (2.42, 0.21, 0.23, -0.04), (2.25, 0.25, 0.27, 0.00),
               (2.08, 0.24, 0.25, 0.00), (1.96, 0.22, 0.20, -0.02), (1.86, 0.40, 0.24, -0.03), (1.74, 0.50, 0.27, -0.04),
               (1.50, 0.44, 0.27, -0.07), (1.20, 0.42, 0.30, -0.12), (0.90, 0.47, 0.35, -0.19), (0.60, 0.55, 0.42, -0.28),
               (0.35, 0.63, 0.50, -0.38), (0.15, 0.70, 0.56, -0.48)]
    n = len(anillos)
    bm = bmesh.new(); filas = []
    for i, (z, rx, ry, oy) in enumerate(anillos):
        amp = 0.02 + 0.20 * (i / (n - 1)) ** 1.5          # los pliegues crecen hacia el borde inferior
        fila = []
        for j in range(SEG):
            th = 2 * math.pi * j / SEG
            pl = 1 + amp * math.sin(7 * th + i * 0.6) + amp * 0.5 * math.sin(13 * th + i * 1.3)
            dz = rnd.uniform(-0.12, 0.08) if i == n - 1 else 0.0
            fila.append(bm.verts.new((rx * pl * math.cos(th), ry * pl * math.sin(th) + oy, z + Z0 + dz)))
        filas.append(fila)
    bm.faces.new(filas[0])
    for i in range(n - 1):
        for j in range(SEG):
            th = 2 * math.pi * (j + 0.5) / SEG
            frente = abs(th - math.pi / 2) < 0.80
            if frente and 2 <= i <= 3: continue                       # abertura de la capucha
            if i == 4 and abs(th - math.pi / 2) < 0.62: continue      # la abertura baja hasta la mandíbula
            if i == 5 and abs(th - math.pi / 2) < 0.42: continue
            if i == n - 2 and rnd.random() < 0.30: continue           # agujeros en el borde
            if i == n - 3 and rnd.random() < 0.12: continue
            a, b = filas[i][j], filas[i][(j + 1) % SEG]; c, d = filas[i + 1][(j + 1) % SEG], filas[i + 1][j]
            bm.faces.new((a, b, c, d))
    for j in range(0, SEG, 2):                                         # picos de tela rota
        if rnd.random() < 0.75:
            a, b = filas[-1][j], filas[-1][(j + 1) % SEG]
            L = rnd.uniform(0.15, 0.40); m = (a.co + b.co) / 2
            bm.faces.new((a, b, bm.verts.new((m.x * 1.03, m.y - L * 0.5, max(m.z - L, 0.04)))))
    manto = _objeto("ENT_Espectro", bm, ["MAT_EntManto"])

    # cabeza: cráneo con cuencas hundidas y sin mandíbula (0 cara, 1 ojos, 2 negro, 3 dientes)
    bm = bmesh.new(); cz, cy = 2.22 + Z0, 0.11
    cuencas = ((-1, 0.030, 0.034, 0.030, 0.060), (1, 0.024, 0.038, 0.033, 0.066))      # lado, altura, radio x, radio z, profundidad
    def hueco(x, z, ex, ez, rx, rz, prof):
        return prof * math.exp(-(((x - ex) / rx) ** 2 + ((z - ez) / rz) ** 2))
    def hundido(x, z):
        h = hueco(x, z, 0.0, cz - 0.040, 0.014, 0.026, 0.035)                           # fosa nasal
        for sx, ez, rx, rz, prof in cuencas: h += hueco(x, z, sx * 0.048, cz + ez, rx, rz, prof)
        return h
    verts = _elipsoide(bm, (0, cy, cz), (0.118, 0.112, 0.165), 0, 40, 28)
    for v in verts:
        if v.co.y <= cy: continue
        frente = min(1.0, (v.co.y - cy) / 0.112 * 2.5)
        x, z = v.co.x, v.co.z
        h = hundido(x, z)
        for sx in (-1, 1):
            h += hueco(x, z, sx * 0.075, cz - 0.060, 0.030, 0.045, 0.022)               # mejillas chupadas
            h += hueco(x, z, sx * 0.090, cz + 0.060, 0.025, 0.050, 0.012)               # sienes
        h -= hueco(x, z, 0.0, cz + 0.080, 0.085, 0.016, 0.012)                          # arco de las cejas
        v.co.y -= h * frente
    for f in bm.faces:                                                                  # el fondo de cuencas y nariz, en negro
        c = f.calc_center_median()
        if c.y > cy and hundido(c.x, c.z) > 0.030: f.material_index = 2
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.z < cz - 0.095], context="VERTS")   # sin mandíbula: queda el maxilar
    for sx, ez, rx, rz, prof in cuencas:                                                # pupilas: un punto de luz al fondo, desiguales
        r = 0.0075 if sx < 0 else 0.0050
        _elipsoide(bm, (sx * 0.048, cy + 0.058, cz + ez - (0.0 if sx < 0 else 0.008)), (r, r, r), 1, 8, 6)
    _elipsoide(bm, (0, cy - 0.02, cz - 0.13), (0.070, 0.075, 0.075), 2, 14, 10)           # garganta: un vacío negro tras los dientes
    for k in range(-5, 6):                                                              # dientes superiores, largos y disparejos
        a = k * 0.21
        x, y = 0.070 * math.sin(a), cy + 0.097 * math.cos(a) - 0.010
        L = 0.052 if abs(k) == 3 else rnd.uniform(0.022, 0.040)
        _hueso(bm, (x, y, cz - 0.088), (x * 0.97 + rnd.uniform(-0.004, 0.004), y, cz - 0.088 - L), 0.0075, 0.0015, 6, 3)
    _objeto("Mascara", bm, ["MAT_EntCara", "MAT_EntOjos", "MAT_EntNegro", "MAT_EntDiente"], manto)

    # mandíbula: objeto aparte con el origen en la bisagra para abrirla en Unity (0 hueso, 1 dientes)
    bm = bmesh.new()
    arco = [(0.082 * math.sin(a), 0.020 + 0.105 * math.cos(a), -0.110 - 0.022 * math.cos(a)) for a in [-1.25 + 2.5 * i / 10 for i in range(11)]]
    pts = [(-0.084, 0.035, 0.0), (-0.084, 0.030, -0.085)] + arco + [(0.084, 0.030, -0.085), (0.084, 0.035, 0.0)]
    for p, q in zip(pts[:-1], pts[1:]):
        _hueso(bm, p, q, 0.014, 0.014, 8, 0); _elipsoide(bm, q, (0.014, 0.014, 0.014), 0, 8, 6)
    for i, (x, y, z) in enumerate(arco[1:-1]):                                          # dientes inferiores
        L = 0.044 if i in (2, 6) else rnd.uniform(0.018, 0.034)
        _hueso(bm, (x * 0.93, y - 0.006, z + 0.008), (x * 0.90 + rnd.uniform(-0.004, 0.004), y - 0.006, z + 0.008 + L), 0.0070, 0.0015, 6, 1)
    _objeto("Mandibula", bm, ["MAT_EntHueso", "MAT_EntDiente"], manto, (0, cy - 0.02, cz - 0.10))

    # brazos: origen en el hombro; geometría en coordenadas locales, extendida hacia el frente
    for sx, name in ((-1, "BrazoIzq"), (1, "BrazoDer")):
        bm = bmesh.new()
        hombro, codo, muneca, palma = Vector((0, 0, 0)), Vector((sx * 0.10, 0.22, -0.52)), Vector((-sx * 0.06, 0.80, -0.30)), Vector((-sx * 0.08, 0.93, -0.28))
        _hueso(bm, hombro, codo, 0.036, 0.028); _hueso(bm, codo, muneca, 0.028, 0.020); _hueso(bm, muneca, palma, 0.022, 0.030)
        for c, r in ((hombro, 0.045), (codo, 0.036), (muneca, 0.026)): _elipsoide(bm, c, (r, r, r), 0, 10, 8)
        _elipsoide(bm, palma, (0.045, 0.035, 0.016), 0, 10, 8)
        for k, ang in enumerate((-38, -16, 2, 20, 55)):                # cuatro dedos y pulgar
            a = math.radians(ang) * -sx
            base = palma + Vector((math.sin(a) * 0.035, math.cos(a) * 0.03, 0))
            largo = (0.15, 0.19, 0.21, 0.18, 0.12)[k]
            p, r = base, 0.012
            for s, caida in enumerate((-0.10, -0.45, -0.95)):          # tres falanges que se curvan hacia abajo
                d = Vector((math.sin(a) * math.cos(caida), math.cos(a) * math.cos(caida), math.sin(caida))) * largo * (1.0, 0.8, 0.75)[s]
                q = p + d; r2 = (0.010, 0.007, 0.0)[s]
                _hueso(bm, p, q, r, r2, 6)
                if s < 2: _elipsoide(bm, q, (r2 * 1.25,) * 3, 0, 6, 5)
                p, r = q, r2
        _objeto(name, bm, ["MAT_EntHueso"], manto, (sx * 0.44, 0.02, 1.76 + Z0))

    # jirones: tiras de tela que cuelgan del borde, sobre todo por detrás
    z_b, rx_b, ry_b, oy_b = anillos[-1]
    for k, grados in enumerate((205, 235, 262, 290, 318, 345, 180)):
        th = math.radians(grados)
        bm = bmesh.new(); N = 6; L = rnd.uniform(0.30, 0.42); w0 = rnd.uniform(0.09, 0.14)
        tx, ty = -math.sin(th), math.cos(th)                           # tangente al borde
        prev = None
        for i in range(N + 1):
            t = i / N; w = w0 * (1 - t) ** 0.8 * 0.5
            cx, cy, czz = math.sin(t * 5 + k) * 0.03, -0.16 * t ** 1.3, -L * t
            if i == N: cur = (bm.verts.new((cx, cy, czz)),)
            else: cur = (bm.verts.new((cx - tx * w, cy - ty * w, czz)), bm.verts.new((cx + tx * w, cy + ty * w, czz)))
            if prev: bm.faces.new((prev[0], prev[1], cur[1], cur[0]) if len(cur) == 2 else (prev[0], prev[1], cur[0]))
            prev = cur
        _objeto("Jiron_%d" % k, bm, ["MAT_EntManto"], manto,
                (rx_b * 0.95 * math.cos(th), ry_b * 0.95 * math.sin(th) + oy_b, z_b + Z0 + 0.02))
    return manto

def main():
    global COL
    for o in list(bpy.data.objects): bpy.data.objects.remove(o, do_unlink=True)
    for blk in (bpy.data.meshes, bpy.data.materials, bpy.data.images, bpy.data.cameras, bpy.data.lights):
        for b in list(blk):
            if b.users == 0: blk.remove(b)
    COL = bpy.data.collections.get("UMBRAL_Kit") or bpy.data.collections.new("UMBRAL_Kit")
    if COL.name not in bpy.context.scene.collection.children: bpy.context.scene.collection.children.link(COL)
    A = crear_kit()
    os.makedirs(FBX_DIR, exist_ok=True); os.makedirs(os.path.dirname(BLEND), exist_ok=True)
    # exportar cada asset (con sus hijos) en el origen
    for name, ob in A.items():
        bpy.ops.object.select_all(action="DESELECT")
        ob.select_set(True)
        for ch in ob.children_recursive: ch.select_set(True)
        bpy.context.view_layer.objects.active = ob
        bpy.ops.export_scene.fbx(filepath=os.path.join(FBX_DIR, name + ".fbx"), use_selection=True,
            apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z", axis_up="Y", bake_space_transform=True,
            object_types={"MESH", "EMPTY"}, use_mesh_modifiers=True, mesh_smooth_type="FACE", add_leaf_bones=False, bake_anim=False)
    # distribuir en vitrina para revisar el .blend
    x = 0.0
    for name, ob in A.items():
        w = max(ob.dimensions.x, 0.9 if name == "PRP_Digitos" else 0.6)
        ob.location = (x + w / 2 if name not in ("EST_MuroEstandar","EST_MuroMedio","EST_MuroVentana","EST_MuroVano","EST_MarcoPuerta","INT_Puerta") else x, 0, ob.location.z)
        if name in ("INT_Lampara", "INT_Interruptor", "PRP_PlacaNumero"): ob.location.z = 1.5
        if name == "PRP_Candelabro": ob.location.z = 3.0
        x += w + 0.8
    bpy.ops.object.select_all(action="DESELECT")
    bpy.ops.wm.save_as_mainfile(filepath=BLEND)
    return A

RESULT = main()
