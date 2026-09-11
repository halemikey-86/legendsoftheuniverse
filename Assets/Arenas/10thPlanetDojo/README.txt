10th Planet Dojo arena pieces (Meshy FBX):

  DojoWalls/          — wall panels around the table
  Dark Wall/          — alternate wall panel (auto-fallback)
  PurpleMat/          — purple play surface mesh + PBR textures
  DarkMay/            — newer purple mat variant (preferred if present)
  Towel and Gym bag/  — prop: placed near supply row
  Belt and Shoes/     — prop: placed near icon column

Add DojoArenaView to the Table prefab (with TableView). It auto-loads the first FBX
in each subfolder. Tune Walls/Mat scale and position in the inspector until the mat
matches PlaymatZones (32×18 world units). Cards use zone anchors on top of the mat.

Props: select Table → DojoArenaView → right-click → "Auto Assign Dojo Props", then
tune each prop's position/scale in the Props array. Rebuild with "Rebuild Dojo Arena".

Fonts: Willbound Stamp lives in Assets/Fonts/ (source) and Assets/Resources/Fonts/
(runtime). GameFonts.cs loads it for all UI text.

Optional fallback: flat JPG playmat from Assets/Playmats/ — disable Use Mesh Playmat
on DojoArenaView to use the plane again.
