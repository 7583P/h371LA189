"""Offline review gallery using untouched PNG evidence and camera metadata."""
import argparse
from html import escape
import json
from pathlib import Path


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("root", type=Path)
    args = parser.parse_args()
    before = json.loads((args.root / "before/visual-evidence.json").read_text(encoding="utf-8-sig"))
    after = json.loads((args.root / "after/visual-evidence.json").read_text(encoding="utf-8-sig"))
    assert before["navigationSha256"] == after["navigationSha256"]
    assert before["geometrySha256"] == after["geometrySha256"]
    tiles = []
    for a, b in zip(before["views"], after["views"], strict=True):
        assert all(a[k] == b[k] for k in ("file", "position", "target", "fov", "lighting", "quality"))
        name = escape(a["file"])
        tiles.append(f'<article><h2>{name}</h2><div class="compare"><img src="before/{name}" alt="Antes"><div class="after"><img src="after/{name}" alt="Propuesta"></div><span>ANTES</span><b>PROPUESTA QA</b></div><input type="range" min="0" max="100" value="50" aria-label="Comparar antes y propuesta"><details><summary>Cámara y condiciones</summary><pre>{escape(json.dumps(a, indent=2))}</pre></details></article>')
    page = '''<!doctype html><html lang="es"><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>Rainbow Road — revisión QA</title>
<style>body{background:#0a101e;color:#e5ebf9;font:16px system-ui;max-width:1400px;margin:32px auto;padding:0 20px}h1{font-size:30px}p{line-height:1.6;color:#b9c7dc}.tag{color:#ffcf70}article{margin:36px 0;background:#111b2c;padding:18px;border-radius:12px}h2{font-size:17px}.compare{position:relative;aspect-ratio:16/9;overflow:hidden}.compare>img,.after>img{width:100%;display:block}.after{position:absolute;inset:0;clip-path:inset(0 0 0 50%)}.compare span,.compare b{position:absolute;top:12px;background:#061023dc;padding:7px;font-size:12px;letter-spacing:2px}.compare span{left:10px}.compare b{right:10px;color:#8cdff5}input{width:100%;margin:15px 0;accent-color:#55d9ff}pre{white-space:pre-wrap;font-size:12px}</style>
<h1>Rainbow Road / propuesta orbital</h1><p class="tag">REVISIÓN HUMANA PENDIENTE · NO ES APROBACIÓN DE RELEASE</p><p>Desliza para comparar PNG originales con posición, orientación, FOV y resolución idénticos. Navegación y colisiones tienen hashes iguales. La propuesta requiere <code>--rainbow-preview</code> en Editor/Development. Las capturas de día/Low son pruebas host; Android requiere su propio dispositivo. No se ha cambiado el perfil físico de la carretera.</p>
''' + "".join(tiles) + '''<script>document.querySelectorAll('article').forEach(a=>a.querySelector('input').addEventListener('input',e=>a.querySelector('.after').style.clipPath=`inset(0 0 0 ${e.target.value}%)`))</script></html>'''
    with (args.root / "REVIEW.html").open("x", encoding="utf-8") as stream:
        stream.write(page)
    print(f"{len(tiles)} fixed-camera comparisons; navigation and collision parity verified")


if __name__ == "__main__":
    main()
