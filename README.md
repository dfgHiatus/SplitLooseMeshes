#  SplitLooseMeshes

I woke up one Saturday and thought this would be a good idea for a weekend project. This is a [NeosModLoader](https://github.com/zkxs/NeosModLoader) mod for [Neos VR](https://neos.com/), that adds a button to split a mesh by loose parts [just like in Blender!](https://docs.blender.org/manual/en/latest/modeling/meshes/editing/mesh/separate.html)

## Installation
1. Install [NeosModLoader](https://github.com/zkxs/NeosModLoader).
1. Place [SplitLooseMeshes.dll](https://github.com/dfgHiatus/SplitLooseMeshes/releases/latest) into your `nml_mods` folder. This folder should be at `C:\Program Files (x86)\Steam\steamapps\common\NeosVR\nml_mods` for a default install. You can create it if it's missing, or if you launch the game once with NeosModLoader installed it will create the folder for you.
1. Start the game. If you want to verify that the mod is working you can check your Neos logs.

## Known limitations
1) This mod can take a lot of time depending on the size your input (mesh vertex count, etc.)
1) The merging doubles option sometimes seems to not preserve UV's and other important information
1) This mod can be "aggressive" when splitting up a model, sometimes creating more loose parts than what is needed. This can cause some unwanted lag
1) As of v1.0.0, tested only on MeshRenderers

### And of course, back up your models before doing any kind of work on them!
