# Thumbnail

The picture mod.io shows on the mod card, at the top of the mod page and in the in-game browser. mod.io calls it the
logo. Pushing a change here to `main` runs `.github/workflows/thumbnail.yml` and replaces it - there is nothing to
delete first.

**Exactly one file**, jpg, jpeg or png. Two is an error, because which of them is the thumbnail is not a choice a
workflow gets to make.

mod.io's rules for it:

- at least 512x288, and 16 / 9, which it cuts to 320x180, 640x360 and 1280x720
- 8MB at most
- no comma, semicolon or quote in the filename - curl reads those as syntax of its own

Not shipped in the release zip. This folder is for the mod page, not for the game.
