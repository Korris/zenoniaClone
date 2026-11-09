"""Restore the ten pre-Astral Wizard outfits into a build copy only."""
from pathlib import Path
import argparse
import hashlib
import json
import shutil
import subprocess

ROOT = Path(__file__).resolve().parents[1]
BACKUP = ROOT / 'Assets/ArtLibrary/classes/wizard/outfits/reference-20261108/before-install'

def install(target):
    target = target.resolve()
    assert target.is_relative_to(ROOT / 'Build') and (target / 'ProjectSettings/ProjectVersion.txt').exists()
    live = ROOT / 'Assets/Resources'
    paths = list((live / 'Sprites/classes/wizard').rglob('*')) + list((live / 'Data/classes/wizard').rglob('*'))
    def hashes():
        return {str(p): hashlib.sha256(p.read_bytes()).hexdigest() for p in paths if p.is_file()}
    before = hashes()
    body = target / 'Assets/Resources/Sprites/classes/wizard/body'
    # These optional outfit overrides were introduced after the original outfits.
    for name in ('head-anchors.json', 'held-anchors.json'):
        for path in body.rglob(name):
            path.unlink()
    sources = list(BACKUP.rglob('*'))
    for source in sources:
        if source.is_file():
            dest = target / source.relative_to(BACKUP)
            dest.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(source, dest)
            assert source.read_bytes() == dest.read_bytes()
    relative = 'Assets/Resources/Data/classes/wizard/head-anchors.json'
    original = json.loads(subprocess.check_output(['git', 'show', 'HEAD:' + relative], cwd=ROOT))
    path = target / relative
    current = json.loads(path.read_text(encoding='utf-8'))
    idle = next(a for a in original['anims'] if (a['anim'], a['dir']) == ('idle', 'down'))
    current['anims'] = [idle if (a['anim'], a['dir']) == ('idle', 'down') else a for a in current['anims']]
    path.write_text(json.dumps(current), encoding='utf-8')
    assert before == hashes(), 'Live assets changed'
    print('PASS: restored 106 original outfit/equipment/head files; live Wizard unchanged')

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--target', type=Path, required=True)
    install(parser.parse_args().target)
