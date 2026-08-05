# Media

The gallery on the mod page - the screenshots under the description. Arrange it here, then start `Media` from the
Actions tab on GitHub: the gallery ends up as exactly this folder, so a picture deleted here is deleted there. Pushing
alone changes nothing on the mod page.

- jpg, jpeg and png only. mod.io ignores everything else, this README included.
- 8MB each at most.
- Uploaded in filename order, so prefix them `01-`, `02-` to decide what the page shows first.
- No comma, semicolon or quote in the filename - curl reads those as syntax of its own.

Emptying the folder does **not** empty the gallery: the workflow refuses to run with nothing to upload, so a bad merge
cannot wipe the mod page. Clearing it is a job for the mod.io page itself.

Not shipped in the release zip. This folder is for the mod page, not for the game.
