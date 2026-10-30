// Baja modelos CC0 de Poly Haven (FBX 1k + sus texturas) a Assets/Art/Hospital/PolyHaven/<id>/
// Uso: node polyhaven_bajar.js <carpeta_destino> id1 id2 ...
const fs = require('fs'), path = require('path');
const [, , destino, ...ids] = process.argv;
const UA = { headers: { 'User-Agent': 'EstanDentro-mejoras' } };

async function bajar(url, archivo) {
  const r = await fetch(url, UA);
  if (!r.ok) throw new Error(`${r.status} ${url}`);
  fs.mkdirSync(path.dirname(archivo), { recursive: true });
  fs.writeFileSync(archivo, Buffer.from(await r.arrayBuffer()));
}

(async () => {
  const creditos = [];
  for (const id of ids) {
    const info = await (await fetch(`https://api.polyhaven.com/files/${id}`, UA)).json();
    const f = info.fbx && (info.fbx['1k'] || info.fbx['2k']);
    if (!f) { console.log(`${id}: sin FBX`); continue; }
    const dir = path.join(destino, id);
    await bajar(f.fbx.url, path.join(dir, path.basename(f.fbx.url)));
    for (const [rel, inc] of Object.entries(f.fbx.include || {})) await bajar(inc.url, path.join(dir, rel));
    const meta = await (await fetch(`https://api.polyhaven.com/info/${id}`, UA)).json();
    creditos.push(`- ${meta.name || id} (${id}) — ${Object.keys(meta.authors || {}).join(', ')} — CC0, polyhaven.com/a/${id}`);
    console.log(`${id}: ok (${Object.keys(f.fbx.include || {}).length} texturas)`);
  }
  fs.writeFileSync(path.join(destino, 'CREDITOS.md'), '# Modelos de Poly Haven (CC0)\n\n' + creditos.join('\n') + '\n');
})().catch(e => { console.error(e); process.exit(1); });
