# Game-ready weapon meshes

Each weapon type has one reusable FBX and one prefab in `Assets/Resources/Prefabs/Visual/Weapons`. Weapon asset names describe the weapon type, not the character. The unit prefabs mount these reusable weapon prefabs under the listed attachment bones. For the second pistol and dagger, the same mesh is mirrored with a negative local scale. Source character FBXs are unchanged.

| Character | Weapon prefab(s) | Attachment bone(s) |
| --- | --- | --- |
| Buccaneer | Pistol (two instances; one mirrored) | holdObject_R / holdObject_L |
| Rogue | Dagger (two instances; one mirrored) | holdObject_R / holdObject_L |
| Pirate | Hook / Cutlass | Wrist_R / holdObject_L |
| Sailor | Oar | holdObject_L |
| Militia | Spear / Shield | holdObject_R / holdObject_L |
| SkeletonArcher | Bow / Arrow | holdObject_L / holdObject_R |
| SkeletonSoldier | Sword | holdObject_L |
| Demon | Trident | holdObject_L |

Imp uses the existing separated `Characters/Bes.fbx` and has no detached weapon.
