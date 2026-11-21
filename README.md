# Unfair Balance

A [Unity Mod Manager](https://github.com/newman55/unity-mod-manager) mod for *Pathfinder: Wrath of the Righteous*.

On Hard and Unfair, the **Damage to party** setting raises the damage your characters take. Unfair Balance scales the party's hit points and healing by that same multiplier, so the extra incoming damage is matched by extra life and extra healing. On Core and below, where the multiplier is 1, hit points and healing stay as they are.

The mod only affects characters in the player faction.

## Options

All three options are on by default. They appear in the Unity Mod Manager window.

**2x Life.** Multiplies a party member's finished hit-point total by the Damage to party multiplier. Class hit dice, Constitution, the favored class bonus, Toughness, and Mythic Toughness are added first, then the total is multiplied.

**2x Heal.** Multiplies healing received by a party member by the same multiplier, after the roll. This covers cure spells, channel energy, lay on hands, potions, and fast healing.

**2x Background.** Adds an optional second background during character creation, mercenary hiring, and respec. A button on the background screen lets you pick it. Pets are not offered a second background.

The option names say "2x" because Unfair's Damage to party value is 2. The actual scale is whatever that difficulty setting currently is, as long as it is above 1.

## Requirements

- *Pathfinder: Wrath of the Righteous*
- Unity Mod Manager 0.23 or newer

## Install

Build the project, or copy `UnfairBalance.dll` and `Info.json` into `Mods/UnfairBalance` under the game folder. A build copies both files there when `GamePath` in `Directory.Build.props` points at an existing install.
