# Bomb Balance Reference Pass - 2026-06-01

## Reference Check

- LINE GAME official material identifies the 10+ reward as `Super Bomb` and describes it as a surrounding-block clear.
- PokoPang guide material describes the regular bomb as a star-shaped explosion and the Super Bomb as an explosion around the bomb.
- PokoPang Town official help is not the same game, but it reinforces the common LINE puzzle taxonomy: line-style bombs, local double bombs, and rainbow bombs are distinct special-block identities.

References:

- https://linegame-official.blog.jp/archives/30767380.html
- https://smartphoneapp.hatenablog.com/entry/2014/03/10/100203
- https://help2.line.me/LGPKV/ios/categoryId/50001019/3/pc?lang=ja

## Project Interpretation

- Red Bomb is this prototype's line-clear joker: six hex directions continue until the board boundary.
- Blue Bomb is this prototype's compact Super Bomb: the origin plus adjacent hex ring clears as a local radius.
- Rainbow Bomb is the no-radius color clear: one selected color disappears across the board.

## Applied Balance

- Blue Bomb now clears the origin-adjacent 2-ring hex area.
- Blue Bomb loses value near edges and corners because off-board radius cells do not exist.
- Red Bomb keeps its boundary-to-boundary line clear, so it remains visually and mechanically screen-sweeping even from edges.
- Tests now lock the contrast: center Blue has 17 cells, corner Blue loses cells, and corner Red still clears more than corner Blue.

## Design Rationale

This makes the player's perceived balance match the visible effect language:

- Blue feels controlled and compact because it pulls nearby tiles inward.
- Red feels like a board-state reset because fire lines travel across the board.
- Rainbow feels premium because it ignores shape and clears a color globally.
