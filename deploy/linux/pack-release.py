"""Pack a linux-x64 publish folder into chemeats.tgz with Unix permissions.

Windows `tar` cannot mark files executable, and chemeats-deploy rejects an archive whose
`WebApi` is not executable. Usage (repo root):  python deploy/linux/pack-release.py publish-linux chemeats.tgz
"""
import sys
import tarfile

EXECUTABLES = {'WebApi', 'createdump'}


def normalize(info: tarfile.TarInfo) -> tarfile.TarInfo:
    name = info.name[2:] if info.name.startswith('./') else info.name
    info.mode = 0o755 if info.isdir() or name in EXECUTABLES else 0o644
    info.uid = info.gid = 0
    info.uname = info.gname = ''
    return info


def main() -> None:
    if len(sys.argv) != 3:
        sys.exit('usage: pack-release.py <publish-dir> <archive.tgz>')
    source, archive = sys.argv[1], sys.argv[2]
    with tarfile.open(archive, 'w:gz') as tar:
        tar.add(source, arcname='.', filter=normalize)
    with tarfile.open(archive) as tar:
        mode = tar.getmember('./WebApi').mode
    if mode != 0o755:
        sys.exit(f'WebApi mode is {oct(mode)}, expected 0o755')
    print(f'{archive}: WebApi is executable')


if __name__ == '__main__':
    main()
