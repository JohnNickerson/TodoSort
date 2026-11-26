# TodoSort read-later service sync

## Destination

An implementation-ready behavior spec (no code) for pushing a whole TodoSort file or selected Context to one or more read-later/bookmark services, reading and marking items complete on a phone, and pulling completion updates back into the desktop file.

## Notes

- Domain: TodoSort's single context. Use the canonical terms in [GLOSSARY.md](../../GLOSSARY.md), especially ActionItem and Context.
- Standing scope: Instapaper first, with provider-neutral concepts; explicit user-triggered push and pull; CLI first; bookmark URLs and available metadata, not article bodies.
- Push selection: URL-bearing head-only ActionItems (no priority parent) in the whole-file or selected-Context scope, excluding done and deferred items; order by Upvotes descending, then existing priority order. The head itself is the cap; do not add a numeric limit.
- Before writes, preview the selected ActionItems or completion updates and require confirmation. On pull, use TodoSort's native done behavior and the service completion time when available.
- This map is for planning; do not change application code while working it.

## Decisions so far

<!-- Decisions are indexed here only after their tickets are resolved. -->
- Instapaper API v2 supports bookmark add/list and archived-state pulls but exposes no archive timestamp; use OAuth 2 for new integrations. [Research findings](research/instapaper-api-capabilities.md); partial-run recovery is tracked in [07](issues/07-resuming-after-partial-sync.md).

## Not yet specified

- How to handle network or service failures partway through a push or pull, and how a later run resumes safely ([07](issues/07-resuming-after-partial-sync.md)).
- What guarantees the provider-neutral design should make beyond the first Instapaper integration.

## Out of scope

- Sending article bodies to the service; the initial flow sends bookmark URLs and available metadata.
- Automatic or background synchronization; push and pull are explicit actions.
- Requiring desktop GUI support in the initial spec; the CLI is the first entry point.
- Shipping multiple service integrations in the first release; Instapaper is first, while later providers remain possible.
