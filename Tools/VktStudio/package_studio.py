"""Build a portable, deterministic local marketplace kit without credentials or caches."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import zipfile


def package(marketplace, output):
    root=Path(marketplace).resolve(strict=True)
    output=Path(output).resolve(strict=False)
    if output.exists() or output.is_relative_to(root):
        raise ValueError("Choose a new archive path outside the source marketplace")
    registry=json.loads((root/'.agents/plugins/marketplace.json').read_text(encoding='utf-8'))
    if len(registry['plugins'])!=1: raise ValueError("This kit must contain exactly one plugin")
    entry=registry['plugins'][0]
    plugin=(root/entry['source']['path']).resolve(strict=True)
    if not plugin.is_relative_to(root): raise ValueError("Plugin source leaves the marketplace")
    manifest=json.loads((plugin/'plugin.json').read_text(encoding='utf-8'))
    overlay=json.loads((plugin/'.codex-plugin/plugin.json').read_text(encoding='utf-8'))
    ui=manifest['extensions']['com.openai']['interface']
    if manifest['name']!=plugin.name or entry['name']!=manifest['name']: raise ValueError("Plugin identities differ")
    for key in ('name','version'):
        if overlay[key]!=manifest[key]: raise ValueError("Compatibility manifest differs: "+key)
    for key in ('displayName','shortDescription'):
        if overlay['interface'][key]!=ui[key]: raise ValueError("Compatibility presentation differs")
    if len(ui['shortDescription'])>30: raise ValueError("Subtitle exceeds 30 characters")
    skills=sorted((plugin/'skills').iterdir())
    if len(skills)!=3: raise ValueError("Expected three connected skills")
    for skill in skills:
        text=(skill/'SKILL.md').read_text(encoding='utf-8')
        if not re.search(r'^name: '+re.escape(skill.name)+r'\s*$',text,re.M): raise ValueError("Skill identity differs")
        if not (skill/'agents/openai.yaml').is_file(): raise ValueError("Skill host metadata missing")
    files=[]
    for path in sorted(root.rglob('*')):
        if any(part in ('__pycache__','.pytest_cache') for part in path.relative_to(root).parts): continue
        if path.is_symlink() or not path.resolve().is_relative_to(root): raise ValueError("Linked paths outside the kit are not packaged")
        if not path.is_file(): continue
        if path.suffix not in ('.md','.json','.yaml','.py') and path.name!='LICENSE': raise ValueError("Unexpected kit file: "+path.name)
        data=path.read_bytes()
        if re.search(rb'gh[pousr]_[A-Za-z0-9_]{20,}|github_pat_[A-Za-z0-9_]{20,}|-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----',data):
            raise ValueError("Potential secret in kit; inspect locally")
        files.append((path.relative_to(root).as_posix(),data))
    output.parent.mkdir(parents=True,exist_ok=True)
    with output.open('xb') as stream:
        with zipfile.ZipFile(stream,'w',compression=zipfile.ZIP_DEFLATED,compresslevel=6) as archive:
            for name,data in files:
                info=zipfile.ZipInfo('konglomerat-local/'+name,date_time=(2026,10,18,0,0,0))
                info.compress_type=zipfile.ZIP_DEFLATED;info.external_attr=0o100644<<16
                archive.writestr(info,data)
    return dict(name=output.name,size=output.stat().st_size,sha256=hashlib.sha256(output.read_bytes()).hexdigest(),files=len(files),version=manifest['version'])


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--marketplace',type=Path,required=True);parser.add_argument('--output',type=Path,required=True)
    args=parser.parse_args()
    try: print(json.dumps(package(args.marketplace,args.output),indent=2))
    except (OSError,ValueError,KeyError) as error: parser.exit(2,str(error)+'\n')
