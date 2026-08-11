# Changelog

The release workflow reads the section matching the `v*` tag being released and sends it to mod.io as the changelog. No
section, no release - so write one before tagging.

## 3.18.0

- **Three snap levels.** Press `Tab` while the snapper key is held to cycle Point, Edge and Face.
  Point moves the selection as before. Edge and Face also turn it, so it arrives lined up instead of
  merely nearby.
- **Face snapping aligns all three axes.** A face alone leaves the turn around its own normal open,
  which used to mean snapping twice. Face mode now asks for four picks instead of two: a face, then
  one of its edges to align along, and the same again on the target. Nothing is guessed.
- **The cursor shows what it found.** A cube on a point, a bar covering the edge end to end, and the
  surface itself for a face. A cube side lights up whole rather than as one of its two triangles.
- **Undo restores rotation too**, not just position.
- Removed the unfinished gizmo snapping. It never worked, and it was quietly moving the editor's own
  gizmo during ordinary vertex snaps.
- New setting `02 Keybinds / Snap Mode Key`, default `Tab`.
- New setting `01 General / Write a Trace Log` for bug hunting, off by default. With it on, `F8`
  opens a small box to type a note straight into the log.
