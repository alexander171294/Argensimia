# Argensimia — empaquetado de la release (correr desde el MCP de Blender, que tiene acceso a la Mac):
#   exec(open(".../BlenderSrc/release.py").read()); icons(); windows(); macos(); linux()
# Entrada: Builds/{Windows,macOS,Linux} (builds de Unity) + Assets/Art/Icon/AppIcon.png
# Salida:  Release/Argensimia-<versión>-Windows-Setup.exe, -macOS.dmg, -Linux-x86_64.AppImage
import os, shutil, struct, subprocess, urllib.request

PROJECT = '/Users/alexander/git/argensimia/Argensimia'
BUILDS = PROJECT + '/Builds'
OUT = PROJECT + '/Release'
ICON = PROJECT + '/Assets/Art/Icon/AppIcon.png'
ENV = dict(os.environ, PATH=os.environ.get('PATH', '') + ':/opt/homebrew/bin:/usr/local/bin', LANG='en_US.UTF-8', LC_ALL='en_US.UTF-8')  # makensis se cae sin locale

def version():
    for l in open(PROJECT + '/ProjectSettings/ProjectSettings.asset', encoding='utf-8'):
        if l.strip().startswith('bundleVersion:'): return l.split(':', 1)[1].strip()
    return '0.0.0'

def sh(cmd, cwd=None, timeout=1800):
    r = subprocess.run(cmd, shell=True, cwd=cwd, capture_output=True, text=True, timeout=timeout, env=ENV)
    if r.returncode != 0: raise RuntimeError(cmd + '\n' + (r.stdout + r.stderr)[-2000:])
    return (r.stdout + r.stderr)[-1500:]

def _rgba(size):
    # escala el ícono con bpy y devuelve los píxeles RGBA (fila 0 = abajo) como bytes 0-255
    import bpy
    img = bpy.data.images.load(ICON); img.scale(size, size)
    px = [max(0, min(255, int(round(v * 255)))) for v in img.pixels[:]]
    bpy.data.images.remove(img)
    return px

def _png(size, path):
    import bpy
    img = bpy.data.images.load(ICON); img.scale(size, size)
    img.filepath_raw = path; img.file_format = 'PNG'; img.save()
    bpy.data.images.remove(img)
    return path

def icons():
    os.makedirs(OUT + '/tmp', exist_ok=True)
    # .ico: 16-128 como DIB de 32 bits, 256 como PNG
    entries = []
    for s in (16, 24, 32, 48, 64, 128):
        px = _rgba(s)
        rows = b''
        for y in range(s):  # el DIB va de abajo hacia arriba, igual que Blender
            for x in range(s):
                i = (y * s + x) * 4
                r, g, b, a = px[i:i + 4]
                rows += bytes((b, g, r, 255))
        mask_row = ((s + 31) // 32) * 4
        data = struct.pack('<IiiHHIIiiII', 40, s, s * 2, 1, 32, 0, len(rows), 0, 0, 0, 0) + rows + b'\0' * (mask_row * s)
        entries.append((s, data))
    entries.append((256, open(_png(256, OUT + '/tmp/icon256.png'), 'rb').read()))
    head = struct.pack('<HHH', 0, 1, len(entries))
    off = 6 + 16 * len(entries)
    dirs, blobs = b'', b''
    for s, data in entries:
        dirs += struct.pack('<BBBBHHII', s % 256, s % 256, 0, 0, 1, 32, len(data), off + len(blobs))
        blobs += data
    open(OUT + '/tmp/argensimia.ico', 'wb').write(head + dirs + blobs)
    _png(256, OUT + '/tmp/argensimia.png')
    return OUT + '/tmp/argensimia.ico'

NSI = r"""
Unicode true
!include "MUI2.nsh"
Name "Argensimia"
OutFile "{out}"
InstallDir "$PROGRAMFILES64\Argensimia"
InstallDirRegKey HKLM "Software\Argensimia" "InstallDir"
RequestExecutionLevel admin
SetCompressor /SOLID lzma
!define MUI_ICON "{ico}"
!define MUI_UNICON "{ico}"
!define MUI_ABORTWARNING
!define MUI_FINISHPAGE_RUN "$INSTDIR\Argensimia.exe"
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "Spanish"
!insertmacro MUI_LANGUAGE "English"
VIProductVersion "{ver4}"
VIAddVersionKey /LANG=${{LANG_SPANISH}} "ProductName" "Argensimia"
VIAddVersionKey /LANG=${{LANG_SPANISH}} "FileVersion" "{ver}"
VIAddVersionKey /LANG=${{LANG_SPANISH}} "FileDescription" "Instalador de Argensimia"
VIAddVersionKey /LANG=${{LANG_SPANISH}} "LegalCopyright" "alexander171294"

Section "Argensimia"
  SetOutPath "$INSTDIR"
  File /r "{src}\*.*"
  File "/oname=argensimia.ico" "{ico}"
  WriteUninstaller "$INSTDIR\Uninstall.exe"
  CreateDirectory "$SMPROGRAMS\Argensimia"
  CreateShortcut "$SMPROGRAMS\Argensimia\Argensimia.lnk" "$INSTDIR\Argensimia.exe" "" "$INSTDIR\argensimia.ico"
  CreateShortcut "$SMPROGRAMS\Argensimia\Desinstalar Argensimia.lnk" "$INSTDIR\Uninstall.exe"
  CreateShortcut "$DESKTOP\Argensimia.lnk" "$INSTDIR\Argensimia.exe" "" "$INSTDIR\argensimia.ico"
  WriteRegStr HKLM "Software\Argensimia" "InstallDir" "$INSTDIR"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Argensimia" "DisplayName" "Argensimia"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Argensimia" "DisplayVersion" "{ver}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Argensimia" "Publisher" "alexander171294"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Argensimia" "DisplayIcon" "$INSTDIR\argensimia.ico"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Argensimia" "UninstallString" '"$INSTDIR\Uninstall.exe"'
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Argensimia" "NoModify" 1
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Argensimia" "NoRepair" 1
SectionEnd

Section "Uninstall"
  Delete "$DESKTOP\Argensimia.lnk"
  RMDir /r "$SMPROGRAMS\Argensimia"
  RMDir /r "$INSTDIR"
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Argensimia"
  DeleteRegKey HKLM "Software\Argensimia"
SectionEnd
"""

def windows():
    v = version()
    src = BUILDS + '/Windows'
    for junk in ('Argensimia_BurstDebugInformation_DoNotShip', 'Argensimia_BackUpThisFolder_ButDontShipItWithYourGame'):
        if os.path.isdir(src + '/' + junk): shutil.rmtree(src + '/' + junk)
    out = OUT + '/Argensimia-%s-Windows-Setup.exe' % v
    nums = (v.split('.') + ['0', '0', '0', '0'])[:4]
    nsi = NSI.format(out=out, ico=OUT + '/tmp/argensimia.ico', src=src, ver=v, ver4='.'.join(nums))
    open(OUT + '/tmp/argensimia.nsi', 'w', encoding='utf-8').write(nsi)
    sh('makensis -V2 "%s"' % (OUT + '/tmp/argensimia.nsi'))
    return out

def macos():
    v = version()
    app = BUILDS + '/macOS/Argensimia.app'
    sh('codesign --force --deep -s - "%s"' % app)  # firma ad hoc: sin esto Apple Silicon lo da por "dañado"
    stage = OUT + '/tmp/dmg'
    if os.path.isdir(stage): shutil.rmtree(stage)
    os.makedirs(stage)
    sh('ditto "%s" "%s/Argensimia.app"' % (app, stage))
    os.symlink('/Applications', stage + '/Applications')
    out = OUT + '/Argensimia-%s-macOS.dmg' % v
    if os.path.exists(out): os.remove(out)
    sh('hdiutil create -volname "Argensimia %s" -srcfolder "%s" -ov -format UDZO "%s"' % (v, stage, out))
    return out

RUNTIME = 'https://github.com/AppImage/type2-runtime/releases/download/continuous/runtime-x86_64'
APPRUN = """#!/bin/sh
HERE="$(dirname "$(readlink -f "$0")")"
cd "$HERE/usr/bin"
exec "$HERE/usr/bin/Argensimia.x86_64" "$@"
"""
DESKTOP = """[Desktop Entry]
Type=Application
Name=Argensimia
Comment=Tower defense en la Plaza de Mayo
Exec=Argensimia
Icon=argensimia
Categories=Game;StrategyGame;
Terminal=false
"""

def linux():
    v = version()
    appdir = OUT + '/tmp/Argensimia.AppDir'
    if os.path.isdir(appdir): shutil.rmtree(appdir)
    os.makedirs(appdir + '/usr/bin')
    sh('ditto "%s" "%s/usr/bin"' % (BUILDS + '/Linux', appdir))
    for junk in ('Argensimia_BurstDebugInformation_DoNotShip', 'Argensimia_BackUpThisFolder_ButDontShipItWithYourGame'):
        p = appdir + '/usr/bin/' + junk
        if os.path.isdir(p): shutil.rmtree(p)
    open(appdir + '/AppRun', 'w').write(APPRUN)
    os.chmod(appdir + '/AppRun', 0o755)
    os.chmod(appdir + '/usr/bin/Argensimia.x86_64', 0o755)
    open(appdir + '/argensimia.desktop', 'w').write(DESKTOP)
    shutil.copy(OUT + '/tmp/argensimia.png', appdir + '/argensimia.png')
    shutil.copy(OUT + '/tmp/argensimia.png', appdir + '/.DirIcon')
    rt = OUT + '/tmp/runtime-x86_64'
    if not os.path.exists(rt):
        req = urllib.request.Request(RUNTIME, headers={'User-Agent': 'Mozilla/5.0'})
        with urllib.request.urlopen(req, timeout=120) as r: open(rt, 'wb').write(r.read())
    sq = OUT + '/tmp/argensimia.squashfs'
    if os.path.exists(sq): os.remove(sq)
    sh('mksquashfs "%s" "%s" -root-owned -noappend -comp zstd -quiet' % (appdir, sq))
    out = OUT + '/Argensimia-%s-Linux-x86_64.AppImage' % v
    with open(out, 'wb') as f:
        f.write(open(rt, 'rb').read())
        f.write(open(sq, 'rb').read())
    os.chmod(out, 0o755)
    return out
