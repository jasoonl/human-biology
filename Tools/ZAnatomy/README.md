# Z-Anatomy pipeline

The explorer's anatomy comes from the Z-Anatomy atlas (CC BY-SA 4.0; see `/NOTICE.md`). The source data is large and
is not committed: `ZAnatomyData/` (git-ignored) holds it, and the generated meshes under `Assets/Generated/Meshes/` are
what the scene references. Without `ZAnatomyData/`, the build falls back to the hand-sculpted figure.

## Regenerating `ZAnatomyData/`

1. Download `Z-Anatomy.zip` from https://github.com/Z-Anatomy/Models-of-human-anatomy (it contains `Startup.blend`) and
   Blender 4.2 from https://www.blender.org.
2. Run each script with `Blender -b Z-Anatomy/Startup.blend --python <script>` and copy the outputs into `ZAnatomyData/`:
   - `export.py` -> `zana.bin`, `zana_index.json` (every mesh, in the explorer's axes: x left, y up, z back)
   - `export_lines.py` -> `zana_lines.json` (vessel and nerve centre lines with radii)
   - `skinfield.py`, run four times with `SKIN_MODE=body` (also with `SKIN_OUT=skinfield_f.bin` and
     `SKIN_EXCLUDE='penis|^Testis|^Epididymis|^Ductus deferens|Ejaculatory|^Prostate|^Seminal|^Urethra|Glans'` for the
     female exterior), `head` and `hand` -> `skinfield.bin`, `skinfield_f.bin`, `skinhead.bin`, `skinhand.bin`
3. `python3 Tools/ZAnatomy/build_map.py` regenerates `Assets/Editor/Geometry/zanatomy_map.json`, which says which atlas
   objects make up each entity id.
4. Rebuild the scene (Human Body Explorer > Build Full Explorer).
