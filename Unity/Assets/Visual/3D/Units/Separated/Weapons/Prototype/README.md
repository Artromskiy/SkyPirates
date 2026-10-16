# Character weapon split prototypes

These FBX files are derived copies of the separated character FBXs in `../Characters`. The source character FBXs and their importer settings are untouched.

For each character, `<Name>_Character.fbx` contains the body and `<Name>_Weapons.fbx` contains separately named, skinned equipment meshes. The split uses disconnected mesh components. Square hands and arm geometry remain in the character mesh; weapon geometry retains its original vertex colors, material slots, UVs, vertex groups, and armature weights.

| Character | Weapon/equipment meshes |
| --- | --- |
| Bucaneer | left pistol, right pistol |
| Bandit | left dagger, right dagger (grips and blades; hands remain with the character) |
| Pirate | hook, cutlass |
| Sailor | oar |
| Militia | spear, shield |
| SkeletonArcher | bow and string, arrow and tip (held hand remains with the character) |
| SkeletonSoldier | sword (including the grip) |
| Demon | trident |
| Bes | no detached weapon; shown in the full model preview |

Each exported FBX contains the source 25-bone armature so its skinned parts can be inspected independently. A game prefab should use one character armature and bind the equipment meshes to it before replacing the current model. These are extraction prototypes, not prefab replacements.

The FBX outputs were re-imported in Blender to verify polygon counts, vertex groups, armature modifiers, material slots, vertex colors, and UV layers. Unity import and in-game attachment have not been validated in this step.
