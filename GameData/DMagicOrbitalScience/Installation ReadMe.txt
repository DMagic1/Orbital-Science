Version 0.9
KSP Forum thread: http://forum.kerbalspaceprogram.com/threads/64972

Installation:

Place the GameData folder in the main KSP folder. Folder structure should be:
Kerbal Space Program/GameData/DMagic Orbital Science/...

Module Manager* required for some custom science reports - *Not included
http://forum.kerbalspaceprogram.com/threads/55219

Universal Storage* recommended for full part compatibility - *Not included
http://forum.kerbalspaceprogram.com/threads/75129

Parts may require repurchasing in the R&D center for career mode saves.

------------------

Source code available on Github: https://github.com/DMagic1/Orbital-Science









------------------

Note about compatibility:

Because of some conflicts with other mods, and the likelihood of future conflicts, the names of the magnetometer boom and both versions of the telescope have been changed. This will break compatibility with old crafts. 

Also note that the RPWS may end up backwards on existing crafts, this does not affect its ability to function. The part.cfg file in the RPWS should maintain the part's orientation on existing crafts (but has a few issues with attachment).

To keep existing crafts use the parts from the Backward Compatible Alternate Parts folder, available from the KSP forum thread linked above. Delete any old versions and place these in the main KSP GameData folder.

Alternately you can install the default parts and manually change the part names in the .cfg file. 

The first several lines of each .cfg file look like this:

-------------------

PART
{
name = dmmagBoom
module = Part
author = DMagic

mesh = model.mu
scale = 1
rescaleFactor = 1

-------------------

Change the name = dmmagBoom to name = magBoom to retain compatibility with the old versions. For the telescope, change name = dmscope (or name = dmSCANscope) to name = scope. The RPWS does not require any alterations. DO NOT change any other entries in the .cfg file unless you know what you're doing.