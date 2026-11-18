# 关羽守荆州

A single-battle Unity prototype in the style of 三国志英杰传: a top-down square grid, turn-based movement and attacks, and generals who fight with a troop type and 兵力. One player defends Jingzhou against a simple enemy AI.

Unity is not required to read the battle data, but you play it in the Unity Editor. The project uses the built-in render pipeline and a 2D orthographic camera. Unity 2022.3 LTS is the intended version; Unity 6 can open the project and upgrade it.

## Open and play

1. Install [Unity 2022.3 LTS](https://unity.com/releases/editor/archive) (or Unity 6) with the Windows, Mac, or Linux build module.
2. In Unity Hub, choose **Add project from disk** and select this repository folder.
3. Open `Assets/Scenes/JingzhouBattle.unity`. On a fresh project the editor opens that scene for you. Press **Play**.
4. Use a window around 1600×900. The board is on the left and the orders panel is on the right.

The first import builds a small Chinese font. If names show as empty boxes, select `Assets/Resources/Fonts/JingzhouFont.ttf` and set **Character** to **Dynamic**. An editor script does this on load.

If Play mode reports that the old Input class is disabled, set **Project Settings > Player > Active Input Handling** to **Input Manager (Old)** or **Both**, then press Play again.

## How to play

You move first. Each of your generals can move, then attack, then their turn is over. **Esc** cancels a selection before that general has moved.

- Click a Shu general (green token). Green tiles are the move range. Click one to move. Four directions only; forests and hills cost 2 movement, the river cannot be entered.
- Red tiles are enemies you can attack from the current square. Click one to attack. The panel shows the damage, the matchup, and whether they can strike back.
- **待命** ends that general's action without an attack.
- **结束回合** gives the turn to Wu. If someone has not acted, press it again to confirm.
- **再战** starts the same battle over after a result.

弓兵 have range 2 and are not countered by a melee general unless you step next to them. A straight shot is blocked by any general standing on a tile between the archer and the target.

### Troop matchups

| Attacker | Strong against | Weak against |
| --- | --- | --- |
| 骑兵 | 弓兵 | 步兵 |
| 弓兵 | 步兵 | 骑兵 |
| 步兵 | 骑兵 | 弓兵 |

Damage also scales with the attacker's 兵力 and is reduced by forest, hill, and the Jingzhou tile. At 0 兵力 the general retreats.

### The battle

Wu is coming down from the north. The river is impassable except for two fords (the light tiles breaking the blue line). Jingzhou is the gold tile.

| Side | General | Type | Role |
| --- | --- | --- | --- |
| Shu | 关羽 | 骑兵 | Your commander. If his 兵力 hits 0, you lose. |
| Shu | 关平 | 步兵 | Starts on the west ford. |
| Shu | 周仓 | 步兵 | Starts on the east ford. |
| Shu | 廖化 | 弓兵 | Starts on the Jingzhou tile. |
| Wu | 吕蒙 | 步兵 | Enemy commander. Defeat him and Wu retreats. |
| Wu | 陆逊 | 弓兵 | Ranged pressure. |
| Wu | 潘璋 | 骑兵 | West approach. |
| Wu | 韩当 | 骑兵 | East approach. |
| Wu | 朱然 | 步兵 | Supports the advance. |

**Victory:** 吕蒙 retreats, every Wu general retreats, or Jingzhou is still yours at the end of turn 10.

**Defeat:** 关羽 retreats, or any Wu general steps onto the Jingzhou tile.

Hold the fords and answer enemies who step up to them. 廖化 on the city tile stops Wu from simply walking in, and she can shoot generals who reach her. Sending 关羽 alone into the northern camp gets him surrounded. Ending every turn without attacking also loses: Wu will grind the defense down.

## Battle data

`Assets/Resources/Battle/jingzhou.json` is the whole scenario: map rows from north to south, the city coordinate, and both rosters. Terrain characters are `.` plain, `F` forest, `H` hill, `R` river, `+` ford, and `C` city. Troop types are `infantry`, `cavalry`, and `archer`.

## Project layout

- `Assets/Scripts/Core` — map, movement, damage, turn flow, and enemy AI. No Unity types.
- `Assets/Scripts/Battle` — board, tokens, and the battle UI, built when you press Play.
- `Assets/Editor/JingzhouFontSetup.cs` — keeps the font dynamic and opens the battle scene from an empty editor window.
