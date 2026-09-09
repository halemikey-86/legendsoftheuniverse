10th Planet Dojo arena pieces (Meshy FBX):

  DojoWalls/   — wall panels around the table
  PurpleMat/   — purple play surface mesh + PBR textures

Add DojoArenaView to the Table prefab (with TableView). It auto-loads the first FBX
in each subfolder. Tune Walls/Mat scale and position in the inspector until the mat
matches PlaymatZones (32×18 world units). Cards use zone anchors on top of the mat.

Optional fallback: flat JPG playmat from Assets/Playmats/ — disable Use Mesh Playmat
on DojoArenaView to use the plane again.
