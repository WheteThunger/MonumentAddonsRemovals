## Features

- Allows permanently removing entities from monuments

## Required dependencies

- [Monument Addons](https://umod.org/plugins/monument-addons)

## Permissions

This plugin does not define any permissions, but it does check for the `monumentaddons.admin` permission registered by Monument Addons, for all of the below commands.

## Commands

- `mar.add` -- Adds a prefab to the remover you are aiming at.
- `mar.remove` -- Removes a prefab from the remover you are aiming at.
- `mar.radius` -- Updates the radius of the remover you are aiming at.

## How it works

This plugin registers a custom addon called `remover` with the Monument Addons plugin. The remover addon finds and kills nearby entities that match a list of prefabs. For example, if you want to remove a door from a monument, you can place a remover nearby the door that specifically removes that door prefab.

## Getting started

1. Find an entity you want to remove from a monument
2. Aim at the base of the entity and run `maspawn remover` (technically this is a MonumentAddons command, and requires the `monumentaddons.admin` permission)
3. Continue aiming at the same spot and run `mar.add <prefab>` where `<prefab>` is replaced with the full prefab name of the entity you want to remove, such as `mar.add "assets/bundled/prefabs/static/door.hinged.industrial_a_a.prefab"` 

If there are any entities within radius that match that prefab, they will be immediately removed. Additionally, any time the plugin reloads, or the corresponding Monument Addons profile loads, the same search and destroy operation will be performed.
